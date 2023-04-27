# MSBuildCppCrossToolset 示例项目

## 使用方法

### 配置工具链

首先，我们从 [GitHub Release](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases) 下载 MSBuildCppCrossToolset 并解压。

假设最终解压目录是 `/home/mouri/Workspace/MSBuildCppCrossToolsetRelease/`。我们执行下面这条命令：

```
export VCTargetsPath=/home/mouri/Workspace/MSBuildCppCrossToolsetRelease/VCTargets/v170/
```

### 编译项目

假设项目位置为 `/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/MSBuildCppCrossToolsetSamples.sln`。并且编译 Release x86，那么可以输入如下命令：

```
dotnet build '/home/mouri/Workspace/MSBuildCppCrossToolsetWorkspace/Samples/MSBuildCppCrossToolsetSamples.sln' '-p:Configuration=Release;Platform=x86'
```

## 示例列表

### [HelloWorldApplication](HelloWorldApplication/main.cpp)

一个简单的向控制台输出一段文本的应用
