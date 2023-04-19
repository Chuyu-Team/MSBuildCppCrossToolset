# ���� MSBuildCppCrossToolset - ��Linux��MacOS�б���vcxproj
[![license](https://img.shields.io/github/license/Chuyu-Team/MSBuildCppCrossToolset)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/blob/master/LICENSE)
![downloads](https://img.shields.io/github/downloads/Chuyu-Team/MSBuildCppCrossToolset/total)
[![contributors](https://img.shields.io/github/contributors-anon/Chuyu-Team/MSBuildCppCrossToolset)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/graphs/contributors)
[![release](https://img.shields.io/github/v/release/Chuyu-Team/MSBuildCppCrossToolset?include_prereleases)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases)
[![Build](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/actions/workflows/Build.yml/badge.svg)](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/actions/workflows/Build.yml)

��Ҫ��ʾ��VCTargets�е��ļ��� `Microsoft Visual Studio\?\?\MSBuild\Microsoft\VC` ��ȡ��Microsoftӵ��������Ȩ����

��Ҫ��ʾ������Ŀ��δ�깤����

����Ŀ����΢��VCTargets�޸ģ�ΪMSBuildʵ���˿�ƽ̨����vcxproj�����VS���ø߶ȳ���ͳһ������Ŀ�����������һ��һӳ�䡣

> �ٸ����ӣ�������ȫ�Ż���Fullѡ���΢�������ʱ��ӳ��Ϊ `-Ox`����ʹ��GCCʱ��ӳ��Ϊ`-O3`��

δ�������ƻ���
* [ ] ����������벻��Ч���⡣
* [ ] �Ż���������Ч�ʡ�
* [ ] ��Ԫ���ԡ�

# 1. ������
## 1.1. ���ݵĲ���ϵͳ
* Linux
* MacOS���ƻ���

> ע�⣺��֧��Windows����ΪWindows��΢��ֱ��֧�֡�

## 1.2. ���ݵı�����
| ����������     | ��Ӧ��ƽ̨���߼�
| -------------- | -----------
| GCC            | YY_Cross_GCC_1_0
| Clang          | YY_Cross_Clang_1_0

> ������Щ���빤�߽���Ϊƽ̨���߼�ʵ�֣����ʹ��ʱ��Ҫ����Ӧ��vcxproj����ƽ̨���߼���`PlatformToolset`����


## 1.3. ���ݵ�ƽ̨
* ARM
* ARM64
* MIPS
* x64(AMD64)
* x86

# 2. ʹ�÷�ʽ
# 2.1. ��װ .NET SDK
���ص�ַ��https://dotnet.microsoft.com/zh-cn/download

> ����ѡ�� .NET 6.0���߸��߰汾��

## 2.2. ���� MSBuildCppCrossToolset
���ȣ����Ǵ�[Release](https://github.com/Chuyu-Team/MSBuildCppCrossToolset/releases)��������MSBuildCppCrossToolset����ѹ��

�������ս�ѹĿ¼��`/home/john/Desktop/VCTargets`������ִ�������������

```
export VCTargetsPath=/home/john/Desktop/VCTargets/v170/
```

������ʱ���`VCTargetsPath`���������������Ҫ�־����ã��������޸�ϵͳ���á�

## 2.3. ����vcxproj��Ŀ
������Ŀλ�ã� `/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj`�����ұ��� Release x86����ô���������������

```
dotnet msbuild '/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj' -p:Configuration=Release;Platform=x86
```

> ��ܰ��ʾ��`PlatformToolset` ����� 1.2. ���ݵı�����С���ж�Ӧ��ƽ̨���߼���ѡ�񣬱���˵����GCC������`YY_Cross_GCC_1_0`��

# 3. ����ô�Լ����� MSBuildCppCrossToolset��
> ��ܰ��ʾ����ͨ�û����������α��� MSBuildCppCrossToolset��ֻ��Ҫ��Release���������ؼ��ɡ�

ע�⣺����MSBuildCppCrossToolset��Ҫ��װ`.NET 6.0 SDK`��

����MSBuildCppCrossToolset��ĿԴ��������: `D:\MSBuildCppCrossToolset`
```
# Ҳ����ʹ�� dotnet CLI����
msbuild D:\MSBuildCppCrossToolset\Build.proj
```

ִ�гɹ���ReleaseĿ¼����������
