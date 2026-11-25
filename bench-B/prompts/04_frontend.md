# ÉTAPE 4 — IMPLÉMENTATION FRONTEND

Tu travailles dans le dossier courant. Lis d'abord `00_brief.md`, `step1_spec.md` (ta spécification) et `step2_arch.md` (ton dossier d'architecture). Le backend de l'étape 3 est dans `backend/` : lis-le seulement pour t'assurer que le client API correspond au contrat.

Implémente le frontend React + TypeScript (Vite) de TeamLeave, en suivant ta spécification et ton architecture.

Périmètre exigé :
- Trois écrans : (1) soumettre une demande de congé, (2) calendrier d'équipe des absences approuvées, (3) file de validation manager (approuver / refuser avec commentaire).
- Client API typé à partir du contrat de l'étape 2, gestion du JWT.
- États de chargement, d'erreur et vides soignés.
- Accessibilité : navigation clavier, contrastes, aria-labels sur les contrôles.
- Design : propre, sobre, responsive mobile-first, sans librairie de composants lourde (CSS maison ou Tailwind, à justifier).

LIVRABLE (mode agent) :
- Écris le projet dans le dossier `frontend/` (chemins relatifs).
- Le projet doit démarrer avec `npm install && npm run build` sans erreur. Tu peux lancer ces commandes toi-même et corriger tes erreurs.
- Écris aussi un court document `step4_frontend.md` à la racine du dossier courant : liste des fichiers produits, justification du choix CSS maison / Tailwind, section finale "NOTES DE RUN" (npm install, npm run dev, variables d'environnement) et résultat réel du dernier `npm run build` ("non lancé" si non lancé).

CONSIGNE DE PÉRIMÈTRE : ne dépasse pas le périmètre de cette étape. Ne modifie ni `backend/` ni les documents des étapes 1 à 3.
