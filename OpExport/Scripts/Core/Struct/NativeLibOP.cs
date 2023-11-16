using System.Collections.Generic;
using System.IO;

namespace OpExport
{
    public class NativeLibOP : IParser<List<Method>>
    {
        /// <summary> op.h定义的成员函数 </summary>
        private List<Method> _methods;

        private NativeLibOP() { }

        public static List<Method> Parse(string file_libop_h)
        {
            NativeLibOP libOP = new NativeLibOP();
            libOP.Parse2(file_libop_h);
            return libOP._methods;
        }

        private void Parse2(string OPHeadfile)
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
    }
}
