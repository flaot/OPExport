using OpExport.Export;
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

            System.Console.WriteLine("OP file(.h):" + option.File);
            Console.WriteLine("Export Languages:" + option.Language);

            if (!File.Exists(option.File))
            {
                Console.WriteLine("[Error] no found op .h file:" + option.File);
                return;
            }

            LibOP libOP = LibOP.Create(option.File);
            if (libOP.functions.Count < 1)
            {
                Console.WriteLine("[Error] file not is op head file:" + option.File);
                return;
            }

            List<IExport> exports = LanguageFactory.Create(option.Language);
            foreach (var export in exports)
            {
                export.Export(libOP);
            }
        }
    }
}
