# Grille de notation — benchmark TeamLeave (Opus 5.5 vs DeepSeek V4.1 Flash)

Notation à l'aveugle : les sorties sont anonymisées (modèle A / B) avant revue.
Deux évaluateurs si possible, chacun note 0 à 5 par critère, puis moyenne.
Barème : 0 = absent/faux, 1 = très insuffisant, 2 = partiel, 3 = correct, 4 = bon, 5 = exemplaire.

## A. Spécification technico-fonctionnelle — 20 %
| Critère | Note A | Note B |
|---|---|---|
| A1 Complétude : toutes les sections demandées, tous les besoins du brief couverts | | |
| A2 Non-ambiguïté : hypothèses explicitées en section 9, aucun flou non signalé | | |
| A3 Testabilité : critères d'acceptation Given/When/Then réellement exécutables | | |
| A4 Traçabilité : user stories ↔ règles métier RB-xx correctement reliées | | |

## B. Architecture et conception système — 25 %
| Critère | Note A | Note B |
|---|---|---|
| B1 Cohérence globale : conteneurs, couches et dépendances se tiennent | | |
| B2 Justesse des choix : technos et alternatives arbitrées pour de bonnes raisons | | |
| B3 Compromis explicites : ADR réels (contexte, décision, conséquences), pas de décor | | |
| B4 Robustesse : concurrence (double approbation), cohérence des soldes, sécurité OWASP | | |
| B5 Exploitabilité : un dev peut démarrer sans poser de question | | |

## C. Code backend — 25 % (dont objectif, mesuré par le harnais)
| Critère | Note A | Note B |
|---|---|---|
| C1 `dotnet build` réussit sans modification (automatique : 5 si oui, 0 si non, 2 si bricole mineure) | | |
| C2 `dotnet test` : tous les tests passent (automatique, même barème) | | |
| C3 Correction métier : solde décrémenté à l'approbation, règle 50 % correcte, commentaire obligatoire au refus | | |
| C4 Qualité : structure en couches respectée, gestion d'erreurs (ProblemDetails), lisibilité | | |
| C5 Couverture des règles critiques par les tests | | |

## D. Frontend et design UI — 15 %
| Critère | Note A | Note B |
|---|---|---|
| D1 `npm run build` réussit (automatique, même barème) | | |
| D2 Les trois écrans existent et suivent les user stories | | |
| D3 Qualité visuelle et responsive mobile-first (capture 390 px à l'appui) | | |
| D4 Accessibilité : clavier, aria-labels, contrastes | | |
| D5 États de chargement / erreur / vide soignés | | |

## E. Processus et coût — 15 % (rempli par le harnais + observateur)
| Critère | Note A | Note B |
|---|---|---|
| E1 Coût total du run (meta.json) : 5 si ≤ médiane des deux, 0 si ≥ 10× l'autre, interpolation linéaire | | |
| E2 Latence totale (même barème) | | |
| E3 Itérations humaines nécessaires pour faire compiler (0 itération = 5, chaque itération −1) | | |
| E4 Respect des consignes de sortie (format FILE, sections imposées) | | |

## Score final
Score pondéré = 0,20×A + 0,25×B + 0,25×C + 0,15×D + 0,15×E, ramené sur 100.

## Verdicts à rendre
1. Gagnant par domaine (A à E) et gagnant global.
2. Coût par point de qualité : score / coût du run.
3. Recommandation de routage : quelles tâches à quel modèle, chiffres à l'appui.
