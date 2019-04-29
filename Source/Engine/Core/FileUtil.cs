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
        public static bool FileExists(String fileName)
        {
            return System.IO.File.Exists(fileName);
        }

        /// Check if a directory exists.
        public static bool DirExists(String pathName)
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

        public static void SplitPath(String fullPath, out String pathName, out String fileName, out String extension, bool lowercaseExtension = true)
        {
            String fullPathCopy = GetInternalPath(fullPath);

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
                extension = String.Empty;

            pathPos = fullPathCopy.LastIndexOf('/');
            if(pathPos != -1)
            {
                fileName = fullPathCopy.Substring(pathPos + 1);
                pathName = fullPathCopy.Substring(0, pathPos + 1);
            }
            else
            {
                fileName = fullPathCopy;
                pathName = String.Empty;
            }
        }

        public static String GetPath(String fullPath)
        {
            String path, file, extension;
            SplitPath(fullPath, out path, out file, out extension);
            return path;
        }

        public static String GetFileName(String fullPath)
        {
            return Path.GetFileNameWithoutExtension(fullPath);
        }

        public static String GetExtension(String fullPath, bool lowercaseExtension = true)
        {
            return Path.GetExtension(fullPath);
        }

        public static String GetFileNameAndExtension(String fileName)
        {
            return Path.GetFileName(fileName);
        }

        public static String ReplaceExtension(String fullPath, String newExtension)
        {
            return Path.ChangeExtension(fullPath, newExtension);
        }

        public static String AddTrailingSlash(String pathName)
        {
            String ret = pathName.Trim();
            ret.Replace('\\', '/');
            if(!string.IsNullOrEmpty(ret) && ret[ret.Length - 1] != '/')
                ret += '/';
            return ret;
        }

        public static String RemoveTrailingSlash(String pathName)
        {
            String ret = pathName.Trim();
            ret.Replace('\\', '/');
            if(!string.IsNullOrEmpty(ret) && ret[ret.Length - 1] == '/')
                ret = ret.Substring(0, ret.Length - 1);
            return ret;
        }

        public static String GetParentPath(String path)
        {
            int pos = RemoveTrailingSlash(path).LastIndexOf('/');
            if(pos != -1)
                return path.Substring(0, pos + 1);
            else
                return String.Empty;
        }

        public static String GetInternalPath(String pathName)
        {
            return pathName.Replace('\\', '/');
        }

        public static String GetNativePath(String pathName)
        {
#if _WIN32
            return pathName.Replace('/', '\\');
#else
            return pathName;
#endif
        }

        public static bool IsAbsolutePath(String pathName)
        {
            if(string.IsNullOrEmpty(pathName))
                return false;

            String path = GetInternalPath(pathName);

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
