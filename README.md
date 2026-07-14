Overview
===========
OPExport([OP](https://github.com/WallBreaker2/op) & Export)是一个OP的自定义模版代码生成.

## 功能特色
- 根据libop.h、op.idl及自定义模版生成代码
- 生成调用op_c_api_x86.dll、op_c_api_x64.dll的代码
- 代码上附加[函数说明](https://github.com/WallBreaker2/op/wiki)可由IDE提示

## 社区
- [GitHub Issues](https://github.com/WallBreaker2/op/issues)
- [GitHub Discussions](https://github.com/WallBreaker2/op/discussions)
- QQ 群：`743710486`、`27872381`

## 编译
### 依赖
* [CommandLine.Net](https://github.com/AlexGhiondea/CommandLine)
* [Scriban](https://github.com/scriban/scriban)

### 编译环境
* 操作系统: Visual Studio 2026
* 目标框架：.NET 10.0

### 快速开始
```
OpExport.exe {libop.h} -idl {op.idl} -t Template/CSharp.sbncs -out C#/ -doc true
- libop.h OP项目libop.h文件全路径
- op.idl OP项目op.idl文件全路径(可选)
- t 模版文件
- out 输出目录
- doc 是否添加wiki上的注释
```

## 模版编写说明
以`.sbncs`结尾文件为模版文件，[模版文件语法文档](https://scriban.github.io/docs/)

### 内置对象(libOP)
定义如下
```C#
public class LibOP
{
    // 导出函数(OP_XXX)
    public List<Method> functions;
}
public class Method
{
    public string name;        //函数名称
    public string annotation;  //注释
    public string rtype;       //返回类型
    public string rannotation; //返回类型注释
    public List<Arg> args;     //参数类型及参数名称
    public string example;    //使用示范
}
public class Arg
{
    public string name;        //参数名称
    public string type;        //参数类型
    public int rtype;          //传递引用类型 0-None 1-In 2-Out 3-InOut 4-Ret 5-Hide
    public string annotation;  // 参数注释
}
```

### 内置方法
```ini  
# 根据'dll导出函数名称'获取'原始函数'定义
_func_methodByFunction [funcName:str]
    ret [methodObj:method]

# 在参数列表中移除下标为index的
_func_argsRemoveAt [args:List<Arg>] [index:int]
    ret [void]

# 设置输出文件名
_func_setOutFileName [fileName:str]
    ret [void]
```  

### 输出示例
`libOP.functions` 获取dll导出函数定义
```C  
op_handle OpCreate();  
void OpDestroy(op_handle handle);  
//  给指定的字库中添加一条字库信息  
long OpAddDict(op_handle handle, long idx, const wchar_t* dict_info);  
//  A星算法  
const wchar_t* OpAStarFindPath(op_handle handle, long mapWidth, long mapHeight, const wchar_t* disable_points, long beginX, long beginY, long endX, long endY);  
//  查找符合类名或者标题名的顶层可见窗口  
LONG_PTR OpFindWindow(op_handle handle, const wchar_t* class_name, const wchar_t* title);  
size_t OpGetScreenData(op_handle handle, long x1, long y1, long x2, long y2, long* ret);  
```  

`_func_methodByFunction` 获取原始定义
```C  
//  给指定的字库中添加一条字库信息  
void AddDict(long idx, const wchar_t* dict_info, long* ret);  
long ReadDouble(LONG_PTR hwnd, const wchar_t* address, double* ret);  
//  1.版本号Version  
std::wstring Ver();  
```  
