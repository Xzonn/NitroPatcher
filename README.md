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

#### Android 版（MAUI）

支持 Android 平台。

理论上最低支持 Android 5.0（API 21），但仅在 Android 12.0（API 31）上进行了测试。

根据图形界面的提示操作即可。

### CLI（命令行界面）

支持 Windows、Linux、macOS 平台。

需要 [.NET 6.0 运行时](https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0)（Runtime）。

用法：

```
NitroPatcherCli 原始ROM 补丁包 输出ROM
```

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
