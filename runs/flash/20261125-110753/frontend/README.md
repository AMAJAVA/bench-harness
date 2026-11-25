# TeamLeave — Frontend

SPA React 18 + TypeScript + Vite pour la gestion des congés TeamLeave.

## Installation

```bash
npm install
```

## Développement

```bash
npm run dev
```

Le serveur Vite démarre sur `http://localhost:5173`.
Le proxy `/api` redirige vers `VITE_API_PROXY_TARGET` (par défaut `http://localhost:5000`).

## Build de production

```bash
npm run build
npm run preview
```

## Configuration

Copier `.env.example` en `.env` puis adapter :

| Variable | Rôle | Défaut |
|---|---|---|
| `VITE_API_BASE_URL` | URL de base de l'API | `/api/v1` |
| `VITE_API_PROXY_TARGET` | Cible du proxy Vite en dev | `http://localhost:5000` |

## Architecture

```
src/
├── api/           Client HTTP typé (JWT, gestion 401, endpoints)
├── auth/          Contexte d'authentification + route protégée
├── components/    UI réutilisable (layout, alertes, spinner, badges)
├── pages/         Login, soumettre, calendrier, validation, 404
├── utils/         Helpers dates + calcul jours ouvrés côté client
└── styles/        global.css (CSS maison, justifié ci-dessous)
```

**Sécurité** — Conformément à l'ADR d'architecture, le JWT est stocké **en mémoire seulement** : un rechargement de page entraîne une reconnexion. La durée de validité côté API est de 60 minutes.

**Accessibilité** — Navigation clavier complète, skip-link, focus visible `:focus-visible`, contrastes vérifiés AA, `aria-live` pour les retours, libellés sur tous les contrôles, avertissements de chevauchement lisibles sans couleur seule.

**CSS maison** — Pas de framework : contrôle fin des états focus/contrastes, empreinte ~8 KB, aucune dépendance build, variables CSS pour thème futur. L'app ne compte que 3 écrans, un design system serait surdimensionné.
