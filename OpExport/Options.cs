using CommandLine;
using CommandLine.Attributes;
using System;
using System.IO;

namespace OpExport
{
    public class Options
    {

        [RequiredArgument(0, "op", "OP Project")]
        public string Project { get; set; }

        [OptionalArgument("", "t", "Tempalte File")]
        public string Template { get; set; }

        [OptionalArgument("", "out", "Output File")]
        public string OutFile { get; set; }

        [OptionalArgument(false, "doc", "Apply github document by OP")]
        public bool Document { get; set; }

        public string IdlOPFile => Path.Combine(Project, "libop/com/op.idl");
        public string LibOPFile => Path.Combine(Project, "libop/libop.h");


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
#if DEBUG
                args = new string[] { "D:\\git\\op", "-t", "D:\\git\\OPExport\\OpExport\\bin\\Debug\\net10.0\\Template\\Help.sbncs", 
                "-out", "C++\\OpSoft2.md"}; //Debug调试
#endif
                Parser.TryParse(args, out optionsInstance, parserOptions);
                return optionsInstance;
            }
        }
    }
}
