using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DLLExprorter
{
    class Program
    {
        public static string help = @"DLLExporter.exe -in:X:\library.dll [-out:X:\some_folder\library.dll] [-noclean]";
        public static string inArg = "-in:";
        public static string outArg = "-out:";
        public static string noCleanArg = "-noclean";

        public static void Main(string[] args)
        {
            Console.WriteLine(args);

            Console.WriteLine("\n------ DLLExporter Start ------");

            string inDllFile = null;
            string outDllFile = null;
            bool clearOutput = true;
            ParseArgs(args, out inDllFile, out outDllFile, out clearOutput);
            
            if (string.IsNullOrEmpty(inDllFile))
            {
                Console.WriteLine(@"Wrong usage. Help:" + Environment.NewLine + help);
                return;
            }

            var dllPath = inDllFile;
            if (!string.IsNullOrEmpty(outDllFile))
            {
                CopyDLL(inDllFile, outDllFile);
                dllPath = outDllFile;
            }

            Console.WriteLine(String.Format("Processing: {0}", Path.GetFileName(dllPath)));

            String ilPath;
            StringBuilder ilCode;
            var err = ILTool.DLLtoIL(dllPath, out ilPath, out ilCode);
            if (err != 0) return;

            err = ILTool.ExportMarkedMethods(ilCode);
            if (err != 0)
            {
                Console.WriteLine("Exporting Stop");
                return;
            }

            err = ILTool.ILtoDLL(ilPath, ilCode);
            Console.WriteLine(err != 0 ? "Error Compile IL" : "Export Successful");

            if (clearOutput)
            {
                Console.WriteLine("Clean output folder (remove *.il and *.res). To disable run with -noclean argument.");
                string directoryPath = Path.GetDirectoryName(ilPath);
                ILTool.CleanUpDirectory(directoryPath);
            }
        }

        public static void ParseArgs(string[] args, out string inDllFile, out string outDllFile, out bool clearOutput)
        {
            inDllFile = null;
            outDllFile = null;
            clearOutput = true;
            foreach (string arg in args)
            {
                if (arg.StartsWith(inArg))
                {
                    inDllFile = arg.Substring(inArg.Length);
                }

                if (arg.StartsWith(outArg))
                {
                    outDllFile = arg.Substring(outArg.Length);
                }

                if (arg.StartsWith(noCleanArg))
                {
                    clearOutput = false;
                }
            }
        }

        public static void CopyDLL(string fromDllPath, string toDllPath)
        {
            if (toDllPath != fromDllPath)
            {
                Console.WriteLine("Copying dll: " + Environment.NewLine +
                                  "\tfrom: {0}" + Environment.NewLine +
                                  "\tto: {1}", fromDllPath, toDllPath);
                if (!Directory.Exists(Path.GetDirectoryName(toDllPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(toDllPath));
                }
                File.Copy(fromDllPath, toDllPath, true);
            }
        }
    }
}

//Encoding unicode= Encoding.Unicode;
//unicode.GetString(byte[] array);

//var res = Regex.Match(text, @);
//".custom instance void DLLExport::.ctor\(string\) = \(([^\)]*)\)"
//"Func1\(\)([^{;]*){"
//"\(([^\)]*)\)"

//string input = "plum--pear";
//string pattern = "-";            // Split on hyphens

//string[] substrings = Regex.Split(input, pattern);
//foreach (string match in substrings)
//{
//   Console.WriteLine("'{0}'", match);
//}