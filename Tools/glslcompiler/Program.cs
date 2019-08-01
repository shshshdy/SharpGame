using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace glslcompiler
{
    class Program
    {
        static Dictionary<string, string> FilesMap;

        static void ScanDir(string dir)
        {
            string[] dirs = Directory.GetDirectories(dir);
            string[] files = Directory.GetFiles(dir);
            if(FilesMap == null)FilesMap = new Dictionary<string, string>();
            foreach (var file in files)
            {
                FilesMap[Path.GetFileName(file)] = dir + "/" + Path.GetFileName(file);
            }
            foreach (var d in dirs)
            {
                ScanDir(d);
            }
        }

        static string MD5(string input)
        {
            byte[] hash = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        static string ResolveIncludes(string file)
        {
            string[] contents = File.ReadAllLines(file);

            string header = "#include ";
            string guard = "#pragma once";
            bool encloseInGuard = false;
            for(int i=0;i<contents.Length;i++)
            {
                if(contents[i].StartsWith(header))
                {
                    string include = contents[i].Substring(header.Length);
                    include = include.Trim(' ', '\t', '"');
                    if (!FilesMap.ContainsKey(include))
                    {
                        Console.WriteLine("ERROR: NOT FOUND: " + include);
                        throw new FileNotFoundException(include);
                    }
                    contents[i] = ResolveIncludes(FilesMap[include]);
                }
                else if (contents[i].Contains(guard)) {
                    contents[i] = "";
                    encloseInGuard = true;
                }
            }
            string result = string.Join("\n", contents);
            if (encloseInGuard)
            {
                string guid = "X" + MD5(file);
                result = "#ifndef " + guid + "\n#define " + guid + "\n" + result + "\n#endif";
            }
            return result;
        }

        static string Prepare(string file)
        {
            if (!Directory.Exists("tmp")) Directory.CreateDirectory("tmp");
            string newfile = "tmp/" + Path.GetFileNameWithoutExtension(file) + ".tmp" + Path.GetExtension(file);

            string code = ResolveIncludes(file);
            File.WriteAllText(newfile, code);

            return newfile;
        }
        static string CalculateMD5Hash(string input)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hash = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            string dr = string.Join(" ", args);
            //glslangValidator.exe
            Directory.SetCurrentDirectory(dr);
            string[] files = Directory.GetFiles(dr);
            ScanDir(dr);
            foreach (string file in files)
            {
                if (file.EndsWith(".frag") || file.EndsWith(".vert") || file.EndsWith(".comp"))
                {
                    string dstfile = "compiled/" + Path.GetFileName(file) + ".spv";
                    string tmp = Prepare(file);
                    string tmpmd5 = Prepare(file)+".md5";
                    string last_md5 = File.Exists(tmpmd5) ? File.ReadAllText(tmpmd5) : "";
                    string new_md5 = CalculateMD5Hash(File.ReadAllText(tmp));
                    if (last_md5 == new_md5) continue;
                    Console.WriteLine("Compiling " + Path.GetFileName(file) + " to compiled/" + Path.GetFileName(file) + ".spv");
                    var pinfo = new ProcessStartInfo(@"glslangValidator.exe", "-V " + tmp + " -o " + dstfile);
                    pinfo.CreateNoWindow = true;
                    pinfo.UseShellExecute = false;
                    pinfo.RedirectStandardOutput = true;
                    var p = Process.Start(pinfo);
                    string o = p.StandardOutput.ReadToEnd();
                    o = o.Replace("\r\n", "\n");
                    string[] lines = o.Split('\n');
                    bool errored = false;
                    foreach(string line in lines)
                    {
                        if (line.Contains("ERROR"))
                        {
                            Console.WriteLine(line);
                            errored = true;
                        }
                    }
                    if(!errored)File.WriteAllText(tmpmd5, new_md5);
                    //Console.WriteLine(o);
                    //Console.WriteLine();
                }
            }
            Console.WriteLine("Done!");
        }
    }
}
