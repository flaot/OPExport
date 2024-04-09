using System;

namespace OpExport
{
    [Flags]
    public enum LanguageFlags
    {
        /// <summary> OP源码需要加入编译的导出C函数定义文件 </summary>
        OP = 1 << 0,
        /// <summary> OP源码需要加入编译的导出COM接口定义文件 </summary>
        OPCOM = 1 << 1,
        /// <summary> "C语言"的桥接文件 </summary>
        C = 1 << 2,
        /// <summary> "C++语言"的桥接文件 </summary>
        CPlusPlus = 1 << 3,
        /// <summary> "C#"的桥接文件 </summary>
        CShare = 1 << 4,
        /// <summary> "Python"的桥接文件 </summary>
        Python = 1 << 5,
        /// <summary> "Java"的桥接文件 </summary>
        Java = 1 << 6,
        /// <summary> "VB.Net"的桥接文件 </summary>
        VB_NET = 1 << 7,
        /// <summary> 生成所有桥接文件 </summary>
        All = 0x0FFFFFFF,
    }
}
