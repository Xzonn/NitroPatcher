import type { BrowserPatchResult, PatchInfo } from 'nitro-patcher/browser';
export type PatchRequest =
  | {
      type: 'patch';
      id: number;
      rom: File;
      patch: File;
    }
  | { type: 'metadata'; id: number; patch: File }
  | {
      type: 'download';
      id: number;
      url: string;
      expectedPatchId: string;
      expectedVersion: string;
    };
export type WorkerReply =
  | { type: 'ready' }
  | { type: 'metadata'; id: number; info: PatchInfo }
  | { type: 'metadata-error'; id: number; message: string }
  | { type: 'done'; id: number; result: BrowserPatchResult }
  | { type: 'download'; id: number; buffer: ArrayBuffer; name: string }
  | { type: 'download-error'; id: number; message: string }
  | { type: 'error'; id: number; message: string };
