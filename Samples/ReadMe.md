# MSBuildCppCrossToolset 示例项目

## 示例列表
首先，我们从 [GitHub Release](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases) 下载 MSBuildCppCrossToolset 并解压。

假设最终解压目录是：`/home/mouri/Workspace/MSBuildCppCrossToolset/`。

### [HelloWorld----C++](HelloWorld----C++/HelloWorldApplication.vcxproj)
一个简单的向控制台输出一段文本的应用，支持Windows、Linux以及MacOS。
* 假设项目位置为： `/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples`。
* MSBuildCppCrossToolset二进制产物解压到： `/home/mouri/Workspace/MSBuildCppCrossToolset/`。

```
; 设置 MSBuildCppCrossToolset 目录
export VCTargetsPath=/home/mouri/Workspace/MSBuildCppCrossToolset/VCTargets/v170/

; Linux下编译 x86
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----C++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=x86'

; Linux、MacOS下编译 x64
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----C++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=x64'

; Linux、MacOS下编译 ARM64
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----C++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=ARM64'

```


### [HelloWorld----ObjectC++](HelloWorld----ObjectC++/HelloWorldApplication.vcxproj)
使用`ObjectC++`输出一段文本的应用，注意这个示例仅支持MacOS。
* 假设项目位置为： `/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/`。
* MSBuildCppCrossToolset二进制产物解压到： `/home/mouri/Workspace/MSBuildCppCrossToolset/`。

```
; 设置 MSBuildCppCrossToolset 目录
export VCTargetsPath=/home/mouri/Workspace/MSBuildCppCrossToolset/VCTargets/v170/

; MacOS下编译 x64
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----ObjectC++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=x64'

; MacOS下编译 ARM64
dotnet msbuild '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/HelloWorld----ObjectC++/HelloWorldApplication.vcxproj' '-p:Configuration=Release;Platform=ARM64'

```
