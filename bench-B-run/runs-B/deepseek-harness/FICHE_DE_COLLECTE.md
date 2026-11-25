# Fiche de collecte, deepseek-harness (remplie automatiquement par run_bench_B.py)

Outil : DeepSeek Harness dsh (DeepSeek V4.1 Flash, API)   Version de l'outil : 0.2.2-rc.5
Modèle vu par le script dans la sortie de l'outil : deepseek-ai, deepseek-harness, deepseek-harness
Début du run : 2026-11-25T21:49:41   Fin : 2026-11-25T23:34:19
ALERTES : --dump-config ne montre pas le modèle deepseek-flash : le run risque de ne pas utiliser le bon modèle | --dump-config ne montre pas le provider deepseek-official | reasoningEffort vu dans la config : introuvable (attendu : high) | MODELE INATTENDU dans les événements : deepseek-harness (attendu deepseek-flash) | MODELE INATTENDU dans les événements : deepseek-ai, deepseek-harness (attendu deepseek-flash)

| Étape | Début | Fin | Durée | Tours / événements | Relances auto | Build/test | Fichiers présents | Remarques |
|---|---|---|---|---|---|---|---|---|
| 1 spec | 21:49:41 | 22:03:35 | 13m55s | 364 événements | 0 | n/a | oui |  |
| 2 archi | 22:03:35 | 22:24:28 | 20m53s | 352 événements | 0 | n/a | oui |  |
| 3 backend | 22:24:28 | 23:13:06 | 48m38s | 1275 événements | 0 | build: OK / test: OK | oui |  |
| 4 frontend | 23:13:06 | 23:34:19 | 21m13s | 743 événements | 0 | npm_install: OK / build: OK | oui |  |

Totaux : durée 1h44m39s   relances automatiques 0

## À remplir par Ahmed (le script ne peut pas le mesurer)
<!-- Rappel : note le solde / l'usage de la console DeepSeek AVANT et APRES le run, ou lis l'usage sur la console par plage horaire avec les heures de début/fin ci-dessus. -->
Solde console DeepSeek avant : 92.75$   après : 91.88$   Coût total : 0.93 $

Détail machine : run_state.json. Chaque tentative (y compris les relances) a son transcript dans logs/.
Rappels : un seul essai, aucune correction de code à la main, aucune clé dans les fichiers.
