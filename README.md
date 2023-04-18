# 关于 MSBuildCppCrossToolset
重要提示：VCTargets中的文件从 `Microsoft Visual Studio\?\?\MSBuild\Microsoft\VC` 提取，Microsoft拥有其所有权利。
重要提示：本项目还未完工……

本项目基于微软VCTargets修改，为MSBuild实现了跨平台编译vcxproj。

# 1. 兼容性
## 1.1. 兼容的操作系统
* Linux
* MacOS（计划）

> 注意：不支持Windows，因为Windows下微软直接支持。

## 1.2. 兼容的编译器
* GCC - YY_Cross_GCC_1_0
* CLang [正在进行]

> 这些编译工具将作为平台工具集实现。

## 1.3. 兼容的平台
* ARM
* ARM64
* MIPS
* x64(AMD64)
* x86

# 2. 使用方式
# 2.1. 安装 .NET
下载地址：https://dotnet.microsoft.com/zh-cn/download

> 目前我使用的是 .NET 6.0，因为它是长期支持，我不确定安装 7.0 是否兼容我的脚本。

## 2.2. 配置 MSBuildCppCrossToolset
假设 MSBuildCppCrossToolset 下载到的 `/home/john/Desktop/VCTargets`。首先执行下面二条命令：

```
export VCTargetsPath=/home/john/Desktop/VCTargets/v170/
```

它们将临时添加VCTargetsPath、VCTargetsPath17二个环境变量，如果需要持久配置，请自行修改系统配置。

## 2.3. 编译vcxproj项目
假设项目位置： `/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj`。并且编译 Release x86，那么可以输入如下命令：

```
dotnet msbuild '/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj' -p:Configuration=Release;Platform=x86
```

> 温馨提示：`PlatformToolset`必须选择`YY_Cross_GCC_1_0`。
