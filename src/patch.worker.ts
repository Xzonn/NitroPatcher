import { extractZipEntry, patchBuffer, readPatchInfo } from 'nitro-patcher/browser';
import type { PatchRequest, WorkerReply } from './lib/protocol';

const MAX_DOWNLOAD_SIZE = 256 * 1024 * 1024;
const MAX_ARCHIVE_ENTRIES = 10_000;
const filename = (value: string): string => {
  const last = value.replaceAll('\\', '/').split('/').pop();
  return last || 'patch.xzp';
};

const downloadPatch = async (
  urlValue: string,
  expectedPatchId: string,
  expectedVersion: string,
): Promise<{ bytes: Uint8Array<ArrayBuffer>; name: string }> => {
  const url = new URL(urlValue);
  if (url.protocol !== 'https:' && url.protocol !== 'http:')
    throw new Error('补丁下载地址只允许 HTTP 或 HTTPS。');
  let entryName = '';
  if (url.hash) {
    try {
      entryName = decodeURIComponent(url.hash.slice(1));
    } catch {
      throw new Error('补丁下载地址中的 ZIP 文件名编码无效。');
    }
    if (!entryName || entryName.endsWith('/') || entryName.endsWith('\\'))
      throw new Error('补丁下载地址中的 ZIP 文件名无效。');
    url.hash = '';
  }
  const response = await fetch(url);
  if (!response.ok) throw new Error(`补丁下载失败：HTTP ${response.status}。`);
  const declaredSize = Number(response.headers.get('content-length'));
  if (Number.isFinite(declaredSize) && declaredSize > MAX_DOWNLOAD_SIZE)
    throw new Error('下载文件超过 256 MiB 限制。');
  const downloaded = new Uint8Array(await response.arrayBuffer());
  if (downloaded.byteLength > MAX_DOWNLOAD_SIZE) throw new Error('下载文件超过 256 MiB 限制。');

  let bytes: Uint8Array;
  if (entryName) {
    bytes = extractZipEntry(downloaded, entryName, {
      maxEntries: MAX_ARCHIVE_ENTRIES,
      maxUncompressedSize: MAX_DOWNLOAD_SIZE,
    });
  } else {
    bytes = downloaded;
  }

  const info = readPatchInfo(bytes, {
    maxEntries: MAX_ARCHIVE_ENTRIES,
    maxUncompressedSize: 1024 * 1024 * 1024,
  });
  if (info.metadata) {
    if (info.metadata.id !== expectedPatchId)
      throw new Error(`下载的补丁 ID 与列表中的 ${expectedPatchId} 不一致。`);
    if (expectedVersion && info.metadata.version !== expectedVersion)
      throw new Error(`下载的补丁版本与列表中的 ${expectedVersion} 不一致。`);
  }
  return { bytes: new Uint8Array(bytes), name: filename(entryName || url.pathname) };
};
const reply = (message: WorkerReply, transfer: Transferable[] = []): void =>
  self.postMessage(message, { transfer });
self.onmessage = async (event: MessageEvent<PatchRequest>) => {
  const { id } = event.data;
  try {
    if (event.data.type === 'download') {
      const result = await downloadPatch(
        event.data.url,
        event.data.expectedPatchId,
        event.data.expectedVersion,
      );
      reply({ type: 'download', id, buffer: result.bytes.buffer, name: result.name }, [
        result.bytes.buffer,
      ]);
      return;
    }
    if (event.data.type === 'metadata') {
      const { patch } = event.data;
      const archive = await patch.arrayBuffer();
      reply({ type: 'metadata', id, info: readPatchInfo(new Uint8Array(archive)) });
      return;
    }
    const { rom, patch } = event.data;
    const [source, archive] = await Promise.all([rom.arrayBuffer(), patch.arrayBuffer()]);
    const result = patchBuffer(new Uint8Array(source), new Uint8Array(archive));
    reply({ type: 'done', id, result }, [result.buffer.buffer]);
  } catch (error) {
    reply({
      type:
        event.data.type === 'metadata'
          ? 'metadata-error'
          : event.data.type === 'download'
            ? 'download-error'
            : 'error',
      id,
      message: error instanceof Error ? error.message : String(error),
    });
  }
};
reply({ type: 'ready' });
