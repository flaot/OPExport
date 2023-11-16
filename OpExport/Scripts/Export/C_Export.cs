using System.IO;

namespace OpExport.Export
{
    [Language(LanguageFlags.C)]
    public class C_Export : IExport
    {
        public void Export(LibOP libOP)
        {
            string folder = LanguageFactory.ExportPath("C/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string headFile = Path.Combine(folder, "libopExport.h");
            string sourceFile = Path.Combine(folder, "libopExport.c");

            CreateHead(libOP, headFile);
            CreateSource(libOP, sourceFile);
        }
        private void CreateHead(LibOP libOP, string file)
        {
            CodeWriter cw = new CodeWriter();
            cw.Writeln("#ifndef LIBOP_EXPORT_H");
            cw.Writeln("#define LIBOP_EXPORT_H");
            cw.Writeln();
            cw.Writeln("#undef FindWindow");
            cw.Writeln("#undef FindWindowEx");
            cw.Writeln("#undef SetWindowText");
            cw.Writeln();
            //特殊
            cw.Writeln("/* 加载dll成功返回1；反之，为0*/");
            cw.Writeln($"int {LibOP.Prefix}LoadDll(wchar_t *dllPath);");
            cw.Writeln("/*主动卸载dll*/");
            cw.Writeln($"void {LibOP.Prefix}UnLoadDll();");
            cw.Writeln();

            foreach (var func in libOP.functions)
            {
                GenerateAnnotation(cw, func);
                cw.Writeln(string.Format("{0};", FuncDefine(func)));
            }
            cw.Writeln("#endif // !LIBOP_EXPORT_H");
            cw.Save(file);
        }
        private void CreateSource(LibOP libOP, string file)
        {
            CodeWriter cw = new CodeWriter();
            cw.Writeln("#include \"libopExport.h\"");
            cw.Writeln("#include <windows.h>");
            cw.Writeln();
            cw.Writeln("#undef FindWindow");
            cw.Writeln("#undef FindWindowEx");
            cw.Writeln("#undef SetWindowText");
            cw.Writeln();
            cw.Writeln("HMODULE hinst = NULL;");
            cw.Writeln();

            foreach (var func in libOP.functions)
            {
                cw.Writeln(string.Format("typedef {0}(*p{1}Func)({2});", SwtichType(func.returnType), func.name, string.Join(", ", func.args.ConvertAll(item => SwtichType(item.type)))));
            }
            foreach (var func in libOP.functions)
            {
                cw.Writeln(string.Format("p{0}Func _{0} = NULL;", func.name));
            }
            cw.Writeln();

            //function：LoadDll
            cw.Writeln($"int {LibOP.Prefix}LoadDll(wchar_t *dllPath)");
            cw.StartBlock();
            cw.Writeln("hinst = LoadLibraryW(dllPath);");
            cw.Writeln("if (NULL == hinst)");
            cw.IncIndent();
            cw.Writeln("return 0;");
            cw.DecIndent();
            cw.Writeln("else");
            cw.IncIndent();
            cw.Writeln("return 1;");
            cw.DecIndent();
            cw.EndBlock();

            //function：UnLoadDll
            cw.Writeln($"void {LibOP.Prefix}UnLoadDll()");
            cw.StartBlock();
            cw.Writeln("FreeLibrary(hinst);");
            cw.Writeln("hinst = NULL;");
            foreach (var func in libOP.functions)
            {
                cw.Writeln(string.Format("_{0} = NULL;", func.name));
            }
            cw.EndBlock();

            foreach (var func in libOP.functions)
            {
                cw.Writeln(FuncDefine(func));
                cw.StartBlock();
                do
                {
                    cw.Writeln(string.Format("if(_{0} == NULL)", func.name));
                    cw.IncIndent();
                    cw.Writeln(string.Format("_{0} = (p{0}Func)GetProcAddress(hinst, \"{0}\");", func.name));
                    cw.DecIndent();
                    string call = string.Format("_{0}({1});", func.name, string.Join(", ", func.args.ConvertAll(item => item.name)));
                    if (func.returnType != "void")
                        call = call.Insert(0, "return ");
                    cw.Writeln(call);
                } while (false);
                cw.EndBlock();
            }
            cw.Save(file);
        }
        private void GenerateAnnotation(CodeWriter cw, Method func)
        {
            cw.Writeln("/**\\brief {0}", func.annotation);

            for (int i = 0; i < func.args.Count - 1; i++)
                cw.Writeln("*\\param {0} {1}", func.args[i].name, func.args[i].annotation.Replace("\n", " "));

            if (!string.IsNullOrEmpty(func.returnAnnotation))
                cw.Writeln("*\\return {0} {1}", func.returnType, func.returnAnnotation);

            //if (!string.IsNullOrEmpty(func.example))
            //    cw.Writeln("*\\example {0}", func.example);

            cw.Writeln("*/");
        }
        private string SwtichType(string type)
        {
            switch (type)
            {
                case "libop*": return "void*";
                default: return type;
            }
        }
        private string FuncDefine(Method method)
        {
            string argTxt = string.Join(", ", method.args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)));
            return string.Format("{0} {1}({2})", SwtichType(method.returnType), method.name, argTxt);
        }
    }
}
