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
