import { test, expect } from '@playwright/test';
test('shows an accessible local-only patch flow and handles bad ROMs', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'NDS ROM 补丁工具' })).toBeVisible();
  await expect(page.getByRole('button', { name: '开始', exact: true })).toBeDisabled();
  await page.locator('#rom-file').setInputFiles({
    name: 'invalid.nds',
    mimeType: 'application/octet-stream',
    buffer: Buffer.alloc(512),
  });
  await page.locator('#patch-file').setInputFiles({
    name: 'test.xzp',
    mimeType: 'application/octet-stream',
    buffer: Buffer.from('bad patch'),
  });
  await page.getByRole('button', { name: '开始', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('错误：');
  await expect(page.getByRole('button', { name: '开始', exact: true })).toBeEnabled();
});
test('mobile layout fits the viewport', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'NDS ROM 补丁工具' })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(390);
});

test('applies a synthetic patch and reports an MD5 mismatch without losing the output', async ({
  page,
  context,
}) => {
  const { readFile } = await import('node:fs/promises');
  const expected = JSON.parse(await readFile('test/fixtures/expected.json', 'utf8')) as {
    outputMd5: string;
  };
  await page.goto('/');
  await expect(page.locator('main')).toHaveAttribute('data-ready', 'true');
  await page.waitForLoadState('networkidle');
  await context.setOffline(true);
  await page.locator('#rom-file').setInputFiles('test/fixtures/synthetic.nds');
  await page.locator('#patch-file').setInputFiles('test/fixtures/patch.zip');
  await page.getByLabel('输出 ROM 文件名').fill('custom.bin');
  await page.getByRole('button', { name: '开始', exact: true }).click();
  await expect(page.getByText('已完成。', { exact: true })).toBeVisible();
  await expect(page.getByText(expected.outputMd5, { exact: true })).toBeVisible();
  const downloading = page.waitForEvent('download');
  await page.getByRole('link', { name: '下载 ROM' }).click();
  const download = await downloading;
  expect(download.suggestedFilename()).toBe('custom.bin');

  await page.locator('#patch-file').setInputFiles('test/fixtures/mismatch.zip');
  await expect(page.getByRole('link', { name: '下载 ROM' })).toHaveCount(0);
  await page.getByRole('button', { name: '开始', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('MD5 校验失败');
  await expect(page.getByRole('link', { name: '下载 ROM' })).toBeVisible();
  await expect(page.getByText(expected.outputMd5, { exact: true })).toBeVisible();
});

test('serves the license file referenced by the bundled worker', async ({ request }) => {
  const { readFile } = await import('node:fs/promises');
  const expected = await readFile('node_modules/nitro-patcher/dist/browser.js.LEGAL.txt', 'utf8');
  const response = await request.get('/assets/browser.js.LEGAL.txt');
  expect(response.status()).toBe(200);
  expect(await response.text()).toBe(expected);
});
