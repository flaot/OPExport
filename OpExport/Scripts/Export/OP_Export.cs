using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpExport.Export
{
    [Language(LanguageFlags.OP)]
    public class OP_Export : AbstractExport
    {
        protected override bool ManuallyOP => false;

        private string sourceFile;
        private CodeWriter cw;
        protected override void Start()
        {
            string folder = LanguageFactory.ExportPath("OP/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            //.h文件
            string headFile = Path.Combine(folder, "libopExport.h");
            CodeWriter cwHead = new CodeWriter();
            cwHead.Writeln("#pragma once");
            cwHead.Writeln("#ifndef LIBOP_EXPORT_H");
            cwHead.Writeln("#define LIBOP_EXPORT_H");
            cwHead.Writeln();
            cwHead.Writeln("#include \"libop.h\"");
            cwHead.Writeln();
            cwHead.Writeln("#define DLLAPT extern \"C\" __declspec(dllexport)");
            cwHead.Writeln();
            foreach (var func in libOP.functions)
                cwHead.Writeln(string.Format("DLLAPT {0};", func));
            cwHead.Writeln("#endif // !LIBOP_EXPORT_H");
            cwHead.Save(headFile);

            //.cpp文件
            sourceFile = Path.Combine(folder, "libopExport.cpp");
            cw = new CodeWriter();
            cw.Writeln("#include \"libopExport.h\"");
            cw.Writeln();

            cw.Writeln($"libop* {LibOP.CreateFunc}()");
            cw.StartBlock();
            cw.Writeln("return new libop();");
            cw.EndBlock();

            cw.Writeln($"void {LibOP.ReleaseFunc}(libop* {LibOP.ObjName})");
            cw.StartBlock();
            cw.Writeln(string.Format("delete {0};", LibOP.ObjName));
            cw.EndBlock();
        }
        protected override void Finish()
        {
            cw.Save(sourceFile);
        }

        protected override void GenerateMethod(Method showFunc, Method dllFunc)
        {
            var method = libOP.MethodByFunction(dllFunc.name);
            cw.Writeln(dllFunc.ToString());
            cw.StartBlock();
            if (method.returnType == "std::wstring")
            {
                string call = string.Format("std::wstring wstr = {0}->{1}({2});", LibOP.ObjName, method.name, string.Join(", ", method.args.ConvertAll(item => item.name)));
                cw.Writeln(call);
                cw.Writeln($"if ({LibOP.PStr} == nullptr || {LibOP.PStrSize} <= (int)(wstr.length() * sizeof(wchar_t)))");
                cw.IncIndent();
                cw.Writeln("return (int)(wstr.length() + 1) * sizeof(wchar_t);");
                cw.DecIndent();
                cw.Writeln($"wcscpy_s({LibOP.PStr}, {LibOP.PStrSize} / sizeof(wchar_t), wstr.c_str());");
                cw.Writeln("return 0;");
                cw.EndBlock();
            }
            else if (showFunc.returnType == "std::wstring") //有改变返回值位置
            {
                var lastArg = method.args.Last();
                cw.Writeln($"std::wstring {lastArg.name};");
                List<string> names = method.args.ConvertAll(item => item.name);
                string call = string.Format("{0}->{1}({2});", LibOP.ObjName, method.name, string.Join(", ", names));
                cw.Writeln(call);
                cw.Writeln($"if ({LibOP.PStr} == nullptr || {LibOP.PStrSize} <= (int)({lastArg.name}.length() * sizeof(wchar_t)))");
                cw.IncIndent();
                cw.Writeln($"return (int)({lastArg.name}.length() + 1) * sizeof(wchar_t);");
                cw.DecIndent();
                cw.Writeln($"wcscpy_s({LibOP.PStr}, {LibOP.PStrSize} / sizeof(wchar_t), {lastArg.name}.c_str());");
                cw.Writeln("return 0;");
                cw.EndBlock();
            }
            else if (method.returnType != "long" && showFunc.returnType == "long")//有改变返回值位置
            {
                var lastArg = method.args.Last();
                cw.Writeln(string.Format("long {0};", lastArg.name));
                List<string> names = method.args.ConvertAll(item => item.name);
                names[names.Count - 1] = names[names.Count - 1].Insert(0, "&");
                string call = string.Format("{0}->{1}({2});", LibOP.ObjName, method.name, string.Join(", ", names));
                cw.Writeln(call);
                cw.Writeln(string.Format("return {0};", lastArg.name));
                cw.EndBlock();
            }
            else
            {
                string tempCall = string.Format("return {0}->{1}({2});", LibOP.ObjName, method.name, string.Join(", ", method.args.ConvertAll(item => item.name)));
                cw.Writeln(tempCall);
                cw.EndBlock();
            }
        }
    }
}
