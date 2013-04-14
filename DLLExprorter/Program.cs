using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DLLExprorter
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("\n------ DLLExporter Start ------");
            if (args.Length < 1)
            {
                Console.WriteLine(@"Wrong Parametr -> DLLExporter.exe X:\library.dll");
                return;
            }

            var dllPath = args[0];
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