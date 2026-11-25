# TeamLeave : étape 4, implémentation frontend

| Élément | Valeur |
|---|---|
| Entrées | `00_brief.md`, `step1_spec.md`, `step2_arch.md` ; `backend/` lu pour aligner le client API (`Contracts/Dtos.cs`, contrôleurs, `Problems.cs`, `Hosting.cs`, `launchSettings.json`). Aucun de ces fichiers n'a été modifié. |
| Livrable | `frontend/` : SPA React 19 + TypeScript, construite avec Vite 8 |
| Dépendances d'exécution | `react` 19.3.1, `react-dom` 19.3.1, `@tanstack/react-query` 5.108.4 (choix de step2_arch §2.2), `react-router` 8.5.3 |
| Dépendances de build | `vite` 8.3.8, `@vitejs/plugin-react` 6.3.0, `typescript` 7.0.2, `@types/react` 19.3.3, `@types/react-dom` 19.3.0 |

Les versions sont épinglées exactement dans `package.json` ; `package-lock.json` est livré.

## 1. Fichiers produits

Les chemins sont relatifs à `frontend/`.

**Configuration**

| Fichier | Rôle |
|---|---|
| `package.json`, `package-lock.json` | Scripts `dev`, `typecheck`, `build` (`tsc --noEmit -p . && vite build`), `preview`. |
| `tsconfig.json` | TypeScript strict (`noUncheckedIndexedAccess`, `noUnusedLocals`, `verbatimModuleSyntax`), résolution `bundler`. |
| `vite.config.ts` | Proxy `/api` vers l'API en développement, `assetsInlineLimit: 0` (aucun asset en `data:`, voir §3), sortie `dist/`. |
| `index.html` | Coquille `lang="fr"`, sans script ni style en ligne. |
| `.env.example` | Variables facultatives (voir NOTES DE RUN). |
| `src/vite-env.d.ts` | Types des variables d'environnement Vite (`VITE_API_BASE_URL`). |
| `public/favicon.svg` | Icône. |

**Client API et session (`src/api`, `src/auth`)**

| Fichier | Rôle |
|---|---|
| `api/types.ts` | Types du contrat §5 (DTO, énumérations, Problem Details et codes d'erreur §5.8), recopiés de `Dtos.cs`. |
| `api/client.ts` | `fetch` typé : base `/api/v1`, en-tête `Authorization: Bearer`, conversion des Problem Details en `ApiError` (`status`, `code`, `errors` par champ, `Retry-After`). Un 401 déclenche « session expirée » (sauf `POST /auth/login`) ; un 403 `PASSWORD_CHANGE_REQUIRED` déclenche le changement de mot de passe. |
| `api/endpoints.ts` | Une fonction par couple verbe/chemin utilisé, plus les clés TanStack Query. |
| `api/errors.ts` | Message français par code stable ; `detail` du serveur pour les 422 ; heure de fin de verrouillage (`lockoutEndUtc`) en heure de Paris ; normalisation des clés `errors` (`$.startDate` → `startDate`). |
| `auth/session.ts` | Jeton en `sessionStorage` (step2_arch §2.4), expiration contrôlée au chargement. |
| `auth/AuthContext.tsx` | Connexion, déconnexion, profil `GET /me`, rôles, déconnexion automatique à l'expiration du jeton (60 min, pas de rafraîchissement, ADR-02). |

**Écrans (`src/pages`)**

| Fichier | Rôle |
|---|---|
| `NewRequestPage.tsx` | **Écran 1, soumettre une demande** (US-02, US-03, US-04). Type, dates, commentaire avec l'avertissement « N'indiquez aucune information médicale » (ENF-RGPD3) et compteur de 500 caractères. Simulation automatique (`POST /leave-requests/simulate`, 400 ms après la dernière saisie) : jours décomptés, disponible avant et après, motifs bloquants (GAP-10). Soldes affichés à côté du formulaire. Confirmation après le 201, qui reçoit le focus. |
| `CalendarPage.tsx` | **Écran 2, calendrier d'équipe** (US-07, US-12, US-19). Absences approuvées du mois (≤ 93 jours, ENF-P3). Portées `team`, `reports` (Manager) et `all` (Admin RH) ; par défaut `reports` pour un manager, `team` sinon. Vue **grille** (tableau avec en-têtes de ligne et de colonne, code du type en texte, week-ends et jours fériés hachurés) et vue **liste** (ENF-A3). Troncature à la fenêtre faite côté client (§5.5). |
| `ApprovalQueuePage.tsx` | **Écran 3, file de validation** (US-08). `GET /leave-requests?scope=to-decide`, paginée, actualisée toutes les 60 s. Indicateur texte « Chevauchement > 50 % » avec le taux maximal (CA-08.2, ENF-A4). |
| `DecisionDialog.tsx` | Examen et décision dans un `<dialog>` modal natif (piège du focus, Échap). Détail `GET /leave-requests/{id}` et analyse de chevauchement (jours, taux, collègues absents, US-09). **Approuver** : commentaire facultatif ; case d'acquittement obligatoire si l'analyse est en alerte. Un 422 `OVERLAP_NOT_ACKNOWLEDGED` affiche l'analyse recalculée renvoyée par le serveur (RB-17, RB-18). **Refuser** : commentaire obligatoire, contrôlé côté client et code `REJECTION_COMMENT_REQUIRED` géré (RB-15). Sur 403, 404 ou 409, un message s'affiche et la file est actualisée. |
| `LoginPage.tsx` | Connexion (US-01) : 401 `INVALID_CREDENTIALS`, 429 `ACCOUNT_LOCKED` et `RATE_LIMITED`. |
| `ChangePasswordPage.tsx` | Changement du mot de passe temporaire (GAP-06). Sans cet écran, le compte RH amorcé ne peut rien faire, car l'API répond 403 `PASSWORD_CHANGE_REQUIRED`. Après le 204, le tampon `sst` change, donc l'utilisateur se reconnecte. |
| `NotFoundPage.tsx` | Route inconnue. |

**Composants et utilitaires**

| Fichier | Rôle |
|---|---|
| `App.tsx`, `main.tsx` | Routes (`/connexion`, `/mot-de-passe`, `/demandes/nouvelle`, `/calendrier`, `/validation`), `QueryClient` (une seule relance, réservée aux pannes réseau et aux 5xx). |
| `components/Layout.tsx` | En-tête, navigation principale (`aria-current`), lien d'évitement « Aller au contenu ». Le lien « Demandes à valider » n'apparaît que pour les rôles Manager et Admin RH. |
| `components/RouteGuards.tsx` | Redirections ergonomiques (authentification, changement de mot de passe, rôle de décideur). Le serveur reste seul juge (ENF-I2). |
| `components/Feedback.tsx` | États de chargement (`role="status"`), d'erreur avec bouton « Réessayer » (`role="alert"`), vides, messages. |
| `components/OverlapDetails.tsx` | Analyse de chevauchement présentée en texte (alerte, sans alerte, non applicable). |
| `components/Pagination.tsx` | Pagination (§5.1). |
| `lib/dates.ts` | Dates civiles calculées en UTC, sans conversion de fuseau ; horodatages affichés en Europe/Paris, avec ajout de `Z` si l'API l'omet (GAP-09). |
| `lib/format.ts` | Libellés français des statuts, rôles et motifs bloquants ; nombres de jours au format `fr-FR`. |
| `lib/usePageHeading.ts` | Titre du document et focus sur le `h1` à chaque changement d'écran. |
| `lib/useDebouncedValue.ts` | Temporisation de la simulation. |
| `styles/app.css` | Feuille de style unique (voir §2). |

## 2. CSS maison plutôt que Tailwind

Choix : **une feuille CSS maison** (`src/styles/app.css`, environ 15 ko, 3,6 ko gzip), construite sur des variables CSS (couleurs, espacements, rayons) et des classes nommées par composant.

| Critère | CSS maison | Tailwind |
|---|---|---|
| Dépendances et outillage | Aucune : Vite traite le CSS nativement. | Plugin Vite ou PostCSS, configuration du thème, mises à jour majeures à suivre (v3 → v4). |
| Volume de l'interface | Trois écrans et quatre écrans de service : une feuille unique reste lisible. | Gain de vitesse réel sur une grande interface, faible ici. |
| Accessibilité | Contrastes fixés et documentés dans les variables : texte ≥ 4,5:1, bordures de champs ≥ 3:1. États pilotés par les attributs ARIA (`[aria-invalid]`, `[aria-pressed]`, `[aria-current]`), ce qui garde le style aligné sur la sémantique. | Possible, mais les couleurs sont choisies classe par classe et le contraste n'est vérifié nulle part de façon centrale. |
| CSP de l'API (`default-src 'self'`) | Un fichier CSS externe, aucun style en ligne. | Compatible aussi, une fois compilé. |
| Reprise par l'équipe | CSS standard, lisible par tout développeur. | Il faut connaître le vocabulaire Tailwind. |

Approche **mobile d'abord** : mise en page en une colonne jusqu'à 360 px (ENF-D5), puis deux points de rupture (`30rem`, `48rem`). Cibles tactiles d'au moins 44 px. Focus visible (anneau double, blanc puis bleu). `prefers-reduced-motion` est respecté. La grille du calendrier défile horizontalement, avec la colonne des noms figée. Sur petit écran, la vue liste est proposée par défaut.

## 3. Écarts et décisions de l'étape 4

| ID | Constat | Décision |
|---|---|---|
| F-01 | step2_arch §2.2 prévoit des types **générés** depuis OpenAPI (`openapi-typescript`). Il faut une API en marche pour obtenir `/openapi/v1.json`. Pendant ce run, l'environnement a exigé une approbation pour lancer l'exécutable de l'API, et elle n'a pas été accordée (§5). Un `dotnet run` classique aurait de plus écrit une base SQLite et des sorties de build dans `backend/`, que cette étape interdit de modifier. | Types **écrits à la main** dans `src/api/types.ts`, champ par champ depuis `Dtos.cs`. Énumérations en chaînes, car l'API utilise `JsonStringEnumConverter(allowIntegerValues: false)`. Pour revenir à la génération plus tard : `npx openapi-typescript http://localhost:5080/openapi/v1.json -o src/api/schema.gen.ts`, puis remplacer `types.ts`. |
| F-02 | Seules les ressources des trois écrans sont typées (connexion, profil, soldes, types, jours fériés, simulation, soumission, file, détail, décisions, calendrier). | Le reste du contrat (administration RH, journaux, annulation, « mes demandes ») est hors du périmètre de l'étape. Il s'ajoute dans `types.ts` et `endpoints.ts` sur le même modèle. |
| F-03 | La SPA doit être servie par l'API depuis `wwwroot` (step2_arch §2.2). | `vite build` écrit dans `frontend/dist`, pas dans `backend/` (interdit à cette étape). La copie vers `backend/src/TeamLeave.Api/wwwroot` revient au pipeline de déploiement (voir NOTES DE RUN). |
| F-04 | CSP de l'API : `default-src 'self'; script-src 'self'`, sans `style-src` ni `img-src`. Le serveur Vite n'envoie pas cet en-tête, donc une violation ne se verrait qu'en production. | Aucun script ni style en ligne dans `index.html`, aucune police ni CDN externe (pile de polices système), aucun `data:` URI (`assetsInlineLimit: 0`, pas de SVG en `url()` dans le CSS), API en chemin relatif (`/api/v1`). |
| F-05 | Le changement de mot de passe et la connexion ne font pas partie des trois écrans demandés. | Ils sont livrés quand même, réduits au minimum : sans eux, l'application est inutilisable (gestion du JWT, GAP-06). |
| F-06 | L'équipe d'un calendrier n'est connue qu'à travers les absences renvoyées : l'API ne liste pas les membres sans absence. | La grille affiche une ligne par personne ayant au moins une absence approuvée dans le mois. Une fenêtre vide affiche un état vide explicite. |
| F-07 | La file de décision doit refléter les décisions prises ailleurs (autre manager, admin RH). | Actualisation toutes les 60 s, plus invalidation après chaque décision ou erreur 403, 404 ou 409. |

## 4. Couverture du périmètre exigé

| Exigence | Où |
|---|---|
| Écran 1 : soumettre une demande | `NewRequestPage.tsx` (simulation US-03, soumission US-04, soldes US-02) |
| Écran 2 : calendrier des absences approuvées | `CalendarPage.tsx` (portées RB-23, grille et liste ENF-A3, jours fériés) |
| Écran 3 : file de validation, approuver ou refuser avec commentaire | `ApprovalQueuePage.tsx`, `DecisionDialog.tsx` (US-08 à US-11, acquittement RB-18, commentaire de refus RB-15) |
| Client API typé, gestion du JWT | `api/*`, `auth/*` : Bearer, `sessionStorage`, expiration, 401, 403 `PASSWORD_CHANGE_REQUIRED`, 429 |
| États de chargement, d'erreur et vides | `Feedback.tsx`, utilisé dans chaque bloc de données (avec « Réessayer ») ; chargement des boutons pendant l'envoi ; message si une actualisation échoue alors que des données sont affichées |
| Accessibilité | Lien d'évitement ; focus sur le `h1` à chaque écran ; tous les champs ont un `label` ; erreurs liées par `aria-describedby` et `aria-invalid` ; focus sur le premier champ invalide ; messages en `role="alert"` ou `status` ; simulation en `aria-live` ; `<dialog>` modal ; boutons à icône avec texte masqué ; grille en vrai `<table>` (`caption`, `th scope`) ; alerte de chevauchement en texte ; rien ne repose sur la seule couleur. |
| Design responsive mobile d'abord, sans bibliothèque de composants | `styles/app.css` (§2) |

## 5. Limites de ce livrable

- **Aucune exécution de bout en bout n'a été faite.** L'environnement a exigé une approbation pour lancer l'API, puis pour piloter un navigateur (Playwright). Ni l'une ni l'autre n'a été accordée. Le serveur de développement Vite a démarré sans erreur (« ready in 536 ms »), puis a été arrêté. La vérification réelle se limite donc à la compilation TypeScript stricte et au build de production. La conformité au contrat repose sur la lecture du code du backend (`Dtos.cs`, contrôleurs, `Problems.cs`, `Hosting.cs`).
- Aucun test automatisé frontend n'est livré : ni l'étape ni l'architecture (§8, xUnit côté backend) ne l'exigent.
- L'accessibilité (RGAA 4.1) n'a pas été auditée avec un lecteur d'écran. Elle est conçue pour, mais pas vérifiée.

## NOTES DE RUN

Prérequis : Node.js **22.22 ou plus récent**, la contrainte la plus stricte des dépendances (`react-router` 8 : `engines.node >=22.22.0` ; Vite 8 : `^20.19.0 || >=22.12.0`). Testé avec Node 24.15.0 et npm 11.12.1. Accès au registre npm requis.

```bash
cd frontend
npm install
npm run build        # vérification TypeScript (tsc --noEmit) puis build Vite dans frontend/dist
npm run dev          # http://localhost:5173, /api relayé vers http://localhost:5080
npm run typecheck    # vérification des types seule
npm run preview      # sert dist/ (sans proxy : pour une vérification statique uniquement)
```

Pour utiliser l'application en développement, lancer l'API à côté (voir `step3_backend.md`) :

```bash
cd backend
dotnet run --project src/TeamLeave.Api --launch-profile http    # http://localhost:5080
```

Compte amorcé en développement : `admin.rh@pme.local` / `ChangeMe-Dev-2026!`. L'écran de changement de mot de passe s'affiche à la première connexion, puis il faut se reconnecter. L'interface d'administration RH n'existe pas à cette étape : il faut créer les autres utilisateurs (employés, managers) et leurs soldes par l'API (`POST /api/v1/users`, `POST /api/v1/users/{id}/balances/{typeId}/movements`, step2_arch §5.6).

Variables d'environnement (toutes facultatives, voir `.env.example`) :

| Variable | Défaut | Usage |
|---|---|---|
| `VITE_API_BASE_URL` | `/api/v1` | Préfixe des appels API, intégré au build. Il doit rester relatif (même origine) pour respecter la CSP `connect-src 'self'` de l'API. |
| `TEAMLEAVE_API_PROXY_TARGET` | `http://localhost:5080` | Cible du proxy `/api` du serveur de développement uniquement. |

Déploiement (step2_arch §2.2) : copier le contenu de `frontend/dist/` dans `backend/src/TeamLeave.Api/wwwroot/` avant la publication de l'API. L'API sert alors les fichiers statiques et renvoie `index.html` pour les routes de la SPA (`MapFallbackToFile`).

### Résultat réel du dernier `npm install && npm run build` (2026-11-25)

```
found 0 vulnerabilities

> teamleave-frontend@1.0.0 build
> tsc --noEmit -p . && vite build

vite v8.3.8 building client environment for production...
transforming...
✓ 159 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.50 kB │ gzip:   0.30 kB
dist/assets/index-DHFZzsMn.css   15.49 kB │ gzip:   3.63 kB
dist/assets/index-BgHRveC1.js   343.05 kB │ gzip: 105.52 kB

✓ built in 461ms
```

0 erreur TypeScript, 0 avertissement de build.
