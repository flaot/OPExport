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

            System.Console.WriteLine("OP Project:" + option.Project);
            Console.WriteLine("Export Languages:" + option.Language);
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

            LibOP libOP = LibOP.Create(option.LibOPFile, option.IdlOPFile);
            if (libOP.functions.Count < 1)
            {
                Console.WriteLine("[Error] not is op project:" + option.Project);
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
