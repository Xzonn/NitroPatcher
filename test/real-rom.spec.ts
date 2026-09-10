import { test, expect } from '@playwright/test';
import { createHash } from 'node:crypto';
import { readFile, stat } from 'node:fs/promises';
const rom = process.env.NITRO_TEST_ROM;
const patch = process.env.NITRO_TEST_PATCH;
test('real Platinum patch runs offline and downloads the reference bytes', async ({
  page,
  context,
}, testInfo) => {
  test.skip(!rom || !patch, 'Set NITRO_TEST_ROM and NITRO_TEST_PATCH for the local real-ROM check');
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/');
  await expect(page.locator('main')).toHaveAttribute('data-ready', 'true');
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: testInfo.outputPath('initial-desktop.png') });
  const requests: string[] = [];
  context.on('request', (request) => requests.push(`${request.method()} ${request.url()}`));
  await context.setOffline(true);
  await page.locator('#rom-file').setInputFiles(rom!);
  await page.locator('#patch-file').setInputFiles(patch!);
  await page.getByLabel('输出 ROM 文件名').fill('白金汉化.nds');
  await page.getByRole('button', { name: '开始', exact: true }).click();
  await expect(page.getByText('已完成。', { exact: true })).toBeVisible({ timeout: 90_000 });
  await expect(page.getByText('203b3fe94134406db7681ea51af5fd0f', { exact: true })).toBeVisible();
  const downloadEvent = page.waitForEvent('download');
  await page.getByRole('link', { name: '下载 ROM' }).click();
  const download = await downloadEvent;
  expect(download.suggestedFilename()).toBe('白金汉化.nds');
  const path = testInfo.outputPath('platinum-patched.nds');
  await download.saveAs(path);
  expect((await stat(path)).size).toBe(134217728);
  expect(
    createHash('md5')
      .update(await readFile(path))
      .digest('hex'),
  ).toBe('203b3fe94134406db7681ea51af5fd0f');
  expect(requests).toEqual([]);
  expect(errors).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('result-desktop.png') });
});
