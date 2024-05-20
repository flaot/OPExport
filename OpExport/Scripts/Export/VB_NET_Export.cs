using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace OpExport.Export
{
    [Language(LanguageFlags.VB_NET)]
    public class VB_NET_Export : AbstractExport
    {
        protected override bool ManuallyOP => false;

        private string writeFile;
        private CodeWriter cw;
        protected override void Start()
        {
            string folder = LanguageFactory.ExportPath("VB.Net/");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            writeFile = Path.Combine(folder, "OpSoft.vb");
            cw = new CodeWriter(string.Empty, string.Empty, false, fileMark: "" +
            "' This is an automatically generated class by OpExport. Please do not modify it. \n" +
            "' License：https://github.com/WallBreaker2/op/blob/master/LICENSE");

            cw.Writeln("Imports System.Runtime.InteropServices");
            cw.Writeln();
            cw.Writeln("Partial Public Class OpSoft");
            cw.IncIndent();
            cw.Writeln("Implements IDisposable");
            cw.Writeln("Implements IComparable(Of OpSoft)");
            cw.DecIndent();
            cw.IncIndent();
            cw.Writeln("Const DLL_NAME As String = \"op\"");
            cw.Writeln();
            cw.Writeln($"Private {LibOP.ObjName} As IntPtr ' 非托管资源");
            cw.Writeln($"Private {LibOP.PStr} As IntPtr = IntPtr.Zero");
            cw.Writeln($"Private {LibOP.PStrSize} As Integer = 0");
            cw.Writeln();

            //Dispose
            cw.Rawln("#Region \"Dispose\"");
            cw.Writeln("Private disposed As Boolean = False   ' 是否已经释放资源的标志");
            cw.Writeln("Public Sub New()");
            cw.IncIndent();
            cw.Writeln($"{LibOP.ObjName} = {LibOP.CreateFunc}()");
            cw.DecIndent().Writeln("End Sub");
            cw.Writeln("Protected Overrides Sub Finalize()");
            cw.IncIndent();
            cw.Writeln("Dispose(False)");
            cw.Writeln("MyBase.Finalize()");
            cw.DecIndent().Writeln("End Sub");
            cw.Writeln("Protected Overridable Sub Dispose(disposing As Boolean)");
            cw.IncIndent();
            cw.Writeln("If Not disposed Then");
            cw.IncIndent();
            cw.Writeln("If disposing Then");
            cw.IncIndent();
            cw.DecIndent().Writeln("End If");
            cw.Writeln($"{LibOP.ReleaseFunc}({LibOP.ObjName})");
            cw.Writeln($"{LibOP.ObjName} = IntPtr.Zero");
            cw.Writeln($"If {LibOP.PStrSize} > 0 Then");
            cw.IncIndent();
            cw.Writeln($"Marshal.FreeHGlobal({LibOP.PStr})");
            cw.Writeln($"{LibOP.PStr} = IntPtr.Zero");
            cw.Writeln($"{LibOP.PStrSize} = 0");
            cw.DecIndent().Writeln("End If");
            cw.Writeln("disposed = True");
            cw.DecIndent().Writeln("End If");
            cw.DecIndent().Writeln("End Sub");
            cw.Writeln("Public Sub Dispose() Implements IDisposable.Dispose");
            cw.IncIndent();
            cw.Writeln("Dispose(True)");
            cw.Writeln("GC.SuppressFinalize(Me)");
            cw.DecIndent().Writeln("End Sub");
            cw.Rawln("#End Region");

            //Overroid
            cw.Rawln("#Region \"Overroid\"");
            cw.Writeln("Public Overrides Function Equals(ByVal obj As Object) As Boolean");
            cw.IncIndent();
            cw.Writeln("Dim soft As OpSoft = TryCast(obj, OpSoft)");
            cw.Writeln("Return soft IsNot Nothing AndAlso GetID() = soft.GetID()");
            cw.DecIndent().Writeln("End Function");
            cw.Writeln("Public Overrides Function GetHashCode() As Integer");
            cw.IncIndent();
            cw.Writeln("Return GetID().GetHashCode()");
            cw.DecIndent().Writeln("End Function");
            cw.Writeln("Public Overrides Function ToString() As String");
            cw.IncIndent();
            cw.Writeln("Return String.Format(\"id:{0}\", GetID())");
            cw.DecIndent().Writeln("End Function");
            cw.Writeln("Public Function CompareTo(other As OpSoft) As Integer Implements IComparable(Of OpSoft).CompareTo");
            cw.IncIndent();
            cw.Writeln("Return GetID().CompareTo(other.GetID())");
            cw.DecIndent().Writeln("End Function");
            cw.Rawln("#End Region");
        }
        protected override void Finish()
        {
            //Define
            cw.Rawln("#Region \"DLL Import Define\"");
            foreach (var func in libOP.functions)
            {
                List<string> names = func.args.ConvertAll(item => item.name);
                cw.Writeln(string.Format("<DllImport(DLL_NAME, CharSet:=CharSet.Unicode, CallingConvention:=CallingConvention.Cdecl)>", func.name));
                if (func.returnType == "void")
                {
                    cw.Writeln(string.Format("Private Shared Sub {0}({1})", 
                        func.name, string.Join(", ", func.args.ConvertAll(item => string.Format("{2}{1} As {0}", 
                        SwtichType(item, out var preifx), item.name, preifx)))));
                    cw.Writeln("End Sub");
                }
                else
                {
                    cw.Writeln(string.Format("Private Shared Function {1}({2}) As {0}", mapType.OutRtn(func.returnType),
                        func.name, string.Join(", ", func.args.ConvertAll(item => string.Format("{2}{1} As {0}",
                        SwtichType(item, out var preifx), item.name, preifx)))));
                    cw.Writeln("End Function");
                }
                cw.Writeln();
            }
            cw.Rawln("#End Region");
            cw.DecIndent().Writeln("End Class");
            cw.Save(writeFile);
        }

        protected override void GenerateMethod(Method showFunc, Method dllFunc)
        {
            CheckFuncArgName(showFunc, dllFunc);
            GenerateAnnotation(cw, showFunc);
            string showArgs = string.Join(", ", showFunc.args.ConvertAll(item => string.Format("{2}{1} As {0}", SwtichType(item, out var preifx), item.name, preifx)));
            List<string> callNames = dllFunc.args.ConvertAll(item => item.name);
            if (showFunc.returnType == "std::wstring")
            {
                string functionName = string.Format("Public Function {1}({2}) As {0}", mapType.OutRtn(showFunc.returnType), showFunc.name, showArgs);
                cw.Writeln(functionName);
                cw.IncIndent();
                string call = string.Format("{0}({1})", dllFunc.name, string.Join(", ", callNames));
                cw.Writeln("Dim _size As Integer = " + call);
                cw.Writeln("If _size > 0 Then");
                cw.IncIndent();
                cw.Writeln($"If {LibOP.PStrSize} > 0 Then Marshal.FreeHGlobal({LibOP.PStr})");
                cw.Writeln($"{LibOP.PStrSize} = _size");
                cw.Writeln($"{LibOP.PStr} = Marshal.AllocHGlobal({LibOP.PStrSize})");
                cw.Writeln(call);
                cw.DecIndent().Writeln("End If");
                cw.Writeln($"Dim str As String = Marshal.PtrToStringUni({LibOP.PStr})");
                cw.Writeln("Return str");
                cw.DecIndent().Writeln("End Function");
            }
            else
            {
                if (showFunc.returnType == "void")
                {
                    cw.Writeln(string.Format("Public Sub {0}({1})", showFunc.name, showArgs));
                    cw.IncIndent();
                    cw.Writeln(string.Format("{0}({1})", dllFunc.name, string.Join(", ", callNames)));
                    cw.DecIndent().Writeln("End Sub");
                }
                else
                {
                    cw.Writeln(string.Format("Public Function {1}({2}) As {0}", mapType.OutRtn(showFunc.returnType), showFunc.name, showArgs));
                    cw.IncIndent();
                    cw.Writeln(string.Format("Return {0}({1})", dllFunc.name, string.Join(", ", callNames)));
                    cw.DecIndent().Writeln("End Function");
                }
            }
        }

        /// <summary>
        /// 检查参数名与方法名称一致时,更改下名字
        /// </summary>
        private void CheckFuncArgName(Method showFunc, Method dllFunc)
        {
            foreach (var arg in showFunc.args)
            {
                if (arg.name == showFunc.name)
                    arg.name = "_" + arg.name;
            }
            foreach (var arg in dllFunc.args)
            {
                if (arg.name == showFunc.name)
                    arg.name = "_" + arg.name;
            }
        }

        private void GenerateAnnotation(CodeWriter cw, Method func)
        {
            cw.Writeln("''' <summary>");
            foreach (var item in func.annotation.Split('\n'))
                cw.Writeln(string.Format("''' {0}", item));
            cw.Writeln("''' </summary>");

            for (int i = 0; i < func.args.Count; i++)
                cw.Writeln("''' <param name=\"{0}\">{1}</param>", func.args[i].name, func.args[i].annotation.Replace("\n", " "));

            if (!string.IsNullOrEmpty(func.returnAnnotation))
                cw.Writeln("''' <returns>{0}</returns>", func.returnAnnotation.Replace("\n", " "));

            if (string.IsNullOrEmpty(func.example))
                return;
            cw.Writeln("''' <example>");
            cw.Writeln("''' <code>");
            foreach (var item in func.example.Split('\n'))
                cw.Writeln(string.Format("''' {0}", item));
            cw.Writeln("''' </code>");
            cw.Writeln("''' </example>");
        }
        private string SwtichType(Arg arg, out string prefix)
        {
            prefix = string.Empty;
            if (arg.refType == Reference.Out)
                prefix = "<Out> ByRef ";
            if (arg.refType == Reference.InOut)
                prefix = "ByRef ";

            return mapType.OutArg(arg.type, arg.refType);
        }
        #region 转换类型定义
        private MapTypeData mapType = new MapTypeData(SwtichType)
        {
                { "wchar_t*"      , "IntPtr" },
                { "libop*"        , "IntPtr" },
                { "const wchar_t*", "String" },
                { "long"          , "Integer"},
                { "void*"         , "IntPtr" },
                { "std::wstring"  , "String" },
                { "int"           , "Integer"},
                { "double"        , "Double" },

                //返回值是个 ByRef 类型的
                { "int*"          , "IntPtr"},
                { "long*"         , "IntPtr"},
                { "size_t*"       , "IntPtr"},
                { "char*"         , "String"},
        };
        private static string SwtichType(string type, string swtichType, Reference refer)
        {
            if (refer == Reference.Ret)
                return swtichType;

            switch (type)
            {
                case "int*": return "Integer";
                case "long*": return "Integer";
                default: return swtichType;
            }
        }
        #endregion
    }
}
