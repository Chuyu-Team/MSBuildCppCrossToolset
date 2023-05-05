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

目前开发计划：
* [x] [Fea 5](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/issues/5), 添加最小化生成支持。
* [ ] 优化并行生成效率。
* [ ] 单元测试。

# 1. 兼容性

| 兼容的操作系统   | ApplicationType名称 | 支持的PlatformToolset（平台工具集）
| ---------------- | ------------------- | -----------------
| Linux            | Linux               | YY_Cross_GCC_1_0（默认值）、YY_Cross_Clang_1_0
| MacOS            | OSX                 | YY_Cross_GCC_1_0、YY_Cross_Clang_1_0（默认值）

> 温馨提示：Windows系统由自己微软MSVC直接支持，所以这边不提供支持。

一般来说，vcxproj中的`ApplicationType`以及`PlatformToolset`无需设置，MSBuildCppCrossToolset会自动根据运行情况自动适应。
如果你想自行设置，请严格按上述表格中的值配置ApplicationType与PlatformToolset。

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
我们也提供了示例项目，点击查看[Samples](Samples)

一般来说，Platform拥有以下几种可能：
* ARM
* ARM64
* MIPS
* x64
* x86（注意：对于Windows系统下的vcxproj来说叫Win32）


假设项目位置： `/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj`。并且编译 Release版的x86版本，那么可以输入如下命令：

```
; Linux下编译 x86
dotnet msbuild '/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj' '-p:Configuration=Release;Platform=x86'

; Linux下编译 x64
dotnet msbuild '/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj' '-p:Configuration=Release;Platform=x64'


; Windows下编译 x86。新人特别注意了，vcxproj里没有x86，只有Win32！！！
msbuild "D:\ConsoleApplication2\ConsoleApplication2.vcxproj" -p:Configuration=Release;Platform=Win32

; Windows下编译 x64
msbuild "D:\ConsoleApplication2\ConsoleApplication2.vcxproj" -p:Configuration=Release;Platform=x64

```

# 3. 支持的属性以及元素参数映射情况
## 3.1. ClCompile
它描述了C/C++代码编译参数配置情况。

### 3.1.1. CompileAs 属性（枚举）
选择源代码文件的编译语言选项。

| 选项              | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ----------------- | ----------------- | ----------------- | --------          | ------
| Default           |                   |                   |                   | 使用默认语言（`.c` 文件编译为 C代码，`.m` 文件编译为Object-C代码，`.mm` 文件编译为Object-C++），其他统一编译为C++代码。
| CompileAsC        | /TC               | -x c              | -x c              | 编译为 C 代码。
| CompileAsCpp      | /TP               | -x c++            | -x c++            | 编译为 C++ 代码。
| CompileAsObjC     | 不支持            | 不支持            | -x objective-c    | 编译为 Object-C 代码。
| CompileAsObjCpp   | 不支持            | 不支持            | -x objective-c++  | 编译为 Object-C++ 代码。

示例：
```xml
<ClCompile Include="C:\123.cpp">
  <CompileAs>CompileAsC</CompileAs>
</ClCompile>
```

### 3.1.2. AdditionalIncludeDirectories 属性（字符串列表）
指定一个或多个要添加到包括路径的目录；如果有多个目录，请用分号分开。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /I                | -I                | -I

示例：
```xml
<ClCompile>
  <AdditionalIncludeDirectories>C:\CppInlcude;D:\CppInlcude</AdditionalIncludeDirectories>
</ClCompile>
```

### 3.1.3. DebugInformationFormat 属性（枚举）
指定编译器生成的调试信息类型。

| 选项              | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ----------------- | ----------------- | ----------------- | --------          | ------
| None              |                   | -g0               | -g0               | 没有生成调试信息，因此编译可能会更快。
| Minimal           | 不支持            | -g1               | -g1               | 生成最小调试信息。
| FullDebug         | 不支持            | -g2 -gdwarf-2     | -g2 -gdwarf-2     | 生成 DWARF2 调试信息。

示例：
```xml
<ClCompile>
  <DebugInformationFormat>FullDebug</DebugInformationFormat>
</ClCompile>
```

### 3.1.4. ObjectFileName 属性（字符串）
指定重写默认对象文件名的名称；可以是文件名或目录名。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /Fo               | -o                | -o

示例：
```xml
<ClCompile Include="C:\123.cpp">
  <ObjectFileName>123.o</ObjectFileName>
</ClCompile>
```

### 3.1.5. WarningLevel 属性（枚举）
选择编译器对于外部标头中代码错误的严格程度。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| TurnOffAllWarnings | /external:W0      | -w                | -w                | 关闭所有警告。
| Level1             | /external:W1      | -Wall             | -Wall             | 警告等级1。
| Level2             | /external:W2      | -Wall             | -Wall             | 警告等级2。
| Level3             | /external:W3      | -Wall             | -Wall             | 警告等级3。
| Level4             | /external:W4      | -Wall -Wextra     | -Wall -Wextra     | 警告等级4。

示例：
```xml
<ClCompile>
  <WarningLevel>TurnOffAllWarnings</WarningLevel>
</ClCompile>
```

### 3.1.6. TreatWarningAsError 属性（bool）
将所有编译器警告都视为错误。对于新项目，最好在所有编译中使用；对所有警告进行解析可确保将可能难以发现的代码缺陷减至最少。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /WX-              | -Werror           | -Werror
| false             |                   |                   |

示例：
```xml
<ClCompile>
  <TreatWarningAsError>true</TreatWarningAsError>
</ClCompile>
```

### 3.1.7. AdditionalWarning 属性（字符串列表）
开启特定的警告。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | -W                | -W


### 3.1.8. Optimization 属性（枚举）
选择代码优化选项；选择“自定义”可使用特定的优化选项。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| Custom             |                   |                   |                   | 自定义
| Disabled           | /Od               | -O0               | -O0               | 禁用代码优化。
| MinSize            | /O1               | -Os               | -Os               | 针对文件大小进行代码优化。
| MaxSpeed           | /O2               | -O2               | -O2               | 针对速度进行代码优化。
| Full               | /Ox               | -O3               | -O3               | 完全优化，类似于MaxSpeed。

示例：
```xml
<ClCompile>
  <Optimization>Full</Optimization>
</ClCompile>
```

### 3.1.9. StrictAliasing 属性（bool）
假设使用最严格的别名检查规则。一种类型的对象将始终不会被假定驻留在与另一种类型的对象相同的位置。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fstrict-aliasing | -fstrict-aliasing
| false             | 自动忽略          | -fno-strict-aliasing | -fno-strict-aliasing

示例：
```xml
<ClCompile>
  <StrictAliasing>true</StrictAliasing>
</ClCompile>
```

### 3.1.10. UnrollLoops 属性（bool）
Unroll loops to make application faster by reducing number of branches executed at the cost of larger code size.

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -funroll-all-loops | -funroll-all-loops
| false             | 自动忽略          |                    |

示例：
```xml
<ClCompile>
  <UnrollLoops>true</UnrollLoops>
</ClCompile>
```

### 3.1.11. WholeProgramOptimization 属性（bool）
通过允许优化器跨应用程序中的对象文件进行查看，来实现过程间优化。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /GL               | -flto             | -flto
| false             |                   |                   |

示例：
```xml
<ClCompile>
  <WholeProgramOptimization>true</WholeProgramOptimization>
</ClCompile>
```

### 3.1.12. OmitFramePointers 属性（bool）
禁止在调用堆栈上创建帧指针。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /Oy               | -fomit-frame-pointer | -fomit-frame-pointer
| false             | /Oy-              | -fno-omit-frame-pointer | -fno-omit-frame-pointer

示例：
```xml
<ClCompile>
  <OmitFramePointers>true</OmitFramePointers>
</ClCompile>
```

### 3.1.13. NoCommonBlocks 属性（bool）
在对象文件的数据节中分配甚至未初始化的全局变量，而不是以公共块的形式生成它们。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fno-common       | -fno-common
| false             | 自动忽略          |                   |

### 3.1.14. PreprocessorDefinitions 属性（字符串列表）
定义源文件的预处理符号。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /D                | -D                | -D

示例：
```xml
<ClCompile>
  <PreprocessorDefinitions>__VERSION=8848;__VERSION_INFO;%(PreprocessorDefinitions)</PreprocessorDefinitions>
</ClCompile>
```

### 3.1.15. UndefinePreprocessorDefinitions 属性（字符串列表）
指定取消一个或多个预处理器定义。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /U                | -U                | -U

示例：
```xml
<ClCompile>
  <UndefinePreprocessorDefinitions>__VERSION;__VERSION_INFO;%(UndefinePreprocessorDefinitions)</UndefinePreprocessorDefinitions>
</ClCompile>
```

### 3.1.16. UndefineAllPreprocessorDefinitions 属性（bool）
取消以前定义的所有预处理器值。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /u                | -undef            | -undef
| false             |                   |                   |

示例：
```xml
<ClCompile>
  <UndefineAllPreprocessorDefinitions>true</UndefineAllPreprocessorDefinitions>
</ClCompile>
```

### 3.1.17. PositionIndependentCode 属性（bool）
生成位置无关代码(PIC)以便在共享库中使用。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fpic             | -fpic
| false             |                   |                   |

### 3.1.18. ThreadSafeStatics 属性（bool）
发出额外代码以使用 C++ ABI 中指定的例程实现局部静态变量的线程安全初始化。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fthreadsafe-statics | -fthreadsafe-statics
| false             | 自动忽略          | -fno-threadsafe-statics | -fno-threadsafe-statics

### 3.1.19. FloatingPointModel 属性（枚举）
设置浮点模型。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| Precise            | /fp:precise       |                   |                   | 默认值。改进有关相等和不相等的浮点测试的一致性。
| Strict             | /fp:strict        |                   |                   | 最严格的浮点模型。相对性能较低。
| Fast               | /fp:fast          | -ffast-math       | -ffast-math       | 在大多数情况下，创建运行速度最快的代码。

### 3.1.20. HideInlineMethods 属性（bool）
启用时，内联方法的外联副本会声明为“private extern”。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fvisibility-inlines-hidden | -fvisibility-inlines-hidden
| false             | 自动忽略          |                   |

### 3.1.21. SymbolsHiddenByDefault 属性（bool）
所有符号都声明为“private extern”，除非显式标记为使用“__attribute”宏导出。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fvisibility=hidden | -fvisibility=hidden
| false             | 自动忽略          |                   |

### 3.1.22. ExceptionHandling 属性（枚举）
指定将由编译器使用的异常处理模型。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| false              |                   | -fno-exceptions   | -fno-exceptions   | 无异常，禁用异常。
| Async              | /EHa              | -fexceptions      | -fexceptions      | 捕获异步(SEH)和同步(C++)异常的异常处理模型。但是SEH只有Windows有，其他平台自动忽略。
| Sync               | /EHsc             | -fexceptions      | -fexceptions      | 仅捕获 C++ 异常并通知编译器假定 Extern C 函数从不引发 C++ 异常的异常处理模型。
| SyncCThrow         | /EHs              | -fexceptions      | -fexceptions      | 仅捕获 C++ 异常并通知编译器假定 Extern C 函数引发异常的异常处理模型。

示例：
```xml
<ClCompile>
  <ExceptionHandling>Sync</ExceptionHandling>
</ClCompile>
```

### 3.1.23. RuntimeTypeInfo 属性（bool）
添加在运行时检查 C++ 对象类型(运行时类型信息)的代码。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /GR               | -frtti            | -frtti
| false             | /GR-              | -fno-rtti         | -fno-rtti


### 3.1.24. LanguageStandard_C 属性（枚举）
确定编译器将强制执行的 C 语言标准。建议尽可能使用最新版本。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| Default            |                   |                   |                   | 使用编译器默认标准，对于Windows它是旧 MSVC标准（C89 + 微软扩展），而GCC/CLang等价于gnu17标准。
| stdc11             | /std:c11          | -std=c11          | -std=c11          | ISO C11 标准。
| stdc17             | /std:c17          | -std=c17          | -std=c17          | ISO C17 (2018)标准。
| c89                | 不支持            | -std=c89          | -std=c89          | ISO C89 语言标准。
| c99                | 不支持            | -std=c99          | -std=c99          | ISO C99 语言标准。
| c11                | 不支持            | -std=c11          | -std=c11          | ISO C11 标准。
| c17                | 不支持            | -std=c17          | -std=c17          | ISO C17 (2018)标准。
| c2x                | 不支持            | -std=c2x          | -std=c2x          | C89 (GNU Dialect)语言标准。
| gnu89              | 不支持            | -std=gnu89        | -std=gnu89        | C89 (GNU Dialect)语言标准。
| gnu90              | 不支持            | -std=gnu90        | -std=gnu90        | C90 (GNU Dialect)语言标准。
| gnu99              | 不支持            | -std=gnu99        | -std=gnu99        | C99 (GNU Dialect)语言标准。
| gnu11              | 不支持            | -std=gnu11        | -std=gnu11        | C11 (GNU Dialect)语言标准。
| gnu17              | 不支持            | -std=gnu17        | -std=gnu17        | C17 (GNU Dialect)语言标准。

示例：
```xml
<ClCompile>
  <LanguageStandard_C>stdc11</LanguageStandard_C>
</ClCompile>
```

### 3.1.25. LanguageStandard 属性（枚举）
确定编译器将强制执行的 C++ 语言标准。建议尽可能使用最新版本。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| Default            |                   |                   |                   | 使用编译器默认标准，对于Windows它是默认(ISO C++14 标准)，而GCC/CLang等价于gnu++17标准。
| stdcpp14           | /std:c++14        | -std=c++14        | -std=c++14        | ISO C++14 标准。
| stdcpp17           | /std:c++17        | -std=c++17        | -std=c++17        | ISO C++17 标准。
| stdcpp20           | /std:c++20        | -std=c++20        | -std=c++20        | ISO C++20 标准。
| stdcpplatest       | /std:c++latest    | -std=c++2b        | -std=c++2b        | 最新 C++ 工作草案中的功能。不推荐使用。
| c++98              | 不支持            | -std=c++98        | -std=c++98        | C++98 语言标准。
| c++03              | 不支持            | -std=c++03        | -std=c++03        | C++03 语言标准。
| c++11              | 不支持            | -std=c++11        | -std=c++11        | C++11 语言标准。
| c++1y              | 不支持            | -std=c++14        | -std=c++14        | C++14 语言标准。
| c++14              | 不支持            | -std=c++14        | -std=c++14        | C++14 语言标准。（建议使用 stdcpp14
| c++17              | 不支持            | -std=c++17        | -std=c++17        | C++17 语言标准。（建议使用 stdcpp17）
| c++2a              | 不支持            | -std=c++2a        | -std=c++2a        | C++2a 语言标准。
| c++20              | 不支持            | -std=c++20        | -std=c++20        | C++20 语言标准。（建议使用 stdcpp20）
| c++2b              | 不支持            | -std=c++2b        | -std=c++2b        | C++2b 语言标准。（建议使用 stdcpplatest）
| gnu++98            | 不支持            | -std=gnu++98      | -std=gnu++98      | C++98 (GNU Dialect)语言标准。
| gnu++03            | 不支持            | -std=gnu++03      | -std=gnu++03      | C++03 (GNU Dialect)语言标准。
| gnu++11            | 不支持            | -std=gnu++11      | -std=gnu++11      | C++11 (GNU Dialect)语言标准。
| gnu++1y            | 不支持            | -std=gnu++1y      | -std=gnu++1y      | C++1y (GNU Dialect)语言标准。
| gnu++14            | 不支持            | -std=gnu++14      | -std=gnu++14      | C++14 (GNU Dialect)语言标准。
| gnu++1z            | 不支持            | -std=gnu++1z      | -std=gnu++1z      | C++1z (GNU Dialect)语言标准。
| gnu++17            | 不支持            | -std=gnu++17      | -std=gnu++17      | C++17 (GNU Dialect)语言标准。
| gnu++20            | 不支持            | -std=gnu++20      | -std=gnu++20      | C++20 (GNU Dialect)语言标准。
| gnu++2b            | 不支持            | -std=gnu++2b      | -std=gnu++2b      | C++2b (GNU Dialect)语言标准。

示例：
```xml
<ClCompile>
  <LanguageStandard>stdcpp14</LanguageStandard>
</ClCompile>
```
### 3.1.26. ForcedIncludeFiles 属性（字符串列表）
一个或多个要强制的包含文件。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /FI               | -include          | -include

示例：
```xml
<ClCompile>
  <ForcedIncludeFiles>C:\123.h;D:456.h;%(ForcedIncludeFiles)</ForcedIncludeFiles>
</ClCompile>
```

### 3.1.27. EnableASAN 属性（bool）
使用 AddressSanitizer 编译和链接程序。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /fsanitize=address | -fsanitize=address | -fsanitize=address
| false             |                    |                    |

示例：
```xml
<ClCompile>
  <AddressSanitizer>true</AddressSanitizer>
</ClCompile>
```

### 3.1.28. ObjCAutomaticRefCounting 属性（bool）
为Object-C对象开启自动引用技术支持。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | 自动忽略          | -fobjc-arc
| false             | 自动忽略          | 自动忽略          |

示例：
```xml
<ClCompile>
  <ObjCAutomaticRefCounting>true</ObjCAutomaticRefCounting>
</ClCompile>
```

### 3.1.29. ObjCAutomaticRefCountingExceptionHandlingSafe 属性（bool）
ObjCAutomaticRefCounting开启时发生异常保证不泄露内存。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | 自动忽略          | -fobjc-arc-exceptions
| false             | 自动忽略          | 自动忽略          |

示例：
```xml
<ClCompile>
  <ObjCAutomaticRefCountingExceptionHandlingSafe>true</ObjCAutomaticRefCountingExceptionHandlingSafe>
</ClCompile>
```

### 3.1.30. ObjCExceptionHandling 属性（枚举）
为Object-C开启异常支持。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| Disabled           | 自动忽略          | 自动忽略          |                   | Obecjt-C不开启异常。
| Enabled            | 自动忽略          | 自动忽略          | -fobjc-exceptions | Obecjt-C开启异常。

示例：
```xml
<ClCompile>
  <ObjCExceptionHandling>Enabled</ObjCExceptionHandling>
</ClCompile>
```

## 3.2. Link
链接配置。

### 3.2.1. OutputFile 属性（string）
重写链接器创建的程序的默认名称和位置。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /OUT              | -o                | -o

### 3.2.2. ShowProgress 属性(字符串)
打印链接器进度消息。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| NotSet             |                   |                   |                   | 无详细程度。
| LinkVerbose        | /VERBOSE          | -Wl,--verbose     | 自动忽略          | 显示所有进度消息。
| LinkVerboseLib     | /VERBOSE:Lib      | 自动忽略          | 自动忽略          | 显示只指示所搜索的库的进度消息。
| LinkVerboseICF     | /VERBOSE:ICF      | 自动忽略          | 自动忽略          | 显示有关优化链接期间的 COMDAT 折叠的信息。
| LinkVerboseREF     | /VERBOSE:REF      | 自动忽略          | 自动忽略          | 显示有关优化链接期间移除的函数和数据的信息。
| LinkVerboseSAFESEH | /VERBOSE:SAFESEH  | 自动忽略          | 自动忽略          | 显示有关与安全异常处理不兼容的模块的信息 。
| LinkVerboseCLR     | /VERBOSE:CLR      | 自动忽略          | 自动忽略          | 显示有关托管代码相关的链接器活动的信息。

示例：
```xml
<Link>
  <ShowProgress>LinkVerbose</ShowProgress>
</Link>
```

### 3.2.3. TraceSymbols 属性（字符串列表）
打印符号显示在其中的文件列表。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | -Wl,--trace-symbol | -Wl,--trace-symbol

示例：
```xml
<Link>
  <TraceSymbols>main;%(TraceSymbols)</TraceSymbols>
</Link>
```

### 3.2.4. GenerateMapFile 属性（bool）
通知链接器输出链接映射。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -Wl,--print-map   | -Wl,--print-map
| false             | 自动忽略          |                   |

示例：
```xml
<Link>
  <GenerateMapFile>true</GenerateMapFile>
</Link>
```

### 3.2.5. UnresolvedSymbolReferences 属性（bool）
报告未解析的符号引用。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -Wl,--no-undefined | -Wl,-undefined,error
| false             | 自动忽略          |                    |

示例：
```xml
<Link>
  <UnresolvedSymbolReferences>true</UnresolvedSymbolReferences>
</Link>
```

### 3.2.6. OptimizeforMemory 属性（bool）
如有必要，通过重读符号表优化内存使用率。
|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -Wl,--no-keep-memory | -Wl,--no-keep-memory
| false             | 自动忽略          |                    |

示例：
```xml
<Link>
  <OptimizeforMemory>true</OptimizeforMemory>
</Link>
```

### 3.2.7. SharedLibrarySearchPath 属性（字符串列表）
共享库搜索路径。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | -Wl,-L            | -Wl,-L

示例：
```xml
<Link>
  <SharedLibrarySearchPath>C:\123;D:\456;%(SharedLibrarySearchPath)</SharedLibrarySearchPath>
</Link>
```

### 3.2.8. IgnoreSpecificDefaultLibraries 属性（字符串列表）
指定要忽略的一个或多个默认库的名称；用分号分隔多个库。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | /NODEFAULTLIB     | -Wl,--exclude-libs | -Wl,--exclude-libs

示例：
```xml
<Link>
  <IgnoreSpecificDefaultLibraries>123.a;456.a;%(IgnoreSpecificDefaultLibraries)</IgnoreSpecificDefaultLibraries>
</Link>
```

### 3.2.9. ForceUndefineSymbolReferences 属性（字符串列表）
强制将符号作为未定义符号输入在输入文件中。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | -Wl,-u--undefined | -Wl,-u--undefined

示例：
```xml
<Link>
  <ForceUndefineSymbolReferences>main;%(ForceUndefineSymbolReferences)</ForceUndefineSymbolReferences>
</Link>
```

### 3.2.10. DebuggerSymbolInformation 属性（枚举）
输出文件中的调试器符号信息。

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| true               | 自动忽略          |                   |                   | 包含全部符号。
| IncludeAll         | 自动忽略          |                   |                   | 包含全部符号。
| OmitDebuggerSymbolInformation | 自动忽略 | -Wl,--strip-debug | -Wl,--strip-debug | 仅忽略调试器符号信息。
| OmitAllSymbolInformation | 自动忽略    | -Wl,--strip-all   | -Wl,--strip-all   | 忽略所有符号信息。 
```

示例：
```xml
<Link>
  <DebuggerSymbolInformation>true</DebuggerSymbolInformation>
</Link>
```

### 3.2.11. MapFileName 属性（字符串）
让链接器创建具有用户指定名称的映射文件。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | -Wl,-Map          | -Wl,-Map


示例：
```xml
<Link>
  <MapFileName>123.map</MapFileName>
</Link>
```

### 3.2.12. Relocation 属性（bool）
重定位后此选项标记变量为只读。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -Wl,-z,relro      | -Wl,-z,relro
| false             | 自动忽略          | -Wl,-z,norelro    | -Wl,-z,norelro


示例：
```xml
<Link>
  <Relocation>true</Relocation>
</Link>
```

### 3.2.13. FunctionBinding 属性（bool）
此选项标记对象用于即时函数绑定。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -Wl,-z,now        | 自动忽略
| false             | 自动忽略          |                   | 自动忽略

### 3.2.14. NoExecStackRequired 属性（bool）
此选项标记输出为不需要可执行堆栈的输出。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -Wl,-z,noexecstack | 自动忽略
| false             | 自动忽略          |                   | 自动忽略

### 3.2.15. LinkDll 属性（bool）
|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -shared           | -shared
| false             | 自动忽略          |                   |

### 3.2.16. AdditionalDependencies 属性（字符串列表）
指定要添加到链接命令行的附加项。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          |                   |                   |

示例：
```xml
<Link>
  <AdditionalDependencies>C:\1111.a;%(AdditionalDependencies)</AdditionalDependencies>
</Link>
```

### 3.2.17. LibraryDependencies 属性（字符串列表）
此选项允许指定要添加到链接器命令行的其他库。其他库将添加到前缀为“lib”和结尾扩展名为“.a”的链接器命令行的末尾。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | -l                | -l

示例：
```xml
<Link>
  <LibraryDependencies>1111.a;%(LibraryDependencies)</LibraryDependencies>
</Link>
```

### 3.2.18. EnableASAN 属性（bool）
使用 AddressSanitizer 链接程序。还必须使用地址擦除系统选项进行编译。必须使用调试程序运行以查看诊断结果。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | 自动忽略          | -fsanitize=address | -fsanitize=address
| false             | 自动忽略          |                   |

示例：
```xml
<Link>
  <EnableASAN>true</EnableASAN>
</Link>
```

### 3.2.19. UseOfStl 属性（bool）

| 选项               | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)    | 选项含义
| ------------------ | ----------------- | ----------------- | --------          | ------
| libstdc++_shared   | 自动忽略          |                   |                   | 动态使用。
| libstdc++_static   | 自动忽略          | -static-libstdc++ | -static-libstdc++ | 静态使用。

### 3.2.20. LinkStatus 属性（bool）
指定链接器是否应显示进度指示器，它显示完成的链接百分比。默认情况下不显示此状态信息。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| true              | /LTCG:STATUS      | -Wl,--stats       | 自动忽略
| false             | /LTCG:NOSTATUS    |                   |

示例：
```xml
<Link>
  <LinkStatus>true</LinkStatus>
</Link>
```

### 3.2.21. Frameworks 属性（字符串列表）
Apple特有的Framework引用（-framework）。

|                   | Windows(MSVC)     | Linux(GCC/CLang)  | OSX(GCC/CLang)
| ----------------- | ----------------- | ----------------- | --------
| 映射参数          | 自动忽略          | 自动忽略          | -framework

示例：
```xml
<Link>
  <Frameworks>Foundation;Cocoa;%(Frameworks)</Frameworks>
</Link>
```


# 附： 我怎么自己编译 MSBuildCppCrossToolset？
> 温馨提示：普通用户无需关心如何编译 MSBuildCppCrossToolset。只需要从Release产物中下载即可。

注意：编译MSBuildCppCrossToolset需要安装`.NET 6.0 SDK`。

假设MSBuildCppCrossToolset项目源代码存放在: `D:\MSBuildCppCrossToolset`
```
# 也可以使用 dotnet CLI编译
msbuild D:\MSBuildCppCrossToolset\Build.proj
```

执行成功后，Release目录就是输出产物。
