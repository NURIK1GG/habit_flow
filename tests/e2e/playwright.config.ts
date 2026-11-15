import { defineConfig, devices } from '@playwright/test';
const chromium = process.env.CHROMIUM_PATH ? { executablePath: process.env.CHROMIUM_PATH } : {};
export default defineConfig({
  testDir: './specs', timeout: 60000, expect: { timeout: 15000 }, workers: 1,
  reporter: [['list'], ['html', { open: 'never' }], ['json', { outputFile: 'test-results/results.json' }]],
  use: { baseURL: process.env.HABITFLOW_URL || 'http://127.0.0.1:5080', trace: 'retain-on-failure', screenshot: 'only-on-failure' },
  projects: [
    { name: 'chromium-desktop', use: { ...devices['Desktop Chrome'], launchOptions: chromium } },
    { name: 'chromium-tablet', use: { ...devices['iPad (gen 7)'], defaultBrowserType: 'chromium', launchOptions: chromium } },
    { name: 'chromium-mobile', use: { ...devices['Pixel 7'], launchOptions: chromium } },
    { name: 'firefox-desktop', use: { ...devices['Desktop Firefox'] } }
  ]
});
