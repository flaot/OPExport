using System.IO;
using static OpExport.LibOP;

namespace OpExport.Export
{
    [Language(LanguageFlags.CPlusPlus)]
    public class CPlusPlus_Export : AbstractExport
    {
        protected override bool ManuallyOP => true;
        private string writeHeadFile;
        private string writeSourceFile;
        private CodeWriter cwHead;
        private CodeWriter cwSource;
        protected override void Start()
        {
            string folder = LanguageFactory.ExportPath("C++/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            writeHeadFile = Path.Combine(folder, "libopExport.h");
            writeSourceFile = Path.Combine(folder, "libopExport.cpp");
            cwHead = new CodeWriter();
            cwHead.Writeln("#pragma once");
            cwHead.Writeln("#ifndef LIBOP_EXPORT_H");
            cwHead.Writeln("#define LIBOP_EXPORT_H");
            cwHead.Writeln();
            foreach (var func in libOP.functions)
            {
                cwHead.Writeln(string.Format("typedef {0}(*p{1}Func)({2});", SwtichType(func.returnType), func.name, string.Join(", ", func.args.ConvertAll(item => SwtichType(item.type)))));
            }
            cwHead.Writeln();
            cwHead.Writeln("#include <windows.h>");
            cwHead.Writeln("#include <string>");
            cwHead.Writeln();
            cwHead.Writeln("#undef FindWindow");
            cwHead.Writeln("#undef FindWindowEx");
            cwHead.Writeln("#undef SetWindowText");
            cwHead.Writeln();
            cwHead.Writeln("class libop");
            cwHead.StartBlock();
            cwHead.Writeln("public:");
            cwHead.IncIndent();
            cwHead.Writeln("libop();");
            cwHead.Writeln("~libop();");
            cwHead.Writeln("/* 加载dll成功返回true；反之，为false*/");
            cwHead.Writeln($"static bool LoadDll(const std::wstring &dllPath);");
            cwHead.Writeln("/*主动卸载dll*/");
            cwHead.Writeln($"static void UnLoadDll();");
            cwHead.DecIndent();
            cwHead.Writeln();
            cwHead.Writeln("public:");
            cwHead.IncIndent();

            cwSource = new CodeWriter();
            cwSource.Writeln("#include \"libopExport.h\"");
            cwSource.Writeln();
            cwSource.Writeln("#undef FindWindow");
            cwSource.Writeln("#undef FindWindowEx");
            cwSource.Writeln("#undef SetWindowText");
            cwSource.Writeln();
            cwSource.Writeln("HMODULE libop::_hinst = nullptr;");
            cwSource.Writeln();

            //function：libop
            cwSource.Writeln("libop::libop()");
            cwSource.StartBlock();
            foreach (var func in libOP.functions)
            {
                cwSource.Writeln(string.Format("_{0} = nullptr;", func.name));
            }
            cwSource.Writeln($"{LibOP.PStr} = nullptr;");
            cwSource.Writeln($"{LibOP.PStrSize} = 0;");
            cwSource.Writeln($"{LibOP.ObjName} = {LibOP.CreateFunc}();");
            cwSource.EndBlock();

            //function：~libop
            cwSource.Writeln("libop::~libop()");
            cwSource.StartBlock();
            cwSource.Writeln($"{LibOP.ReleaseFunc}({LibOP.ObjName});");
            cwSource.Writeln($"{LibOP.ObjName} = nullptr;");
            cwSource.Writeln($"if({LibOP.PStrSize} > 0)");
            cwSource.StartBlock();
            cwSource.Writeln($"free({LibOP.PStr});");
            cwSource.Writeln($"{LibOP.PStr} = nullptr;");
            cwSource.Writeln($"{LibOP.PStrSize} = 0;");
            cwSource.EndBlock();
            cwSource.EndBlock();

            //function：LoadDll
            cwSource.Writeln($"bool libop::LoadDll(const std::wstring &dllPath)");
            cwSource.StartBlock();
            cwSource.Writeln("_hinst = LoadLibraryW(dllPath.c_str());");
            cwSource.Writeln("if (nullptr == _hinst)");
            cwSource.IncIndent();
            cwSource.Writeln("return false;");
            cwSource.DecIndent();
            cwSource.Writeln("else");
            cwSource.IncIndent();
            cwSource.Writeln("return true;");
            cwSource.DecIndent();
            cwSource.EndBlock();

            //function：UnLoadDll
            cwSource.Writeln($"void libop::UnLoadDll()");
            cwSource.StartBlock();
            cwSource.Writeln("FreeLibrary(_hinst);");
            cwSource.Writeln("_hinst = nullptr;");
            cwSource.EndBlock();
        }
        protected override void Finish()
        {
            cwHead.DecIndent();
            cwHead.Writeln();
            //特殊
            cwHead.Writeln("private:");
            cwHead.IncIndent();
            cwHead.Writeln($"void* {LibOP.ObjName};");
            cwHead.Writeln($"wchar_t* {LibOP.PStr};");
            cwHead.Writeln($"int {LibOP.PStrSize};");
            cwHead.Writeln($"static HMODULE _hinst;");
            cwHead.Writeln();
            foreach (var func in libOP.functions)
            {
                cwHead.Writeln(string.Format("p{0}Func _{0};", func.name));
            }
            cwHead.DecIndent();
            cwHead.Rawln("};");
            cwHead.Writeln("#endif // !LIBOP_EXPORT_H");
            cwHead.Save(writeHeadFile);

            cwSource.Save(writeSourceFile);
        }
        protected override void GenerateMethod(Method showFunc, Method dllFunc)
        {
            GenerateAnnotation(cwHead, showFunc);
            if (showFunc.returnType == "std::wstring")
            {
                var args = showFunc.args.ConvertAll(item => (Arg)item.Clone());
                string argTxt = string.Join(", ", args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)));
                cwHead.Writeln(string.Format("{0} {1}({2});", SwtichType(showFunc.returnType), showFunc.name, argTxt));
            }
            else
            {
                cwHead.Writeln(string.Format("{0};", FuncDefine(showFunc, false)));
            }
            cwHead.Writeln();


            if (showFunc.returnType == "std::wstring")
            {
                var args = showFunc.args.ConvertAll(item => (Arg)item.Clone());
                string argTxt = string.Join(", ", args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)));
                cwSource.Writeln(string.Format("std::wstring libop::{0}({1})", showFunc.name, argTxt));
                cwSource.StartBlock();
                cwSource.Writeln(string.Format("if(_{0} == nullptr)", dllFunc.name));
                cwSource.IncIndent();
                cwSource.Writeln(string.Format("_{0} = (p{0}Func)GetProcAddress(_hinst, \"{0}\");", dllFunc.name));
                cwSource.DecIndent();
                string call = string.Format("_{0}({1});", dllFunc.name, string.Join(", ", dllFunc.args.ConvertAll(item => item.name)));
                cwSource.Writeln("int _size = " + call);
                cwSource.Writeln("if (_size > 0)");
                cwSource.StartBlock();
                cwSource.Writeln($"if ({LibOP.PStrSize} > 0) free({LibOP.PStr});");
                cwSource.Writeln($"{LibOP.PStr} = (wchar_t *)malloc({LibOP.PStrSize} = _size);");
                cwSource.Writeln(call);
                cwSource.EndBlock();
                cwSource.Writeln($"std::wstring wstr = {LibOP.PStr};");
                cwSource.Writeln("return wstr;");
            }
            else
            {
                string argTxt = string.Join(", ", showFunc.args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)));
                cwSource.Writeln(string.Format("{0} libop::{1}({2})", SwtichType(showFunc.returnType), showFunc.name, argTxt));
                cwSource.StartBlock();
                cwSource.Writeln(string.Format("if(_{0} == nullptr)", dllFunc.name));
                cwSource.IncIndent();
                cwSource.Writeln(string.Format("_{0} = (p{0}Func)GetProcAddress(_hinst, \"{0}\");", dllFunc.name));
                cwSource.DecIndent();
                string call = string.Format("_{0}({1});", dllFunc.name, string.Join(", ", dllFunc.args.ConvertAll(item => item.name)));
                if (dllFunc.returnType != "void")
                    call = call.Insert(0, "return ");
                cwSource.Writeln(call);
            }
            cwSource.EndBlock();
        }
        private void GenerateAnnotation(CodeWriter cw, Method func)
        {
            cw.Writeln("/**\\brief {0}", func.annotation);

            for (int i = 0; i < func.args.Count; i++)
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
        private string FuncDefine(Method method, bool define = true)
        {
            string argTxt = string.Join(", ", method.args.ConvertAll(item => string.Format("{0} {1}", SwtichType(item.type), item.name)));
            if (define)
                return string.Format("{0} libop::{1}({2})", SwtichType(method.returnType), method.name, argTxt);
            else
                return string.Format("{0} {1}({2})", SwtichType(method.returnType), method.name, argTxt);
        }
    }
}
