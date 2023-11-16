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

        [OptionalArgument(LanguageFlags.OP, "lang", "Language Flags")]
        public LanguageFlags Language { get; set; }

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
                //args = new string[] { "D:\\git\\op" }; //Debug调试
                Parser.TryParse(args, out optionsInstance, parserOptions);
                return optionsInstance;
            }
        }
    }
}
