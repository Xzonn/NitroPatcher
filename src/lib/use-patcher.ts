import { useEffect, useRef, useState } from 'react';
import type { PatchInfo } from 'nitro-patcher/browser';
import type { PatchRequest, WorkerReply } from './protocol';
interface Result {
  url: string;
  name: string;
  inputMd5: string;
  outputMd5: string;
  mismatch: boolean;
}
export const usePatcher = (patch: File | null) => {
  const worker = useRef<Worker | null>(null);
  const requestId = useRef(0);
  const metadataId = useRef(0);
  const [metadataState, setMetadataState] = useState<{
    file: File;
    info: PatchInfo | null;
    error: string;
  } | null>(null);
  const metadataFile = useRef<File | null>(null);
  const outputName = useRef('output.nds');
  const downloadUrl = useRef<string | null>(null);
  const [ready, setReady] = useState(false);
  const [status, setStatus] = useState<'idle' | 'working' | 'success' | 'error'>('idle');
  const [error, setError] = useState('');
  const [result, setResult] = useState<Result | null>(null);
  useEffect(() => {
    const engine = new Worker(new URL('../patch.worker.ts', import.meta.url), { type: 'module' });
    worker.current = engine;
    engine.onmessage = ({ data }: MessageEvent<WorkerReply>) => {
      if (data.type === 'ready') {
        setReady(true);
        return;
      }
      if (data.type === 'metadata' || data.type === 'metadata-error') {
        if (data.id !== metadataId.current || !metadataFile.current) return;
        setMetadataState({
          file: metadataFile.current,
          info: data.type === 'metadata' ? data.info : null,
          error: data.type === 'metadata-error' ? data.message : '',
        });
        return;
      }
      if (data.id !== requestId.current) return;
      if (data.type === 'error') {
        setError(data.message);
        setStatus('error');
      }
      if (data.type === 'done') {
        const url = URL.createObjectURL(
          new Blob([data.result.buffer], { type: 'application/octet-stream' }),
        );
        downloadUrl.current = url;
        setResult({
          url,
          name: outputName.current,
          inputMd5: data.result.inputMd5,
          outputMd5: data.result.outputMd5,
          mismatch: data.result.returnValue === 'MD5_MISMATCH',
        });
        setStatus('success');
      }
    };
    engine.onerror = (event) => {
      setError(event.message);
      setStatus('error');
      setReady(false);
    };
    return () => {
      engine.terminate();
      worker.current = null;
      if (downloadUrl.current) URL.revokeObjectURL(downloadUrl.current);
    };
  }, []);
  useEffect(() => {
    metadataFile.current = patch;
    const id = ++metadataId.current;
    if (patch && ready) {
      worker.current?.postMessage({ type: 'metadata', id, patch } satisfies PatchRequest);
    }
    return () => {
      ++metadataId.current;
    };
  }, [patch, ready]);
  const reset = (): void => {
    if (downloadUrl.current) URL.revokeObjectURL(downloadUrl.current);
    downloadUrl.current = null;
    setResult(null);
    setError('');
    setStatus('idle');
  };
  const run = (rom: File, patch: File, name: string): void => {
    if (!worker.current || !ready || status === 'working') return;
    reset();
    setStatus('working');
    outputName.current = name;
    worker.current.postMessage({
      type: 'patch',
      id: ++requestId.current,
      rom,
      patch,
    } satisfies PatchRequest);
  };
  const currentMetadata = metadataState?.file === patch ? metadataState : null;
  return {
    ready,
    status,
    error,
    result,
    reset,
    run,
    metadata: currentMetadata?.info?.metadata ?? null,
    readme: currentMetadata?.info?.readme ?? null,
    metadataError: currentMetadata?.error ?? '',
    metadataLoading: !!patch && !currentMetadata && ready,
  };
};
