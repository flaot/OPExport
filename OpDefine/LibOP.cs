using System.Text;

namespace OpDefine
{
    public class LibOP
    {
        /// <summary> 导出OP函数的前缀名称 </summary>
        public const string Prefix = "Op";
        /// <summary> 插入的对象名称 </summary>
        public const string ObjName = "handle";
        /// <summary> OP对象类型 </summary>
        private const string ObjType = "op_handle";
        /// <summary> 导出函数需要的对象创建函数名称 </summary>
        public const string CreateFunc = "OpCreate";
        /// <summary> 导出函数需要的对象释放函数名称 </summary>
        public const string ReleaseFunc = "OpDestroy";

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
            return (Method)_methods.Find(item => item.name == methodName).Clone();
        }

        /// <summary>
        /// 解析op函数定义
        /// </summary>
        /// <param name="libopFile">libop.h文件路径</param>
        /// <param name="idlFile">com定义文件(idl)路径, 额外补充参数引用类型</param>
        /// <returns>成功返回true；反之为false</returns>
        private bool FullOPLibOP(string libopFile, string idlFile)
        {
            //解析'libop.h'文件
            var nativeLibOPMethods = NativeLibOP.Parse(libopFile);
            //解析'op.idl'文件补充参数 in out 说明
            if (!string.IsNullOrEmpty(idlFile))
            {
                var idl = NativeIdl.Parse(idlFile);
                FullReferenceByIDLFile(nativeLibOPMethods, idl);
            }
            //识别返回值类型
            FullReferenceByRet(nativeLibOPMethods);
            //自动识别未定义的引用类型
            FullReferenceByAuto(nativeLibOPMethods);
            //检查是否还有未知引用类型
            if (IsUnknownReference(nativeLibOPMethods))
                return false;
            _methods = nativeLibOPMethods;
            return true;
        }
        private static void FullReferenceByIDLFile(List<Method> methods, List<Method> idl)
        {
            foreach (var naMethod in methods)
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
                    if (naMethod.args[i].rtype == Reference.None)
                        naMethod.args[i].rtype = findIdl.args[i].rtype;
                }
            }
        }
        private static void FullReferenceByRet(List<Method> methods)
        {
            string[] retStrs = new string[] { "ret", "retstr", "bret", "retjson", "ret_str", "rettitle" };
            foreach (var method in methods)
            {
                for (int i = 0; i < method.args.Count; i++)
                {
                    var arg = method.args[i];
                    if (Array.Exists(retStrs, arg.name.Equals))
                        arg.rtype = Reference.Ret;
                }
            }

            //这两个返回图片数据
            foreach (var method in methods)
            {
                if (method.name == "GetScreenData" ||
                    method.name == "GetScreenDataBmp")
                {
                    var arg = method.args.Find(item => item.name == "data");
                    arg.rtype = Reference.Ret;
                    arg = method.args.Find(item => item.name == "ret");
                    arg.rtype = Reference.Out;
                }
            }

            //为内存读取添加返回值
            foreach (var method in methods)
            {
                if (method.name == "ReadDouble" ||
                    method.name == "ReadFloat" ||
                    method.name == "ReadInt")
                {
                    method.rtype = "long";
                    method.args[method.args.Count - 1].rtype = Reference.Out;
                }
            }

            //末尾存在两个返回值,并且倒数第二个为retjson，则隐藏末尾参数
            foreach (var method in methods)
            {
                if (method.args.Count <= 0)
                    continue;
                if (method.args.Count < 2)
                    continue;
                var lastArg1 = method.args[method.args.Count - 1];
                var lastArg2 = method.args[method.args.Count - 2];

                if (lastArg1.rtype == Reference.Ret && lastArg2.name == "retjson" &&
                    lastArg2.rtype == Reference.Ret)
                    lastArg1.rtype = Reference.Hide;
            }

            //末尾存在两个返回值，则将倒数第二个为函数返回值
            foreach (var method in methods)
            {
                if (method.args.Count <= 0)
                    continue;
                if (method.args.Count < 2)
                    continue;
                var lastArg1 = method.args[method.args.Count - 1];
                var lastArg2 = method.args[method.args.Count - 2];
                if (lastArg1.rtype == Reference.Hide)
                    continue;

                if (lastArg1.rtype == Reference.Ret &&
                    lastArg2.rtype == Reference.Ret)
                {
                    lastArg1.rtype = Reference.Out;
                }
            }
        }
        private static void FullReferenceByAuto(List<Method> methods)
        {
            foreach (var method in methods)
            {
                for (int i = 0; i < method.args.Count; i++)
                {
                    var arg = method.args[i];
                    if (arg.rtype != Reference.None)
                        continue;
                    if (arg.type.StartsWith("const ", StringComparison.Ordinal))
                    {
                        arg.rtype = Reference.In;
                        continue;
                    }
                    if (arg.type[arg.type.Length - 1] != '*' && arg.type[arg.type.Length - 1] != '&')
                    {
                        arg.rtype = Reference.In;
                        continue;
                    }
                }
            }
        }
        private static bool IsUnknownReference(List<Method> methods)
        {
            bool reault = false;
            foreach (var naMethod in methods)
            {
                for (int i = 0; i < naMethod.args.Count; i++)
                {
                    var arg = naMethod.args[i];
                    if (arg.rtype == Reference.None)
                    { 
                        Console.WriteLine($"[Error] {naMethod.name}.{arg.name}:引用类型未知");
                        reault = true;
                    }
                }
            }
            foreach (var naMethod in methods)
            {
                if (naMethod.args.Count(item => item.rtype == Reference.Ret) > 1)
                {
                    Console.WriteLine($"[Error] {naMethod.name}:存在多个返回值");
                    reault = true;
                }
            }
            return reault;
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
                bool isVer = func.name == "Ver";
                func.name = func.name.Insert(0, Prefix);
                if (isVer) continue;
                func.args.Insert(0, new Arg()
                {
                    type = ObjType,
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
                rtype = "void",
                rannotation = string.Empty,
                example = string.Empty,
                name = ReleaseFunc,
                annotation = string.Empty,
                args = new List<Arg>() { new Arg() { type = ObjType, name = ObjName, annotation = string.Empty } },
            });
            functions.Insert(0, new Method()
            {
                rtype = ObjType,
                rannotation = string.Empty,
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
            //OPVer函数
            if (function.rtype == "std::wstring")
            {
                function.rtype = "const wchar_t*";
                return;
            }

            var findArg = function.args.Find(item => item.rtype == Reference.Ret);
            if (findArg == null)
                return;

            //处理参数转返回类型
            bool isHandle = false;
            if (!isHandle && findArg.type == "std::wstring&")
            {
                function.rtype = "const wchar_t*";
                isHandle = true;
            }
            if (!isHandle && findArg.type.EndsWith('*'))
            {
                function.rtype = findArg.type.Substring(0, findArg.type.Length - 1);
                isHandle = true;
            }
            if (isHandle)
            {
                if (!string.IsNullOrEmpty(findArg.annotation))
                    function.rannotation = findArg.annotation;
                function.args.Remove(findArg);
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
                method.rannotation = annotation.returnAnnotation;
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
        public static LibOP Create(string libopFile, string idlFile, bool fullDocument)
        {
            LibOP libOP = new LibOP();
            //读取libop.h文件及com定义文件
            if (!libOP.FullOPLibOP(libopFile, idlFile))
                return null;

            //填充注释
            if (fullDocument)
                libOP.FullAnnotation();

            //step.3 根据方法定义填充C函数定义方式的函数
            libOP.FullFunciton();
            return libOP;
        }
    }
}
