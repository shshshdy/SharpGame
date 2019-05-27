using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SharpGame
{
    public class FileSystem : System<FileSystem>
    {
        public static string ContentRoot { get; set; }

        public static readonly string DataPath = "data/";
        public static readonly string CoreDataPath = "coredata/";
        public static readonly string CachePath = "cache/";

        /// Mutex for thread-safe access to the resource directories, resource packages and resource dependencies.
        object resourceMutex_ = new object();
        /// Resource load directories.
        List<string> resourceDirs_ = new List<string>();
        /// Package files.
        List<PackageFile> packages_ = new List<PackageFile>();
        /// Search priority flag.
        bool searchPackagesFirst_ = false;

        public FileSystem(string contentRoot)
        {
            ContentRoot = contentRoot;

            AddResourceDir(contentRoot+DataPath);
            AddResourceDir(contentRoot + CoreDataPath);
            AddResourceDir(contentRoot + CachePath);
        }

        public string CurrentDir
        {
            get
            {
                return System.IO.Directory.GetCurrentDirectory();
            }
        }

        public string ProgramDir
        {
            get
            {
                string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                return path;
            }
        }

        public Stream OpenStream(string path) => new FileStream(path, FileMode.Open);
        
        public bool AddResourceDir(string pathName, int priority = int.MaxValue)
        {
            lock (resourceMutex_)
            {
                if (!FileUtil.DirExists(pathName))
                {
                    Log.Error("Could not open directory " + pathName);
                    return false;
                }

                // Convert path to absolute
                string fixedPath = SanitateResourceDirName(pathName);

                // Check that the same path does not already exist
                for (int i = 0; i < resourceDirs_.Count; ++i)
                {
                    if (0 == string.Compare(resourceDirs_[i], fixedPath, true))
                        return true;
                }

                if (priority < resourceDirs_.Count)
                    resourceDirs_.Insert(priority, fixedPath);
                else
                    resourceDirs_.Add(fixedPath);
                Log.Info("Added resource path " + fixedPath);
                return true;
            }

        }

        public bool AddPackageFile(PackageFile package, int priority = int.MaxValue)
        {
            lock (resourceMutex_)
            {
                // Do not add packages that failed to load
                if (!package || package.NumFiles == 0)
                {
                    Log.Error("Could not add package file {0} due to load failure", package.Name);
                    return false;
                }

                if (priority < packages_.Count)
                    packages_.Insert(priority, package);
                else
                    packages_.Add(package);

                Log.Info("Added resource package " + package.Name);
                return true;
            }

        }

        public bool AddPackageFile(string fileName, int priority)
        {
            PackageFile package = new PackageFile();
            return package.Open(fileName) && AddPackageFile(package);
        }

        public void RemoveResourceDir(string pathName)
        {
            lock (resourceMutex_)
            {
                string fixedPath = SanitateResourceDirName(pathName);

                for (int i = 0; i < resourceDirs_.Count; ++i)
                {
                    if (0 == string.Compare(resourceDirs_[i], fixedPath, true))
                    {
                        resourceDirs_.RemoveAt(i);
                        Log.Info("Removed resource path " + fixedPath);
                        return;
                    }
                }
            }

        }

        public void RemovePackageFile(PackageFile package, bool releaseResources, bool forceRelease)
        {
            lock (resourceMutex_)
            {
                for (int i = 0; i < packages_.Count; i++)
                {
                    if (packages_[i] == package)
                    {/*
                        if(releaseResources)
                            ReleasePackageResources(packages_[i], forceRelease);*/
                        Log.Info("Removed resource package " + packages_[i].Name);
                        packages_.RemoveAt(i);
                        return;
                    }
                }
            }

        }

        public void RemovePackageFile(string fileName, bool releaseResources, bool forceRelease)
        {
            lock (resourceMutex_)
            {
                // Compare the name and extension only, not the path
                string fileNameNoPath = FileUtil.GetFileNameAndExtension(fileName);

                for (int i = 0; i < packages_.Count; i++)
                {
                    PackageFile pkg = packages_[i];
                    if (0 == string.Compare(FileUtil.GetFileNameAndExtension(pkg.Name), fileNameNoPath, true))
                    {/*
                        if(releaseResources)
                            ReleasePackageResources(pkg, forceRelease);*/
                        Log.Info("Removed resource package " + pkg.Name);
                        packages_.RemoveAt(i);
                        return;
                    }
                }
            }

        }

        public File GetFile(string nameIn, bool sendEventOnFailure = true)
        {
            lock (resourceMutex_)
            {
                string name = SanitateResourceName(nameIn);
                if (name.Length > 0)
                {
                    File file;
                    if (searchPackagesFirst_)
                    {
                        file = SearchPackages(name);
                        if (file == null)
                            file = SearchResourceDirs(name);
                    }
                    else
                    {
                        file = SearchResourceDirs(name);
                        if (file == null)
                            file = SearchPackages(name);
                    }

                    if (file != null)
                        return file;
                }

                if (sendEventOnFailure)
                {
                    Log.Error("Could not find resource " + name);

                    //if (Context.IsCoreThread)
                    {
                        this.SendGlobalEvent(new ResourceNotFound(name));
                    }
                }

                return null;
            }

        }

        public bool Exists(string nameIn)
        {
            lock (resourceMutex_)
            {
                string name = SanitateResourceName(nameIn);
                if (string.IsNullOrEmpty(name))
                    return false;

                for (int i = 0; i < packages_.Count; ++i)
                {
                    if (packages_[i].Exists(name))
                        return true;
                }

                for (int i = 0; i < resourceDirs_.Count; ++i)
                {
                    if (FileUtil.FileExists(resourceDirs_[i] + name))
                        return true;
                }

                // Fallback using absolute path
                return FileUtil.FileExists(name);
            }

        }

        public string GetResourceFileName(string name)
        {
            for (int i = 0; i < resourceDirs_.Count; ++i)
            {
                if (FileUtil.FileExists(resourceDirs_[i] + name))
                    return resourceDirs_[i] + name;
            }

            if (FileUtil.IsAbsolutePath(name) && FileUtil.FileExists(name))
                return name;
            else
                return string.Empty;
        }


        public string SanitateResourceName(string nameIn)
        {
            // Sanitate unsupported constructs from the resource name
            string name = FileUtil.GetInternalPath(nameIn);
            name = name.Replace("../", "");
            name = name.Replace("./", "");

            // If the path refers to one of the resource directories, normalize the resource name
            if (resourceDirs_.Count > 0)
            {
                string namePath = FileUtil.GetPath(name);
                string exePath = CurrentDir.Replace("/./", "/");
                for (int i = 0; i < resourceDirs_.Count; ++i)
                {
                    string relativeResourcePath = resourceDirs_[i];
                    if (relativeResourcePath.StartsWith(exePath))
                        relativeResourcePath = relativeResourcePath.Substring(exePath.Length);

                    if (namePath.StartsWith(resourceDirs_[i], StringComparison.CurrentCultureIgnoreCase))
                        namePath = namePath.Substring(resourceDirs_[i].Length);
                    else if (namePath.StartsWith(relativeResourcePath, StringComparison.CurrentCultureIgnoreCase))
                        namePath = namePath.Substring(relativeResourcePath.Length);
                }

                name = System.IO.Path.Combine(namePath, FileUtil.GetFileNameAndExtension(name));
            }

            return name.Trim();
        }

        public string SanitateResourceDirName(string nameIn)
        {
            string fixedPath = FileUtil.AddTrailingSlash(nameIn);
            if (!FileUtil.IsAbsolutePath(fixedPath))
                fixedPath = System.IO.Path.Combine(CurrentDir, fixedPath);

            // Sanitate away /./ construct
            fixedPath.Replace("/./", "/");

            return fixedPath.Trim();
        }

        File SearchResourceDirs(string nameIn)
        {
            for (int i = 0; i < resourceDirs_.Count; ++i)
            {
                if (FileUtil.FileExists(resourceDirs_[i] + nameIn))
                {
                    File file = new File(System.IO.File.OpenRead(resourceDirs_[i] + nameIn));
                    file.Name = nameIn;
                    return file;
                }
            }

            // Fallback using absolute path
            if (FileUtil.FileExists(nameIn))
            {
                File file = new File(System.IO.File.OpenRead(nameIn));
                file.Name = nameIn;
                return file;
            }

            return null;
        }

        File SearchPackages(string nameIn)
        {
            for (int i = 0; i < packages_.Count; ++i)
            {
                PackageFile pkg = packages_[i];
                PackageEntry? entry = pkg.GetEntry(nameIn);
                if (entry != null)
                {
                    File file = new File(System.IO.File.OpenRead(pkg.Name));
                    // todo offset ?
                    file.Name = nameIn;
                    //entry.Value.offset_;
                    //entry.Value.size_;
                    return file;
                }
            }

            return null;
        }

        public static File OpenRead(string path)
        {
            return Instance.GetFile(path);
        }

        public static byte[] ReadAllBytes(string path)
        {
            using (File file = OpenRead(path))
            {
                if (file == null)
                {
                    return null;
                }

                return file.ReadAllBytes();
            }
        }

    }
}
