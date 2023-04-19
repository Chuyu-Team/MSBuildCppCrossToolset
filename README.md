# 关于 MSBuildCppCrossToolset - 在Linux、MacOS中编译vcxproj
[![license](https://img.shields.io/github/license/Chuyu-Team/MSBuildCppCrossToolset)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/blob/master/LICENSE)
![downloads](https://img.shields.io/github/downloads/Chuyu-Team/MSBuildCppCrossToolset/total)
[![contributors](https://img.shields.io/github/contributors-anon/Chuyu-Team/MSBuildCppCrossToolset)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/graphs/contributors)
[![release](https://img.shields.io/github/v/release/Chuyu-Team/MSBuildCppCrossToolset?include_prereleases)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases)
[![Build](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/actions/workflows/Build.yml/badge.svg)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/actions/workflows/Build.yml)

重要提示：VCTargets中的文件从 `Microsoft Visual Studio\?\?\MSBuild\Microsoft\VC` 提取，Microsoft拥有其所有权利。

重要提示：本项目还未完工……

本项目基于微软VCTargets修改，为MSBuild实现了跨平台编译vcxproj。相关VS配置高度抽象统一，并与目标编译器功能一比一映射。

> 举个例子：代码完全优化（Full选项），微软编译器时中映射为 `-Ox`，而使用GCC时则映射为`-O3`。

未来开发计划：
* [ ] 解决增量编译不生效问题。
* [ ] 优化并行生成效率。
* [ ] 单元测试。

# 1. 兼容性
## 1.1. 兼容的操作系统
* Linux
* MacOS（计划）

> 注意：不支持Windows，因为Windows下微软直接支持。

## 1.2. 兼容的编译器
| 编译器名称     | 对应的平台工具集
| -------------- | -----------
| GCC            | YY_Cross_GCC_1_0
| Clang          | YY_Cross_Clang_1_0

> 由于这些编译工具将作为平台工具集实现，因此使用时需要给对应的vcxproj配置平台工具集（`PlatformToolset`）。


## 1.3. 兼容的平台
* ARM
* ARM64
* MIPS
* x64(AMD64)
* x86

# 2. 使用方式
# 2.1. 安装 .NET SDK
下载地址：https://dotnet.microsoft.com/zh-cn/download

> 必须选择 .NET 6.0或者更高版本。

## 2.2. 配置 MSBuildCppCrossToolset
首先，我们从[Release](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases)产物下载MSBuildCppCrossToolset并解压。

假设最终解压目录是`/home/john/Desktop/VCTargets`。我们执行下面这条命令：

```
export VCTargetsPath=/home/john/Desktop/VCTargets/v170/
```

它将临时添加`VCTargetsPath`环境变量，如果需要持久配置，请自行修改系统配置。

## 2.3. 编译vcxproj项目
假设项目位置： `/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj`。并且编译 Release x86，那么可以输入如下命令：

```
dotnet msbuild '/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj' -p:Configuration=Release;Platform=x86
```

> 温馨提示：`PlatformToolset` 必须从 1.2. 兼容的编译器小节中对应的平台工具集中选择，比如说想用GCC就设置`YY_Cross_GCC_1_0`。

# 3. 我怎么自己编译 MSBuildCppCrossToolset？
> 温馨提示：普通用户无需关心如何编译 MSBuildCppCrossToolset。只需要从Release产物中下载即可。

注意：编译MSBuildCppCrossToolset需要安装`.NET 6.0 SDK`。

假设MSBuildCppCrossToolset项目源代码存放在: `D:\MSBuildCppCrossToolset`
```
# 也可以使用 dotnet CLI编译
msbuild D:\MSBuildCppCrossToolset\Build.proj
```

执行成功后，Release目录就是输出产物。
