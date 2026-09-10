# NDS ROM 补丁工具

React + TypeScript + Ant Design 静态页面，参照原版 GUI：原始 ROM、补丁包、输出 ROM 文件名、开始按钮及处理结果。

通过 `nitro-patcher/browser` 在 Web Worker 中处理文件，无后端、无文件上传。完成后由浏览器下载输出 ROM。

```sh
pnpm install --frozen-lockfile
pnpm dev
```

`pnpm dev` 仅提供静态开发预览。`pnpm build` 输出到 `dist/`，可部署到静态文件托管。

`pnpm check` 执行类型、ESLint、Prettier、构建及浏览器测试。首次测试前运行 `pnpm exec playwright install chromium`。

依赖从 npm registry 安装的 `nitro-patcher@0.1.0`。

源码保存在 [Xzonn/NitroPatcher 的 web 分支](https://github.com/Xzonn/NitroPatcher/tree/web)。推送到 `web` 后，GitHub Actions 自动执行检查和浏览器测试，通过后将 `dist/` 更新到 `docs` 分支并触发 GitHub Pages 发布。面向 `web` 的 Pull Request 只执行检查和构建。

GitHub Pages 的发布源为 `docs` 分支的根目录。`public/.nojekyll` 会随构建复制到发布根目录。

在线页面：https://xzonn.github.io/NitroPatcher/
