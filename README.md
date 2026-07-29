# NitroPatcher

NDS ROM 补丁工具。基于 [NitroHelper](https://github.com/Xzonn/NitroHelper)。

其实质是通过替换 NDS ROM 所包含的文件来实现汉化、修改等功能。本工具支持 [Xdelta](https://en.wikipedia.org/wiki/Xdelta) 补丁。

替代[《宝可梦》第四、第五世代汉化修正工具](https://github.com/Xzonn/PCTRTools) 中的补丁应用工具。

## 应用补丁
### GUI（图形化界面）
#### Windows 版（WPF）

支持 Windows 平台。

需要 [.NET Framework 4.7](https://dotnet.microsoft.com/zh-cn/download/dotnet-framework/net47) 或更高版本的运行时（Runtime）。Windows 10/11 应该已经预装了 .NET Framework 4.7。Windows 7 可能需要安装 [KB976932](https://www.catalog.update.microsoft.com/Search.aspx?q=KB976932)、[KB4503548](https://www.catalog.update.microsoft.com/Search.aspx?q=KB4503548) 更新。

根据图形界面的提示操作即可，或者参照 [视频教程](https://www.bilibili.com/video/BV1oH1xYXEdb/t=69)。

#### Windows/Android/macOS 版（MAUI）
[<img src="https://get.microsoft.com/images/zh-cn%20dark.svg" width="200"/>](https://apps.microsoft.com/detail/9NPLXRZ04F04?mode=direct)

支持 Windows、Android 和 Apple 芯片 Mac。

Windows 平台需从 [Microsoft Store](https://www.microsoft.com/store/apps/9NPLXRZ04F04) 下载安装，需要 Windows 10 17763.0 或更高版本。

Android 平台理论上最低支持 Android 5.0（API 21），但仅在 Android 12.0（API 31）上进行了测试。

Apple 芯片版支持 macOS 11 或更高版本。从 GitHub Release 下载 `NitroPatcherMAUI.<版本>-macos-arm64.zip`，解压后将 `NitroPatcherMaui.app` 移入“应用程序”目录即可。正式发布包使用 Developer ID 签名并经过 Apple 公证，不需要安装 Rosetta 或 .NET 运行时。

根据图形界面的提示操作即可。

### CLI（命令行界面）

支持 Windows、Linux、macOS 平台。

Windows、Linux 和 Intel Mac 版本需要 [.NET 6.0 运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0)（Runtime）。`NitroPatcherCli.<版本>-osx-arm64.zip` 是 Apple 芯片自包含版本，不需要另行安装 .NET 或 Rosetta。

用法：

```
NitroPatcherCli 原始ROM 补丁包 输出ROM
```

## 构建 Apple 芯片版本

需要 Apple 芯片 Mac、Xcode、.NET 8 SDK 和 `maui-maccatalyst` workload。无发布签名的本地构建命令：

```sh
dotnet workload install maui-maccatalyst
scripts/build-macos.sh
```

推送版本标签时，GitHub Actions 会构建、签名、公证并将 macOS GUI 和 CLI 包加入同一个草稿 Release。仓库需要配置以下 Actions Secrets：

- `APPLE_CERTIFICATE_BASE64`：Developer ID Application 证书及私钥的 P12 文件，Base64 编码。
- `APPLE_CERTIFICATE_PASSWORD`：P12 密码。
- `APPLE_SIGNING_IDENTITY`：完整的 Developer ID Application 签名身份。
- `APPLE_PROVISIONING_PROFILE_BASE64`：Mac Catalyst Developer ID provisioning profile，Base64 编码。
- `APPLE_API_KEY_BASE64`：App Store Connect API 私钥，Base64 编码。
- `APPLE_API_KEY_ID`：API Key ID。
- `APPLE_API_ISSUER_ID`：API Issuer ID。

发布任务会执行 `codesign`、Gatekeeper 评估及公证票据验证；任一步失败都不会创建不完整的正式发布。

## 创建补丁
### 基本用法

使用 [ndstool](https://github.com/devkitPro/ndstool) 分别提取原始 ROM 和修改后的 ROM 的文件系统，例如：

```
ndstool -x example.nds -9 "arm9.bin" -7 "arm7.bin" -y9 "overarm9.bin" -y7 "overarm7.bin" -d "data" -y "overlay" -t "banner.bin" -h "header.bin"
```

然后筛选出具有差异的文件，将所有文件打包为 `.zip` 压缩包（需注意 arm9.bin 等文件需要在根目录下）。

### 额外功能
#### 添加 MD5 校验码

可在补丁包的根目录下创建名为 `md5.txt` 的文件，在文件中包含原始 ROM 的 MD5 校验码。在应用补丁时，补丁工具会自动计算原始 ROM 和修改后的 ROM 的 MD5 校验码，并与 `md5.txt` 中的校验码进行比对。

如果有原始 ROM 有多个版本（例如，对未汉化的 ROM 和已汉化的 ROM 使用同一个补丁），可以在 `md5.txt` 中添加多个校验码，每行一个。以“#”为首的行及空行会被忽略。

[这里提供了一个示例](https://github.com/Xzonn/PokemonChineseTranslationRevise/blob/9f0632f22c3982bd2ce541c6c902dee54bd0db1a/original_files/DP/D/md5.txt)。

#### 使用 Xdelta 补丁

可以在补丁包的根目录下创建名为 `xdelta` 的文件夹，然后计算原始 ROM 和修改后的 ROM 之间的差异，并将差异文件保存到 `xdelta` 文件夹中。例如，若对 `data/some/sub/folder/pack.pak` 这个文件计算了 Xdelta 补丁，则补丁位置应为 `xdelta/data/some/sub/folder/pack.pak`。

此功能对于 ROM 中包含较大文件（例如开发商自定义的打包格式）的情况非常有用。

[这里提供了一个批量计算脚本](https://github.com/Xzonn/EO3ChsLocalization/blob/8517972a855d1243886d59dcad3596eae832c424/scripts/create_xdelta.py)。

## 授权协议

本项目使用 [GNU General Public License v3.0](LICENSE) 授权。请阅读 `LICENSE` 获取更多信息。
