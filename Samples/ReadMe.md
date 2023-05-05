# MSBuildCppCrossToolset 示例项目

## 使用方法

### 配置工具链

首先，我们从 [GitHub Release](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases) 下载 MSBuildCppCrossToolset 并解压。

假设最终解压目录是 `/home/mouri/Workspace/MSBuildCppCrossToolsetRelease/`。我们执行下面这条命令：

```
export VCTargetsPath=/home/mouri/Workspace/MSBuildCppCrossToolsetRelease/VCTargets/v170/
```

### 编译项目
一般来说，Platform拥有以下几种可能：
* ARM
* ARM64
* MIPS
* x64
* x86（注意：对于Windows系统下的vcxproj来说叫Win32）

> 温馨提示：Windows下搞不懂x86还是Win32的可以先尝试Win32，如果报错那么尝试一下x86。

; 假设项目位置为 `/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples`。并且编译一些 Release，那么可以输入如下命令：

```
; Linux、MacOS下编译 x86
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----C++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=x86'

; Linux、MacOS下编译 x64
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----C++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=x64'

; Linux、MacOS下编译 ARM64
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----C++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=ARM64'

```

我们也可以在Windows平台编译`HelloWorldApplication.vcxproj`，命令如下：
```
; Windows下编译 x86，注意它叫 Win32！
msbuild "D:\MSBuildCppCrossToolsetWorkspace\Samples\HelloWorld----C++\HelloWorldApplication.vcxproj" -p:Configuration=Release;Platform=Win32

; Windows下编译 x64
msbuild "D:\MSBuildCppCrossToolsetWorkspace\Samples\HelloWorld----C++\HelloWorldApplication.vcxproj" -p:Configuration=Release;Platform=x64

; Windows下编译 ARM64
msbuild "D:\MSBuildCppCrossToolsetWorkspace\Samples\HelloWorld----C++\HelloWorldApplication.vcxproj" -p:Configuration=Release;Platform=ARM64

```

## 示例列表

### [HelloWorld----C++](HelloWorld----C++/HelloWorldApplication.vcxproj)

一个简单的向控制台输出一段文本的应用，支持Windows、Linux以及MacOS。


### [HelloWorld----ObjectC++](HelloWorld----ObjectC++/HelloWorldApplication.vcxproj)

使用`ObjectC++`输出一段文本的应用，注意这个示例仅支持MacOS。
