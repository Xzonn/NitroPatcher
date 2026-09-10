import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './test',
  timeout: 120_000,
  workers: 1,
  use: {
    baseURL: 'http://127.0.0.1:4175',
    headless: true,
    viewport: { width: 1440, height: 1100 },
  },
  webServer: {
    command: 'pnpm exec vite preview --host 127.0.0.1 --port 4175 --strictPort',
    url: 'http://127.0.0.1:4175',
    reuseExistingServer: !process.env.CI,
  },
});
