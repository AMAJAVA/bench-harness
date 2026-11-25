import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

/**
 * En développement, l'application appelle `/api/v1/...` en relatif et Vite
 * proxifie vers l'API ASP.NET Core. Cela rend le navigateur same-origin, ce qui
 * permet au cookie de rafraîchissement `HttpOnly` d'être posé et renvoyé.
 * En production, VITE_API_BASE_URL doit pointer vers l'URL absolue de l'API.
 */
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const proxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5080';

  const proxy = {
    '/api': {
      target: proxyTarget,
      changeOrigin: false,
      secure: false,
    },
  };

  return {
    plugins: [react()],
    server: {
      port: 5173,
      strictPort: true,
      proxy,
    },
    preview: {
      port: 4173,
      strictPort: true,
      proxy,
    },
  };
});
