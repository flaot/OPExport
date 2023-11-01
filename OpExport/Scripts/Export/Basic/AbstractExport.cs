using System.Linq;
using static OpExport.LibOP;

namespace OpExport.Export
{
    public abstract class AbstractExport : IExport
    {
        /// <summary>
        /// 手动管理OP对象，不由桥接脚本管理(GenerateMethod不回调'LibOP.CreateFunc' && 'LibOP.ReleaseFunc')
        /// </summary>
        protected abstract bool ManuallyOP { get; }
        protected LibOP libOP;
        public void Export(LibOP libOP)
        {
            this.libOP = libOP;
            Start();
            Generate();
            Finish();
        }
        private void Generate()
        {
            if (ManuallyOP)
            {
                foreach (var func in libOP.functions)
                {
                    if (func.name != LibOP.CreateFunc && func.name != LibOP.ReleaseFunc)
                        continue;
                    Method showFunc = (Method)func.Clone();
                    Method dllFunc = (Method)func.Clone();
                    GenerateMethod(showFunc, dllFunc);
                }
            }
            foreach (var func in libOP.functions)
            {
                if (func.name == LibOP.CreateFunc || func.name == LibOP.ReleaseFunc)
                    continue;
                var method = libOP.MethodByFunction(func.name);
                Method showFunc = (Method)method.Clone();
                Method dllFunc = (Method)func.Clone();
                if (showFunc.returnType == "void" && showFunc.args.Count > 0)
                {
                    //将引用传参从参数中调整到返回值
                    var lastArg = showFunc.args.Last();
                    if (lastArg.type == "long*")
                    {
                        showFunc.returnType = "long";
                        if (showFunc.args.Count > 0)
                            showFunc.args.RemoveAt(showFunc.args.Count - 1);
                    }
                    if (lastArg.type == "std::wstring&")
                    {
                        showFunc.returnType = "std::wstring";
                        if (showFunc.args.Count > 0)
                            showFunc.args.RemoveAt(showFunc.args.Count - 1);
                    }
                }
                GenerateMethod(showFunc, dllFunc);
            }
        }

        protected abstract void Start();
        protected abstract void GenerateMethod(Method showFunc, Method dllFunc);
        protected abstract void Finish();
    }
}
