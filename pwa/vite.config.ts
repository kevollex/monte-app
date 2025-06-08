import { defineConfig, loadEnv } from 'vite';
import { VitePWA } from 'vite-plugin-pwa';

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');

    return {
      base: "/",
      build: {
        sourcemap: true,
        assetsDir: "code",
        target: ["esnext"],
        cssMinify: true,
        lib: false
      },
      plugins: [
        VitePWA({
          strategies: "injectManifest",
          injectManifest: {
            swSrc: 'public/sw.js',
            swDest: 'dist/sw.js',
            globDirectory: 'dist',
            globPatterns: [
              '**/*.{html,js,css,json,png}',
            ],
          },
          injectRegister: false,
          manifest: false,
          devOptions: {
            enabled: true
          }
        })
      ],
      server: {
        port: parseInt(env.VITE_PORT),
        proxy: {
                '/api': {
                    target: process.env.services__apiservice__https__0 ||
                        process.env.services__apiservice__http__0,
                    changeOrigin: true,
                    rewrite: (path) => path.replace(/^\/api/, ''),
                    secure: false,
                }
            }
      }
    }
})
