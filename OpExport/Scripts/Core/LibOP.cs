using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private void Parse(string OPHeadfile)
        {
            _methods = new List<Method>();
            string[] lines = File.ReadAllLines(OPHeadfile);
            bool start = false;
            List<string> methodTxt = new List<string>(10);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line == "};")
                    continue;
                if (line.Contains("-------------"))
                    continue;
                //判定对外开放的方法
                int permission = ParsePermission(line);
                if (permission > 0)
                {
                    methodTxt.Clear();
                    start = permission == 1;
                    continue;
                }
                if (!start)
                    continue;

                //获取有关方法的注释及定义
                if (line.StartsWith("//"))
                {
                    line = line.Substring(2); //去掉注释前的两个"//"
                    if (line.Length > 0)
                        methodTxt.Add(line);
                }
                else
                {
                    methodTxt.Add(line);
                    Method method = ParseMethod(methodTxt);
                    if (method != null)
                        _methods.Add(method);
                    methodTxt.Clear();
                }
            }
            _methods.Sort((l, r) => l.name.CompareTo(r.name));   //排个序
        }
        private int ParsePermission(string line)
        {
            string txt = line.Replace(" ", string.Empty);
            switch (txt)
            {
                case "public:": return 1;
                case "private:": return 2;
                case "protected:": return 3;
                default: return 0;
            }
        }
        private Method ParseMethod(List<string> methodTxt)
        {
            string methodDefine = methodTxt[methodTxt.Count - 1];
            methodTxt.RemoveAt(methodTxt.Count - 1);
            int methonStartIndex = methodDefine.IndexOf('(');

            //没有返回值的构造函数等
            int returnTypeSplitIndex = methodDefine.LastIndexOf(' ', methonStartIndex);
            if (methodDefine.LastIndexOf(' ', methonStartIndex) < 0)
                return null;
            //符号重载
            if (methodDefine.IndexOf("operator") >= 0)
                return null;

            Method method = new Method();
            method.returnType = methodDefine.Substring(0, returnTypeSplitIndex).Trim();
            method.returnAnnotation = string.Empty;
            method.example = string.Empty;
            method.annotation = string.Join("\n", methodTxt);
            method.name = methodDefine.Substring(returnTypeSplitIndex, methonStartIndex - returnTypeSplitIndex).Trim();
            method.args = new List<Arg>();
            int findIndex = methonStartIndex + 1;
            char[] findChar = new char[] { ',', ')' };
            while (findIndex > 0 && findIndex < methodDefine.Length)
            {
                int tempIndex = methodDefine.IndexOfAny(findChar, findIndex);
                if (tempIndex < 0)
                    break;
                string argTxt = methodDefine.Substring(findIndex, tempIndex - findIndex);
                findIndex = tempIndex + 1;
                int splitIndex = argTxt.LastIndexOf(' ');
                if (splitIndex < 0) //没有参数
                    break;

                Arg arg = new Arg();
                arg.name = argTxt.Substring(splitIndex).Trim();
                arg.type = argTxt.Substring(0, splitIndex).Trim();
                arg.annotation = string.Empty;
                method.args.Add(arg);

                //Fix:可能未按指针标准写法走，比如：int* a; 写成int *a;
                if (argTxt[splitIndex + 1] == '*')
                {
                    arg.name = arg.name.Substring(1);
                    arg.type += '*';
                }
            }
            return method;
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
            if (function.returnType == "void" && function.args.Count > 0)
            {
                string lastArgType = function.args.Last().type;
                if (lastArgType == "long*")
                {
                    function.returnType = "long";
                    function.args.RemoveAt(function.args.Count - 1);
                    return;
                }
                if (lastArgType == "std::wstring&")
                {
                    function.returnType = "int";
                    function.args.RemoveAt(function.args.Count - 1);
                    function.args.Add(new Arg() { type = "wchar_t*", name = PStr, annotation = string.Empty });
                    function.args.Add(new Arg() { type = "int", name = PStrSize, annotation = string.Empty });
                    return;
                }
            }
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
        public static LibOP Create(string OPHeadfile)
        {
            LibOP libOP = new LibOP();
            libOP.Parse(OPHeadfile);
            if (Options.Inst.Document)
                libOP.FullAnnotation();
            libOP.FullFunciton();  //根据方法定义填充C函数定义方式的函数
            return libOP;
        }

        public class Method : ICloneable
        {
            /// <summary> 方法名称 </summary>
            public string name;
            /// <summary> 方法注释 </summary>
            public string annotation;
            /// <summary> 返回类型 </summary>
            public string returnType;
            /// <summary> 返回类型注释 </summary>
            public string returnAnnotation;
            /// <summary> 参数类型及参数名称 </summary>
            public List<Arg> args;
            /// <summary> 方法使用示范 </summary>
            public string example;

            public object Clone()
            {
                var method = (Method)MemberwiseClone();
                method.args = args.ConvertAll(item => (Arg)item.Clone());
                return method;
            }
            public override string ToString()
            {
                string argTxt = string.Join(", ", args);
                return string.Format("{0} {1}({2})", returnType, name, argTxt);
            }
        }
        public class Arg : ICloneable
        {
            /// <summary> 参数名称 </summary>
            public string name;
            /// <summary> 参数类型 </summary>
            public string type;
            /// <summary> 参数注释 </summary>
            public string annotation;

            public object Clone() => MemberwiseClone();
            public override string ToString()
            {
                return string.Format("{0} {1}", type, name);
            }
        }
    }
}
