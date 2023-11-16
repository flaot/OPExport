using System;
using System.Collections.Generic;

namespace OpExport
{
    public class LibOP
    {
        /// <summary> 导出OP函数的前缀名称 </summary>
        public const string Prefix = "OP_";
        /// <summary> 插入的对象名称 </summary>
        public const string ObjName = "_op";
        /// <summary> 插入的字符串指针名称 </summary>
        public const string PStr = "_pStr";
        /// <summary> 插入的字符串指针长度名称 </summary>
        public const string PStrSize = "_nSize";
        /// <summary> 导出函数需要的对象创建函数名称 </summary>
        public const string CreateFunc = "OP_CreateOP";
        /// <summary> 导出函数需要的对象释放函数名称 </summary>
        public const string ReleaseFunc = "OP_ReleaseOP";

        /// <summary> 导出的函数(OP_XXX) </summary>
        public List<Method> functions;
        /// <summary> op.h定义的成员函数 </summary>
        private List<Method> _methods;

        /// <summary>
        /// 根据函数名称查找该函数在DLL中的方法定义
        /// </summary>
        /// <param name="functionName">导出的函数名称(OP_XXX)</param>
        /// <returns>op.h定义的成员函数</returns>
        public Method MethodByFunction(string functionName)
        {
            if (functionName == CreateFunc || functionName == ReleaseFunc)
                return null;
            string methodName = functionName.Substring(Prefix.Length);
            return _methods.Find(item => item.name == methodName);
        }

        /// <summary> 读取libop.h文件及com定义文件 </summary>
        private void FullOPLibOP(string libopFile, string idlFile)
        {
            //按个数填充
            var idl = NativeIdl.Parse(idlFile);
            var nativeLibOPMethods = NativeLibOP.Parse(libopFile);
            foreach (var naMethod in nativeLibOPMethods)
            {
                var findIdl = idl.Find(item => item.name.Equals(naMethod.name, StringComparison.OrdinalIgnoreCase));
                if (findIdl == null)
                {
                    Console.WriteLine("[Waring] 未定义在文件'op.idl'中:" + naMethod.ToString());
                    continue;
                }

                if (naMethod.args.Count != findIdl.args.Count)
                {
                    Console.WriteLine("[Waring] 参数与文件'op.idl'不匹配:" + naMethod.ToString());
                    continue;
                }
                for (int i = 0; i < naMethod.args.Count; i++)
                {
                    naMethod.args[i].refType = findIdl.args[i].refType;
                }
            }
            _methods = nativeLibOPMethods;
        }
        /// <summary> 确定传递的引用类型 </summary>
        private void FullReference()
        {
            if (_methods == null) return;
            foreach (var method in _methods)
            {
                foreach (var arg in method.args)
                {
                    if (arg.refType != Reference.None)
                        continue;
                    if (arg.name == "ret" || arg.name == "retstr")
                        arg.refType = Reference.Ret;
                    else if (arg.type.Contains('*'))
                        arg.refType = Reference.InOut;
                    else
                        arg.refType = Reference.In;
                }
            }
        }
        /// <summary>
        /// 将成员函数转换为全局函数
        /// </summary>
        private void FullFunciton()
        {
            if (_methods == null) return;
            functions = _methods.ConvertAll(item => (Method)item.Clone());
            //增加对象参数
            foreach (var func in functions)
            {
                func.name = func.name.Insert(0, Prefix);
                func.args.Insert(0, new Arg()
                {
                    type = "libop*",
                    name = ObjName,
                    annotation = string.Empty,
                });
            }
            //转化C++对象为C支持的类型
            foreach (var func in functions)
            {
                SwtichCPlusPlus(func);
            }
            //补充额外的函数
            functions.Insert(0, new Method()
            {
                returnType = "void",
                returnAnnotation = string.Empty,
                example = string.Empty,
                name = ReleaseFunc,
                annotation = string.Empty,
                args = new List<Arg>() { new Arg() { type = "libop*", name = ObjName, annotation = string.Empty } },
            });
            functions.Insert(0, new Method()
            {
                returnType = "libop*",
                returnAnnotation = string.Empty,
                example = string.Empty,
                name = CreateFunc,
                annotation = string.Empty,
                args = new List<Arg>(),
            });
        }
        /// <summary>
        /// 将C++的类成员函数声明改为C的函数声明
        /// </summary>
        /// <param name="function"></param>
        private void SwtichCPlusPlus(Method function)
        {
            if (function.returnType == "std::wstring")
            {
                function.returnType = "int";
                function.args.Add(new Arg() { type = "wchar_t*", name = PStr, annotation = string.Empty });
                function.args.Add(new Arg() { type = "int", name = PStrSize, annotation = string.Empty });
                return;
            }

            var findArg = function.args.Find(item => item.refType == Reference.Ret);
            if (findArg == null)
                return;

            if (findArg.type == "long*")
            {
                function.returnType = "long";
                if (!string.IsNullOrEmpty(findArg.annotation))
                    function.returnAnnotation = findArg.annotation;
                function.args.Remove(findArg);
                return;
            }
            if (findArg.type == "std::wstring&")
            {
                function.returnType = "int";
                if (!string.IsNullOrEmpty(findArg.annotation))
                    function.returnAnnotation = findArg.annotation;
                function.args.Remove(findArg);
                function.args.Add(new Arg() { type = "wchar_t*", name = PStr, annotation = string.Empty });
                function.args.Add(new Arg() { type = "int", name = PStrSize, annotation = string.Empty });
                return;
            }
            Console.WriteLine("[Error] 未处理的返回类型:" + function.ToString());
        }
        /// <summary>
        /// 填充官网上的注释
        /// </summary>
        private void FullAnnotation()
        {
            if (_methods == null) return;
            var annotations = OpDocument.Document.Generate();
            foreach (var method in _methods)
            {
                var annotation = annotations.Find(item => item.funcName.Equals(method.name, StringComparison.OrdinalIgnoreCase));
                if (annotation == null)
                    continue;

                method.example = annotation.example;
                method.annotation = annotation.funcAnnotation;
                method.returnAnnotation = annotation.returnAnnotation;
                for (int i = 0; i < method.args.Count - 1; i++)
                {
                    var arg = method.args[i];
                    if (i >= annotation.argDescs.Count)
                    {
                        Console.WriteLine("[Warn] 参数个数不匹配，注释可能错误：" + annotation.funcName);
                        break;
                    }
                    arg.annotation = annotation.argDescs[i].annotation;
                }
            }
        }
        private LibOP() { }
        public static LibOP Create(string libopFile, string idlFile)
        {
            LibOP libOP = new LibOP();
            //step.1 读取libop.h文件及com定义文件
            libOP.FullOPLibOP(libopFile, idlFile);

            //step.1.1 填充注释
            if (Options.Inst.Document)
                libOP.FullAnnotation();

            //step.2 自动识别未定义的引用类型
            libOP.FullReference();

            //step.3 根据方法定义填充C函数定义方式的函数
            libOP.FullFunciton();
            return libOP;
        }
    }
}
