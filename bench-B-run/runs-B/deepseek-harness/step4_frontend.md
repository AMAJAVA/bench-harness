# TeamLeave — Implémentation frontend (étape 4)

**Source de vérité** : `00_brief.md`, `step1_spec.md` (v1.0), `step2_arch.md` (v1.0) et contrat réel du backend `backend/` (étape 3).
**Périmètre livré** : SPA React 19 + TypeScript, build Vite, trois écrans fonctionnels et le socle d'authentification nécessaire à leur usage.

---

## 1. Écrans livrés

| Écran | Route | Contenu |
|---|---|---|
| **1. Soumettre une demande** | `/demandes/nouvelle` | Formulaire type / dates / demi-journées / commentaire, soldes disponibles par année, estimation indicative du décompte, confirmation détaillée après création. |
| **2. Calendrier d'équipe** | `/calendrier` | Vue mensuelle accessible des absences **approuvées**, navigation mois précédent / suivant / aujourd'hui, liste textuelle détaillée des absences, masquage du type pour les pairs. |
| **3. File de validation manager** | `/validation` | Demandes en attente des managés directs, détail avec solde du demandeur, signal de chevauchement recalculé, approbation (acquittement obligatoire si signal actif) et refus avec commentaire obligatoire. |
| Connexion (socle) | `/connexion` | Authentification email / mot de passe, erreurs génériques, gestion du 429. |
| Changement de mot de passe forcé (socle) | affiché à la place des routes | Débloque les comptes créés ou réinitialisés par l'Admin RH (`mustChangePassword`). |
| 404 | `*` | Page d'erreur avec retour à l'espace. |

Les trois écrans exigés sont autonomes et accessibles depuis la navigation principale. La validation n'apparaît que pour les comptes portant le rôle `MANAGER` ; le contrôle serveur reste seul juge.

---

## 2. Fichiers produits

### Racine du projet `frontend/`

| Fichier | Rôle |
|---|---|
| `package.json` | Dépendances, scripts `dev`, `build`, `preview`, `typecheck`. |
| `package-lock.json` | Verrouillage reproductible des versions. |
| `index.html` | Point d'entrée HTML, `lang="fr"`, métadonnées. |
| `vite.config.ts` | Plugin React, proxy `/api` de développement, ports 5173 / 4173. |
| `tsconfig.json` | TypeScript strict, `noUncheckedIndexedAccess`, `noUnusedLocals`. |
| `.env.example` | Variables d'environnement documentées. |
| `.gitignore` | Exclusions locales. |

### Code source `frontend/src/`

| Fichier | Rôle |
|---|---|
| `main.tsx` | Montage React, `QueryClient`, `BrowserRouter`, `AuthProvider`. |
| `App.tsx` | Routes, skip-link, annonce de navigation, gestion du focus. |
| `styles/global.css` | Design system et styles de tous les écrans (CSS maison). |
| `api/types.ts` | Types du contrat API v1 (miroirs des DTO C# et de `step2_arch.md` §5). |
| `api/client.ts` | `fetch` typé, en-tête Bearer, corrélation, refresh single-flight, normalisation `ProblemDetails`. |
| `api/endpoints.ts` | Surface d'appels API utilisée par les écrans. |
| `auth/AuthContext.tsx` | Session, reprise par refresh cookie, login/logout, expiration. |
| `auth/RequireAuth.tsx` | Garde d'authentification et garde de rôle Manager. |
| `auth/PasswordChangeGate.tsx` | Écran bloquant de définition d'un nouveau mot de passe. |
| `components/AppShell.tsx` | En-tête, navigation, identité, déconnexion, pied de page. |
| `components/PageHeader.tsx` | En-tête de page homogène. |
| `components/Feedback.tsx` | Alertes, chargements, erreurs, états vides, permission. |
| `components/Badges.tsx` | Statuts, alerte capacité, type de congé. |
| `components/icons.tsx` | Icônes SVG locales (aucune librairie d'icônes). |
| `hooks/usePageTitle.ts` | Titre de document par écran. |
| `lib/dates.ts` | Dates civiles, `Europe/Paris`, grille mensuelle, formats français, estimation. |
| `lib/errors.ts` | Traduction des codes métier en messages actionnables. |
| `lib/labels.ts` | Libellés des rôles et de la chronologie. |
| `pages/LoginPage.tsx` | Connexion. |
| `pages/SubmitLeavePage.tsx` | Écran 1. |
| `pages/CalendarPage.tsx` | Écran 2. |
| `pages/ManagerQueuePage.tsx` | Écran 3. |
| `pages/NotFoundPage.tsx` | 404. |

Le dossier `dist/` est produit par le build et n'est pas un livrable source (ignoré par `.gitignore`).

---

## 3. Choix techniques

### 3.1 Socle

- **React 19 + TypeScript 5.9 strict** : le mode strict, `noUncheckedIndexedAccess` et `noUnusedLocals` sont activés.
- **Vite 8** : dernière version stable au moment de l'implémentation ; elle s'appuie sur Oxc/Rolldown et le plugin `@vitejs/plugin-react` 6. L'architecture prévoyait Vite 7 ; l'écart est assumé car Vite 8 est la version courante, compatible React 19 et Node 24, et ne change pas le code applicatif.
- **React Router 7** pour les routes et les gardes.
- **TanStack Query 5** pour l'état serveur, le cache, les invalidations et les états de chargement/erreur. Aucun store global n'est nécessaire.
- **React Hook Form + Zod** pour les formulaires et leurs validations de forme. Les validations métier définitives restent côté serveur.
- **Dates** : `Intl.DateTimeFormat` natif (fuseau `Europe/Paris` pour « aujourd'hui », UTC pour les dates civiles) plutôt que `date-fns`/`date-fns-tz`, afin de limiter les dépendances et de rendre explicites les calculs de dates sans heure. Le décompte officiel des jours reste calculé par le serveur.

### 3.2 Pourquoi du CSS maison plutôt que Tailwind + Radix

L'architecture de l'étape 2 proposait Tailwind CSS 4 + Radix UI. Pour ce périmètre, le choix retenu est un **CSS maison**, avec justification :

1. **Surface fonctionnelle réduite** : trois écrans, dont un calendrier et une file maître-détail très spécifiques. Les primitives Radix (dialog, menu, popover) ne sont pas utilisées ; les contrôles sont des éléments HTML natifs, déjà accessibles au clavier, complétés par un socle de styles et de gestion du focus.
2. **Pas de librairie de composants lourde** : l'exigence est explicite. Un fichier de tokens + composants CSS évite un thème imposé, un bundle supplémentaire et une chaîne de build additionnelle.
3. **Accessibilité maîtrisée** : contrastes vérifiés (texte/fond ≥ 4,5:1), focus visible, `aria-*` explicites, table sémantique avec `caption`, information jamais portée par la couleur seule (statuts et alerte ont un libellé), libellés persistants et erreurs associées. Le CSS permet d'auditer ces règles directement.
4. **Poids et performance** : la feuille produite pèse ~32 kB (7,2 kB gzip), sans dépendance UI. Le bundle reste compact (≈136 kB gzip, React et Router inclus) et vise la cible de premier affichage utile ≤ 2,5 s sur le réseau interne.
5. **Cohérence visuelle** : les tokens (`--brand`, `--warning`, `--radius`, etc.) centralisent la palette et les espacements ; les composants (`card`, `alert`, `badge`, `field`, `btn`) garantissent l'homogénéité des trois écrans.

Le coût est une feuille de styles plus longue à maintenir qu'un jeu d'utilitaires. À l'échelle de la v1 (3 écrans), il est inférieur au coût d'intégration et de revue de Tailwind + Radix. Si le produit s'étend aux écrans d'administration (types, comptes, soldes, audit), l'introduction d'un design system ou de Tailwind pourra être réévaluée sans réécrire les composants métier.

### 3.3 Client API et gestion du JWT

- **Contrat typé** : `api/types.ts` reprend les DTO du backend (`TeamLeave.Application/Contracts`) et les conventions de `step2_arch.md` §5 (camelCase, dates `YYYY-MM-DD`, horodatages ISO UTC, enveloppe paginée, `application/problem+json`). Les types sont écrits à la main pour rester alignés sur le backend réel ; ils peuvent être régénérés depuis OpenAPI (`/openapi/v1.json`) lors d'une prochaine itération.
- **Jeton d'accès** : conservé **en mémoire uniquement**, jamais dans `localStorage`, et injecté en `Authorization: Bearer`.
- **Jeton de rafraîchissement** : porté par le cookie `HttpOnly` `__Host-tl_rt` posé par l'API. Le client appelle `POST /auth/refresh` avec `credentials: "include"`.
- **Reprise de session** : au démarrage, un refresh silencieux est tenté ; s'il réussit, `GET /me` charge le profil. Sinon, l'écran de connexion est affiché.
- **Single-flight** : plusieurs 401 simultanés ne déclenchent qu'un seul refresh ; chaque requête est ensuite rejouée une fois.
- **Expiration** : si le refresh échoue, la session est purgée et l'utilisateur est redirigé avec un message explicite.
- **Changement de mot de passe forcé** : `GET /me` et `PUT /me/password` sont les seuls appels autorisés par le backend tant que `mustChangePassword` est vrai ; l'interface affiche donc un écran bloquant, puis déconnecte l'utilisateur après succès (les jetons sont révoqués côté serveur).
- **Erreurs** : `ApiError` expose `status`, `code`, `detail`, `traceId`, `balanceShortages`, `conflict`, `errors`. Les codes `PERSONAL_OVERLAP`, `NO_APPROVER`, `INSUFFICIENT_BALANCE`, `INSUFFICIENT_BALANCE_AT_APPROVAL`, `OVERLAP_ACK_REQUIRED`, `INVALID_STATE`, `COMMENT_REQUIRED` sont traduits en messages et en actions concrètes.
- **Corrélation** : chaque requête porte un `X-Correlation-Id` pour l'exploitation.

### 3.4 Accessibilité et responsive

- `lang="fr"`, titres de page explicites, annonce `aria-live` du changement d'écran, focus programmatique sur `<main>`.
- Skip-link « Aller au contenu principal ».
- Navigation clavier complète, focus visible `3px` avec offset.
- Champs avec `<label>` persistants, `aria-invalid`, `aria-describedby`, messages d'erreur associés.
- Tableau du calendrier avec `caption`, en-têtes `scope="col"`, résumé `sr-only` par cellule et liste textuelle alternative des absences.
- Statuts et alertes de capacité portés par un libellé, pas seulement par la couleur.
- Responsive mobile-first de 320/360 px à 1 920 px : passage à une colonne, navigation compacte, calendrier en cellules synthétiques et liste détaillée.
- `prefers-reduced-motion` respecté, `color-scheme: light`.

---

## 4. Points d'attention contractuels

- **File manager et identité du demandeur** : le DTO de liste du backend (`LeaveRequestListItem`) ne contient pas l'identité du demandeur. La file affiche donc le type, la période, le nombre de jours et la date de soumission ; le nom du collaborateur est affiché dès l'ouverture du détail (`GET /manager/leave-requests/{id}`), qui est la source autoritative. Aucun appel N+1 n'a été ajouté pour contourner ce contrat.
- **Refresh cookie en développement** : le cookie est `Secure`, `HttpOnly`, `SameSite=Strict`. Il fonctionne sur `http://localhost` (localhost est traité comme contexte sûr par les navigateurs modernes). En production, l'API et la SPA doivent être servies sous le même site (ou via un reverse proxy) pour que le cookie soit renvoyé.
- **Aucun écran d'administration** : hors périmètre de l'étape 4 (trois écrans exigés). Les endpoints d'administration du backend ne sont pas appelés.
- **Annulation** : non exposée dans ces trois écrans, conformément au périmètre demandé ; l'API et le contrat la prévoient par ailleurs.

---

## NOTES DE RUN

### Prérequis

- **Node.js 24 LTS** (testé avec Node 24.15.0) et npm 11.x.
- Le backend `TeamLeave.Api` doit être démarré et accessible (par défaut `http://localhost:5080`), avec une base PostgreSQL initialisée et au moins un compte utilisateur.
- Navigateurs ciblés : deux dernières versions de Chrome, Edge, Firefox et Safari.

### Installation

```powershell
cd frontend
npm install
```

Le projet n'utilise aucune librairie de composants ni de police distante. Le build n'exige aucun script post-installation spécifique.

### Développement

```powershell
cd frontend
npm run dev
```

- SPA : `http://localhost:5173`
- Le proxy Vite redirige `/api` vers `http://localhost:5080` (`changeOrigin: false`, pour conserver une origine same-site et le cookie de refresh).
- Si le backend tourne sur un autre port : `$env:VITE_API_PROXY_TARGET="http://localhost:5099"; npm run dev`.

### Build et prévisualisation

```powershell
cd frontend
npm run build
npm run preview
```

- `npm run build` exécute `tsc --noEmit` puis `vite build`.
- `npm run preview` sert le build sur `http://localhost:4173` (origine également autorisée par le backend).
- Sortie : `frontend/dist/`.

### Variables d'environnement

| Variable | Défaut | Rôle |
|---|---|---|
| `VITE_API_BASE_URL` | `/api/v1` | URL de base de l'API, préfixe de version inclus. En production, renseigner l'URL absolue de l'API (ou conserver un reverse proxy same-site). |
| `VITE_API_PROXY_TARGET` | `http://localhost:5080` | Cible du proxy Vite en développement et en preview. Ignorée si l'on appelle directement une URL absolue. |

Copier `.env.example` vers `.env.local` pour personnaliser. Ne jamais committer de secret : le frontend ne manipule que des URL publiques.

### Dernier `npm run build` exécuté

Commande : `cd frontend && npm run build` — **succès**.

```text
> teamleave-frontend@1.0.0 build
> tsc --noEmit && vite build

vite v8.3.8 building client environment for production...
transforming...
✓ 100 modules transformed.
rendering chunks...
computing gzip size...
dist/index.html                   0.60 kB │ gzip:   0.36 kB
dist/assets/index-DmfOEJfX.css   32.43 kB │ gzip:   7.22 kB
dist/assets/index-UWi6BGC-.js   457.56 kB │ gzip: 136.61 kB

✓ built in 582ms
```

Le contrôle TypeScript (`tsc --noEmit`) est inclus dans le build et passe sans erreur.
