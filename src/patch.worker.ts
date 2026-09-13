import { patchBuffer, readPatchInfo } from 'nitro-patcher/browser';
import type { PatchRequest, WorkerReply } from './lib/protocol';
const reply = (message: WorkerReply, transfer: Transferable[] = []): void =>
  self.postMessage(message, { transfer });
self.onmessage = async (event: MessageEvent<PatchRequest>) => {
  const { id, patch } = event.data;
  try {
    if (event.data.type === 'metadata') {
      const archive = await patch.arrayBuffer();
      reply({ type: 'metadata', id, info: readPatchInfo(new Uint8Array(archive)) });
      return;
    }
    const { rom } = event.data;
    const [source, archive] = await Promise.all([rom.arrayBuffer(), patch.arrayBuffer()]);
    const result = patchBuffer(new Uint8Array(source), new Uint8Array(archive));
    reply({ type: 'done', id, result }, [result.buffer.buffer]);
  } catch (error) {
    reply({
      type: event.data.type === 'metadata' ? 'metadata-error' : 'error',
      id,
      message: error instanceof Error ? error.message : String(error),
    });
  }
};
reply({ type: 'ready' });
