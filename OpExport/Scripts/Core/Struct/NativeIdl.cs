using System;
using System.Collections.Generic;
using System.IO;

namespace OpExport
{
    public class NativeIdl : IParser<List<Method>>
    {
        /// <summary> op.idl定义的成员函数 </summary>
        private List<Method> _methods;

        private NativeIdl() { }
        public static List<Method> Parse(string file_op_idl)
        {
            NativeIdl idl = new NativeIdl();
            idl.Parse2(file_op_idl);
            return idl._methods;
        }
        private void Parse2(string OPIdlfile)
        {
            _methods = new List<Method>();
            string[] lines = File.ReadAllLines(OPIdlfile);
            List<string> methodTxt = new List<string>(10);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith("//")) //被注释的行
                    continue;
                //判定对外开放的方法
                if (!ParsePermission(line))
                    continue;

                methodTxt.Add(line);
                Method method = ParseMethod(methodTxt);
                if (method != null)
                    _methods.Add(method);
                methodTxt.Clear();
            }
            _methods.Sort((l, r) => l.name.CompareTo(r.name));   //排个序
        }
        private bool ParsePermission(string line)
        {
            return line.StartsWith("[id(");
        }
        private Method ParseMethod(List<string> methodTxt)
        {
            string methodDefine = methodTxt[methodTxt.Count - 1];
            int tempSplitIndex = methodDefine.IndexOf(']');
            methodDefine = methodDefine.Substring(tempSplitIndex + 1).Trim();
            int methonStartIndex = methodDefine.IndexOf('(');

            //没有返回值的构造函数等
            int returnTypeSplitIndex = methodDefine.LastIndexOf(' ', methonStartIndex);
            if (methodDefine.LastIndexOf(' ', methonStartIndex) < 0)
                return null;

            Method method = new Method();
            method.returnType = methodDefine.Substring(0, returnTypeSplitIndex).Trim();
            method.returnAnnotation = string.Empty;
            method.example = string.Empty;
            method.annotation = string.Empty;
            method.name = methodDefine.Substring(returnTypeSplitIndex, methonStartIndex - returnTypeSplitIndex).Trim();
            method.args = new List<Arg>();
            int findIndex = methonStartIndex + 1;
            char[] findChar = new char[] { ',', ')' };
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

                //分离变量类型与引用类型
                string argType = string.Empty;
                string argRef = string.Empty;
                splitIndex = argTxt.LastIndexOf(']');
                if (splitIndex < 0) //没有注明引用类型
                {
                    argType = argTxt;
                }
                else
                {
                    argType = argTxt.Substring(splitIndex + 1).Trim();
                    argRef = argTxt.Substring(0, splitIndex + 1).Trim();
                }

                Arg arg = new Arg();
                arg.name = argName;
                arg.type = argType;
                arg.refType = PauseReference(argRef);
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
        private int FindNextArgStartIndex(string methodDefine, int startIndex)
        {
            bool start = false;
            int findIndex = -1;
            while (findIndex < 0 && startIndex < methodDefine.Length)
            {
                var ch = methodDefine[startIndex];
                if (ch == '[')
                    start = true;
                if (ch == ']')
                    start = false;
                if (!start && (ch == ',' || ch == ')'))
                    findIndex = startIndex;
                ++startIndex;
            }
            return findIndex;
        }
        private Reference PauseReference(string str)
        {
            if (string.IsNullOrEmpty(str))
                return Reference.In;

            str = str.Substring(1, str.Length - 2);
            string[] refTypes = str.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (Array.Exists(refTypes, "retval".Equals))
                return Reference.Ret;

            Reference reference = Reference.None;
            foreach (var item in refTypes)
            {
                if (item.Equals("out"))
                    reference |= Reference.Out;
                if (item.Equals("in"))
                    reference |= Reference.In;
            }
            return reference;
        }
    }
}
