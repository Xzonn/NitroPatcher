import { readFile } from 'node:fs/promises';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({
  plugins: [
    react(),
    {
      name: 'nitro-browser-license',
      async generateBundle() {
        const license = new URL(
          'browser.js.LEGAL.txt',
          import.meta.resolve('nitro-patcher/browser'),
        );
        this.emitFile({
          type: 'asset',
          fileName: 'assets/browser.js.LEGAL.txt',
          source: await readFile(license, 'utf8'),
        });
      },
    },
  ],
  base: './',
  worker: { format: 'es' },
});
