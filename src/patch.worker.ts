import { patchBuffer } from 'nitro-patcher/browser';
import type { PatchRequest, WorkerReply } from './lib/protocol';
const reply = (message: WorkerReply, transfer: Transferable[] = []): void =>
  self.postMessage(message, { transfer });
self.onmessage = async (event: MessageEvent<PatchRequest>) => {
  const { id, rom, patch } = event.data;
  try {
    const [source, archive] = await Promise.all([rom.arrayBuffer(), patch.arrayBuffer()]);
    const result = patchBuffer(new Uint8Array(source), new Uint8Array(archive));
    reply({ type: 'done', id, result }, [result.buffer.buffer]);
  } catch (error) {
    reply({ type: 'error', id, message: error instanceof Error ? error.message : String(error) });
  }
};
reply({ type: 'ready' });
