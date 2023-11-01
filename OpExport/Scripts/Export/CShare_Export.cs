using System.Collections.Generic;
using System.IO;
using static OpExport.LibOP;

namespace OpExport.Export
{
    [Language(LanguageFlags.CShare)]
    public class CShare_Export : AbstractExport
    {
        protected override bool ManuallyOP => false;

        private string writeFile;
        private CodeWriter cw;
        protected override void Start()
        {
            string folder = LanguageFactory.ExportPath("C#/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            writeFile = Path.Combine(folder, "OpSoft.cs");
            cw = new CodeWriter();
            cw.Writeln("using System;");
            cw.Writeln("using System.Runtime.InteropServices;");
            cw.Writeln("public partial class OpSoft: IDisposable, IComparable<OpSoft>");
            cw.StartBlock();
            cw.Writeln("const string DLL_NAME = \"op\";");
            cw.Writeln();
            cw.Writeln($"private IntPtr {LibOP.ObjName}; // 非托管资源");
            cw.Writeln($"private IntPtr {LibOP.PStr} = IntPtr.Zero;");
            cw.Writeln($"private int {LibOP.PStrSize} = 0;");
            cw.Writeln();

            //Dispose
            cw.Writeln("#region Dispose");
            cw.Writeln("private bool disposed = false;   // 是否已经释放资源的标志");
            cw.Writeln("public OpSoft()");
            cw.StartBlock();
            cw.Writeln($"{LibOP.ObjName} = {LibOP.CreateFunc}();");
            cw.EndBlock();
            cw.Writeln("~OpSoft() => Dispose(false);");
            cw.Writeln("protected virtual void Dispose(bool disposing)");
            cw.StartBlock();
            cw.Writeln("if (!this.disposed)");
            cw.StartBlock();
            cw.Writeln("if (disposing)");
            cw.StartBlock();
            cw.EndBlock();
            cw.Writeln($"{LibOP.ReleaseFunc}({LibOP.ObjName});");
            cw.Writeln($"{LibOP.ObjName} = IntPtr.Zero;");
            cw.Writeln($"if ({LibOP.PStrSize} > 0)");
            cw.StartBlock();
            cw.Writeln($"Marshal.FreeHGlobal({LibOP.PStr});");
            cw.Writeln($"{LibOP.PStr} = IntPtr.Zero;");
            cw.Writeln($"{LibOP.PStrSize} = 0;");
            cw.EndBlock();
            cw.EndBlock();
            cw.Writeln("disposed = true;");
            cw.EndBlock();
            cw.Writeln("public void Dispose()");
            cw.StartBlock();
            cw.Writeln("Dispose(true);");
            cw.Writeln("GC.SuppressFinalize(this);");
            cw.EndBlock();
            cw.Writeln("#endregion");

            //Overroid
            cw.Writeln("#region Overroid");
            cw.Writeln("public override bool Equals(object obj) => obj is OpSoft soft && GetID() == soft.GetID();");
            cw.Writeln("public override int GetHashCode() => GetID().GetHashCode();");
            cw.Writeln("public override string ToString() => string.Format(\"id:{0}\", GetID());");
            cw.Writeln("public int CompareTo(OpSoft other) => GetID().CompareTo(other.GetID());");
            cw.Writeln("#endregion");
        }
        protected override void Finish()
        {
            //Define
            cw.Writeln("#region DLL Import Define");
            foreach (var func in libOP.functions)
            {
                List<string> names = func.args.ConvertAll(item => item.name);
                cw.Writeln(string.Format("[DllImport(DLL_NAME, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]", func.name));
                cw.Writeln(string.Format("private static extern {0} {1}({2});", SwtichType(func.returnType), func.name, string.Join(", ", func.args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)))));
                cw.Writeln();
            }
            cw.Writeln("#endregion");
            cw.EndBlock();
            cw.Save(writeFile);
        }

        protected override void GenerateMethod(Method showFunc, Method dllFunc)
        {
            GenerateAnnotation(cw, showFunc);
            showFunc.args.ForEach(item => item.type = SwtichType(item.type));
            string showArgs = string.Join(", ", showFunc.args.ConvertAll(item => string.Format("{0} {1}", item.type, item.name)));
            List<string> callNames = dllFunc.args.ConvertAll(item =>
            {
                var findIndex = showFunc.args.FindIndex(arg => arg.name == item.name);
                if (findIndex < 0) return item.name;
                if (showFunc.args[findIndex].type.StartsWith("ref ")) return "ref " + item.name;
                else return item.name;
            });
            if (showFunc.returnType == "std::wstring")
            {
                string functionName = string.Format("public {0} {1}({2})", SwtichType(showFunc.returnType), showFunc.name, showArgs);
                cw.Writeln(functionName);
                cw.StartBlock();
                string call = string.Format("{0}({1});", dllFunc.name, string.Join(", ", callNames));
                cw.Writeln("int _size = " + call);
                cw.Writeln("if (_size > 0)");
                cw.StartBlock();
                cw.Writeln($"if ({LibOP.PStrSize} > 0) Marshal.FreeHGlobal({LibOP.PStr});");
                cw.Writeln($"{LibOP.PStr} = Marshal.AllocHGlobal({LibOP.PStrSize} = _size);");
                cw.Writeln(call);
                cw.EndBlock();
                cw.Writeln($"string str = Marshal.PtrToStringUni({LibOP.PStr});");
                cw.Writeln("return str;");
                cw.EndBlock();
            }
            else
            {
                string call = string.Format("public {0} {1}({2}) => {3}({4});", SwtichType(showFunc.returnType), showFunc.name, showArgs, dllFunc.name, string.Join(", ", callNames));
                cw.Writeln(call);
            }
        }

        private void GenerateAnnotation(CodeWriter cw, Method func)
        {
            cw.Writeln("/// <summary>");
            foreach (var item in func.annotation.Split('\n'))
                cw.Writeln(string.Format("/// {0}", item));
            cw.Writeln("/// </summary>");

            for (int i = 0; i < func.args.Count; i++)
                cw.Writeln("/// <param name=\"{0}\">{1}</param>", func.args[i].name, func.args[i].annotation.Replace("\n", " "));

            if (!string.IsNullOrEmpty(func.returnAnnotation))
                cw.Writeln("/// <returns>{0}</returns>", func.returnAnnotation.Replace("\n", " "));

            if (string.IsNullOrEmpty(func.example))
                return;
            cw.Writeln("/// <example>");
            cw.Writeln("/// <code>");
            foreach (var item in func.example.Split('\n'))
                cw.Writeln(string.Format("/// {0}", item));
            cw.Writeln("/// </code>");
            cw.Writeln("/// </example>");
        }
        private string SwtichType(string type)
        {
            switch (type)
            {
                case "wchar_t*": return "IntPtr";
                case "libop*": return "IntPtr";
                case "const wchar_t*": return "string";
                case "int*": return "ref int";
                case "long": return "int";
                case "long*": return "ref int";
                case "char*": return "ref string";
                case "size_t*": return "ref IntPtr";
                case "void*": return "IntPtr";
                case "std::wstring": return "string";
                default: return type;
            }
        }
    }
}
