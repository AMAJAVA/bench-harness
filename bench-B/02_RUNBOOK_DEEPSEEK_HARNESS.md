# Runbook DeepSeek Harness (`dsh`, DeepSeek V4.1 Flash, API)

Coût attendu : quelques centimes à quelques dizaines de centimes. On le mesure sur la console DeepSeek.
Sources : https://github.com/deepseek-ai/deepseek-harness (docs/cli-help.md) et https://deepseekharness.io/download/. dsh est en préversion : si une option diffère dans ta version, `dsh --help` et `dsh headless --help` font foi, note l'écart sur la fiche.

## 0. Préparation (une fois)
Prérequis : Node.js ^22.19.0 ou >= 24, .NET SDK (dernière LTS), clé API DeepSeek.
```
mkdir C:\bench\deepseek-harness
cd C:\bench\deepseek-harness
copy <zip>\prompts\00_brief.md .
mkdir _prompts
copy <zip>\prompts\0*.md _prompts\
set DEEPSEEK_API_KEY=ta_cle        (PowerShell : $env:DEEPSEEK_API_KEY="ta_cle")
```
(La clé ne doit jamais apparaître dans un fichier poussé.)

## 1. Modèle
Dans `%USERPROFILE%\.dsh\settings.yaml` (rechargé à chaud) :
```
agent-default-model:
  provider: deepseek-official
  model: deepseek-flash

llm-deepseek:
  reasoningEffort: high
```
`deepseek-flash` est l'alias de DeepSeek V4.1 Flash (les anciens noms `deepseek-v4-flash` marchent aussi, mais utilise `deepseek-flash` pour rester aligné avec le benchmark précédent). Sauvegarde une copie du fichier dans `logs/settings.yaml` après avoir effacé toute clé éventuelle. Note `dsh --version`.

## 2. Pour chaque étape (1 à 4)
Mode recommandé : headless (une tâche, un prompt, une session neuve, exit code propre).
1. Console DeepSeek : note le solde ou l'usage avant l'étape.
2. Note l'heure de début.
3. Dans `C:\bench\deepseek-harness`, lance (une ligne, remplace N et le fichier) :
```
dsh --profile headless --permission-mode workspace-write --json "Exécute exactement les instructions du fichier _prompts/0N_xxx.md. Ne fais rien d'autre." > logs\stepN_events.jsonl
```
   - Étape 1 = `01_spec.md`, 2 = `02_architecture.md`, 3 = `03_backend.md`, 4 = `04_frontend.md`.
   - Avec `npx` si dsh n'est pas installé : `npx @deepseek-ai/dsh --profile headless ...`.
   - Si le mode `workspace-write` ne laisse pas lancer `dotnet` / `npm`, regarde l'aide (`dsh headless --help`) pour le preset de permission adapté et note ce que tu as choisi. N'autorise rien en dehors du dossier de run.
   - Alternative si le headless pose problème : `dsh web` (interface web), même prompt, session neuve par étape ; copie le texte de la conversation dans `logs/stepN_transcript.txt`.
4. Note l'heure de fin, le code de sortie, et le nombre d'itérations / tours si visible (sinon compte les événements de tour dans le .jsonl, ou "non visible").
5. Console DeepSeek : note le solde / usage après l'étape. Coût de l'étape = différence.
6. Vérifie les fichiers attendus : étape 1 `step1_spec.md`, 2 `step2_arch.md`, 3 `backend/` + `step3_backend.md`, 4 `frontend/` + `step4_frontend.md`.
7. Étapes 3 et 4 : vérification objective, une seule fois, sortie dans `logs/` (mêmes commandes que Claude Code) :
   - étape 3 : `cd backend && dotnet build > ..\logs\step3_build.txt 2>&1 && dotnet test > ..\logs\step3_test.txt 2>&1`
   - étape 4 : `cd frontend && npm install && npm run build > ..\logs\step4_build.txt 2>&1`
   Si KO : relance (max 2 par étape, erreur brute collée, session neuve avec le même prompt plus l'erreur brute, ou `--session-id` de la session pour la reprendre ; note lequel), puis refais la vérification. Chaque relance est notée.
8. Remplis la fiche de l'étape tout de suite.

## 3. Fin
Supprime `node_modules`, `bin`, `obj`, `dist` avant de pousser. Vérifie qu'aucun fichier ne contient la clé (`findstr /s /i "sk-" *`).
