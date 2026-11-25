import { Link } from 'react-router';
import { usePageHeading } from '../lib/usePageHeading';

export function NotFoundPage() {
  const headingRef = usePageHeading('Page introuvable');
  return (
    <div className="page">
      <h1 ref={headingRef} tabIndex={-1}>
        Page introuvable
      </h1>
      <p>Cette adresse ne correspond à aucun écran de TeamLeave.</p>
      <p>
        <Link to="/demandes/nouvelle">Revenir à la nouvelle demande</Link>
      </p>
    </div>
  );
}
