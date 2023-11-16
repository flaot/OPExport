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

                var findArg = showFunc.args.Find(item => item.refType == Reference.Ret);
                if (findArg != null)
                {
                    if (findArg.type == "long*")
                    {
                        showFunc.returnType = "long";
                        if (!string.IsNullOrEmpty(findArg.annotation))
                            showFunc.returnAnnotation = findArg.annotation;
                        showFunc.args.Remove(findArg);
                    }
                    if (findArg.type == "std::wstring&")
                    {
                        showFunc.returnType = "std::wstring";
                        if (!string.IsNullOrEmpty(findArg.annotation))
                            showFunc.returnAnnotation = findArg.annotation;
                        showFunc.args.Remove(findArg);
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
