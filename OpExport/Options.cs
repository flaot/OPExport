using CommandLine;
using CommandLine.Attributes;
using System;
using System.IO;

namespace OpExport
{
    public class Options
    {
        [RequiredArgument(0, "op", "libop.h File")]
        public string LibOPFile { get; set; }

        [OptionalArgument("", "idl", "op.idl File")]
        public string IdlOPFile { get; set; }

        [OptionalArgument("", "t", "Tempalte File")]
        public string Template { get; set; }

        [OptionalArgument("", "out", "Output File")]
        public string OutFile { get; set; }

        [OptionalArgument(false, "doc", "Apply github document by OP")]
        public bool Document { get; set; }


        private static Options optionsInstance;
        public static Options Inst
        {
            get
            {
                if (optionsInstance != null)
                    return optionsInstance;

                ParserOptions parserOptions = new ParserOptions() { VariableNamePrefix = "OpExport", ReadFromEnvironment = false };
                string[] commandArgs = System.Environment.GetCommandLineArgs();
                string[] args = new string[commandArgs.Length - 1];
                Array.Copy(commandArgs, 1, args, 0, args.Length);
                Parser.TryParse(args, out optionsInstance, parserOptions);
                return optionsInstance;
            }
        }
    }
}
