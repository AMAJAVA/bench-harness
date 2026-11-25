import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

// En développement, les appels /api sont relayés vers l'API ASP.NET Core (même origine qu'en production,
// où l'API sert la SPA : aucun CORS, CSP `default-src 'self'`).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', 'TEAMLEAVE_');
  const apiTarget = env.TEAMLEAVE_API_PROXY_TARGET || 'http://localhost:5080';

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
      },
    },
    build: {
      outDir: 'dist',
      // Aucun asset inliné en data: URI — la CSP de l'API (`default-src 'self'`) les bloquerait.
      assetsInlineLimit: 0,
      sourcemap: false,
    },
  };
});
