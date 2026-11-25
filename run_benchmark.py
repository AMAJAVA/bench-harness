#!/usr/bin/env python3
"""
Harnais de benchmark TeamLeave : Claude Opus 5.5 vs DeepSeek V4.1 Flash. v3
Stdlib uniquement (Python 3.10+). Aucune dépendance externe.

Usage :
    export ANTHROPIC_API_KEY=sk-ant-...
    export DEEPSEEK_API_KEY=sk-...
    python3 run_benchmark.py --self-test                       # hors ligne : payloads + parsing, aucun appel
    python3 run_benchmark.py --max-cost-usd 12.00              # run complet (les 2 modèles)
    python3 run_benchmark.py --max-cost-usd 8.00 --models opus --steps 2,3,4 \
        --resume-from runs/opus/20261124-162806                # reprend les étapes déjà réussies

--max-cost-usd est OBLIGATOIRE pour tout appel réel : le script estime chaque
appel au pire cas avant de le lancer et s'arrête au plafond approuvé.

Nouveautés v3 (24 nov. 2026, après le premier essai tronqué) :
- max_tokens porté à 128k (Opus 5.5 supporte 128k de sortie en natif ;
  DeepSeek accepte jusqu'à 384k).
- Poursuite automatique : si une étape est tronquée, le script demande au modèle
  de continuer exactement où il s'est arrêté (jusqu'à 3 reprises), puis recolle
  les morceaux. Une troncature persistante reste un échec propre.
- --resume-from : réutilise les sorties des étapes déjà réussies d'un run
  précédent (pas de double facturation).
- meta.json 100 % ASCII (clés et échappements unicode) : illisible en mojibake
  sous Windows, s'ouvre partout.

Sources des paramètres (vérifiées le 24 nov. 2026) :
- Anthropic : https://platform.claude.com/docs/en/models/opus-5-5/migration-guide
  Opus 5.5 REJETTE temperature/top_p/top_k et le champ thinking : omis ici.
  Réflexion toujours active, pilotée par "effort". max_tokens couvre
  réflexion + texte ; les tokens de réflexion sont facturés en sortie.
- DeepSeek : https://api-docs.deepseek.com/quick_start/pricing/ et
  https://api-docs.deepseek.com/api/create-chat-completion
  Alias documenté : "deepseek-flash" (servi par DeepSeek-V4.1-Flash-1017).
  Prix distincts cache hit / cache miss et peak / off-peak.
"""
import argparse, json, os, re, sys, time, urllib.request, urllib.error, datetime, pathlib

# ---------------------------------------------------------------------------
# CONFIG : prix et identifiants vérifiés le 24 nov. 2026 (voir sources ci-dessus).
# Revérifier au moment du run : les prix évoluent.
# ---------------------------------------------------------------------------
CONFIG = {
    "opus": {
        "label": "Claude Opus 5.5",
        "provider": "anthropic",
        "model": "claude-opus-5-5",
        "max_tokens": 128000,       # sortie native max 128k (réflexion + texte)
        "effort": "high",           # low/medium/high/xhigh/max ; temperature omise (rejetée)
        "price_in_per_mtok": 4.0,   # $/M tokens entrée, tarif standard
        "price_out_per_mtok": 20.0, # $/M tokens sortie (réflexion incluse)
    },
    "flash": {
        "label": "DeepSeek V4.1 Flash",
        "provider": "deepseek",
        "model": "deepseek-flash",  # alias documenté ; servi par DeepSeek-V4.1-Flash-1017
        "max_tokens": 128000,       # limite API : 384k
        "temperature": 0.0,
        "reasoning_effort": "high", # none/low/high/max
        # $/M tokens, grille du 24 nov. 2026 :
        "price_peak":    {"hit": 0.006, "miss": 0.30, "out": 1.20},
        "price_offpeak": {"hit": 0.003, "miss": 0.15, "out": 0.60},
    },
}

PROMPT_DIR = pathlib.Path(__file__).parent / "prompts"
STEPS = {1: "01_spec.md", 2: "02_architecture.md", 3: "03_backend.md", 4: "04_frontend.md"}
STEP_FILES = {1: "step1_spec.md", 2: "step2_arch.md", 3: "step3_backend.md", 4: "step4_frontend.md"}
PRICING_DATE = "24 nov. 2026"
MAX_CONTINUATIONS = 3
CONTINUE_MSG = ("Ta réponse a été coupée par la limite de tokens. Continue EXACTEMENT "
                "là où tu t'es arrêté : reprends au caractère suivant, sans recommencer, "
                "sans répéter la fin, sans commentaire.")


class TruncationError(RuntimeError):
    """Sortie toujours tronquée après toutes les reprises : étape en échec propre."""


# ---------------------------------------------------------------------------
# Fenêtre tarifaire DeepSeek : peak = lun.-ven. 01:00-04:00 et 06:00-10:00 UTC,
# hors fériés chinois. Approximation assumée : les fériés chinois ne sont pas
# énumérés, un férié en semaine est traité "peak" -> coût SURESTIMÉ (conservateur).
# ---------------------------------------------------------------------------
def deepseek_tier(now_utc=None):
    now_utc = now_utc or datetime.datetime.now(datetime.timezone.utc)
    if now_utc.weekday() >= 5:
        return "offpeak"
    hm = now_utc.hour * 60 + now_utc.minute
    peak = (60 <= hm < 240) or (360 <= hm < 600)
    return "peak" if peak else "offpeak"


def est_tokens(text):
    """Surestimation volontaire (~3 caractères/token) pour le contrôle du plafond."""
    return len(text) // 3 + 1


def worst_case_cost(cfg, messages_text):
    tin, tout = est_tokens(messages_text), cfg["max_tokens"]
    if cfg["provider"] == "anthropic":
        return (tin * cfg["price_in_per_mtok"] + tout * cfg["price_out_per_mtok"]) / 1e6
    p = cfg["price_peak"]
    return (tin * p["miss"] + tout * p["out"]) / 1e6


def deepseek_cost(cfg, usage, tier):
    hit = usage.get("prompt_cache_hit_tokens")
    miss = usage.get("prompt_cache_miss_tokens")
    tout = usage.get("completion_tokens") or 0
    if hit is None or miss is None:
        hit, miss = 0, usage.get("prompt_tokens") or 0
    p = cfg["price_peak" if tier == "peak" else "price_offpeak"]
    return round((hit * p["hit"] + miss * p["miss"] + tout * p["out"]) / 1e6, 6)


# ---------------------------------------------------------------------------
# Payloads (séparés du réseau pour le --self-test)
# ---------------------------------------------------------------------------
def build_anthropic_payload(cfg, messages):
    # Opus 5.5 : PAS de temperature/top_p/top_k, PAS de champ thinking (erreur 400).
    return {
        "model": cfg["model"],
        "max_tokens": cfg["max_tokens"],
        "output_config": {"effort": cfg["effort"]},
        "messages": messages,
    }


def build_deepseek_payload(cfg, messages):
    return {
        "model": cfg["model"],
        "max_tokens": cfg["max_tokens"],
        "temperature": cfg["temperature"],
        "reasoning_effort": cfg["reasoning_effort"],
        "messages": messages,
    }


def parse_anthropic_response(data):
    """Retourne (texte, usage, info, tronqué)."""
    text = "".join(b.get("text", "") for b in data.get("content", []) if b.get("type") == "text")
    usage = data.get("usage", {})
    stop = data.get("stop_reason")
    info = {
        "reported_model": data.get("model"),
        "stop_reason": stop,
        "input_tokens": usage.get("input_tokens"),
        "output_tokens": usage.get("output_tokens"),
    }
    if stop not in ("end_turn", "stop_sequence", "max_tokens"):
        info["warning"] = f"stop_reason inattendu : {stop}"
    return text, usage, info, stop == "max_tokens"


def parse_deepseek_response(data, cfg, tier):
    """Retourne (texte, usage, info, tronqué)."""
    choice = data.get("choices", [{}])[0]
    text = choice.get("message", {}).get("content") or ""
    finish = choice.get("finish_reason")
    usage = data.get("usage", {})
    info = {
        "reported_model": data.get("model"),
        "finish_reason": finish,
        "input_tokens": usage.get("prompt_tokens"),
        "output_tokens": usage.get("completion_tokens"),
        "cache_hit_tokens": usage.get("prompt_cache_hit_tokens"),
        "cache_miss_tokens": usage.get("prompt_cache_miss_tokens"),
        "price_tier": tier,
    }
    if finish not in ("stop", "length", None):
        info["warning"] = f"finish_reason inattendu : {finish}"
    info["cost_usd"] = deepseek_cost(cfg, usage, tier)
    return text, usage, info, finish == "length"


# ---------------------------------------------------------------------------
# Appel réseau unitaire (injectable pour les tests)
# ---------------------------------------------------------------------------
def http_post(url, payload, headers):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                 headers=headers, method="POST")
    with urllib.request.urlopen(req, timeout=1800) as r:
        return json.loads(r.read())


def anthropic_call_once(cfg, messages, poster=http_post):
    data = poster("https://api.anthropic.com/v1/messages",
                  build_anthropic_payload(cfg, messages),
                  {"content-type": "application/json",
                   "x-api-key": os.environ.get("ANTHROPIC_API_KEY", ""),
                   "anthropic-version": "2023-06-01"})
    text, usage, info, truncated = parse_anthropic_response(data)
    tin, tout = info["input_tokens"] or 0, info["output_tokens"] or 0
    info["cost_usd"] = round((tin * cfg["price_in_per_mtok"] + tout * cfg["price_out_per_mtok"]) / 1e6, 6)
    # Les blocs renvoyés (réflexion incluse) doivent être re-passés tels quels.
    return text, info, truncated, data.get("content", [])


def deepseek_call_once(cfg, messages, poster=http_post):
    tier = deepseek_tier()
    data = poster("https://api.deepseek.com/chat/completions",
                  build_deepseek_payload(cfg, messages),
                  {"content-type": "application/json",
                   "authorization": f"Bearer {os.environ.get('DEEPSEEK_API_KEY', '')}"})
    text, _, info, truncated = parse_deepseek_response(data, cfg, tier)
    return text, info, truncated, text


def messages_text(messages):
    parts = []
    for m in messages:
        c = m.get("content")
        if isinstance(c, str):
            parts.append(c)
        else:
            parts.extend(b.get("text", "") or b.get("thinking", "") for b in c if isinstance(b, dict))
    return "".join(parts)


def run_step_with_continuation(cfg, prompt_text, budget, poster=http_post):
    """Exécute une étape avec reprise automatique sur troncature.

    budget = dict cumul/cap modifié en place. Retourne (texte, liste d'appels).
    Lève TruncationError si toujours tronqué après MAX_CONTINUATIONS reprises,
    CapExceededError si le plafond bloque une reprise.
    """
    provider = cfg["provider"]
    call_once = anthropic_call_once if provider == "anthropic" else deepseek_call_once
    messages = [{"role": "user", "content": prompt_text}]
    parts, calls = [], []
    for attempt in range(1 + MAX_CONTINUATIONS):
        est = worst_case_cost(cfg, messages_text(messages))
        if budget["cumul"] + est > budget["cap"]:
            raise CapExceededError(
                f"pire cas estimé {est:.2f}$ > reste du plafond "
                f"({budget['cumul']:.2f} cumulés / {budget['cap']:.2f})")
        t0 = time.time()
        text, info, truncated, raw_assistant = call_once(cfg, messages, poster=poster)
        info["latency_s"] = round(time.time() - t0, 1)
        info["attempt"] = attempt + 1
        calls.append(info)
        budget["cumul"] += info["cost_usd"]
        parts.append(text)
        if not truncated:
            return "".join(parts), calls
        messages.append({"role": "assistant", "content": raw_assistant})
        messages.append({"role": "user", "content": CONTINUE_MSG})
        if attempt < MAX_CONTINUATIONS:
            print(f"      tronqué ({info.get('output_tokens')} tokens), reprise {attempt + 1}/{MAX_CONTINUATIONS}...")
    raise TruncationError(f"toujours tronqué après {MAX_CONTINUATIONS} reprises")


class CapExceededError(RuntimeError):
    """Le plafond de dépense approuvé bloque l'appel suivant."""


FILE_RE = re.compile(r"^=== FILE: (.+?) ===\s*$", re.MULTILINE)


def extract_files(step_text, dest_dir):
    """Extrait les blocs === FILE: path === des étapes 3 et 4."""
    marks = list(FILE_RE.finditer(step_text))
    count = 0
    for i, m in enumerate(marks):
        path = m.group(1).strip()
        end = marks[i + 1].start() if i + 1 < len(marks) else len(step_text)
        content = step_text[m.end():end]
        content = re.split(r"^NOTES DE RUN\s*$", content, flags=re.MULTILINE)[0]
        safe = pathlib.PurePosixPath(path)
        if safe.is_absolute() or ".." in safe.parts:
            continue
        out = dest_dir / pathlib.Path(*safe.parts)
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(content.strip("\n") + "\n", encoding="utf-8")
        count += 1
    return count


def save_meta(outdir, meta):
    # ensure_ascii=True : fichier 100 % ASCII, aucun mojibake possible sous Windows.
    (outdir / "meta.json").write_text(json.dumps(meta, indent=2, ensure_ascii=True), encoding="utf-8")


def run_model(key, steps, brief, templates, cap, resume_from):
    cfg = CONFIG[key]
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    outdir = pathlib.Path("runs") / key / stamp
    outdir.mkdir(parents=True, exist_ok=True)
    settings = ({"effort": cfg["effort"], "sampling": "omis (temperature/top_p/top_k rejetes par Opus 5.5)"}
                if cfg["provider"] == "anthropic"
                else {"temperature": cfg["temperature"], "reasoning_effort": cfg["reasoning_effort"]})
    meta = {"model": cfg["label"], "model_id_envoye": cfg["model"], "reglages": settings,
            "base_tarifaire": PRICING_DATE, "plafond_usd": cap, "started": stamp,
            "resume_from": str(resume_from) if resume_from else None, "steps": {}}
    outputs = {}
    if resume_from:
        for n, fname in STEP_FILES.items():
            p = pathlib.Path(resume_from) / fname
            if p.exists():
                outputs[n] = p.read_text(encoding="utf-8")
                meta["steps"][n] = {"reutilise_de": str(p)}
                print(f"[{key}] étape {n} : sortie réutilisée depuis {p}")
    budget = {"cumul": 0.0, "cap": cap}
    for n in steps:
        if n in outputs and resume_from:
            continue
        prompt = templates[n].replace("{{BRIEF}}", brief)
        prompt = prompt.replace("{{SPEC}}", outputs.get(1, "(non fournie)"))
        prompt = prompt.replace("{{ARCH}}", outputs.get(2, "(non fourni)"))
        try:
            text, calls = run_step_with_continuation(cfg, prompt, budget)
        except (TruncationError, CapExceededError) as e:
            print(f"[{key}] étape {n} ÉCHEC : {e}", file=sys.stderr)
            meta["steps"][n] = {"error": str(e)}
            save_meta(outdir, meta)
            break
        except urllib.error.HTTPError as e:
            print(f"[{key}] étape {n} : HTTP {e.code} — {e.read()[:300]!r}", file=sys.stderr)
            meta["steps"][n] = {"error": f"HTTP {e.code}"}
            save_meta(outdir, meta)
            break
        outputs[n] = text
        (outdir / STEP_FILES[n]).write_text(text, encoding="utf-8")
        step_meta = {
            "cost_usd": round(sum(c["cost_usd"] for c in calls), 6),
            "cout_cumule_usd": round(budget["cumul"], 4),
            "latency_s": round(sum(c["latency_s"] for c in calls), 1),
            "input_tokens": sum(c.get("input_tokens") or 0 for c in calls),
            "output_tokens": sum(c.get("output_tokens") or 0 for c in calls),
            "continuations": len(calls) - 1,
            "reported_model": calls[-1].get("reported_model"),
            "appels": calls,
        }
        if n in (3, 4):
            sub = outdir / ("backend" if n == 3 else "frontend")
            step_meta["files_extracted"] = extract_files(text, sub)
        meta["steps"][n] = step_meta
        print(f"[{key}] étape {n} ok : {step_meta['latency_s']:.0f}s, "
              f"{step_meta['input_tokens']}→{step_meta['output_tokens']} tokens "
              f"({len(calls)} appel(s)), coût={step_meta['cost_usd']}$, cumul={budget['cumul']:.4f}$")
        save_meta(outdir, meta)
    meta["cout_total_usd"] = round(budget["cumul"], 4)
    save_meta(outdir, meta)
    print(f"[{key}] terminé → {outdir} (total {budget['cumul']:.4f}$)")


# ---------------------------------------------------------------------------
# Self-test hors ligne : vérifie payloads, parsing et reprises sans réseau.
# ---------------------------------------------------------------------------
def self_test():
    ok = True

    def check(name, cond, detail=""):
        nonlocal ok
        print(("PASS" if cond else "FAIL"), "-", name, ("| " + str(detail) if detail else ""))
        ok = ok and cond

    # 1. Payload Anthropic : aucun paramètre d'échantillonnage, effort présent.
    pa = build_anthropic_payload(CONFIG["opus"], [{"role": "user", "content": "test"}])
    check("anthropic : pas de temperature/top_p/top_k",
          all(k not in pa for k in ("temperature", "top_p", "top_k")))
    check("anthropic : pas de champ thinking", "thinking" not in pa)
    check("anthropic : effort transmis", pa.get("output_config", {}).get("effort") == "high")
    check("anthropic : max_tokens 128k", pa["max_tokens"] == 128000)

    # 2. Payload DeepSeek : alias documenté.
    pd = build_deepseek_payload(CONFIG["flash"], [{"role": "user", "content": "test"}])
    check("deepseek : alias deepseek-flash", pd["model"] == "deepseek-flash")
    check("deepseek : temperature 0 explicite", pd["temperature"] == 0.0)
    check("deepseek : max_tokens 128k (<= 384k API)", pd["max_tokens"] == 128000)

    # 3. Réponse Anthropic normale (bloc de réflexion en tête).
    mock_a = {"model": "claude-opus-5-5", "stop_reason": "end_turn",
              "content": [{"type": "thinking", "thinking": "..."},
                          {"type": "text", "text": "SPEC OK"}],
              "usage": {"input_tokens": 1000, "output_tokens": 500}}
    text, _, info, truncated = parse_anthropic_response(mock_a)
    check("anthropic : texte extrait hors blocs thinking", text == "SPEC OK" and not truncated)

    # 4. Reprise sur troncature Anthropic : 1re réponse coupée, 2e complète, recollage.
    seq = [
        {"model": "claude-opus-5-5", "stop_reason": "max_tokens",
         "content": [{"type": "thinking", "thinking": "t"}, {"type": "text", "text": "PART1/"}],
         "usage": {"input_tokens": 100, "output_tokens": 128000}},
        {"model": "claude-opus-5-5", "stop_reason": "end_turn",
         "content": [{"type": "text", "text": "PART2"}],
         "usage": {"input_tokens": 200, "output_tokens": 50}},
    ]
    poster_a = lambda url, payload, headers: seq.pop(0)
    budget = {"cumul": 0.0, "cap": 100.0}
    text, calls = run_step_with_continuation(CONFIG["opus"], "prompt", budget, poster=poster_a)
    check("anthropic : reprise recolle les morceaux", text == "PART1/PART2", text)
    check("anthropic : 2 appels, 1 continuation", len(calls) == 2)
    attendu = (100 * 4 + 128000 * 20) / 1e6 + (200 * 4 + 50 * 20) / 1e6
    check("anthropic : coût cumulé sur les 2 appels", abs(budget["cumul"] - attendu) < 1e-6,
          f"{budget['cumul']:.4f}$ vs {attendu:.4f}$")

    # 5. Troncature persistante Anthropic -> échec propre après les reprises.
    inf = lambda url, payload, headers: {"model": "m", "stop_reason": "max_tokens",
                                         "content": [{"type": "text", "text": "x"}],
                                         "usage": {"input_tokens": 1, "output_tokens": 1}}
    try:
        run_step_with_continuation(CONFIG["opus"], "p", {"cumul": 0.0, "cap": 100.0}, poster=inf)
        check("anthropic : troncature persistante = échec propre", False)
    except TruncationError:
        check("anthropic : troncature persistante = échec propre", True)

    # 6. Reprise DeepSeek : finish_reason length puis stop.
    seq_d = [
        {"model": "deepseek-flash",
         "choices": [{"message": {"role": "assistant", "content": "MOITIE1/"}, "finish_reason": "length"}],
         "usage": {"prompt_tokens": 1000, "completion_tokens": 128000,
                   "prompt_cache_hit_tokens": 0, "prompt_cache_miss_tokens": 1000}},
        {"model": "deepseek-flash",
         "choices": [{"message": {"role": "assistant", "content": "MOITIE2"}, "finish_reason": "stop"}],
         "usage": {"prompt_tokens": 5000, "completion_tokens": 60,
                   "prompt_cache_hit_tokens": 4000, "prompt_cache_miss_tokens": 1000}},
    ]
    poster_d = lambda url, payload, headers: seq_d.pop(0)
    budget_d = {"cumul": 0.0, "cap": 100.0}
    text, calls = run_step_with_continuation(CONFIG["flash"], "prompt", budget_d, poster=poster_d)
    check("deepseek : reprise recolle les morceaux", text == "MOITIE1/MOITIE2", text)
    check("deepseek : cache hit/miss cumulés", calls[1]["cache_hit_tokens"] == 4000)

    # 7. Coût DeepSeek off-peak vs peak.
    usage = {"prompt_tokens": 1000000, "completion_tokens": 1000000,
             "prompt_cache_hit_tokens": 600000, "prompt_cache_miss_tokens": 400000}
    off = deepseek_cost(CONFIG["flash"], usage, "offpeak")
    check("deepseek : peak = 2x off-peak", abs(deepseek_cost(CONFIG["flash"], usage, "peak") - 2 * off) < 1e-9)

    # 8. Plafond : un plafond minuscule bloque le 1er appel.
    try:
        run_step_with_continuation(CONFIG["opus"], "x" * 30000, {"cumul": 0.0, "cap": 0.01}, poster=poster_a)
        check("plafond : appel bloqué sous le pire cas", False)
    except CapExceededError:
        check("plafond : appel bloqué sous le pire cas", True)

    # 9. Fenêtre tarifaire.
    wed2 = datetime.datetime(2026, 11, 25, 2, 0, tzinfo=datetime.timezone.utc)
    sat2 = datetime.datetime(2026, 11, 28, 2, 0, tzinfo=datetime.timezone.utc)
    check("tier : mer. 02:00 UTC = peak", deepseek_tier(wed2) == "peak")
    check("tier : sam. = off-peak", deepseek_tier(sat2) == "offpeak")

    # 10. meta.json : 100 % ASCII (anti-mojibake Windows).
    s = json.dumps({"reglages": "omis (rejetés)", "cout_cumule_usd": 1.0}, ensure_ascii=True)
    check("meta : sérialisation ASCII pure", all(ord(c) < 128 for c in s))

    print()
    print("SELF-TEST", "OK — tous les contrôles passent." if ok else "ÉCHEC — corriger avant tout run réel.")
    return 0 if ok else 1


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--models", default="opus,flash")
    ap.add_argument("--steps", default="1,2,3,4")
    ap.add_argument("--max-cost-usd", type=float, default=None,
                    help="Plafond de dépense approuvé, OBLIGATOIRE pour tout appel réel.")
    ap.add_argument("--resume-from", default=None,
                    help="Dossier d'un run précédent : les étapes déjà réussies y sont lues "
                         "et réinjectées (pas de double facturation).")
    ap.add_argument("--self-test", action="store_true",
                    help="Validation hors ligne des payloads, du parsing et des reprises.")
    args = ap.parse_args()
    if args.self_test:
        sys.exit(self_test())
    if args.max_cost_usd is None:
        sys.exit("Refus d'appeler les API sans plafond : relancez avec --max-cost-usd <montant approuvé>.")
    for var in ("ANTHROPIC_API_KEY", "DEEPSEEK_API_KEY"):
        if not os.environ.get(var):
            sys.exit(f"variable d'environnement manquante : {var}")
    brief, templates = load_prompts()
    steps = [int(s) for s in args.steps.split(",")]
    for key in [m.strip() for m in args.models.split(",")]:
        if key not in CONFIG:
            sys.exit(f"modèle inconnu : {key} (attendu : {', '.join(CONFIG)})")
        run_model(key, steps, brief, templates, args.max_cost_usd, args.resume_from)


def load_prompts():
    brief = (PROMPT_DIR / "00_brief.md").read_text(encoding="utf-8")
    return brief, {n: (PROMPT_DIR / f).read_text(encoding="utf-8") for n, f in STEPS.items()}


if __name__ == "__main__":
    main()
