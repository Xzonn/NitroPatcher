export interface CatalogPatch {
  patchId: string;
  gameCode: string;
  gameName: string;
  author: string;
  latestVersion: string;
  homepage: string;
  downloadUrl: string;
}

const columns = {
  patch_id: 'patchId',
  game_code: 'gameCode',
  game_name: 'gameName',
  author: 'author',
  latest_version: 'latestVersion',
  homepage: 'homepage',
  download_url: 'downloadUrl',
} as const;

const isSafeUrl = (value: string, field: string, line: number): boolean => {
  if (!value) return true;
  let url: URL;
  try {
    url = new URL(value);
  } catch {
    console.warn(`忽略补丁列表第 ${line} 行：${field} 不是有效 URL。`);
    return false;
  }
  if (url.protocol !== 'https:' && url.protocol !== 'http:') {
    console.warn(`忽略补丁列表第 ${line} 行：${field} 只允许 HTTP 或 HTTPS。`);
    return false;
  }
  return true;
};

export const parsePatchCatalog = (source: string): CatalogPatch[] => {
  const lines = source.replace(/^\uFEFF/, '').split(/\r?\n/);
  const headerLine = lines.findIndex((line) => line.trim() && !line.trimStart().startsWith('#'));
  if (headerLine < 0) {
    console.warn('忽略补丁列表：缺少表头。');
    return [];
  }
  const header = lines[headerLine]!.split('\t').map((name) => name.trim());
  const positions = new Map<string, number>();
  header.forEach((name, index) => {
    if (!name) {
      console.warn(`忽略补丁列表中的第 ${index + 1} 个表头：字段名为空。`);
      return;
    }
    if (positions.has(name)) {
      console.warn(`忽略补丁列表中的重复表头：${name}。`);
      return;
    }
    positions.set(name, index);
  });
  const missingColumns = Object.keys(columns).filter((name) => !positions.has(name));
  if (missingColumns.length) {
    console.warn(`忽略补丁列表：缺少字段 ${missingColumns.join('、')}。`);
    return [];
  }

  const patches: CatalogPatch[] = [];
  const ids = new Set<string>();
  for (let index = headerLine + 1; index < lines.length; index++) {
    const line = lines[index]!;
    if (!line.trim() || line.trimStart().startsWith('#')) continue;
    const values = line.split('\t');
    const record = Object.fromEntries(
      Object.entries(columns).map(([column, property]) => [
        property,
        values[positions.get(column)!]?.trim() ?? '',
      ]),
    ) as unknown as CatalogPatch;
    const lineNumber = index + 1;
    if (!record.patchId) {
      console.warn(`忽略补丁列表第 ${lineNumber} 行：缺少 patch_id。`);
      continue;
    }
    if (ids.has(record.patchId)) {
      console.warn(`忽略补丁列表第 ${lineNumber} 行：patch_id 重复。`);
      continue;
    }
    if (!/^[\x20-\x7e]{4}$/.test(record.gameCode)) {
      console.warn(`忽略补丁列表第 ${lineNumber} 行：game_code 必须是四个 ASCII 字符。`);
      continue;
    }
    if (!record.gameName) {
      console.warn(`忽略补丁列表第 ${lineNumber} 行：缺少 game_name。`);
      continue;
    }
    if (
      !isSafeUrl(record.homepage, 'homepage', lineNumber) ||
      !isSafeUrl(record.downloadUrl, 'download_url', lineNumber)
    )
      continue;
    ids.add(record.patchId);
    patches.push(record);
  }
  return patches;
};

const semver =
  /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$/;

const compareText = (left: string, right: string): number =>
  left === right ? 0 : left < right ? -1 : 1;

const compareNumeric = (left: string, right: string): number =>
  left.length === right.length ? compareText(left, right) : left.length - right.length;

export const compareVersions = (left: string, right: string): number | null => {
  const a = semver.exec(left);
  const b = semver.exec(right);
  if (!a && !b) return compareText(left, right);
  if (!a || !b) return null;
  if (
    [a[4], b[4]].some((prerelease) =>
      prerelease
        ?.split('.')
        .some((part) => /^\d+$/.test(part) && part.length > 1 && part.startsWith('0')),
    )
  )
    return null;
  for (let index = 1; index <= 3; index++) {
    const compared = compareNumeric(a[index]!, b[index]!);
    if (compared) return Math.sign(compared);
  }
  const aPre = a[4]?.split('.');
  const bPre = b[4]?.split('.');
  if (!aPre || !bPre) return aPre ? -1 : bPre ? 1 : 0;
  for (let index = 0; index < Math.max(aPre.length, bPre.length); index++) {
    const aPart = aPre[index];
    const bPart = bPre[index];
    if (aPart === undefined || bPart === undefined) return aPart === undefined ? -1 : 1;
    if (aPart === bPart) continue;
    const aNumeric = /^\d+$/.test(aPart) && (aPart === '0' || !aPart.startsWith('0'));
    const bNumeric = /^\d+$/.test(bPart) && (bPart === '0' || !bPart.startsWith('0'));
    if (aNumeric !== bNumeric) return aNumeric ? -1 : 1;
    return aNumeric ? Math.sign(compareNumeric(aPart, bPart)) : compareText(aPart, bPart);
  }
  return 0;
};

export interface RomIdentity {
  gameTitle: string;
  gameCode: string;
}

const ascii = new TextDecoder('ascii');

export const readRomIdentity = async (file: File): Promise<RomIdentity> => {
  if (file.size < 16) throw new Error('文件太小，不是有效的 NDS ROM。');
  const header = new Uint8Array(await file.slice(0, 16).arrayBuffer());
  const gameCode = ascii.decode(header.subarray(12, 16));
  if (!/^[\x20-\x7e]{4}$/.test(gameCode)) throw new Error('ROM 的游戏代码无效。');
  return {
    gameTitle: ascii.decode(header.subarray(0, 12)).replace(/\0+$/, '').trim(),
    gameCode,
  };
};
