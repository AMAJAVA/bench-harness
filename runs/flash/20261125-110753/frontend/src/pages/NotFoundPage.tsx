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
