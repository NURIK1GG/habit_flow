import { test, expect, Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
async function login(page: Page, email = 'arman@habitflow.local') {
  await page.goto('/account/login');
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill('Demo!Pass123');
  await page.getByRole('button', { name: 'Sign in →' }).click();
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello,');
  await expect(page.locator('[data-interactive-ready="true"]')).toBeAttached();
}
async function register(page: Page, name = 'E2E Member') {
  const email = `e2e-${Date.now()}-${Math.random().toString(16).slice(2)}@example.com`;
  await page.goto('/account/register');
  await page.getByLabel('Display name').fill(name);
  await page.getByLabel('Email address').fill(email);
  await page.getByLabel('Password', { exact: true }).fill('Demo!Pass123');
  await page.getByRole('button', { name: 'Start building momentum' }).click();
  await expect(page.getByRole('heading', { level: 1 })).toContainText('Hello,');
  await expect(page.locator('[data-interactive-ready="true"]')).toBeAttached();
  return email;
}
async function createRoutine(page: Page, title: string) {
  await page.getByRole('button', { name: 'New routine' }).click();
  await page.getByLabel('Routine name').fill(title);
  await page.getByLabel('What does this routine mean to you?').fill('A repeatable commitment to progress.');
  await page.getByLabel('Type', { exact: true }).selectOption('Workout');
  await page.getByRole('button', { name: 'Add exercise' }).click();
  await page.getByLabel('Exercise', { exact: true }).fill('Bodyweight squat');
  await page.getByRole('button', { name: 'Save routine' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
}
test('registration, routine builder, check-in, history, privacy, archive and restore', async ({ page }) => {
  const title = `Daily strength ${Date.now()}`;
  await register(page); await createRoutine(page, title);
  const card = page.locator('.routine-card').filter({ has: page.getByRole('heading', { name: title }) });
  await card.getByRole('button', { name: 'Check in' }).click();
  await page.getByLabel('Minutes spent').fill('25');
  await page.getByLabel('A note to your future self').fill('First session completed.');
  await page.getByRole('button', { name: 'Save check-in' }).click();
  await expect(card.getByRole('button', { name: 'Completed' })).toBeDisabled();
  await page.getByRole('link', { name: 'Progress', exact: true }).click();
  await expect(page.locator('.history-row')).toContainText('First session completed.');
  await page.locator('.history-row').getByRole('button', { name: 'Undo' }).click();
  await expect(page.locator('.history-row')).toHaveCount(0);
  await page.getByRole('link', { name: 'Settings', exact: true }).click();
  await page.getByLabel('Join the community leaderboard').check();
  await page.getByRole('button', { name: 'Save preferences' }).click();
  await expect(page.locator('.toast-body')).toContainText('preferences');
  await page.getByRole('link', { name: 'Community', exact: true }).click();
  await expect(page.locator('.leader-row.is-me')).toContainText('E2E Member');
  await page.getByRole('link', { name: 'My routines', exact: true }).click();
  await page.getByRole('button', { name: `Archive ${title}`, exact: true }).click();
  await expect(page.locator('.routine-card')).toHaveCount(0);
  await page.getByRole('button', { name: 'Archived', exact: true }).click();
  await page.getByRole('button', { name: `Restore ${title}`, exact: true }).click();
  await page.getByRole('button', { name: 'Active', exact: true }).click();
  await expect(page.getByRole('heading', { name: title })).toBeVisible();
});
test('real-time synchronization refreshes a second authenticated browser session', async ({ page, browser }) => {
  await login(page);
  const second = await browser.newContext(); const other = await second.newPage();
  await login(other); await other.goto('/progress');
  await expect(other.getByRole('heading', { level: 1 })).toContainText('Look how far');
  const title = `Live routine ${Date.now()}`;
  await createRoutine(page, title);
  await expect(other.locator('.best-grid')).toContainText(title);
  const card = page.locator('.routine-card').filter({ has: page.getByRole('heading', { name: title }) });
  await card.getByRole('button', { name: 'Check in' }).click();
  await page.getByRole('button', { name: 'Save check-in' }).click();
  await expect(other.locator('.history-row').filter({ hasText: title })).toHaveCount(1);
  await second.close();
});
test('role-based navigation and administrator suspension', async ({ page, browser }) => {
  const email = await register(page, 'Access Test');
  await page.goto('/admin');
  await expect(page.getByRole('heading', { name: 'Community accounts' })).toHaveCount(0);
  const context = await browser.newContext(); const admin = await context.newPage();
  await login(admin, 'ayan@habitflow.local'); await admin.goto('/admin');
  const row = admin.getByRole('row').filter({ hasText: email });
  await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Suspend', exact: true }).click();
  await expect(row).toContainText('Suspended');
  await row.getByRole('button', { name: 'Restore access' }).click();
  await expect(row).toContainText('Active'); await context.close();
});
test('responsive overview and accessibility', async ({ page }) => {
  await login(page, 'aruzhan@habitflow.local');
  await expect(page.locator('.chart-panel svg')).toBeVisible();
  await expect(page.locator('.stat-card')).toHaveCount(4);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1)).toBe(false);
  const scan = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  expect(scan.violations).toEqual([]);
  await page.screenshot({ path: `test-results/overview-${test.info().project.name}.png`, fullPage: true });
});
