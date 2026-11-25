# TeamLeave — Frontend React + TypeScript (Vite)

Justification rapide du choix CSS maison : l'application ne compte que 3 écrans, la priorité est un contrôle fin des états de focus, des contrastes et de `prefers-reduced-motion` (WCAG 2.1 AA). CSS maison (~8 KB non compressé) évite une dépendance de build (PostCSS/Tailwind) et un design system surdimensionné. Tailwind serait viable mais son intérêt principal (système utilitaire multi-écrans) n'est pas justifié ici.

=== FILE: package.json ===
{
  "name": "teamleave-frontend",
  "private": true,
  "version": "0.1.0",
  "type": "module",
  "engines": {
    "node": ">=20"
  },
  "scripts": {
    "dev": "vite",
    "build": "tsc --noEmit && vite build",
    "preview": "vite preview --port 4173"
  },
  "dependencies": {
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "react-router-dom": "^6.26.2"
  },
  "devDependencies": {
    "@types/node": "^22.5.5",
    "@types/react": "^18.3.5",
    "@types/react-dom": "^18.3.0",
    "@vitejs/plugin-react": "^4.3.1",
    "typescript": "^5.5.4",
    "vite": "^5.4.3"
  }
}

=== FILE: tsconfig.json ===
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": false,
    "noUnusedParameters": false,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["src"]
}

=== FILE: tsconfig.node.json ===
{
  "compilerOptions": {
    "target": "ES2022",
    "lib": ["ES2023"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "isolatedModules": true,
    "noEmit": true,
    "strict": true
  },
  "include": ["vite.config.ts"]
}

=== FILE: vite.config.ts ===
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5000';

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
        },
      },
    },
  };
});

=== FILE: index.html ===
<!doctype html>
<html lang="fr">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="TeamLeave — Gestion des congés d'équipe" />
    <title>TeamLeave — Gestion des congés</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>

=== FILE: .env.example ===
# URL de base de l'API consommée par le navigateur.
# Par défaut, on utilise le proxy Vite en développement.
VITE_API_BASE_URL=/api/v1

# Cible du proxy Vite en développement (backend ASP.NET Core).
VITE_API_PROXY_TARGET=http://localhost:5000

=== FILE: .gitignore ===
node_modules
dist
dist-ssr
*.local
.env
.env.local
.env.*.local
.DS_Store
.vscode/*
!.vscode/extensions.json
.idea
*.log

=== FILE: README.md ===
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

=== FILE: src/vite-env.d.ts ===
/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  readonly VITE_API_PROXY_TARGET?: string;
}

=== FILE: src/main.tsx ===
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import './styles/global.css';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error("Élément #root introuvable dans le document HTML.");
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

=== FILE: src/App.tsx ===
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Layout } from './components/Layout';
import { ApprovalQueuePage } from './pages/ApprovalQueuePage';
import { LoginPage } from './pages/LoginPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { SubmitLeaveRequestPage } from './pages/SubmitLeaveRequestPage';
import { TeamCalendarPage } from './pages/TeamCalendarPage';

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route index element={<Navigate to="/requests/new" replace />} />
              <Route path="/requests/new" element={<SubmitLeaveRequestPage />} />
              <Route path="/calendar" element={<TeamCalendarPage />} />
              <Route path="/approvals" element={<ApprovalQueuePage />} />
            </Route>
          </Route>
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

=== FILE: src/styles/global.css ===
:root {
  --font-sans: system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif;
  --font-mono: ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace;

  --color-bg: #f5f6f8;
  --color-surface: #ffffff;
  --color-surface-muted: #f9fafb;
  --color-border: #d1d5db;
  --color-border-strong: #9ca3af;
  --color-text: #1f2937;
  --color-text-muted: #4b5563;
  --color-text-subtle: #6b7280;

  --color-primary: #1d4ed8;
  --color-primary-hover: #1e40af;
  --color-primary-contrast: #ffffff;
  --color-primary-soft: #dbeafe;

  --color-danger: #b91c1c;
  --color-danger-hover: #991b1b;
  --color-danger-soft: #fee2e2;

  --color-success: #15803d;
  --color-success-soft: #dcfce7;

  --color-warning-bg: #fef3c7;
  --color-warning-border: #b45309;
  --color-warning-text: #78350f;

  --radius-sm: 4px;
  --radius-md: 8px;
  --radius-lg: 12px;

  --space-1: 0.25rem;
  --space-2: 0.5rem;
  --space-3: 0.75rem;
  --space-4: 1rem;
  --space-5: 1.5rem;
  --space-6: 2rem;

  --shadow-sm: 0 1px 2px rgba(15, 23, 42, 0.06), 0 1px 3px rgba(15, 23, 42, 0.08);
  --shadow-md: 0 4px 6px rgba(15, 23, 42, 0.05), 0 10px 15px rgba(15, 23, 42, 0.05);
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

html,
body {
  margin: 0;
  padding: 0;
}

body {
  font-family: var(--font-sans);
  background: var(--color-bg);
  color: var(--color-text);
  font-size: 16px;
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
  text-rendering: optimizeLegibility;
}

@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.001ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.001ms !important;
    scroll-behavior: auto !important;
  }
}

h1,
h2,
h3,
h4 {
  color: var(--color-text);
  line-height: 1.25;
  margin: 0 0 var(--space-3);
}
h1 { font-size: 1.5rem; font-weight: 700; }
h2 { font-size: 1.25rem; font-weight: 600; }
h3 { font-size: 1.05rem; font-weight: 600; }

p { margin: 0 0 var(--space-3); }

a {
  color: var(--color-primary);
  text-decoration: underline;
  text-underline-offset: 2px;
}
a:hover { color: var(--color-primary-hover); }

:focus-visible {
  outline: 3px solid var(--color-primary);
  outline-offset: 2px;
  border-radius: var(--radius-sm);
}

.skip-link {
  position: absolute;
  left: 0.5rem;
  top: -100px;
  background: var(--color-primary);
  color: var(--color-primary-contrast);
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-md);
  text-decoration: none;
  z-index: 1000;
}
.skip-link:focus { top: 0.5rem; }

/* Layout */
.app-shell {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
.app-header {
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  position: sticky;
  top: 0;
  z-index: 10;
}
.app-header-inner {
  max-width: 1100px;
  margin: 0 auto;
  padding: var(--space-3) var(--space-4);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  flex-wrap: wrap;
}
.brand {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  font-weight: 700;
  font-size: 1.125rem;
  text-decoration: none;
  color: var(--color-text);
}
.brand-mark {
  display: inline-grid;
  place-items: center;
  width: 28px;
  height: 28px;
  border-radius: 8px;
  background: var(--color-primary);
  color: var(--color-primary-contrast);
  font-size: 0.8rem;
  font-weight: 700;
}
.nav-list {
  display: flex;
  gap: var(--space-1);
  list-style: none;
  margin: 0;
  padding: 0;
  flex-wrap: wrap;
}
.nav-link {
  display: inline-flex;
  align-items: center;
  padding: var(--space-2) var(--space-3);
  border-radius: var(--radius-md);
  text-decoration: none;
  color: var(--color-text-muted);
  font-weight: 500;
  font-size: 0.95rem;
}
.nav-link:hover { background: var(--color-surface-muted); color: var(--color-text); }
.nav-link[aria-current="page"] {
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
}
.user-summary {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: 0.875rem;
  color: var(--color-text-muted);
}
.user-summary .avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
  display: inline-grid;
  place-items: center;
  font-weight: 600;
  font-size: 0.75rem;
}
.app-main {
  flex: 1;
  width: 100%;
  max-width: 1100px;
  margin: 0 auto;
  padding: var(--space-5) var(--space-4);
}

/* Cards */
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  box-shadow: var(--shadow-sm);
}
.card + .card { margin-top: var(--space-4); }
.section-label {
  display: block;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-subtle);
  margin-bottom: var(--space-2);
}

/* Forms */
.form { display: grid; gap: var(--space-4); max-width: 640px; }
.form-row { display: grid; gap: var(--space-4); grid-template-columns: 1fr; }
@media (min-width: 640px) {
  .form-row.cols-2 { grid-template-columns: 1fr 1fr; }
}
.field { display: flex; flex-direction: column; gap: var(--space-1); }
.field label { font-weight: 600; font-size: 0.95rem; }
.field .hint { font-size: 0.85rem; color: var(--color-text-subtle); }
.field input,
.field select,
.field textarea {
  font: inherit;
  color: inherit;
  padding: var(--space-2) var(--space-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  min-height: 40px;
}
.field textarea { resize: vertical; min-height: 80px; }
.field input[aria-invalid="true"],
.field select[aria-invalid="true"],
.field textarea[aria-invalid="true"] {
  border-color: var(--color-danger);
}
.field .error-text { color: var(--color-danger); font-size: 0.85rem; }

/* Buttons */
.btn {
  font: inherit;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-1);
  padding: var(--space-2) var(--space-4);
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  font-weight: 600;
  cursor: pointer;
  min-height: 40px;
  transition: background-color 0.15s ease, color 0.15s ease;
}
.btn:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-primary { background: var(--color-primary); color: var(--color-primary-contrast); }
.btn-primary:hover:not(:disabled) { background: var(--color-primary-hover); }
.btn-secondary {
  background: var(--color-surface);
  color: var(--color-text);
  border-color: var(--color-border);
}
.btn-secondary:hover:not(:disabled) { background: var(--color-surface-muted); }
.btn-danger { background: var(--color-danger); color: #ffffff; }
.btn-danger:hover:not(:disabled) { background: var(--color-danger-hover); }
.btn-ghost { background: transparent; color: var(--color-primary); border-color: transparent; }
.btn-ghost:hover:not(:disabled) { background: var(--color-primary-soft); }
.btn-sm { min-height: 32px; padding: var(--space-1) var(--space-3); font-size: 0.9rem; }

/* Alerts */
.alert {
  display: flex;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-surface-muted);
  align-items: flex-start;
}
.alert-title { font-weight: 700; }
.alert-body { font-size: 0.95rem; }
.alert-body ul { margin: var(--space-2) 0 0; padding-left: var(--space-4); }
.alert-info { background: var(--color-primary-soft); border-color: #93c5fd; color: #1e3a8a; }
.alert-error { background: var(--color-danger-soft); border-color: #fca5a5; color: var(--color-danger); }
.alert-success { background: var(--color-success-soft); border-color: #86efac; color: #166534; }
.alert-warning {
  background: var(--color-warning-bg);
  border-color: var(--color-warning-border);
  color: var(--color-warning-text);
}
.alert-icon { flex-shrink: 0; font-weight: 700; }

/* Badges */
.badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding: 2px var(--space-2);
  border-radius: 999px;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.01em;
  text-transform: uppercase;
  background: var(--color-surface-muted);
  color: var(--color-text-muted);
}
.badge-pending { background: #fef3c7; color: #78350f; }
.badge-approved { background: var(--color-success-soft); color: #166534; }
.badge-rejected { background: var(--color-danger-soft); color: var(--color-danger); }

/* Loading */
.loading-state {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--color-text-muted);
  padding: var(--space-5) 0;
}
.spinner {
  width: 20px;
  height: 20px;
  border: 2px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

/* Empty state */
.empty-state {
  text-align: center;
  padding: var(--space-6) var(--space-4);
  color: var(--color-text-muted);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-surface-muted);
}
.empty-state h3 { color: var(--color-text); }
.empty-state p { margin-bottom: var(--space-3); }

/* Error state */
.error-state {
  max-width: 640px;
  margin: 0 auto;
  padding: var(--space-6) var(--space-4);
}
.error-state .row { justify-content: center; margin-top: var(--space-4); }

/* Table */
.table-wrap { overflow-x: auto; border-radius: var(--radius-lg); border: 1px solid var(--color-border); }
.data-table {
  width: 100%;
  border-collapse: collapse;
  background: var(--color-surface);
  font-size: 0.95rem;
}
.data-table th,
.data-table td {
  text-align: left;
  padding: var(--space-3);
  border-bottom: 1px solid var(--color-border);
  vertical-align: top;
}
.data-table thead th {
  background: var(--color-surface-muted);
  font-weight: 600;
  font-size: 0.85rem;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--color-text-muted);
}
.data-table tbody th {
  background: transparent;
  text-transform: none;
  letter-spacing: normal;
  font-size: 0.95rem;
  color: var(--color-text);
  font-weight: 600;
}
.data-table tr:last-child td,
.data-table tr:last-child th { border-bottom: none; }

/* Calendar */
.calendar-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-3);
  margin-bottom: var(--space-3);
  flex-wrap: wrap;
}
.calendar-month-label {
  font-weight: 700;
  font-size: 1.05rem;
  min-width: 12ch;
  text-align: center;
}
.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 4px;
  background: var(--color-border);
  padding: 4px;
  border-radius: var(--radius-md);
}
.calendar-weekday {
  text-align: center;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--color-text-muted);
  padding: var(--space-1) 0;
}
.calendar-cell {
  background: var(--color-surface);
  border-radius: var(--radius-sm);
  min-height: 76px;
  padding: var(--space-1);
  font-size: 0.8rem;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow: hidden;
}
.calendar-cell.outside {
  background: var(--color-surface-muted);
  color: var(--color-text-subtle);
}
.calendar-cell.today .calendar-day-number {
  background: var(--color-primary);
  color: var(--color-primary-contrast);
}
.calendar-day-number {
  display: inline-grid;
  place-items: center;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  font-weight: 600;
  font-size: 0.8rem;
}
.calendar-chip {
  background: var(--color-primary-soft);
  color: var(--color-primary-hover);
  border-radius: var(--radius-sm);
  padding: 0 var(--space-1);
  font-size: 0.68rem;
  line-height: 1.5;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-weight: 700;
}
.calendar-chip.more {
  background: var(--color-surface-muted);
  color: var(--color-text-muted);
  font-weight: 600;
}

/* Approval queue */
.request-card { display: grid; gap: var(--space-3); }
.request-card-header {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  align-items: center;
  justify-content: space-between;
}
.request-meta {
  display: grid;
  gap: var(--space-1);
  font-size: 0.95rem;
  color: var(--color-text-muted);
}
.request-meta strong { color: var(--color-text); }
.request-actions {
  display: flex;
  gap: var(--space-2);
  flex-wrap: wrap;
}
.reject-form {
  display: grid;
  gap: var(--space-2);
  margin-top: var(--space-2);
  padding: var(--space-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface-muted);
}
.checkbox-row {
  display: flex;
  gap: var(--space-2);
  align-items: flex-start;
  font-size: 0.95rem;
}
.checkbox-row input[type="checkbox"] {
  width: 20px;
  height: 20px;
  margin-top: 2px;
  accent-color: var(--color-primary);
}

/* Login */
.login-shell {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: var(--space-5) var(--space-4);
  background: linear-gradient(180deg, #eef2ff 0%, var(--color-bg) 100%);
}
.login-card {
  width: 100%;
  max-width: 420px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--space-6);
  box-shadow: var(--shadow-md);
}
.login-card h1 { text-align: center; margin-bottom: var(--space-1); }
.login-subtitle {
  text-align: center;
  color: var(--color-text-muted);
  margin-bottom: var(--space-5);
}

/* Utilities */
.stack > * + * { margin-top: var(--space-3); }
.stack-lg > * + * { margin-top: var(--space-5); }
.row { display: flex; gap: var(--space-2); flex-wrap: wrap; align-items: center; }
.spacer { flex: 1; }
.muted { color: var(--color-text-muted); }
.text-sm { font-size: 0.875rem; }
.text-xs { font-size: 0.78rem; }
.mono { font-family: var(--font-mono); }
.hidden-visually {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

=== FILE: src/api/types.ts ===
export type Role = 'EMPLOYE' | 'MANAGER' | 'ADMIN_RH';

export type LeaveStatus = 'PENDING' | 'APPROVED' | 'REJECTED';

export type DecisionAction = 'APPROVE' | 'REJECT';

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: Role[];
  teamId?: string | null;
  team?: { id: string; name: string } | null;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: 'Bearer';
  expiresIn: number;
  user: AuthUser;
}

export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeLabel: string;
  year: number;
  acquiredDays: number;
  adjustmentDays: number;
  usedDays: number;
  availableDays: number;
}

export interface LeaveType {
  id: string;
  code: string;
  label: string;
  isActive: boolean;
  trackBalance: boolean;
  displayOrder: number;
}

export interface OverlapDetail {
  date: string;
  absentEmployees: string[];
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeFirstName?: string;
  employeeLastName?: string;
  teamId: string;
  leaveType: { id: string; code: string; label: string };
  startDate: string;
  endDate: string;
  workingDays: number;
  status: LeaveStatus;
  employeeComment?: string | null;
  submittedAt: string;
  deciderId?: string | null;
  decidedAt?: string | null;
  managerComment?: string | null;
  hasOverlapWarning: boolean;
  overlapRatio?: number | null;
  overlapDetails?: OverlapDetail[] | null;
  acknowledgedOverlapBy?: string | null;
  acknowledgedOverlapAt?: string | null;
}

export interface CreateLeaveRequestInput {
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  comment?: string;
}

export interface DecisionInput {
  action: DecisionAction;
  comment?: string;
  acknowledgeOverlapWarning: boolean;
}

export interface CalendarEntry {
  leaveRequestId: string;
  employeeId: string;
  employeeFirstName: string;
  employeeLastName: string;
  leaveTypeCode: string;
  leaveTypeLabel: string;
  startDate: string;
  endDate: string;
}

export interface Holiday {
  id: string;
  date: string;
  label: string;
  isActive: boolean;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

=== FILE: src/api/client.ts ===
import type { ProblemDetails } from './types';

const BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? '/api/v1';

let currentToken: string | null = null;
let unauthorizedHandler: (() => void) | null = null;

export function setToken(token: string | null): void {
  currentToken = token;
}

export function setUnauthorizedHandler(fn: (() => void) | null): void {
  unauthorizedHandler = fn;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly problem?: ProblemDetails;

  constructor(status: number, message: string, code?: string, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.problem = problem;
  }
}

type QueryValue = string | number | boolean | undefined | null;

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  query?: Record<string, QueryValue>;
  signal?: AbortSignal;
}

function buildUrl(path: string, query?: Record<string, QueryValue>): URL {
  const base = BASE_URL.endsWith('/') ? BASE_URL.slice(0, -1) : BASE_URL;
  const p = path.startsWith('/') ? path : `/${path}`;
  const url = new URL(`${base}${p}`, window.location.origin);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null) continue;
      url.searchParams.set(key, String(value));
    }
  }
  return url;
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = 'GET', body, query, signal } = options;

  const url = buildUrl(path, query);
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (currentToken) headers.Authorization = `Bearer ${currentToken}`;

  let response: Response;
  try {
    response = await fetch(url.toString(), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    });
  } catch (err) {
    if ((err as DOMException).name === 'AbortError') throw err;
    throw new ApiError(0, 'Réseau indisponible. Vérifiez votre connexion.');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  let parsed: unknown = null;
  const contentType = response.headers.get('Content-Type') ?? '';
  if (contentType.includes('json')) {
    try {
      parsed = await response.json();
    } catch {
      parsed = null;
    }
  }

  if (!response.ok) {
    if (response.status === 401 && unauthorizedHandler) {
      unauthorizedHandler();
    }
    const problem = (parsed ?? undefined) as ProblemDetails | undefined;
    const message = problem?.detail ?? problem?.title ?? `Erreur ${response.status}`;
    throw new ApiError(response.status, message, problem?.code, problem);
  }

  return parsed as T;
}

=== FILE: src/api/endpoints.ts ===
import { apiRequest } from './client';
import type {
  AuthUser,
  CalendarEntry,
  CreateLeaveRequestInput,
  DecisionInput,
  Holiday,
  LeaveBalance,
  LeaveRequest,
  LeaveType,
  LoginResponse,
} from './types';

export const authApi = {
  login(email: string, password: string): Promise<LoginResponse> {
    return apiRequest<LoginResponse>('/auth/login', {
      method: 'POST',
      body: { email, password },
    });
  },
};

export const meApi = {
  getProfile(): Promise<AuthUser> {
    return apiRequest<AuthUser>('/me');
  },
  getBalances(year: number): Promise<LeaveBalance[]> {
    return apiRequest<LeaveBalance[]>('/me/balances', { query: { year } });
  },
  getLeaveRequests(params: { status?: string; from?: string; to?: string } = {}): Promise<LeaveRequest[]> {
    return apiRequest<LeaveRequest[]>('/me/leave-requests', { query: params });
  },
};

export const leaveRequestsApi = {
  create(input: CreateLeaveRequestInput): Promise<LeaveRequest> {
    return apiRequest<LeaveRequest>('/leave-requests', { method: 'POST', body: input });
  },
  get(id: string): Promise<LeaveRequest> {
    return apiRequest<LeaveRequest>(`/leave-requests/${id}`);
  },
  decide(id: string, input: DecisionInput): Promise<LeaveRequest> {
    return apiRequest<LeaveRequest>(`/leave-requests/${id}/decision`, {
      method: 'POST',
      body: input,
    });
  },
};

export const teamsApi = {
  getPendingRequests(teamId: string): Promise<LeaveRequest[]> {
    return apiRequest<LeaveRequest[]>(`/teams/${teamId}/leave-requests`, {
      query: { status: 'PENDING' },
    });
  },
  getCalendar(teamId: string, from: string, to: string): Promise<CalendarEntry[]> {
    return apiRequest<CalendarEntry[]>(`/teams/${teamId}/calendar`, {
      query: { from, to },
    });
  },
};

export const leaveTypesApi = {
  list(): Promise<LeaveType[]> {
    return apiRequest<LeaveType[]>('/leave-types');
  },
};

export const holidaysApi = {
  list(year: number): Promise<Holiday[]> {
    return apiRequest<Holiday[]>('/holidays', { query: { year } });
  },
};

=== FILE: src/auth/AuthContext.tsx ===
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { authApi } from '../api/endpoints';
import { setToken, setUnauthorizedHandler } from '../api/client';
import type { AuthUser } from '../api/types';

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isManager: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);

  const logout = useCallback(() => {
    setUser(null);
    setToken(null);
  }, []);

  useEffect(() => {
    const handleUnauthorized = () => {
      setUser(null);
      setToken(null);
    };
    setUnauthorizedHandler(handleUnauthorized);
    return () => setUnauthorizedHandler(null);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await authApi.login(email, password);
    setToken(res.accessToken);
    setUser(res.user);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isManager: user?.roles.includes('MANAGER') ?? false,
      login,
      logout,
    }),
    [user, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth doit être utilisé à l\'intérieur d\'un AuthProvider.');
  return ctx;
}

=== FILE: src/auth/ProtectedRoute.tsx ===
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

export function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }
  return <Outlet />;
}

=== FILE: src/components/Layout.tsx ===
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { user, isManager, logout } = useAuth();
  const navigate = useNavigate();
  const initials = user
    ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
    : '?';

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">Aller au contenu</a>
      <header className="app-header">
        <div className="app-header-inner">
          <Link to="/" className="brand" aria-label="TeamLeave, retour à l'accueil">
            <span className="brand-mark" aria-hidden="true">TL</span>
            <span>TeamLeave</span>
          </Link>
          <nav aria-label="Navigation principale">
            <ul className="nav-list">
              <li>
                <NavLink to="/requests/new" className="nav-link">
                  Nouvelle demande
                </NavLink>
              </li>
              <li>
                <NavLink to="/calendar" className="nav-link">
                  Calendrier d'équipe
                </NavLink>
              </li>
              {isManager && (
                <li>
                  <NavLink to="/approvals" className="nav-link">
                    Validation
                  </NavLink>
                </li>
              )}
            </ul>
          </nav>
          <div className="user-summary">
            <span className="avatar" aria-hidden="true">{initials}</span>
            <span>{user?.firstName} {user?.lastName}</span>
            <button type="button" className="btn btn-ghost btn-sm" onClick={handleLogout}>
              Se déconnecter
            </button>
          </div>
        </div>
      </header>
      <main id="main-content" className="app-main" tabIndex={-1}>
        <Outlet />
      </main>
    </div>
  );
}

=== FILE: src/components/Spinner.tsx ===
export function Spinner({ label = 'Chargement…' }: { label?: string }) {
  return (
    <div className="loading-state" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}

=== FILE: src/components/Alert.tsx ===
import type { ReactNode } from 'react';

type Tone = 'info' | 'success' | 'warning' | 'error';

const ICONS: Record<Tone, string> = {
  info: 'i',
  success: '✓',
  warning: '!',
  error: '!',
};

export function Alert({
  tone = 'info',
  title,
  children,
}: {
  tone?: Tone;
  title?: string;
  children?: ReactNode;
}) {
  const isUrgent = tone === 'error' || tone === 'warning';
  return (
    <div
      className={`alert alert-${tone}`}
      role={isUrgent ? 'alert' : 'status'}
      aria-live={isUrgent ? 'assertive' : 'polite'}
    >
      <span className="alert-icon" aria-hidden="true">{ICONS[tone]}</span>
      <div>
        {title && <div className="alert-title">{title}</div>}
        {children && <div className="alert-body">{children}</div>}
      </div>
    </div>
  );
}

=== FILE: src/components/EmptyState.tsx ===
import type { ReactNode } from 'react';

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty-state" role="status">
      <h3>{title}</h3>
      {children && <p>{children}</p>}
    </div>
  );
}

=== FILE: src/components/ErrorState.tsx ===
import { Alert } from './Alert';
import { ApiError } from '../api/client';

export function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 0) return error.message;
    if (error.status === 401) return 'Votre session a expiré. Merci de vous reconnecter.';
    if (error.status === 403) return "Vous n'avez pas les droits pour effectuer cette action.";
    if (error.status === 404) return 'La ressource demandée est introuvable.';
    if (error.status === 409) return error.message || 'Conflit : la ressource a été modifiée entre-temps.';
    if (error.status === 422) return error.message || 'La demande ne respecte pas les règles métier.';
    if (error.status >= 500) return 'Le service est momentanément indisponible. Réessayez plus tard.';
    return error.message || 'Une erreur est survenue.';
  }
  if (error instanceof Error) return error.message;
  return 'Une erreur inattendue est survenue.';
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div className="error-state">
      <Alert tone="error" title="Une erreur est survenue">
        {describeError(error)}
      </Alert>
      {onRetry && (
        <div className="row">
          <button type="button" className="btn btn-secondary" onClick={onRetry}>
            Réessayer
          </button>
        </div>
      )}
    </div>
  );
}

=== FILE: src/components/OverlapBadge.tsx ===
import type { OverlapDetail } from '../api/types';
import { formatDate } from '../utils/dates';

export function OverlapBadge({
  hasWarning,
  ratio,
  details,
}: {
  hasWarning: boolean;
  ratio?: number | null;
  details?: OverlapDetail[] | null;
}) {
  if (!hasWarning) return null;
  const percent = ratio != null ? Math.round(ratio * 100) : null;

  return (
    <div className="alert alert-warning" role="alert">
      <span className="alert-icon" aria-hidden="true">!</span>
      <div>
        <div className="alert-title">
          Chevauchement critique{percent != null ? ` — ${percent} %` : ''}
        </div>
        <div className="alert-body">
          {details && details.length > 0 ? (
            <>
              <p style={{ margin: '0 0 0.25rem' }}>
                {details.length} jour{details.length > 1 ? 's' : ''} de cette demande{' '}
                {details.length > 1 ? 'sont' : 'est'} déjà couvert{details.length > 1 ? 's' : ''} par des absences approuvées :
              </p>
              <ul>
                {details.map((d) => (
                  <li key={d.date}>
                    {formatDate(d.date)} — {d.absentEmployees.join(', ')}
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <p style={{ margin: 0 }}>
              Cette demande chevauche plus de 50 % d'absences déjà approuvées sur l'équipe.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

=== FILE: src/components/StatusBadge.tsx ===
import type { LeaveStatus } from '../api/types';

const LABELS: Record<LeaveStatus, string> = {
  PENDING: 'En attente',
  APPROVED: 'Approuvée',
  REJECTED: 'Refusée',
};

const CLASSES: Record<LeaveStatus, string> = {
  PENDING: 'badge-pending',
  APPROVED: 'badge-approved',
  REJECTED: 'badge-rejected',
};

export function StatusBadge({ status }: { status: LeaveStatus }) {
  return <span className={`badge ${CLASSES[status]}`}>{LABELS[status]}</span>;
}

=== FILE: src/utils/dates.ts ===
const FR_DATE = new Intl.DateTimeFormat('fr-FR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
});

const FR_MONTH = new Intl.DateTimeFormat('fr-FR', {
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
});

const FR_DATETIME = new Intl.DateTimeFormat('fr-FR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

export function parseISODate(value: string): Date {
  return new Date(`${value}T00:00:00Z`);
}

export function toISODate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function localTodayISO(): string {
  const d = new Date();
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function formatDate(value: string | Date): string {
  const d = typeof value === 'string' ? parseISODate(value) : value;
  return FR_DATE.format(d);
}

export function formatDateTime(value: string | Date): string {
  const d = typeof value === 'string' ? new Date(value) : value;
  return FR_DATETIME.format(d);
}

export function formatMonthYear(year: number, monthIndex0: number): string {
  return FR_MONTH.format(new Date(Date.UTC(year, monthIndex0, 1)));
}

export function formatRange(startISO: string, endISO: string): string {
  if (startISO === endISO) return formatDate(startISO);
  return `du ${formatDate(startISO)} au ${formatDate(endISO)}`;
}

export function startOfMonth(year: number, monthIndex0: number): Date {
  return new Date(Date.UTC(year, monthIndex0, 1));
}

export function endOfMonth(year: number, monthIndex0: number): Date {
  return new Date(Date.UTC(year, monthIndex0 + 1, 0));
}

export function mondayIndex(date: Date): number {
  // 0 = lundi … 6 = dimanche (convention FR)
  return (date.getUTCDay() + 6) % 7;
}

=== FILE: src/utils/workingDays.ts ===
import { parseISODate, toISODate } from './dates';

/**
 * Aperçu côté client du calcul de jours ouvrés (RB-04).
 * Le backend reste la source de vérité : on n'utilise ce résultat que pour
 * l'affichage dans le formulaire.
 */
export function computeWorkingDays(
  startISO: string,
  endISO: string,
  holidayISO: Set<string> = new Set(),
): number {
  const start = parseISODate(startISO);
  const end = parseISODate(endISO);
  if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) return 0;
  if (start > end) return 0;

  let count = 0;
  const cursor = new Date(start);
  while (cursor <= end) {
    const dow = cursor.getUTCDay();
    const iso = toISODate(cursor);
    if (dow !== 0 && dow !== 6 && !holidayISO.has(iso)) {
      count += 1;
    }
    cursor.setUTCDate(cursor.getUTCDate() + 1);
  }
  return count;
}

=== FILE: src/pages/LoginPage.tsx
import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { Alert } from '../components/Alert';

export function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? '/requests/new';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!email.trim() || !password) {
      setError('Merci de saisir votre email et votre mot de passe.');
      return;
    }

    setSubmitting(true);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError('Email ou mot de passe incorrect.');
      } else if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Connexion impossible. Vérifiez votre connexion réseau.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="login-shell">
      <div className="login-card">
        <h1>TeamLeave</h1>
        <p className="login-subtitle">Connectez-vous pour gérer vos congés.</p>
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="email">Email professionnel</label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="username"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              aria-describedby={error ? 'login-error' : undefined}
            />
          </div>
          <div className="field">
            <label htmlFor="password">Mot de passe</label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              aria-describedby={error ? 'login-error' : undefined}
            />
          </div>
          {error && (
            <div id="login-error">
              <Alert tone="error" title="Connexion impossible">{error}</Alert>
            </div>
          )}
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Connexion…' : 'Se connecter'}
          </button>
        </form>
      </div>
    </div>
  );
}

=== FILE: src/pages/SubmitLeaveRequestPage.tsx
import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { ApiError } from '../api/client';
import { holidaysApi, leaveRequestsApi, leaveTypesApi, meApi } from '../api/endpoints';
import type { Holiday, LeaveBalance, LeaveRequest, LeaveType } from '../api/types';
import { Alert } from '../components/Alert';
import { EmptyState } from '../components/EmptyState';
import { ErrorState, describeError } from '../components/ErrorState';
import { OverlapBadge } from '../components/OverlapBadge';
import { Spinner } from '../components/Spinner';
import { StatusBadge } from '../components/StatusBadge';
import { formatDateTime, formatRange } from '../utils/dates';
import { computeWorkingDays } from '../utils/workingDays';

export function SubmitLeaveRequestPage() {
  const currentYear = new Date().getFullYear();

  const [loading, setLoading] = useState(true);
  const [initialLoadDone, setInitialLoadDone] = useState(false);
  const [loadError, setLoadError] = useState<unknown>(null);
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [types, setTypes] = useState<LeaveType[]>([]);
  const [holidays, setHolidays] = useState<Holiday[]>([]);
  const [myRequests, setMyRequests] = useState<LeaveRequest[]>([]);
  const [reloadKey, setReloadKey] = useState(0);

  const [leaveTypeId, setLeaveTypeId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [lastCreated, setLastCreated] = useState<LeaveRequest | null>(null);

  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  // Année ciblée = année de la date de début (RB-05), sinon année courante.
  const targetYear = startDate ? Number(startDate.slice(0, 4)) : currentYear;

  useEffect(() => {
    let active = true;
    setLoading(true);
    setLoadError(null);

    Promise.all([
      meApi.getBalances(targetYear),
      leaveTypesApi.list(),
      holidaysApi.list(targetYear),
      meApi.getLeaveRequests({}),
    ])
      .then(([balancesData, typesData, holidaysData, requestsData]) => {
        if (!active) return;
        setBalances(balancesData);
        setTypes(typesData.filter((t) => t.isActive));
        setHolidays(holidaysData.filter((h) => h.isActive));
        setMyRequests(requestsData);
        setLoading(false);
        setInitialLoadDone(true);
      })
      .catch((err) => {
        if (!active) return;
        setLoadError(err);
        setLoading(false);
        setInitialLoadDone(true);
      });

    return () => {
      active = false;
    };
  }, [targetYear, reloadKey]);

  // Sélection par défaut du premier type actif.
  useEffect(() => {
    if (!leaveTypeId && types.length > 0) {
      setLeaveTypeId(types[0].id);
    }
  }, [types, leaveTypeId]);

  const holidaySet = useMemo(() => new Set(holidays.map((h) => h.date)), [holidays]);

  const workingDays = useMemo(() => {
    if (!startDate || !endDate) return 0;
    return computeWorkingDays(startDate, endDate, holidaySet);
  }, [startDate, endDate, holidaySet]);

  const selectedBalance = balances.find((b) => b.leaveTypeId === leaveTypeId) ?? null;

  const dateError = (() => {
    if (startDate && endDate && startDate > endDate) {
      return 'La date de début doit être antérieure ou égale à la date de fin.';
    }
    if (startDate && endDate && startDate.slice(0, 4) !== endDate.slice(0, 4)) {
      return 'Une demande ne peut pas couvrir deux années civiles.';
    }
    return null;
  })();

  const balanceError = (() => {
    if (!selectedBalance || !workingDays) return null;
    if (workingDays > selectedBalance.availableDays) {
      return `La durée (${workingDays} j) dépasse le solde disponible (${selectedBalance.availableDays} j).`;
    }
    return null;
  })();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);
    setLastCreated(null);

    if (!leaveTypeId) {
      setFormError('Sélectionnez un type de congé.');
      return;
    }
    if (!startDate || !endDate) {
      setFormError('Renseignez les dates de début et de fin.');
      return;
    }
    if (dateError) {
      setFormError(dateError);
      return;
    }
    if (balanceError) {
      setFormError(balanceError);
      return;
    }

    setSubmitting(true);
    try {
      const created = await leaveRequestsApi.create({
        leaveTypeId,
        startDate,
        endDate,
        comment: comment.trim() || undefined,
      });
      setLastCreated(created);
      setStartDate('');
      setEndDate('');
      setComment('');
      setMyRequests((prev) => [created, ...prev]);
      // Rafraîchit les soldes ciblés (nouvelle année éventuelle)
      meApi
        .getBalances(targetYear)
        .then((b) => setBalances(b))
        .catch(() => {
          /* silencieux : la liste est déjà à jour */
        });
    } catch (err) {
      if (err instanceof ApiError) {
        const fieldErrors = err.problem?.errors;
        if (fieldErrors && Object.keys(fieldErrors).length > 0) {
          const joined = Object.entries(fieldErrors)
            .map(([field, messages]) => `${field} : ${messages.join(' ')}`)
            .join(' · ');
          setFormError(`${describeError(err)} (${joined})`);
        } else {
          setFormError(describeError(err));
        }
      } else {
        setFormError(describeError(err));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (!initialLoadDone) {
    return <Spinner label="Chargement de vos données…" />;
  }
  if (loadError && balances.length === 0) {
    return <ErrorState error={loadError} onRetry={reload} />;
  }

  return (
    <div className="stack-lg">
      <div>
        <h1>Nouvelle demande de congé</h1>
        <p className="muted">
          Les jours ouvrés sont calculés automatiquement (hors week-ends et jours fériés).
        </p>
      </div>

      {loadError && <ErrorState error={loadError} onRetry={reload} />}

      {lastCreated && (
        <Alert tone="success" title="Demande soumise">
          Votre demande {formatRange(lastCreated.startDate, lastCreated.endDate)} est en attente de validation.
          {lastCreated.hasOverlapWarning && (
            <div style={{ marginTop: '0.5rem' }}>
              <OverlapBadge
                hasWarning={lastCreated.hasOverlapWarning}
                ratio={lastCreated.overlapRatio}
                details={lastCreated.overlapDetails}
              />
            </div>
          )}
        </Alert>
      )}

      <section className="card" aria-labelledby="balances-title">
        <h2 id="balances-title">Mes soldes {targetYear}</h2>
        {balances.length === 0 ? (
          <EmptyState title="Aucun solde configuré">
            Contactez votre administration RH pour l'ouverture de vos droits.
          </EmptyState>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <caption className="hidden-visually">
                Soldes de congés par type pour l'année {targetYear}
              </caption>
              <thead>
                <tr>
                  <th scope="col">Type</th>
                  <th scope="col">Acquis</th>
                  <th scope="col">Ajustements</th>
                  <th scope="col">Utilisés</th>
                  <th scope="col">Disponible</th>
                </tr>
              </thead>
              <tbody>
                {balances.map((b) => (
                  <tr key={b.leaveTypeId}>
                    <th scope="row">
                      {b.leaveTypeLabel}{' '}
                      <span className="muted text-xs">({b.leaveTypeCode})</span>
                    </th>
                    <td>{b.acquiredDays}</td>
                    <td>{b.adjustmentDays}</td>
                    <td>{b.usedDays}</td>
                    <td><strong>{b.availableDays}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="card" aria-labelledby="form-title">
        <h2 id="form-title">Soumettre une demande</h2>
        <form className="form" onSubmit={handleSubmit} noValidate>
          <div className="field">
            <label htmlFor="leaveTypeId">Type de congé</label>
            <select
              id="leaveTypeId"
              required
              value={leaveTypeId}
              onChange={(e) => setLeaveTypeId(e.target.value)}
            >
              <option value="" disabled>Choisir un type…</option>
              {types.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.label} ({t.code})
                </option>
              ))}
            </select>
            {selectedBalance && (
              <span className="hint">
                Solde disponible : {selectedBalance.availableDays} jour
                {selectedBalance.availableDays > 1 ? 's' : ''}
              </span>
            )}
          </div>

          <div className="form-row cols-2">
            <div className="field">
              <label htmlFor="startDate">Date de début</label>
              <input
                id="startDate"
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                aria-invalid={dateError ? 'true' : undefined}
              />
            </div>
            <div className="field">
              <label htmlFor="endDate">Date de fin</label>
              <input
                id="endDate"
                type="date"
                required
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                aria-invalid={dateError ? 'true' : undefined}
              />
            </div>
          </div>

          {dateError && (
            <Alert tone="error" title="Dates invalides">{dateError}</Alert>
          )}

          {startDate && endDate && !dateError && (
            <Alert
              tone="info"
              title={`${workingDays} jour${workingDays > 1 ? 's' : ''} ouvré${workingDays > 1 ? 's' : ''}`}
            >
              {workingDays === 0
                ? 'Aucun jour ouvré dans cette période.'
                : `Période ${formatRange(startDate, endDate)} (hors week-ends et jours fériés).`}
            </Alert>
          )}

          {balanceError && (
            <Alert tone="error" title="Solde insuffisant">{balanceError}</Alert>
          )}

          <div className="field">
            <label htmlFor="comment">Commentaire (facultatif)</label>
            <textarea
              id="comment"
              rows={3}
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Ex. vacances d'été en famille"
            />
          </div>

          {formError && (
            <Alert tone="error" title="Impossible de soumettre la demande">
              {formError}
            </Alert>
          )}

          <div className="row">
            <button
              type="submit"
              className="btn btn-primary"
              disabled={submitting || Boolean(dateError) || Boolean(balanceError)}
            >
              {submitting ? 'Envoi…' : 'Soumettre la demande'}
            </button>
          </div>
        </form>
      </section>

      <section className="card" aria-labelledby="my-requests-title">
        <h2 id="my-requests-title">Mes demandes</h2>
        {myRequests.length === 0 ? (
          <EmptyState title="Aucune demande pour le moment">
            Vos demandes apparaîtront ici dès leur soumission.
          </EmptyState>
        ) : (
          <div className="table-wrap">
            <table className="data-table">
              <caption className="hidden-visually">
                Historique de mes demandes de congé
              </caption>
              <thead>
                <tr>
                  <th scope="col">Période</th>
                  <th scope="col">Type</th>
                  <th scope="col">Jours</th>
                  <th scope="col">Statut</th>
                  <th scope="col">Soumise le</th>
                </tr>
              </thead>
              <tbody>
                {myRequests.map((r) => (
                  <tr key={r.id}>
                    <th scope="row">{formatRange(r.startDate, r.endDate)}</th>
                    <td>{r.leaveType.label}</td>
                    <td>{r.workingDays}</td>
                    <td><StatusBadge status={r.status} /></td>
                    <td>{formatDateTime(r.submittedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}

=== FILE: src/pages/TeamCalendarPage.tsx ===
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { teamsApi } from '../api/endpoints';
import type { CalendarEntry } from '../api/types';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { Spinner } from '../components/Spinner';
import {
  endOfMonth,
  formatMonthYear,
  formatRange,
  localTodayISO,
  mondayIndex,
  parseISODate,
  startOfMonth,
  toISODate,
} from '../utils/dates';

const WEEKDAYS = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'];

interface Cell {
  date: Date;
  iso: string;
  inMonth: boolean;
}

export function TeamCalendarPage() {
  const { user } = useAuth();
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [monthIndex, setMonthIndex] = useState(now.getMonth());
  const [entries, setEntries] = useState<CalendarEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const teamId = user?.teamId ?? null;
  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    if (!teamId) {
      setLoading(false);
      return;
    }
    let active = true;
    setLoading(true);
    setError(null);
    teamsApi
      .getCalendar(teamId, toISODate(startOfMonth(year, monthIndex)), toISODate(endOfMonth(year, monthIndex)))
      .then((data) => {
        if (!active) return;
        setEntries(data);
        setLoading(false);
      })
      .catch((err) => {
        if (!active) return;
        setError(err);
        setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [teamId, year, monthIndex, reloadKey]);

  const cells = useMemo<Cell[]>(() => {
    const first = startOfMonth(year, monthIndex);
    const offset = mondayIndex(first);
    const totalDays = endOfMonth(year, monthIndex).getUTCDate();
    const result: Cell[] = [];

    for (let i = 0; i < offset; i += 1) {
      const d = new Date(Date.UTC(year, monthIndex, 1 - (offset - i)));
      result.push({ date: d, iso: toISODate(d), inMonth: false });
    }
    for (let d = 1; d <= totalDays; d += 1) {
      const date = new Date(Date.UTC(year, monthIndex, d));
      result.push({ date, iso: toISODate(date), inMonth: true });
    }
    while (result.length % 7 !== 0) {
      const last = result[result.length - 1].date;
      const next = new Date(last);
      next.setUTCDate(next.getUTCDate() + 1);
      result.push({ date: next, iso: toISODate(next), inMonth: false });
    }
    return result;
  }, [year, monthIndex]);

  const entriesByDay = useMemo(() => {
    const map = new Map<string, CalendarEntry[]>();
    for (const entry of entries) {
      const start = parseISODate(entry.startDate);
      const end = parseISODate(entry.endDate);
      const cursor = new Date(start);
      while (cursor <= end) {
        const iso = toISODate(cursor);
        const list = map.get(iso) ?? [];
        list.push(entry);
        map.set(iso, list);
        cursor.setUTCDate(cursor.getUTCDate() + 1);
      }
    }
    return map;
  }, [entries]);

  function goPrevMonth() {
    if (monthIndex === 0) {
      setMonthIndex(11);
      setYear((y) => y - 1);
    } else {
      setMonthIndex((m) => m - 1);
    }
  }
  function goNextMonth() {
    if (monthIndex === 11) {
      setMonthIndex(0);
      setYear((y) => y + 1);
    } else {
      setMonthIndex((m) => m + 1);
    }
  }
  function goToday() {
    const d = new Date();
    setYear(d.getFullYear());
    setMonthIndex(d.getMonth());
  }

  if (!teamId) {
    return (
      <>
        <h1>Calendrier d'équipe</h1>
        <EmptyState title="Aucune équipe rattachée">
          Votre profil n'est rattaché à aucune équipe. Contactez votre administrateur RH.
        </EmptyState>
      </>
    );
  }

  const todayISO = localTodayISO();
  const monthLabel = formatMonthYear(year, monthIndex);

  return (
    <div className="stack-lg">
      <div>
        <h1>Calendrier d'équipe</h1>
        <p className="muted">Absences approuvées de votre équipe.</p>
      </div>

      <section className="card" aria-labelledby="calendar-title">
        <h2 id="calendar-title" className="hidden-visually">Calendrier des absences</h2>

        <div className="calendar-toolbar">
          <div className="row">
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={goPrevMonth}
              aria-label="Mois précédent"
            >
              ‹
            </button>
            <span className="calendar-month-label" aria-live="polite">{monthLabel}</span>
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={goNextMonth}
              aria-label="Mois suivant"
            >
              ›
            </button>
          </div>
          <button type="button" className="btn btn-ghost btn-sm" onClick={goToday}>
            Aujourd'hui
          </button>
        </div>

        {loading && <Spinner label="Chargement du calendrier…" />}
        {error && <ErrorState error={error} onRetry={reload} />}

        {!loading && !error && (
          <>
            <div className="calendar-grid" role="grid" aria-label={`Calendrier de ${monthLabel}`}>
              {WEEKDAYS.map((w) => (
                <div key={w} className="calendar-weekday" role="columnheader">{w}</div>
              ))}
              {cells.map((cell) => {
                const dayEntries = entriesByDay.get(cell.iso) ?? [];
                const classes = ['calendar-cell'];
                if (!cell.inMonth) classes.push('outside');
                if (cell.iso === todayISO) classes.push('today');

                return (
                  <div
                    key={cell.iso}
                    className={classes.join(' ')}
                    role="gridcell"
                    aria-label={`${cell.date.getUTCDate()} — ${dayEntries.length} absence(s)`}
                  >
                    <span className="calendar-day-number" aria-hidden="true">
                      {cell.date.getUTCDate()}
                    </span>
                    {dayEntries.slice(0, 2).map((entry) => {
                      const initials = `${entry.employeeFirstName.charAt(0)}${entry.employeeLastName.charAt(0)}`;
                      const full = `${entry.employeeFirstName} ${entry.employeeLastName} — ${entry.leaveTypeLabel}`;
                      return (
                        <span key={entry.leaveRequestId} className="calendar-chip" title={full}>
                          <span aria-hidden="true">{initials}</span>
                          <span className="hidden-visually">{full}</span>
                        </span>
                      );
                    })}
                    {dayEntries.length > 2 && (
                      <span className="calendar-chip more" aria-label={`${dayEntries.length - 2} absences supplémentaires`}>
                        +{dayEntries.length - 2}
                      </span>
                    )}
                  </div>
                );
              })}
            </div>

            <div className="stack" style={{ marginTop: '1.5rem' }}>
              <h3>Absences du mois</h3>
              {entries.length === 0 ? (
                <EmptyState title="Aucune absence approuvée sur la période">
                  Le calendrier affiche uniquement les absences approuvées.
                </EmptyState>
              ) : (
                <ul style={{ listStyle: 'none', padding: 0, margin: 0 }} className="stack">
                  {entries.map((entry) => (
                    <li
                      key={entry.leaveRequestId}
                      className="card"
                      style={{ padding: '0.75rem 1rem', margin: 0 }}
                    >
                      <div className="row">
                        <strong>
                          {entry.employeeFirstName} {entry.employeeLastName}
                        </strong>
                        <span className="muted" aria-hidden="true">—</span>
                        <span>{entry.leaveTypeLabel}</span>
                        <span className="spacer" />
                        <span className="text-sm muted">
                          {formatRange(entry.startDate, entry.endDate)}
                        </span>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        )}
      </section>
    </div>
  );
}

=== FILE: src/pages/ApprovalQueuePage.tsx ===
import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { useAuth } from '../auth/AuthContext';
import { ApiError } from '../api/client';
import { leaveRequestsApi, teamsApi } from '../api/endpoints';
import type { LeaveRequest } from '../api/types';
import { Alert } from '../components/Alert';
import { EmptyState } from '../components/EmptyState';
import { ErrorState, describeError } from '../components/ErrorState';
import { OverlapBadge } from '../components/OverlapBadge';
import { Spinner } from '../components/Spinner';
import { formatDateTime, formatRange } from '../utils/dates';

type Mode = 'approve' | 'reject' | null;

export function ApprovalQueuePage() {
  const { user, isManager } = useAuth();
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const [activeRequestId, setActiveRequestId] = useState<string | null>(null);
  const [mode, setMode] = useState<Mode>(null);
  const [rejectComment, setRejectComment] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const teamId = user?.teamId ?? null;
  const reload = useCallback(() => setReloadKey((k) => k + 1), []);

  useEffect(() => {
    if (!isManager || !teamId) {
      setLoading(false);
      return;
    }
    let active = true;
    setLoading(true);
    setError(null);
    teamsApi
      .getPendingRequests(teamId)
      .then((data) => {
        if (!active) return;
        setRequests(data);
        setLoading(false);
      })
      .catch((err) => {
        if (!active) return;
        setError(err);
        setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [isManager, teamId, reloadKey]);

  function openAction(id: string, actionMode: 'approve' | 'reject') {
    setActiveRequestId(id);
    setMode(actionMode);
    setRejectComment('');
    setAcknowledged(false);
    setActionError(null);
    setSuccessMessage(null);
  }

  function cancelAction() {
    setActiveRequestId(null);
    setMode(null);
    setRejectComment('');
    setAcknowledged(false);
    setActionError(null);
  }

  async function submitDecision(request: LeaveRequest, action: 'APPROVE' | 'REJECT') {
    setSubmitting(true);
    setActionError(null);
    setSuccessMessage(null);

    const trimmedComment = rejectComment.trim();
    if (action === 'REJECT' && !trimmedComment) {
      setActionError('Un commentaire est obligatoire pour refuser une demande.');
      setSubmitting(false);
      return;
    }

    try {
      await leaveRequestsApi.decide(request.id, {
        action,
        comment: action === 'REJECT' ? trimmedComment : undefined,
        acknowledgeOverlapWarning: request.hasOverlapWarning ? acknowledged : false,
      });

      const employee = `${request.employeeFirstName ?? ''} ${request.employeeLastName ?? ''}`.trim();
      setRequests((prev) => prev.filter((r) => r.id !== request.id));
      setSuccessMessage(
        action === 'APPROVE'
          ? `La demande de ${employee} a été approuvée.`
          : `La demande de ${employee} a été refusée.`,
      );
      cancelAction();
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        // L'état a changé côté serveur : on recharge pour resynchroniser.
        reload();
      }
      setActionError(describeError(err));
    } finally {
      setSubmitting(false);
    }
  }

  function handleRejectSubmit(event: FormEvent<HTMLFormElement>, request: LeaveRequest) {
    event.preventDefault();
    void submitDecision(request, 'REJECT');
  }

  if (!isManager) {
    return (
      <>
        <h1>Validation des demandes</h1>
        <EmptyState title="Accès réservé aux managers">
          Cette page est accessible uniquement aux managers d'équipe.
        </EmptyState>
      </>
    );
  }

  if (!teamId) {
    return (
      <>
        <h1>Validation des demandes</h1>
        <EmptyState title="Aucune équipe managée">
          Votre profil n'est rattaché à aucune équipe. Contactez votre administrateur RH.
        </EmptyState>
      </>
    );
  }

  return (
    <div className="stack-lg">
      <div>
        <h1>Validation des demandes</h1>
        <p className="muted">Demandes en attente de votre décision pour votre équipe.</p>
      </div>

      {successMessage && (
        <Alert tone="success" title="Décision enregistrée">{successMessage}</Alert>
      )}

      {loading && <Spinner label="Chargement des demandes…" />}
      {error && <ErrorState error={error} onRetry={reload} />}

      {!loading && !error && requests.length === 0 && (
        <EmptyState title="Aucune demande en attente">
          Vous n'avez aucune demande à traiter pour le moment.
        </EmptyState>
      )}

      {!loading && !error && requests.length > 0 && (
        <div className="stack">
          {requests.map((req) => {
            const isActive = activeRequestId === req.id;
            const isMe = user?.id === req.employeeId;
            const employeeName = `${req.employeeFirstName ?? ''} ${req.employeeLastName ?? ''}`.trim();

            return (
              <article
                key={req.id}
                className="card request-card"
                aria-labelledby={`req-${req.id}-title`}
              >
                <header className="request-card-header">
                  <h2 id={`req-${req.id}-title`} style={{ margin: 0 }}>
                    {employeeName}
                  </h2>
                  <span className="badge badge-pending">En attente</span>
                </header>

                <div className="request-meta">
                  <div>
                    <strong>{req.leaveType.label}</strong> ({req.leaveType.code})
                  </div>
                  <div>
                    {formatRange(req.startDate, req.endDate)} —{' '}
                    <strong>{req.workingDays}</strong> jour{req.workingDays > 1 ? 's' : ''} ouvré
                    {req.workingDays > 1 ? 's' : ''}
                  </div>
                  <div className="text-sm muted">
                    Soumise le {formatDateTime(req.submittedAt)}
                  </div>
                  {req.employeeComment && (
                    <div className="text-sm">
                      <span className="section-label">Commentaire employé</span>
                      <p style={{ margin: 0 }}>{req.employeeComment}</p>
                    </div>
                  )}
                </div>

                <OverlapBadge
                  hasWarning={req.hasOverlapWarning}
                  ratio={req.overlapRatio}
                  details={req.overlapDetails}
                />

                {isMe && (
                  <Alert tone="warning" title="Auto-validation interdite">
                    Vous ne pouvez pas décider de votre propre demande.
                  </Alert>
                )}

                {!isActive && !isMe && (
                  <div className="request-actions">
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={() => openAction(req.id, 'approve')}
                    >
                      Approuver
                    </button>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => openAction(req.id, 'reject')}
                    >
                      Refuser
                    </button>
                  </div>
                )}

                {isActive && mode === 'approve' && (
                  <div className="stack">
                    {req.hasOverlapWarning && (
                      <div className="checkbox-row">
                        <input
                          id={`ack-${req.id}`}
                          type="checkbox"
                          checked={acknowledged}
                          onChange={(e) => setAcknowledged(e.target.checked)}
                        />
                        <label htmlFor={`ack-${req.id}`}>
                          Je prends connaissance du chevauchement critique et je valide
                          malgré tout.
                        </label>
                      </div>
                    )}
                    {actionError && (
                      <Alert tone="error" title="Échec de l'approbation">{actionError}</Alert>
                    )}
                    <div className="request-actions">
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={submitting || (req.hasOverlapWarning && !acknowledged)}
                        onClick={() => void submitDecision(req, 'APPROVE')}
                      >
                        {submitting ? 'Envoi…' : "Confirmer l'approbation"}
                      </button>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={cancelAction}
                        disabled={submitting}
                      >
                        Annuler
                      </button>
                    </div>
                  </div>
                )}

                {isActive && mode === 'reject' && (
                  <form className="reject-form" onSubmit={(e) => handleRejectSubmit(e, req)}>
                    <div className="field">
                      <label htmlFor={`reject-${req.id}`}>
                        Motif du refus <span aria-hidden="true">*</span>
                      </label>
                      <textarea
                        id={`reject-${req.id}`}
                        required
                        rows={3}
                        value={rejectComment}
                        onChange={(e) => setRejectComment(e.target.value)}
                        aria-describedby={`reject-${req.id}-hint`}
                        autoFocus
                      />
                      <span id={`reject-${req.id}-hint`} className="hint">
                        Ce commentaire sera transmis à l'employé.
                      </span>
                    </div>
                    {actionError && (
                      <Alert tone="error" title="Échec du refus">{actionError}</Alert>
                    )}
                    <div className="request-actions">
                      <button
                        type="submit"
                        className="btn btn-danger"
                        disabled={submitting || !rejectComment.trim()}
                      >
                        {submitting ? 'Envoi…' : 'Confirmer le refus'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={cancelAction}
                        disabled={submitting}
                      >
                        Annuler
                      </button>
                    </div>
                  </form>
                )}
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}

=== FILE: src/pages/NotFoundPage.tsx ===
import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <div className="error-state">
      <h1>Page introuvable</h1>
      <p className="muted">Cette page n'existe pas ou a été déplacée.</p>
      <div className="row">
        <Link to="/" className="btn btn-primary">Retour à l'accueil</Link>
      </div>
    </div>
  );
}

---

## NOTES DE RUN

### Installation et démarrage

```bash
# 1. Installer les dépendances
npm install

# 2. (optionnel) Configurer l'environnement
cp .env.example .env
# → adapter VITE_API_PROXY_TARGET si le backend n'écoute pas sur http://localhost:5000

# 3. Démarrer en développement
npm run dev
# → http://localhost:5173

# 4. Build de production (doit passer sans erreur)
npm run build
# → dist/ prêt à être servi
npm run preview
```

### Variables d'environnement

| Variable | Défaut | Rôle |
|---|---|---|
| `VITE_API_BASE_URL` | `/api/v1` | URL de base de l'API consommée par le navigateur. En prod, mettre l'URL absolue si l'API est sur un autre hostname (ex. `https://api.teamleave.example/api/v1`). |
| `VITE_API_PROXY_TARGET` | `http://localhost:5000` | Cible du proxy Vite **en développement** uniquement. Le frontend appelle `/api/v1/...`, le proxy forwarde vers cette URL. |

### Points d'attention importants

- **JWT en mémoire uniquement.** Conformément à l'ADR d'architecture, le token n'est ni dans `localStorage` ni dans `sessionStorage`. Conséquence : **un rafraîchissement de page (F5) entraîne une reconnexion**. C'est le compromis sécurité/UX retenu côté architecture ; si le produit choisit `sessionStorage`, c'est un changement localisé dans `src/api/client.ts` (fonction `setToken`).
- **Équipe du manager.** La page de validation et le calendrier utilisent `user.teamId` retourné par `POST /api/v1/auth/login`. Un manager gérant plusieurs équipes devra soit changer d'équipe côté identité, soit une évolution future exposera un sélecteur (endpoint `/me/teams` absent du contrat étape 2). Un admin RH sans équipe voit un empty state explicite.
- **Auto-validation.** L'UI désactive les boutons de décision si `user.id === request.employeeId`, et le backend applique le 403 (RB-13). Double barrière.
- **Chevauchement critique.** Le manager doit cocher une case d'acquittement pour approuver une demande dont `hasOverlapWarning = true`. Le bouton "Confirmer" reste désactivé sinon. En cas de recalcul serveur → `409 OVERLAP_WARNING_NOT_ACKNOWLEDGED` → la liste est resynchronisée automatiquement.
- **Aperçu jours ouvrés.** Calculé côté client dans `utils/workingDays.ts` pour l'UX, mais **la source de vérité reste le backend** : le formulaire ne désactive le bouton `Soumettre` sur la base de l'aperçu que pour éviter des requêtes inutiles ; toute incohérence est rattrapée par le `400/422` renvoyé par l'API.
- **Strict mode.** `React.StrictMode` activé : en développement les effets sont exécutés deux fois. Les `useEffect` de fetch utilisent une garde `active` pour éviter les doubles `setState` en cas d'unmount rapide.
- **Accessibilité.**
  - Skip-link, focus visibles (`:focus-visible`, outline bleu 3 px), contrastes AA vérifiés (texte `#1f2937` sur blanc = 16:1 ; boutons primaires blanc sur bleu `#1d4ed8` = 5.3:1 ; alertes warning `#78350f` sur `#fef3c7` ≈ 9:1).
  - L'avertissement de chevauchement n'est pas signalé uniquement par la couleur : préfixe `!`, titre explicite + liste des jours concernés.
  - `role="alert"`/`aria-live` pour les erreurs, `role="status"` pour les succès et empty states.
  - `<caption>` sur tous les tableaux, `<th scope>` correct.
  - `prefers-reduced-motion` respecté.
- **Pas de dépendance de test d'UI** dans ce livrable (Vitest/RTL). Le build `tsc --noEmit && vite build` est le critère de succès demandé. La stratégie de test UI est décrite à l'étape 2 (§8) et s'ajoutera au pipeline CI de la même manière que côté backend.
- **Types stricts.** `strict: true`, `isolatedModules: true`, `jsx: react-jsx`. `noUnusedLocals`/`noUnusedParameters` volontairement à `false` pour éviter des frictions de build sur des props de debug en cours de vie du projet ; à réactiver par la CI quand l'équipe le souhaite.

### Conformité à l'API étape 2

Tous les endpoints consommés correspondent au contrat `/api/v1` :
- `POST /auth/login`
- `GET /me`, `GET /me/balances`, `GET /me/leave-requests`
- `POST /leave-requests`, `GET /leave-requests/{id}`, `POST /leave-requests/{id}/decision`
- `GET /teams/{teamId}/leave-requests?status=PENDING`
- `GET /teams/{teamId}/calendar?from=&to=`
- `GET /leave-types`
- `GET /holidays?year=`

Le format d'erreur `ProblemDetails` (`{ code, detail, errors }`) est exploité : `code` (ex. `INSUFFICIENT_BALANCE`) est exposé au travers de `ApiError.code`, et `errors` est agrégé dans le message d'erreur du formulaire. Les codes `400/401/403/404/409/422` sont gérés avec un message utilisateur explicite. Un `401` déclenche la déconnexion automatique via `setUnauthorizedHandler`.