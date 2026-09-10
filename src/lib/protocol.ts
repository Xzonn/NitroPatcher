import type { BrowserPatchResult } from 'nitro-patcher/browser';
export interface PatchRequest {
  id: number;
  rom: File;
  patch: File;
}
export type WorkerReply =
  | { type: 'ready' }
  | { type: 'done'; id: number; result: BrowserPatchResult }
  | { type: 'error'; id: number; message: string };
