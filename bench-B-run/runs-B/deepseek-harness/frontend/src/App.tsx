import { useEffect } from 'react';
import {
  Navigate,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom';
import { AppShell } from './components/AppShell';
import { ManagerOnly, RequireAuth } from './auth/RequireAuth';
import { LoginPage } from './pages/LoginPage';
import { SubmitLeavePage } from './pages/SubmitLeavePage';
import { CalendarPage } from './pages/CalendarPage';
import { ManagerQueuePage } from './pages/ManagerQueuePage';
import { NotFoundPage } from './pages/NotFoundPage';

const ROUTE_LABELS: Record<string, string> = {
  '/demandes/nouvelle': 'Nouvelle demande de congé',
  '/calendrier': 'Calendrier d’équipe',
  '/validation': 'File de validation manager',
  '/connexion': 'Connexion',
};

function RouteAnnouncer() {
  const location = useLocation();
  const label = ROUTE_LABELS[location.pathname] ?? 'Page introuvable';

  return (
    <div className="sr-only" aria-live="polite" aria-atomic="true">
      {label}
    </div>
  );
}

function FocusManager() {
  const location = useLocation();

  useEffect(() => {
    const main = document.getElementById('main-content');
    if (main) {
      main.focus({ preventScroll: true });
    }
  }, [location.pathname]);

  return null;
}

export default function App() {
  return (
    <>
      <a className="skip-link" href="#main-content">
        Aller au contenu principal
      </a>
      <RouteAnnouncer />
      <FocusManager />
      <Routes>
        <Route path="/connexion" element={<LoginPage />} />

        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route index element={<Navigate to="/demandes/nouvelle" replace />} />
            <Route path="/demandes/nouvelle" element={<SubmitLeavePage />} />
            <Route path="/calendrier" element={<CalendarPage />} />
            <Route
              path="/validation"
              element={
                <ManagerOnly>
                  <ManagerQueuePage />
                </ManagerOnly>
              }
            />
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </>
  );
}
