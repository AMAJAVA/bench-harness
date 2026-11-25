import { Link } from 'react-router-dom';
import { usePageTitle } from '../hooks/usePageTitle';
import { CalendarIcon } from '../components/icons';

export function NotFoundPage() {
  usePageTitle('Page introuvable');

  return (
    <main id="main-content" className="standalone-main" tabIndex={-1}>
      <div className="standalone-card">
        <span className="standalone-icon" aria-hidden="true">
          <CalendarIcon size={28} />
        </span>
        <p className="eyebrow">Erreur 404</p>
        <h1 className="standalone-title">Cette page n’existe pas</h1>
        <p className="standalone-text">
          Le lien est peut-être incomplet ou la page a été déplacée. Revenez à
          votre espace pour poursuivre.
        </p>
        <Link to="/demandes/nouvelle" className="btn btn-primary">
          Revenir à mon espace
        </Link>
      </div>
    </main>
  );
}
