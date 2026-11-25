# ÉTAPE 2 — ARCHITECTURE ET CONCEPTION SYSTÈME

À partir du brief et de ta spécification (tous deux ci-dessous), produis le dossier d'architecture de TeamLeave.

Contenu imposé :
1. Vue d'ensemble : description C4 niveau conteneurs (texte + diagramme Mermaid).
2. Choix technologiques justifiés (framework, base de données, auth, hébergement Azure) : pour chaque choix, l'alternative écartée et la raison de l'arbitrage.
3. Architecture logicielle du backend (couches, dépendances, ce qui est interdit entre couches).
4. Schéma de base de données (tables, clés, index, avec le SQL de création).
5. Contrat d'API détaillé (endpoints, codes d'erreur, exemples de payloads JSON).
6. Stratégie de concurrence et de cohérence (approbations simultanées, décrément de solde).
7. Sécurité (JWT, rôles, OWASP top 3 applicables).
8. Stratégie de tests (pyramide, ce qui est testé à quel niveau).
9. Plan de déploiement Azure minimal (services, environnements, coût mensuel estimé).
10. Trois ADR (Architecture Decision Records) sur les décisions les plus risquées, format : contexte, décision, conséquences.

Exigences de forme :
- Chaque décision non triviale cite la règle métier ou l'exigence non fonctionnelle qui la motive (RB-xx, ENF).
- Signale explicitement tout point où la spécification est insuffisante et ce que tu as décidé à la place.

Voici le brief :

---
{{BRIEF}}
---

Voici ta spécification (étape 1) :

---
{{SPEC}}
---
