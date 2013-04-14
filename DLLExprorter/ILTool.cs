using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Build.Utilities;

namespace DLLExprorter
{
    class ILTool
    {
        private const string FunPattern =
            @".method[\s\r\n]+.*[\s\r\n]+([A-z0-9_]*)[\s\r\n]*\([^(){};]*\)[^{};]*{[\.\s\r\n\w]*(\.custom instance void.*DllExport::.ctor\(\) = \([ 0-9]*\))";

        private const string CorPattern = 
            @"\.corflags ([^ ]*)";

        //private const string VtPattern =
        //    @"\.vtfixup \[1\] int32 fromunmanaged at VT_0(\d+)[\s\r\n]*\.data VT_0\d+ = int32\(0\)";

        private const string VtMem = "\r\n.vtfixup [1] int32 fromunmanaged at VT_0{0}\r\n\t.data VT_0{0} = int32(0)";
        private const string VtExport = ".vtentry 1 : {0}\r\n.export [{0}] as {1}";

        /// <summary>
        /// Validate path to file with extension = <param name="ext"/>
        /// </summary>
        /// <param name="path">Absolute path to <param name="ext"/> file</param>
        /// <param name="ext">Validate extension </param>
        /// <returns>0 - No errors; 1 - File not found; 2 - Non Absolute Path; 3 - Non <param name="ext"/> file</returns>
        public static int CheckPath(string path, string ext)
        {
            return File.Exists(path) ? Path.GetDirectoryName(path) != string.Empty ? (Path.GetExtension(path) != ext ? 3 : 0) : 2 : 1;
        }

        /// <summary>
        /// Decompile .NET dll
        /// </summary>
        /// <param name="dllPath">In absolute path to dll</param>
        /// <param name="ilPath">Out absolute path to IL code file</param>
        /// <param name="ilCode">Out code with IL asm dll</param>
        /// <returns>0 - No errors; 1 - File not found; 2 - Non Absolute Path; 3 - Non dll file; >3 - Decompiling error</returns>
        public static int DLLtoIL(string dllPath, out string ilPath, out StringBuilder ilCode)
        {
            ilPath = null;
            ilCode = null;

            var err = CheckPath(dllPath, ".dll");
            if (err != 0) return err;

            var ilDAsmArgs = string.Format(@"/nobar /out:""{0}"" ""{1}"" /UTF8", Path.ChangeExtension(dllPath, ".il"), dllPath);
            var ilDAsmPath = ToolLocationHelper.GetPathToDotNetFrameworkSdkFile("ILDAsm.exe", TargetDotNetFrameworkVersion.Version40);
            if (string.IsNullOrEmpty(ilDAsmPath))
            {
                ilDAsmPath = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\ildasm.exe";
            }

            var processInfo = new ProcessStartInfo(ilDAsmPath, ilDAsmArgs)
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = false
            };

            var process = new Process { StartInfo = processInfo };
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.Write("\r\nprocess ERROR = " + process.ExitCode);
                return process.ExitCode;
            }
            
            ilPath = Path.ChangeExtension(dllPath, ".il");
            ilCode = new StringBuilder(File.ReadAllText(ilPath));

            return 0;
        }

        /// <summary>
        /// Decompile .NET dll
        /// </summary>
        /// <param name="ilPath">Path to save il code file</param>
        /// <param name="ilCode">Contains IL code to compile</param>
        /// <returns>>3 - Compiling error</returns>
        public static int ILtoDLL(string ilPath, StringBuilder ilCode)
        {
            File.WriteAllText(ilPath, ilCode.ToString());

            var ilAsmArgs = string.Format(@"/nologo /quiet /out:""{0}"" ""{1}"" /dll", Path.ChangeExtension(ilPath, ".dll"), ilPath);
            var ilAsmPath = ToolLocationHelper.GetPathToDotNetFrameworkFile("ILAsm.exe", TargetDotNetFrameworkVersion.Version40);
            if (string.IsNullOrEmpty(ilAsmPath))
            {
                ilAsmPath = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ILAsm.exe";
            }

            var processInfo = new ProcessStartInfo(ilAsmPath, ilAsmArgs)
            {
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = false
            };

            var process = new Process { StartInfo = processInfo };
            process.Start();
            process.WaitForExit();

            return process.ExitCode != 0 ? process.ExitCode : 0;
        }

        /// <summary>
        /// Export methods marked [DllExport] attribute as unmanaged functions
        /// </summary>
        /// <param name="ilCode">IL asm code</param>
        /// <returns>1 - <param name="ilCode"> not contain marked methods</returns>
        public static int ExportMarkedMethods(StringBuilder ilCode)
        {
            //int num = 1;

            var funMatches = Regex.Matches(ilCode.ToString(), FunPattern);
            if (funMatches.Count == 0)
            {
                Console.WriteLine("Not found marked methods");
                return 1;
            }

            //var vtMatches = Regex.Matches(ilCode.ToString(), VtPattern);
            //foreach (Match vtMatch in vtMatches)
            //{
            //    if (Int32.Parse(vtMatch.Groups[1].Value) >= num)
            //    {
            //        num = Int32.Parse(vtMatch.Groups[1].Value) + 1;
            //    }
            //}

            Console.WriteLine("Find {0} methods:", funMatches.Count);
            for (var i = funMatches.Count-1; i >= 0; i--)
            {
                ilCode.Remove(funMatches[i].Groups[2].Index, funMatches[i].Groups[2].Length);
                ilCode.Insert(funMatches[i].Groups[2].Index, String.Format(VtExport, i + 1, funMatches[i].Groups[1].Value));
            }

            var corMatch = Regex.Match(ilCode.ToString(), CorPattern);
            ilCode.Remove(corMatch.Groups[1].Index,corMatch.Groups[1].Length);
            ilCode.Insert(corMatch.Groups[1].Index,"0x00000002");

            for (var i = 0; i < funMatches.Count; i++)
            {
                Console.WriteLine("\t - {0}", funMatches[i].Groups[1].Value);
                ilCode.Insert(corMatch.Groups[1].Index + corMatch.Groups[1].Length, String.Format(VtMem, i+1));
            }
            return 0;
        }

        /// <summary>
        /// Removes .il and .res in specified folder
        /// </summary>
        public static void CleanUpDirectory(string path)
        {
            RemoveFiles(Directory.GetFiles(path, "*.il"));
            RemoveFiles(Directory.GetFiles(path, "*.res"));
        }

        /// <summary>
        /// Removes files specified in array
        /// </summary>
        public static void RemoveFiles(string[] files)
        {
            foreach (string file in files) 
            {
                File.Delete(file);
            }
        }
    }
}

