import { Navigate, Route, Routes } from 'react-router';
import { Layout } from './components/Layout';
import { RequireAuth, RequireDecider } from './components/RouteGuards';
import { ApprovalQueuePage } from './pages/ApprovalQueuePage';
import { CalendarPage } from './pages/CalendarPage';
import { ChangePasswordPage } from './pages/ChangePasswordPage';
import { LoginPage } from './pages/LoginPage';
import { NewRequestPage } from './pages/NewRequestPage';
import { NotFoundPage } from './pages/NotFoundPage';

export function App() {
  return (
    <Routes>
      <Route path="/connexion" element={<LoginPage />} />
      <Route path="/mot-de-passe" element={<ChangePasswordPage />} />
      <Route
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/demandes/nouvelle" replace />} />
        <Route path="/demandes/nouvelle" element={<NewRequestPage />} />
        <Route path="/calendrier" element={<CalendarPage />} />
        <Route
          path="/validation"
          element={
            <RequireDecider>
              <ApprovalQueuePage />
            </RequireDecider>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
