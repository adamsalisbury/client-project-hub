import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    base: '/',
    build: {
        outDir: '../backend/ProjectHub.Api/wwwroot',
        emptyOutDir: true,
        rollupOptions: {
            output: {
                entryFileNames: 'app.js',
                chunkFileNames: 'app-[name].js',
                assetFileNames: 'app[extname]'
            }
        }
    },
    server: {
        port: 5173,
        proxy: {
            '/api': 'http://localhost:5090'
        }
    }
});
