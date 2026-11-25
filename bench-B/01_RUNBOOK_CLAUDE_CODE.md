# Runbook Claude Code (Opus 5.5, abonnement)

Coût attendu : 0 $ d'API (abonnement). On note à la place la part de quota consommée.
Si une commande ci-dessous n'existe pas dans ta version, note-le sur la fiche et utilise l'équivalent le plus proche.

## 0. Préparation (une fois)
```
mkdir C:\bench\claude-code
cd C:\bench\claude-code
copy <zip>\prompts\00_brief.md .
mkdir _prompts
copy <zip>\prompts\0*.md _prompts\
```
Prérequis : .NET SDK (dernière LTS), Node 20+, Claude Code connecté à ton abonnement.

## 1. Modèle
Lance `claude`, puis `/model` et choisis Opus 5.5 (`claude-opus-5-5`). Vérifie avec `/status`. Note sur la fiche : version exacte du modèle affichée, niveau d'effort / réflexion s'il y en a un (laisse le défaut et note-le), version de Claude Code (`claude --version`).

## 2. Pour chaque étape (1 à 4)
1. Ferme la session précédente et ouvre-en une neuve : `claude` dans `C:\bench\claude-code` (ou `/clear` si tu restes dans la même fenêtre).
2. Vérifie le modèle (`/status`) et note l'usage de départ (`/usage` ou `/cost` selon ta version).
3. Note l'heure de début.
4. Colle ce seul message :
   `Exécute exactement les instructions du fichier _prompts/0N_xxx.md (N = numéro d'étape). Ne fais rien d'autre.`
   (étape 1 = `01_spec.md`, 2 = `02_architecture.md`, 3 = `03_backend.md`, 4 = `04_frontend.md`)
5. Autorisations : accepte les écritures de fichiers et les commandes `dotnet` / `npm` de l'agent (ce sont les commandes du prompt). Refuse tout le reste et note-le.
6. Quand l'agent dit avoir fini, note l'heure de fin, relève `/usage` ou `/cost`, et le nombre de tours / d'appels d'outils si l'interface l'affiche (sinon "non visible").
7. Vérifie que les fichiers attendus existent : étape 1 `step1_spec.md`, étape 2 `step2_arch.md`, étape 3 `backend/` + `step3_backend.md`, étape 4 `frontend/` + `step4_frontend.md`.
8. Étapes 3 et 4 : lance toi-même, une seule fois, la vérification objective et copie la sortie dans `logs/` :
   - étape 3 : `cd backend && dotnet build > ..\logs\step3_build.txt 2>&1 && dotnet test > ..\logs\step3_test.txt 2>&1`
   - étape 4 : `cd frontend && npm install && npm run build > ..\logs\step4_build.txt 2>&1`
   Si KO : relance l'agent (max 2 fois par étape, erreur brute collée, voir règle 4) puis refais la vérification. Note chaque relance.
9. Exporte la conversation si possible (`/export` ou copie du transcript) dans `logs/stepN_transcript.txt`. Optionnel mais utile.
10. Remplis la fiche de l'étape tout de suite, pas après.

## 3. Fin
Supprime `node_modules`, `bin`, `obj`, `dist` avant de pousser (voir `STRUCTURE_DEPOT.md`).
