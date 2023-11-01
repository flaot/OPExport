using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpExport.Export
{
    [Language(LanguageFlags.OP)]
    public class OP_Export : IExport
    {
        public void Export(LibOP libOP)
        {
            string folder = LanguageFactory.ExportPath("OP/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);


            CreateHead(libOP, Path.Combine(folder, "libopExport.h"));
            CreateSource(libOP, Path.Combine(folder, "libopExport.cpp"));
        }

        private void CreateHead(LibOP libOP, string file)
        {
            CodeWriter cw = new CodeWriter();
            cw.Writeln("#pragma once");
            cw.Writeln("#ifndef LIBOP_EXPORT_H");
            cw.Writeln("#define LIBOP_EXPORT_H");
            cw.Writeln();
            cw.Writeln("#include \"libop.h\"");
            cw.Writeln();
            cw.Writeln("#define DLLAPT extern \"C\" __declspec(dllexport)");
            cw.Writeln();
            foreach (var func in libOP.functions)
                cw.Writeln(string.Format("DLLAPT {0};", func));
            cw.Writeln("#endif // !LIBOP_EXPORT_H");
            cw.Save(file);
        }
        private void CreateSource(LibOP libOP, string file)
        {
            CodeWriter cw = new CodeWriter();
            cw.Writeln("#include \"libopExport.h\"");
            cw.Writeln();
            foreach (var func in libOP.functions)
            {
                var method = libOP.MethodByFunction(func.name);
                cw.Writeln(func.ToString());
                cw.StartBlock();
                if (func.name == LibOP.CreateFunc)
                {
                    cw.Writeln("return new libop();");
                }
                else if (func.name == LibOP.ReleaseFunc)
                {
                    cw.Writeln(string.Format("delete {0};", LibOP.ObjName));
                }
                else
                {
                    do
                    {
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
                            break;
                        }
                        if (method.returnType == "void" && method.args.Count > 0)
                        {
                            var lastArg = method.args.Last();
                            if (lastArg.type == "long*")
                            {
                                cw.Writeln(string.Format("long {0};", lastArg.name));
                                List<string> names = method.args.ConvertAll(item => item.name);
                                names[names.Count - 1] = names[names.Count - 1].Insert(0, "&");
                                string call = string.Format("{0}->{1}({2});", LibOP.ObjName, method.name, string.Join(", ", names));
                                cw.Writeln(call);
                                cw.Writeln(string.Format("return {0};", lastArg.name));
                                break;
                            }
                            if (lastArg.type == "std::wstring&")
                            {
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
                                break;
                            }
                        }
                        string tempCall = string.Format("return {0}->{1}({2});", LibOP.ObjName, method.name, string.Join(", ", method.args.ConvertAll(item => item.name)));
                        cw.Writeln(tempCall);
                    } while (false);
                }
                cw.EndBlock();
            }
            cw.Save(file);
        }
    }
}
