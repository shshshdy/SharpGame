using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public class FileUtil
    {
        public static string StandardlizeFile(string path)
        {
            path = path.Replace('\\', '/');
            path = path.ToLower();
            return path;
        }
        
        /// Check if a file exists.
        public static bool FileExists(string fileName)
        {
            return System.IO.File.Exists(fileName);
        }

        /// Check if a directory exists.
        public static bool DirExists(string pathName)
        {
            return System.IO.Directory.Exists(pathName);
        }

        public static byte[] ReadAllBytes(string path)
        {
            return System.IO.File.ReadAllBytes(path);
        }

        public static string ReadAllText(string path)
        {
            return System.IO.File.ReadAllText(path);
        }

        public static void WriteAllBytes(string path, byte[] bytes)
        {
            System.IO.File.WriteAllBytes(path, bytes);
        }

        public static void WriteAllText(string path, string contents)
        {
            System.IO.File.WriteAllText(path, contents);
        }

        public static void SplitPath(string fullPath, out string pathName, out string fileName, out string extension, bool lowercaseExtension = true)
        {
            string fullPathCopy = GetInternalPath(fullPath);

            int extPos = fullPathCopy.LastIndexOf('.');
            int pathPos = fullPathCopy.LastIndexOf('/');

            if(extPos != -1 && (pathPos == -1 || extPos > pathPos))
            {
                extension = fullPathCopy.Substring(extPos);
                if(lowercaseExtension)
                    extension = extension.ToLower();
                fullPathCopy = fullPathCopy.Substring(0, extPos);
            }
            else
                extension = string.Empty;

            pathPos = fullPathCopy.LastIndexOf('/');
            if(pathPos != -1)
            {
                fileName = fullPathCopy.Substring(pathPos + 1);
                pathName = fullPathCopy.Substring(0, pathPos + 1);
            }
            else
            {
                fileName = fullPathCopy;
                pathName = string.Empty;
            }
        }

        public static string CombinePath(string dir, string path)
        {
            if (path.StartsWith("/"))
            {
                if (dir.EndsWith("/"))
                {
                    return dir + path.Substring(1);
                }
                else
                    return dir + path;
            }
            else
            {
                if (dir.EndsWith("/"))
                {
                    return dir + path;
                }
                else
                    return dir + "/" + path;
            }
        }

        public static string GetPath(string fullPath)
        {
            string path, file, extension;
            SplitPath(fullPath, out path, out file, out extension);
            return path;
        }

        public static string GetFileName(string fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }

        public static string GetExtension(string fullPath, bool lowercaseExtension = true)
        {
            return Path.GetExtension(fullPath);
        }

        public static string GetFileNameAndExtension(string fileName)
        {
            return Path.GetFileName(fileName);
        }

        public static string ReplaceExtension(string fullPath, string newExtension)
        {
            return Path.ChangeExtension(fullPath, newExtension);
        }

        public static string AddTrailingSlash(string pathName)
        {
            string ret = pathName.Trim();
            ret.Replace('\\', '/');
            if(!string.IsNullOrEmpty(ret) && ret[ret.Length - 1] != '/')
                ret += '/';
            return ret;
        }

        public static string RemoveTrailingSlash(string pathName)
        {
            string ret = pathName.Trim();
            ret.Replace('\\', '/');
            if(!string.IsNullOrEmpty(ret) && ret[ret.Length - 1] == '/')
                ret = ret.Substring(0, ret.Length - 1);
            return ret;
        }

        public static string GetParentPath(string path)
        {
            int pos = RemoveTrailingSlash(path).LastIndexOf('/');
            if(pos != -1)
                return path.Substring(0, pos + 1);
            else
                return string.Empty;
        }

        public static string GetInternalPath(string pathName)
        {
            return pathName.Replace('\\', '/');
        }

        public static string GetNativePath(string pathName)
        {
#if _WIN32
            return pathName.Replace('/', '\\');
#else
            return pathName;
#endif
        }

        public static bool IsAbsolutePath(string pathName)
        {
            if(string.IsNullOrEmpty(pathName))
                return false;

            string path = GetInternalPath(pathName);

            if(path[0] == '/')
                return true;

#if _WIN32
            if(path.Length > 1 && char.IsLetter(path[0]) && path[1] == ':')
                return true;
#endif

            return false;
        }

    }
}
