Overview
===========
OPExport([OP](https://github.com/WallBreaker2/op) & Export)是一个OP的桥接代码生成.主要功能有:根据libop.h生成C、C#、Java、Python语言等按照LoadLibrary的方式调用的桥接代码。使用c#编写，源代码可跨平台。避免重复封装及避免某些情况下com接口调用不适用的情况

OPExport是为生成OP在特定语言下的桥接(胶水)代码的工具，不局限于各种语言调用OP、甚至可以生成OP中需要定义的COM导出接口。它可以帮助开发者和用户实现编写自由，不用重复编写(封装/定义)接口，例如在Java中使用Com接口则需要包装一层才可调用等。它适用于各种语言场景，例如脚本语言、面向对象语言等。
## 功能特色
- 生成通过LoadLibrary调用DLL的代码
- 代码上附加[函数说明](https://github.com/WallBreaker2/op/wiki)可由IDE提示
- 高可扩展性，添加教程查看末尾

## 交流
* QQ group:743710486
* [Discussion](https://github.com/WallBreaker2/op/discussions)

## 编译
### 依赖
* [CommandLine.Net](https://github.com/AlexGhiondea/CommandLine)

### 编译环境
* 操作系统: Visual Studio 2022
* 目标框架：.NET 7.0
 
### 快速开始
下面以添加生成GO语言的桥接文件为例子
1. 在```LanguageFlags.cs```中添加语言标识GO
2. 在```Export```文件夹创建```GO_Export.cs```文件及其类定义，需要继承自```AbstractExport```
3. 在```GO_Export```类上方添加特性```[Language(LanguageFlags.Go)]```
4. 参考其他已定义好的Export文件及GO语言特性完成编写
5. 使用命令"```OpExport.exe {Path}/libop.h -lang GO -doc true```"后在exe同级目录查看生成文件
