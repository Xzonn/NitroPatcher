import { Fragment, useState } from 'react';
import Markdown from 'react-markdown';
import { Alert, Button, FilePicker, FormField, Input } from './components/ui';
import { usePatcher } from './lib/use-patcher';

export const App = () => {
  const [rom, setRom] = useState<File | null>(null);
  const [patch, setPatch] = useState<File | null>(null);
  const [outputName, setOutputName] = useState('output.nds');
  const engine = usePatcher(patch);
  const busy = engine.status === 'working';
  const select = (kind: 'rom' | 'patch', file: File): void => {
    if (busy) return;
    engine.reset();
    if (kind === 'rom') {
      setRom(file);
      setOutputName(`${file.name.replace(/\.nds$/i, '')}.patched.nds`);
    } else setPatch(file);
  };
  const start = (): void => {
    if (!rom || !patch || busy || !outputName.trim()) return;
    engine.run(rom, patch, outputName);
  };
  const error = engine.status === 'error' ? engine.error : '';
  const metadataFields = (
    [
      ['author', '作者'],
      ['name', '名称'],
      ['homepage', '主页'],
      ['version', '版本'],
    ] as const
  ).flatMap(([field, label]) => {
    const value = engine.metadata?.[field];
    if (value === undefined) return [];
    const suffix = field === 'version' && engine.metadata?.isBeta ? '（测试版）' : '';
    return [{ field, label, value: value.replace(/\r\n?|\n/g, ' ') + suffix }];
  });
  return (
    <main className="app" data-ready={engine.ready}>
      <header className="page-header">
        <h1>NDS ROM 补丁工具</h1>
      </header>
      <form
        onSubmit={(event) => {
          event.preventDefault();
          start();
        }}
      >
        <fieldset disabled={busy} aria-busy={busy}>
          <legend className="visually-hidden">补丁文件设置</legend>
          <FormField label="原始 ROM" htmlFor="rom-file">
            <FilePicker
              id="rom-file"
              label="原始 ROM"
              accept=".nds"
              file={rom}
              disabled={busy}
              onSelect={(file) => select('rom', file)}
            />
          </FormField>
          <FormField label="补丁包" htmlFor="patch-file">
            <FilePicker
              id="patch-file"
              label="补丁包"
              accept=".xzp,.zip"
              file={patch}
              disabled={busy}
              onSelect={(file) => select('patch', file)}
            />
          </FormField>
          <FormField label="输出 ROM" htmlFor="output-name">
            <Input
              id="output-name"
              value={outputName}
              onChange={(event) => {
                setOutputName(event.target.value);
                engine.reset();
              }}
              aria-label="输出 ROM 文件名"
            />
          </FormField>
          <div className="form-actions">
            <Button
              variant="primary"
              type="submit"
              loading={busy}
              disabled={!rom || !patch || !engine.ready || !outputName.trim()}
            >
              开始
            </Button>
          </div>
        </fieldset>
      </form>
      {error || engine.result ? (
        <div className="results">
          {error && <Alert variant="error">错误：{error}</Alert>}
          {engine.result && (
            <>
              <Alert variant={engine.result.mismatch ? 'warning' : 'success'}>
                {engine.result.mismatch
                  ? '已完成，但是原始 ROM 的 MD5 校验失败，可能是因为使用了错误的原始 ROM。'
                  : '已完成。'}
              </Alert>
              <dl className="checksums">
                <dt className="checksum-label">原始 ROM 的 MD5：</dt>
                <dd className="checksum-value">{engine.result.inputMd5}</dd>
                <dt className="checksum-label">生成 ROM 的 MD5：</dt>
                <dd className="checksum-value">{engine.result.outputMd5}</dd>
              </dl>
              <a className="button" href={engine.result.url} download={engine.result.name}>
                下载 ROM
              </a>
            </>
          )}
        </div>
      ) : null}
      {patch && (
        <section
          className="patch-info"
          aria-label="补丁信息"
          aria-live="polite"
          aria-busy={engine.metadataLoading}
        >
          {!engine.ready ? (
            <p>等候补丁引擎就绪…</p>
          ) : engine.metadataLoading ? (
            <p>正在读取补丁信息…</p>
          ) : engine.metadataError ? (
            <Alert variant="warning">无法读取补丁信息：{engine.metadataError}</Alert>
          ) : metadataFields.length > 0 ? (
            <div>
              <h2>补丁信息：</h2>
              <dl className="metadata">
                {metadataFields.map(({ field, label, value }) => (
                  <Fragment key={field}>
                    <dt>{label}</dt>
                    <dd>{value}</dd>
                  </Fragment>
                ))}
              </dl>
            </div>
          ) : !engine.readme ? (
            <p>补丁包未提供元数据或说明。</p>
          ) : null}
          {engine.readme && (
            <div className="patch-readme">
              {engine.readme.format === 'markdown' ? (
                <div className="markdown-content">
                  <Markdown
                    skipHtml
                    components={{
                      a: ({ href, children }) => (
                        <a href={href} target="_blank" rel="noopener noreferrer">
                          {children}
                        </a>
                      ),
                      img: ({ alt }) => <span>{alt ? `[图片：${alt}]` : '[图片]'}</span>,
                    }}
                  >
                    {engine.readme.content.trimEnd()}
                  </Markdown>
                </div>
              ) : (
                <pre>{engine.readme.content.trimEnd()}</pre>
              )}
              <blockquote className="content-notice">
                补丁信息和说明内容来自所选补丁包，并非由当前网站提供或认可，请注意辨别。
              </blockquote>
            </div>
          )}
        </section>
      )}
      <footer className="page-footer">
        <span>网站设计：Xzonn</span>
        <span role="status">{busy ? '正在处理…' : engine.ready ? '就绪' : '正在准备…'}</span>
      </footer>
    </main>
  );
};
