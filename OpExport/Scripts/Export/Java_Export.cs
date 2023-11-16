using System.Collections.Generic;
using System.IO;

namespace OpExport.Export
{
    [Language(LanguageFlags.Java)]
    public class Java_Export : AbstractExport
    {
        protected override bool ManuallyOP => false;

        private const string Inst = "_inst";
        private string writeFile;
        private CodeWriter cw;
        protected override void Start()
        {
            string folder = LanguageFactory.ExportPath("Java/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            writeFile = Path.Combine(folder, "OpSoft.java");
            cw = new CodeWriter(blockStart: " {", blockFromNewLine: false);
            cw.Writeln("import com.sun.jna.*;");
            cw.Writeln("import com.sun.jna.ptr.IntByReference;");
            cw.Writeln();
            cw.Writeln("public class OpSoft implements AutoCloseable, Comparable<OpSoft>");
            cw.StartBlock();
            cw.Writeln("private final String DLL_NAME = \"op\";");
            cw.Writeln($"private CLibrary {Inst} = Native.load(DLL_NAME, CLibrary.class);");
            cw.Writeln();
            cw.Writeln($"private Pointer {LibOP.ObjName};");
            cw.Writeln($"private Pointer {LibOP.PStr} = Pointer.NULL;");
            cw.Writeln($"private int {LibOP.PStrSize} = 0;");
            cw.Writeln();

            //OpSoft()
            cw.Writeln("public OpSoft()");
            cw.StartBlock();
            cw.Writeln($"{LibOP.ObjName} = {Inst}.{LibOP.CreateFunc}();");
            cw.EndBlock();
            cw.Writeln();

            //finalize
            cw.Writeln("@Override");
            cw.Writeln("protected void finalize()");
            cw.StartBlock();
            cw.Writeln("close();");
            cw.EndBlock();
            cw.Writeln();

            //close
            cw.Writeln("@Override");
            cw.Writeln("public void close()");
            cw.StartBlock();
            cw.Writeln($"if ({LibOP.ObjName} != Pointer.NULL)");
            cw.StartBlock();
            cw.Writeln($"{Inst}.OP_ReleaseOP({LibOP.ObjName});");
            cw.Writeln($"{LibOP.ObjName} = Pointer.NULL;");
            cw.EndBlock();
            cw.Writeln($"if ({LibOP.PStrSize} > 0)");
            cw.StartBlock();
            cw.Writeln($"Native.free(Pointer.nativeValue({LibOP.PStr}));");
            cw.Writeln($"Pointer.nativeValue({LibOP.PStr}, 0);");
            cw.Writeln($"{LibOP.ObjName} = Pointer.NULL;");
            cw.Writeln($"{LibOP.PStrSize} = 0;");
            cw.EndBlock();
            cw.Writeln($"{Inst} = null;");
            cw.EndBlock();
            cw.Writeln();

            //hashCode
            cw.Writeln("@Override");
            cw.Writeln("public int hashCode()");
            cw.StartBlock();
            cw.Writeln("return Integer.valueOf(GetID()).hashCode();");
            cw.EndBlock();
            cw.Writeln();

            //equals
            cw.Writeln("@Override");
            cw.Writeln("public boolean equals(Object obj)");
            cw.StartBlock();
            cw.Writeln("if (obj == null) return false;");
            cw.Writeln("if (obj == this) return true;");
            cw.Writeln("if (obj.getClass() != getClass()) return false;");
            cw.Writeln("OpSoft other = (OpSoft) obj;");
            cw.Writeln("return GetID() == other.GetID();");
            cw.EndBlock();
            cw.Writeln();

            //toString
            cw.Writeln("@Override");
            cw.Writeln("public String toString()");
            cw.StartBlock();
            cw.Writeln("return String.format(\"id:{%d}\", GetID());");
            cw.EndBlock();
            cw.Writeln();

            //compareTo
            cw.Writeln("@Override");
            cw.Writeln("public int compareTo(OpSoft o)");
            cw.StartBlock();
            cw.Writeln("return Integer.compare(GetID(), o.GetID());");
            cw.EndBlock();
            cw.Writeln();
        }
        protected override void Finish()
        {
            //Define
            cw.Writeln("private interface CLibrary extends Library");
            cw.StartBlock();
            foreach (var func in libOP.functions)
            {
                cw.Writeln();
                List<string> names = func.args.ConvertAll(item => item.name);
                cw.Writeln(string.Format("{0} {1}({2});", SwtichType(func.returnType), func.name,
                    string.Join(", ", func.args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)))));
            }
            cw.EndBlock();
            cw.EndBlock();
            cw.Save(writeFile);
        }
        protected override void GenerateMethod(Method showFunc, Method dllFunc)
        {
            showFunc.args.ForEach(item => item.type = SwtichType(item.type));
            List<string> switchStrList = new List<string>();
            List<string> showArgs = showFunc.args.ConvertAll(item =>
            {
                if (item.type == "WString")
                {
                    switchStrList.Add(item.name);
                    return string.Format("String {0}", item.name);
                }
                return string.Format("{0} {1}", item.type, item.name);
            });
            List<string> callNames = dllFunc.args.ConvertAll(item =>
            {
                var findIndex = showFunc.args.FindIndex(arg => arg.name == item.name);
                if (findIndex < 0) return item.name;
                if (showFunc.args[findIndex].type == "WString") return "_w" + item.name;
                else return item.name;
            });

            cw.Writeln();
            GenerateAnnotation(cw, showFunc);
            if (showFunc.returnType == "std::wstring")
            {
                string call = string.Format("public {0} {1}({2})", SwitchReturnType(showFunc.returnType), showFunc.name, string.Join(", ", showArgs));
                cw.Writeln(call);
                cw.StartBlock();
                foreach (var swItemName in switchStrList)
                {
                    cw.Writeln(string.Format("WString _w{0} = new WString({0});", swItemName));
                }
                call = string.Format("{2}.{0}({1});", dllFunc.name, string.Join(", ", callNames), Inst);
                cw.Writeln("int _size = " + call);
                cw.Writeln("if (_size > 0)");
                cw.StartBlock();
                cw.Writeln($"if ({LibOP.PStrSize} > 0)");
                cw.StartBlock();
                cw.Writeln($"Native.free(Pointer.nativeValue({LibOP.PStr}));");
                cw.Writeln($"Pointer.nativeValue({LibOP.PStr}, 0);");
                cw.EndBlock();
                cw.Writeln($"_pStr = new Memory({LibOP.PStrSize} = _size);");
                cw.Writeln(call);
                cw.EndBlock();
                cw.Writeln($"String wideString = {LibOP.PStr}.getWideString(0);");
                cw.Writeln("return wideString;");
                cw.EndBlock();
            }
            else
            {
                cw.Writeln(string.Format("public {0} {1}({2})", SwitchReturnType(showFunc.returnType), showFunc.name, string.Join(", ", showArgs)));
                cw.StartBlock();
                foreach (var swItemName in switchStrList)
                {
                    cw.Writeln(string.Format("WString _w{0} = new WString({0});", swItemName));
                }
                cw.Writeln(string.Format("return {2}.{0}({1});", dllFunc.name, string.Join(", ", callNames), Inst));
                cw.EndBlock();
            }
        }
        private void GenerateAnnotation(CodeWriter cw, Method func)
        {
            cw.Writeln("/**");
            foreach (var item in func.annotation.Split('\n'))
                cw.Writeln(string.Format(" *  {0}", item));
            cw.Writeln(" * ");

            for (int i = 0; i < func.args.Count; i++)
                cw.Writeln(" * @param {0} {1}", func.args[i].name, func.args[i].annotation.Replace("\n", " "));

            if (!string.IsNullOrEmpty(func.returnAnnotation))
                cw.Writeln(" * @return {0}", func.returnAnnotation.Replace("\n", " "));

            cw.Writeln(" */");
        }
        private string SwtichType(string type)
        {
            switch (type)
            {
                case "wchar_t*": return "Pointer";
                case "libop*": return "Pointer";
                case "const wchar_t*": return "WString";
                case "int*": return "IntByReference";
                case "long": return "int";
                case "long*": return "IntByReference";
                case "char*": return "Pointer";
                case "size_t*": return "Pointer";
                case "void*": return "Pointer";
                case "std::wstring": return "WString";
                default: return type;
            }
        }
        private string SwitchReturnType(string type)
        {
            string reault = SwtichType(type);
            if (reault == "WString")
                return "String";
            else
                return reault;
        }
    }
}
