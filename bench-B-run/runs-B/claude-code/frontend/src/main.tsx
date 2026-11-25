import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router';
import { ApiError, NETWORK_ERROR } from './api/client';
import { AuthProvider } from './auth/AuthContext';
import { App } from './App';
import './styles/app.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Une seule relance, et seulement pour les pannes transitoires (réseau, 5xx) : une erreur métier ne change pas en réessayant.
      retry: (failureCount, error) =>
        failureCount < 1 && error instanceof ApiError && (error.code === NETWORK_ERROR || error.status >= 500),
    },
    mutations: { retry: false },
  },
});

const root = document.getElementById('root');
if (!root) {
  throw new Error('Élément #root introuvable dans index.html.');
}

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
