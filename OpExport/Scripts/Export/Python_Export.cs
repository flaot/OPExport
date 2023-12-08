using System.Collections.Generic;
using System.IO;

namespace OpExport.Export
{
    [Language(LanguageFlags.Python)]
    public class Python_Export : AbstractExport
    {
        protected override bool ManuallyOP => false;
        private string writeFile;
        private CodeWriter cw;
        protected override void Start()
        {
            string folder = LanguageFactory.ExportPath("Python/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            writeFile = Path.Combine(folder, "OpSoft.py");
            cw = new CodeWriter(string.Empty, string.Empty, false, fileMark: "\"\"\"\n" +
            "This is an automatically generated class by OpExport. Please do not modify it. \n" +
            "License：https://github.com/WallBreaker2/op/blob/master/LICENSE\n" +
            "\"\"\"");
            cw.Writeln("# coding=UTF-8");
            cw.Writeln("from ctypes import *");
            cw.Writeln();
            cw.Writeln();
            cw.Writeln("class OpSoft:");
            cw.StartBlock();

            //__init__
            cw.Writeln("def __init__(self, dll_file):");
            cw.StartBlock();
            cw.Writeln($"self.{LibOP.PStr} = c_wchar_p(0)");
            cw.Writeln($"self.{LibOP.PStrSize} = c_int()");
            cw.Writeln("self.dll = cdll.LoadLibrary(dll_file)");
            cw.Writeln($"self.dll.{LibOP.CreateFunc}.restype = c_void_p");
            cw.Writeln($"self.{LibOP.ObjName} = self.dll.{LibOP.CreateFunc}()");
            cw.EndBlock();

            //__del__
            cw.Writeln("def __del__(self):");
            cw.StartBlock();
            cw.Writeln($"self.dll.{LibOP.ReleaseFunc}.argtypes = [c_void_p]");
            cw.Writeln($"self.dll.{LibOP.ReleaseFunc}(self.{LibOP.ObjName})");
            cw.Writeln($"self.{LibOP.ObjName} = None");
            cw.Writeln("self.dll = None");
            cw.Writeln($"self.{LibOP.PStr} = None");
            cw.Writeln($"self.{LibOP.PStrSize} = None");
            cw.EndBlock();
        }
        protected override void Finish()
        {
            cw.EndBlock();
            cw.Save(writeFile);
        }
        protected override void GenerateMethod(Method showFunc, Method dllFunc)
        {
            List<Arg> args = showFunc.args.ConvertAll(item => (Arg)item.Clone());
            List<Arg> showArgs = new List<Arg>(dllFunc.args.Count);//封装调用的参数
            List<Arg> useArgs = new List<Arg>(dllFunc.args.Count); //调用DLL的参数
            List<string> refArgs = new List<string>(dllFunc.args.Count); //有传'可修改指针'的类型列表 与useArgs对应下标
            foreach (var item in dllFunc.args)
            {
                string sType = SwtichType(item.type);
                var findIndex = args.FindIndex(arg => arg.name == item.name);
                if (sType.IndexOf('|') >= 0)
                    refArgs.Add(sType.Split('|')[1]);
                else
                    refArgs.Add(string.Empty);
                item.type = sType.Split('|')[0];
                useArgs.Add(item);
                if (findIndex < 0)
                    continue;
                showArgs.Add(item);
            }

            //组装 封装调用的参数'showArgTxt'
            string showArgTxt = string.Join(", ", showArgs.ConvertAll(item => string.Format("{1}: {0}", item.type, item.name)));
            if (showArgTxt.Length > 0)
                showArgTxt = showArgTxt.Insert(0, ", ");
            showArgTxt = showArgTxt.Insert(0, "self");

            //组装 调用DLL的参数'names'
            List<string> names = new List<string>(useArgs.Count);
            for (int i = 0; i < useArgs.Count; i++)
            {
                string refName = refArgs[i];
                if (!string.IsNullOrEmpty(refName))
                {
                    names.Add(string.Format("byref(_{0})", useArgs[i].name));
                    continue;
                }
                if (useArgs[i].name.StartsWith("_"))
                    names.Add("self." + useArgs[i].name);
                else
                    names.Add(useArgs[i].name);
            }

            //有'可修改指针'的参数列表
            List<Arg> tempArg = new List<Arg>();
            for (int i = 0; i < names.Count; i++)
            {
                string refName = refArgs[i];
                if (string.IsNullOrEmpty(refName))
                    continue;
                tempArg.Add(useArgs[i]);
            }
            //有'可修改指针'的返回值类型列表
            List<string> retList = tempArg.ConvertAll(item => item.type);
            if (showFunc.returnType == "std::wstring")
            {
                retList.Insert(0, SwtichType(showFunc.returnType).Split('|')[0]);
                string retText = retList.Count > 1 ? string.Format("({0})", string.Join(", ", retList)) : retList[0];
                string call = string.Format("def {0}({1}) -> {2}:", showFunc.name, showArgTxt, retText);
                cw.Writeln(call);
                cw.StartBlock();
                GenerateAnnotation(cw, showFunc);
                GenerateTypeDesc(cw, dllFunc);
                for (int i = 0; i < names.Count; i++)
                {
                    string refName = refArgs[i];
                    if (!string.IsNullOrEmpty(refName))
                        cw.Writeln(string.Format("_{0} = {1}({2})", useArgs[i].name, refName, useArgs[i].name));
                }
                call = string.Format("self.dll.{0}({1})", dllFunc.name, string.Join(", ", names));
                cw.Writeln("_size = " + call);
                cw.Writeln("if _size > 0:");
                cw.StartBlock();
                cw.Writeln($"self.{LibOP.PStrSize} = _size");
                cw.Writeln($"self.{LibOP.PStr} = create_unicode_buffer(int(_size / sizeof(c_wchar)))");
                cw.Writeln(call);
                cw.EndBlock();
                cw.Writeln($"return self.{LibOP.PStr}.value");
                cw.EndBlock();
            }
            else
            {
                retList.Insert(0, SwtichType(showFunc.returnType).Split('|')[0]);
                string retText = retList.Count > 1 ? string.Format("({0})", string.Join(", ", retList)) : retList[0];
                string call = string.Format("def {0}({1}) -> {2}:", showFunc.name, showArgTxt, retText);
                cw.Writeln(call);
                cw.StartBlock();
                GenerateAnnotation(cw, showFunc);
                GenerateTypeDesc(cw, dllFunc);
                for (int i = 0; i < names.Count; i++)
                {
                    string refName = refArgs[i];
                    if (string.IsNullOrEmpty(refName))
                        continue;
                    cw.Writeln(string.Format("_{0} = {1}({2})", useArgs[i].name, refName, useArgs[i].name));
                }
                if (tempArg.Count > 0)
                {
                    cw.Writeln(string.Format("_result = self.dll.{0}({1})", dllFunc.name, string.Join(", ", names)));
                    cw.Writeln(string.Format("return _result, {0}", string.Join(", ", tempArg.ConvertAll(item => string.Format("_{0}.value", item.name)))));
                }
                else
                    cw.Writeln(string.Format("return self.dll.{0}({1})", dllFunc.name, string.Join(", ", names)));
                cw.EndBlock();
            }
        }
        private void GenerateAnnotation(CodeWriter cw, Method showFunc)
        {
            cw.Writeln("\"\"\"");
            if (showFunc.annotation.Length > 0)
                cw.Writeln(string.Format("{0}", showFunc.annotation.Replace("\n", " ")));
            cw.Writeln();
            for (int i = 0; i < showFunc.args.Count; i++)
                cw.Writeln("{0} -- {1}", showFunc.args[i].name, showFunc.args[i].annotation.Replace("\n", " "));

            if (!string.IsNullOrEmpty(showFunc.returnAnnotation))
                cw.Writeln("{0}", showFunc.returnAnnotation);

            if (!string.IsNullOrEmpty(showFunc.example))
                cw.Writeln("{0}", showFunc.example);

            cw.Writeln("\"\"\"");
        }
        private string SwtichType(string type)
        {
            switch (type)
            {
                case "double": return "float";
                case "long": return "int";
                case "const wchar_t*": return "str";
                case "int*": return "int|c_int"; //左边是方法定义类型，右边是给DLL的指针类型
                case "long*": return "int|c_long";
                case "char*": return "str|c_wchar_p";
                case "size_t*": return "int|c_size_t";
                case "void*": return "c_void_p";
                case "std::wstring": return "str";
                default: return type;
            }
        }
        private void GenerateTypeDesc(CodeWriter cw, Method dllFunc)
        {
            var types = dllFunc.args.ConvertAll(item => SwitchTypeDesc(item.type));
            if(types.Count > 0)
                cw.Writeln($"self.dll.{dllFunc.name}.argtypes = [{string.Join(", ", types)}]");
            if(dllFunc.returnType != "void")
                cw.Writeln($"self.dll.{dllFunc.name}.restype = {SwitchTypeDesc(dllFunc.returnType)}");
        }
        private string SwitchTypeDesc(string type)
        {
            switch (type)
            {
                case "libop*":
                case "void*": return "c_void_p";
                case "void": return "c_void";
                case "int": return "c_int";
                case "wchar_t*": return "c_wchar_p";
                case "long": return "c_long";
                default: return type;
            }
        }
    }
}
