using System.Text;

namespace OpDefine
{
    public class NativeCLibOP : IParser<List<Method>>
    {
        private const string EXPORT_HEAD = "OP_C_API";
        private const string FUNC_DEFEND_HEAD = "OP_CALL";

        /// <summary> op_c_api.h定义的成员函数 </summary>
        private List<Method> _methods;

        private NativeCLibOP() { }
        public static List<Method> Parse(string file_op_c_api)
        {
            NativeCLibOP idl = new NativeCLibOP();
            idl.Parse2(file_op_c_api);
            return idl._methods;
        }
        private void Parse2(string op_c_api_file)
        {
            _methods = new List<Method>();
            string[] lines = File.ReadAllLines(op_c_api_file);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith('#')) //忽略宏
                    continue;
                //判定对外开放的方法
                if (!ParsePermission(line))
                    continue;

                //函数定义存在多行
                sb.Clear();
                int bracketStack = 0;
                for (int pIndex = i; pIndex < lines.Length; pIndex++)
                {
                    for (int tIndex = 0; tIndex < lines[pIndex].Length; tIndex++)
                    {
                        var ch = lines[pIndex][tIndex];
                        if (ch == '(')
                            bracketStack++;
                        if (ch == ')')
                            bracketStack--;
                        sb.Append(ch);
                    }
                    if (bracketStack == 0)
                    {
                        i = pIndex;
                        break;
                    }
                }
                //解析函数定义
                Method method = ParseMethod(sb.ToString().Trim());
                if (method != null)
                    _methods.Add(method);
            }
            _methods.Sort((l, r) => l.name.CompareTo(r.name));   //排个序
        }
        private bool ParsePermission(string line)
        {
            return line.StartsWith($"{EXPORT_HEAD} ", StringComparison.Ordinal);
        }
        private Method ParseMethod(string methodDefine)
        {
            methodDefine = methodDefine.Substring(EXPORT_HEAD.Length + 1).TrimStart();
            int methonStartIndex = methodDefine.IndexOf('(');

            //没有返回值的构造函数等
            int returnTypeSplitIndex = methodDefine.LastIndexOf(' ', methonStartIndex);
            if (returnTypeSplitIndex < 0)
                return null;

            Method method = new Method();
            method.rtype = methodDefine.Substring(0, returnTypeSplitIndex).Replace(FUNC_DEFEND_HEAD, string.Empty).Trim();
            method.rannotation = string.Empty;
            method.example = string.Empty;
            method.annotation = string.Empty;
            method.name = methodDefine.Substring(returnTypeSplitIndex, methonStartIndex - returnTypeSplitIndex).Trim();
            method.args = new List<Arg>();
            int findIndex = methonStartIndex + 1;
            while (findIndex > 0 && findIndex < methodDefine.Length)
            {
                int tempIndex = FindNextArgStartIndex(methodDefine, findIndex);
                if (tempIndex < 0)
                    break;
                string argTxt = methodDefine.Substring(findIndex, tempIndex - findIndex);
                findIndex = tempIndex + 1;

                //分离变量名称
                int splitIndex = argTxt.LastIndexOf(' ');
                if (splitIndex < 0) //没有参数
                    break;
                string argName = argTxt.Substring(splitIndex).Trim();
                argTxt = argTxt.Substring(0, splitIndex).Trim();

                Arg arg = new Arg();
                arg.name = argName;
                arg.type = argTxt;
                arg.annotation = string.Empty;
                method.args.Add(arg);

                //Fix:可能未按指针标准写法走，比如：int* a; 写成int *a;
                if (arg.name[0] == '*')
                {
                    arg.name = arg.name.Substring(1);
                    arg.type += '*';
                }
            }
            return method;
        }
        private int FindNextArgStartIndex(string methodDefine, int startIndex)
        {
            bool start = false;
            int findIndex = -1;
            int bracketStack = 0;
            while (findIndex < 0 && startIndex < methodDefine.Length)
            {
                var ch = methodDefine[startIndex];
                if (ch == '[')
                    start = true;
                if (ch == ']')
                    start = false;
                if (ch == '(')
                    ++bracketStack;
                if (!start && bracketStack == 0 && (ch == ',' || ch == ')'))
                    findIndex = startIndex;
                if (ch == ')')
                    --bracketStack;
                ++startIndex;
            }
            return findIndex;
        }
    }
}
