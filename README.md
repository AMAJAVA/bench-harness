# Benchmark TeamLeave — mode d'emploi

Compare Claude Opus 5.5 et DeepSeek V4.1 Flash sur un mini-projet complet,
de la spécification au code, avec des entrées strictement identiques.

## Contenu
- `prompts/00_brief.md` : le brief produit, entrée commune.
- `prompts/01_spec.md` → `04_frontend.md` : la chaîne de prompts (chaque étape
  consomme les sorties des précédentes, pour les deux modèles à l'identique).
- `run_benchmark.py` : exécute la chaîne via les deux API, chronomètre,
  compte les tokens, calcule le coût réel, extrait les fichiers de code.
- `grille_de_notation.md` : la grille de scoring à l'aveugle.

## Prérequis
- Python 3.10+ (aucune dépendance).
- Une clé API Anthropic et une clé API DeepSeek.
- Un plafond de dépense approuvé (le script refuse tout appel sans `--max-cost-usd`).
- .NET SDK (dernière LTS) et Node 20+ pour les portes objectives (build/tests).

## Exécution
    export ANTHROPIC_API_KEY=...
    export DEEPSEEK_API_KEY=...
    python3 run_benchmark.py --self-test                   # hors ligne : payloads, parsing, reprises
    python3 run_benchmark.py --max-cost-usd 12.00          # run complet, les deux modèles

Reprendre un run partiel sans repayer les étapes déjà réussies :
    python3 run_benchmark.py --max-cost-usd 8.00 --models opus --steps 2,3,4 \
        --resume-from runs/opus/<horodatage-du-run-précédent>

Le plafond est contrôlé deux fois : avant chaque appel (estimation au pire cas,
peak + 100 % cache miss) et après chaque appel (coût réel cumulé). Si une sortie
dépasse la limite de tokens, le script demande au modèle de continuer exactement
où il s'est arrêté (jusqu'à 3 reprises) et recolle les morceaux ; une troncature
persistante reste un échec propre, consigné dans meta.json.

Budget indicatif : la spec Opus 5.5 seule a coûté ~1,06 $ au premier essai ;
un run Opus complet peut atteindre 5-8 $ selon les reprises. DeepSeek reste de
l'ordre du centime. Un plafond de 12 $ couvre un run complet confortable.

## Paramètres API (vérifiés le 24 nov. 2026)
- Anthropic `claude-opus-5-5` : Opus 5.5 **rejette** `temperature`/`top_p`/`top_k`
  et le champ `thinking` (erreur 400). Le script ne les envoie pas ; la profondeur
  de réflexion se règle par `effort` (réglé à `high` ; low/medium/high/xhigh/max
  possibles). `max_tokens` couvre réflexion + texte : 128 000 ici (maximum natif).
  Tarif standard : 4 $ / 20 $ par million de tokens entrée / sortie (hors batch
  et fast mode ; les tokens de réflexion sont facturés en sortie).
- DeepSeek `deepseek-flash` (servi par DeepSeek-V4.1-Flash-1017) :
  `temperature` 0 et `reasoning_effort` high explicites. Prix par million de
  tokens : entrée cache hit 0,006 $ peak / 0,003 $ off-peak, cache miss
  0,30 $ / 0,15 $, sortie 1,20 $ / 0,60 $. Peak = lun.-ven. 01:00-04:00 et
  06:00-10:00 UTC hors fériés chinois ; le reste est off-peak. Le script choisit
  la tranche à l'heure de l'appel et la consigne (approximation : les fériés
  chinois non énumérés sont traités comme des jours normaux → coût surestimé).
- Les deux réponses rapportent leur version de modèle : consignée dans
  meta.json (`reported_model`), à comparer au modèle demandé.
- Une sortie qui dépasse la limite (`stop_reason=max_tokens` / `finish_reason=length`)
  déclenche une reprise automatique (jusqu'à 3) ; seule une troncature persistante
  est un échec propre. meta.json est sérialisé en ASCII pur (aucun mojibake
  possible, même sous Windows).

Référence de comparaison entre modèles : Opus 5.5 ne permet pas de figer
l'échantillonnage (pas de temperature 0), Flash oui. Cette asymétrie fait
partie des différences mesurées ; elle est consignée dans meta.json.

## Après les runs
1. Backend de chaque modèle : `cd runs/<modèle>/<ts>/backend && dotnet build && dotnet test`.
2. Frontend : `cd .../frontend && npm install && npm run build`, captures 390 px des 3 écrans.
3. Anonymiser les sorties (A/B), faire noter par deux relecteurs avec la grille.
4. Remplir la section E depuis `meta.json`, calculer le score pondéré.
5. Idéalement 2 à 3 runs par modèle (les sorties varient) : `--models opus --steps 1,2,3,4` en boucle.

## Limites connues
- Le coût est calculé depuis la grille publique du 24 nov. 2026, pas depuis la
  facturation réelle ; revérifier les prix au moment du run.
- Un seul essai par défaut : la variance entre runs n'est mesurée qu'en relançant.
