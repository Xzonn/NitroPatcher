import { useState } from 'react';
import { Alert, Button, Divider, Flex, Form, Input, Typography, Upload } from 'antd';
import { usePatcher } from './lib/use-patcher';

export const App = () => {
  const [rom, setRom] = useState<File | null>(null);
  const [patch, setPatch] = useState<File | null>(null);
  const [outputName, setOutputName] = useState('output.nds');
  const engine = usePatcher();
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
  return (
    <main className="app" data-ready={engine.ready}>
      <header className="page-header">
        <Typography.Title level={3} style={{ margin: 0 }}>
          NDS ROM 补丁工具
        </Typography.Title>
      </header>
      <Form
        layout="horizontal"
        labelCol={{ flex: '96px' }}
        wrapperCol={{ flex: 1 }}
        labelAlign="left"
        onFinish={start}
        disabled={busy}
      >
        <Form.Item label="原始 ROM" htmlFor="rom-name">
          <Flex gap={8}>
            <Input id="rom-name" readOnly value={rom?.name ?? ''} />
            <Upload
              id="rom-file"
              accept=".nds"
              showUploadList={false}
              fileList={[]}
              disabled={busy}
              beforeUpload={(file) => {
                select('rom', file);
                return false;
              }}
            >
              <Button aria-label="选择原始 ROM" disabled={busy}>
                ...
              </Button>
            </Upload>
          </Flex>
        </Form.Item>
        <Form.Item label="补丁包" htmlFor="patch-name">
          <Flex gap={8}>
            <Input id="patch-name" readOnly value={patch?.name ?? ''} />
            <Upload
              id="patch-file"
              accept=".xzp,.zip"
              showUploadList={false}
              fileList={[]}
              disabled={busy}
              beforeUpload={(file) => {
                select('patch', file);
                return false;
              }}
            >
              <Button aria-label="选择补丁包" disabled={busy}>
                ...
              </Button>
            </Upload>
          </Flex>
        </Form.Item>
        <Form.Item label="输出 ROM" htmlFor="output-name">
          <Input
            id="output-name"
            value={outputName}
            onChange={(event) => {
              setOutputName(event.target.value);
              engine.reset();
            }}
            aria-label="输出 ROM 文件名"
          />
        </Form.Item>
        <Flex justify="center">
          <Button
            type="primary"
            htmlType="submit"
            aria-label="开始"
            aria-busy={busy}
            autoInsertSpace={false}
            loading={busy}
            disabled={!rom || !patch || !engine.ready || busy || !outputName.trim()}
            style={{ minWidth: 112 }}
          >
            开始
          </Button>
        </Flex>
      </Form>
      {error && <Alert style={{ marginTop: 20 }} type="error" title={`错误：${error}`} showIcon />}
      {engine.result && (
        <Flex vertical gap={12} style={{ marginTop: 20 }}>
          <Alert
            type={engine.result.mismatch ? 'warning' : 'success'}
            title={
              engine.result.mismatch
                ? '已完成，但是原始 ROM 的 MD5 校验失败，可能是因为使用了错误的原始 ROM。'
                : '已完成。'
            }
            showIcon
          />
          <div className="checksums">
            <Typography.Text className="checksum-label">原始 ROM 的 MD5：</Typography.Text>
            <Typography.Text className="checksum-value">{engine.result.inputMd5}</Typography.Text>
            <Typography.Text className="checksum-label">生成 ROM 的 MD5：</Typography.Text>
            <Typography.Text className="checksum-value">{engine.result.outputMd5}</Typography.Text>
          </div>
          <Button href={engine.result.url} download={engine.result.name}>
            下载 ROM
          </Button>
        </Flex>
      )}
      <Divider style={{ margin: '20px 0 12px' }} />
      <Flex justify="space-between">
        <Typography.Text type="secondary">作者：Xzonn</Typography.Text>
        <Typography.Text type="secondary" role="status">
          {busy ? '正在处理…' : engine.ready ? '就绪' : '正在准备…'}
        </Typography.Text>
      </Flex>
    </main>
  );
};
