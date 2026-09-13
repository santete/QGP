/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

// https://vite.dev/config
export default defineConfig(({ mode }) => {
  // Đích proxy tới qgp-api (.NET). Mặc định port dev chuẩn của launchSettings (http profile).
  // Override qua VITE_API_PROXY_TARGET (vd khi BE chạy port khác / máy có port conflict).
  const env = loadEnv(mode, process.cwd(), '');
  const apiTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5048';

  return {
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    proxy: {
      // Real mode (VITE_USE_MOCK=0): proxy REST /v1 sang qgp-api (.NET).
      // Khi VITE_USE_MOCK=1 (mặc định dev offline) thì client dùng mock, không gọi proxy.
      '/v1': {
        target: apiTarget,
        changeOrigin: true,
      },
      // dev-login (mock OIDC) nằm ở /auth (không dưới /v1).
      '/auth': {
        target: apiTarget,
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
  };
});
