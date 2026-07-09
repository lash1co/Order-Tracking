import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7247',
        changeOrigin: true,
        secure: false
      },
      '/hubs': {
        target: 'https://localhost:7247',
        changeOrigin: true,
        secure: false,
        ws: true
      }
    }
  },
  build: {
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          realtime: ['@microsoft/signalr'],
          map: ['leaflet']
        }
      }
    }
  }
});
