# ���� MSBuildCppCrossToolset
��Ҫ��ʾ��VCTargets�е��ļ��� `Microsoft Visual Studio\?\?\MSBuild\Microsoft\VC` ��ȡ��Microsoftӵ��������Ȩ����
��Ҫ��ʾ������Ŀ��δ�깤����

����Ŀ����΢��VCTargets�޸ģ�ΪMSBuildʵ���˿�ƽ̨����vcxproj��

# 1. ������
## 1.1. ���ݵĲ���ϵͳ
* Linux
* MacOS���ƻ���

> ע�⣺��֧��Windows����ΪWindows��΢��ֱ��֧�֡�

## 1.2. ���ݵı�����
* GCC - YY_Cross_GCC_1_0
* CLang [���ڽ���]

> ��Щ���빤�߽���Ϊƽ̨���߼�ʵ�֡�

## 1.3. ���ݵ�ƽ̨
* ARM
* ARM64
* MIPS
* x64(AMD64)
* x86

# 2. ʹ�÷�ʽ
# 2.1. ��װ .NET
���ص�ַ��https://dotnet.microsoft.com/zh-cn/download

> Ŀǰ��ʹ�õ��� .NET 6.0����Ϊ���ǳ���֧�֣��Ҳ�ȷ����װ 7.0 �Ƿ�����ҵĽű���

## 2.2. ���� MSBuildCppCrossToolset
���� MSBuildCppCrossToolset ���ص��� `/home/john/Desktop/VCTargets`������ִ������������

```
export VCTargetsPath=/home/john/Desktop/VCTargets/v170/
```

���ǽ���ʱ���VCTargetsPath��VCTargetsPath17�������������������Ҫ�־����ã��������޸�ϵͳ���á�

## 2.3. ����vcxproj��Ŀ
������Ŀλ�ã� `/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj`�����ұ��� Release x86����ô���������������

```
dotnet msbuild '/home/john/Desktop/ConsoleApplication2/ConsoleApplication2.vcxproj' -p:Configuration=Release;Platform=x86
```

> ��ܰ��ʾ��`PlatformToolset`����ѡ��`YY_Cross_GCC_1_0`��
