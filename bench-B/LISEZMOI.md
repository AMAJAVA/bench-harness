# Benchmark TeamLeave, montage B (agentique, depuis zéro)

Claude Code (Opus 5.5, abonnement) contre DeepSeek Harness `dsh` (DeepSeek V4.1 Flash, API).
Même brief, mêmes 4 étapes, mêmes livrables que le harnais précédent. On repart de zéro des deux côtés.

NOTE MÉTHODOLOGIQUE (à garder dans la grille) : les étapes comparent des chaînes outil + modèle complètes ; les surfaces d'outils de Claude Code et de dsh diffèrent, donc l'écart mesuré n'est pas celui des modèles seuls.

## Contenu du zip
- `01_RUNBOOK_CLAUDE_CODE.md` : pas à pas Claude Code.
- `02_RUNBOOK_DEEPSEEK_HARNESS.md` : pas à pas dsh.
- `prompts/` : brief + prompts des étapes 1 à 4, adaptés au mode agent (mêmes exigences de livrable que nos prompts API, plus les chemins de fichiers et une consigne de périmètre).
- `FICHE_DE_COLLECTE.md` : une fiche par run (à remplir pendant).
- `STRUCTURE_DEPOT.md` : où pousser dans ton repo (`runs-B/...`).
- `runs-B/` : squelette de dossiers prêt à copier dans le repo.
- `grille_de_notation_REFERENCE.md` : la grille de notation à l'aveugle, pour info (ne la remplis pas, je note à l'aveugle).

## Règles communes (identiques des deux côtés, c'est ce qui rend la comparaison juste)
1. Un dossier vierge par outil. Chaque outil ne voit jamais le dossier de l'autre.
2. Une session neuve par étape (pas de contexte caché entre étapes). La chaîne passe uniquement par les fichiers : `step1_spec.md`, `step2_arch.md`, `backend/`...
3. Prompt collé tel quel, sans ajout ni explication de ta part.
4. Tu ne corriges jamais le code toi-même. Si le build ou les tests échouent, l'agent a le droit d'itérer seul. Si c'est encore cassé à la fin, tu peux relancer l'agent au maximum 2 fois par étape en collant l'erreur brute, sans indice. Chaque relance est notée sur la fiche (c'est le critère E3 de la grille).
5. Un seul essai par outil. Pas de "recommence, c'était mauvais".
6. Ne pousse rien d'autre que les dossiers prévus (pas de clés API, pas de `node_modules`, `bin`, `obj`).
7. Aucune clé API ni mot de passe dans les fichiers ou les logs que tu pousses.

## Ordre conseillé
Lance les deux outils dans deux fenêtres, étape 1 des deux côtés, puis étape 2, etc. Garde le même ordre pour que les étapes des deux côtés arrivent dans les mêmes conditions.
