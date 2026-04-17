using OpDefine;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpExport
{
    internal class Main
    {
        internal static async Task Work(string[] args)
        {
            await Task.FromResult(0);
            Options option = Options.Inst;
            if (option == null) return;

            Console.WriteLine("OP Project:" + option.Project);
            if (!Directory.Exists(option.Project))
            {
                Console.WriteLine("[Error] no found OP Project:" + option.Project);
                return;
            }
            if (!File.Exists(option.LibOPFile))
            {
                Console.WriteLine("[Error] no found libop.h file:" + option.LibOPFile);
                return;
            }
            if (!File.Exists(option.IdlOPFile))
            {
                Console.WriteLine("[Error] no found op.idl file:" + option.IdlOPFile);
                return;
            }
            if (!File.Exists(option.Template))
            {
                Console.WriteLine("[Error] no found template file:" + option.Template);
                return;
            }
            if (string.IsNullOrWhiteSpace(option.OutFile))
            {
                Console.WriteLine("[Error] out file is null");
                return;
            }

            LibOP libOP = LibOP.Create(option.LibOPFile, option.IdlOPFile, option.Document);
            if (libOP.functions.Count < 1)
            {
                Console.WriteLine("[Error] not is op project:" + option.Project);
                return;
            }

            //解析模版
            string fileExtension = string.Empty;
            var templateContext = File.ReadAllText(option.Template);
            var tpl = Template.Parse(templateContext);
            var scriptObject1 = new ScriptObject();
            scriptObject1["libOP"] = libOP;
            scriptObject1.Import("_func_methodByFunction", libOP.MethodByFunction);
            scriptObject1.Import("_func_argsRemoveAt", (List<Arg> a, int index) => a.RemoveAt(index));
            scriptObject1.Import("_func_setOutFileExtension", (string extension) => { fileExtension = extension.StartsWith(".") ? extension : '.' + extension; });

            //应用模版
            var context = new TemplateContext();
            context.LoopLimit = 0;
            context.RecursiveLimit = 0;
            context.StrictVariables = true;
            context.RegexTimeOut = System.Text.RegularExpressions.Regex.InfiniteMatchTimeout;
            context.PushGlobal(scriptObject1);
            string codeContext = tpl.Render(context);

            //保存到文件
            string outFile = Path.GetFullPath(option.OutFile);
            string folder = Path.GetDirectoryName(outFile);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            if (!string.IsNullOrWhiteSpace(fileExtension) && Path.GetExtension(outFile) != fileExtension)
                outFile += fileExtension;
            Console.WriteLine("[Log] out file:" + outFile);
            File.WriteAllText(outFile, codeContext);
        }
    }
}
