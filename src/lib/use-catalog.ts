import { useEffect, useState } from 'react';
import { parsePatchCatalog } from './catalog';
import type { CatalogPatch } from './catalog';

export const usePatchCatalog = () => {
  const [patches, setPatches] = useState<CatalogPatch[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    const controller = new AbortController();
    const load = async (): Promise<void> => {
      try {
        const response = await fetch(new URL('patches.tsv', document.baseURI), {
          cache: 'no-cache',
          signal: controller.signal,
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        setPatches(parsePatchCatalog(await response.text()));
      } catch (reason) {
        if (!controller.signal.aborted)
          setError(reason instanceof Error ? reason.message : String(reason));
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    };
    void load();
    return () => controller.abort();
  }, []);
  return { patches, error, loading };
};
