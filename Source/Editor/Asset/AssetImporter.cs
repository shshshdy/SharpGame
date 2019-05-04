//#define USE_JSON

using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame.Editor
{
    [DataContract]
    public class AssetImporter
    {
        [DataMember]
        public int FileFormatVersion { get; set; }

        [DataMember]
        public Guid Guid { get; set; }
        [DataMember]
        public long AssetTimeStamp { get; set; }

        [DataMember]
        public object UserData { get; set; }

        public virtual void Import() { }



        static List<Tuple<Type, string[]>> fileExtensions_ = new List<Tuple<Type, string[]>>();

        static Dictionary<string, AssetImporter> metaInfos_ = new Dictionary<string, AssetImporter>();
        static PesistDatabase pesistDatabase;
        public int GetFileFormatVersion() => 0;

        internal static void Init()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(AssetImporter)))
                {
                    FileExtensionAttribute attri = type.GetCustomAttribute<FileExtensionAttribute>();
                    if (attri != null)
                    {
                        fileExtensions_.Add(new Tuple<Type, string[]>(type, attri.Ext));
                    }
                }
            }

            LoadPesistInfo();

            DirectoryInfo dataDirInfo = Directory.CreateDirectory(FileSystem.DataPath);
            var entries = dataDirInfo.EnumerateFileSystemInfos("*.*", SearchOption.AllDirectories);
            foreach(var it in entries)
            {
                switch(it)
                {
                    case FileInfo fileInfo:                        
                        ProcessFile(fileInfo, dataDirInfo);
                        break;
                    case DirectoryInfo dirInfo:
                        ProcessDirectory(dirInfo, dataDirInfo);
                        break;
                }
            }

            SavePesistInfo();

        }

        const string pesistPath = "Cache/PesistInfo.bin";
        static void LoadPesistInfo()
        {
            if(System.IO.File.Exists(pesistPath))
            {
#if USE_JSON
                byte[] bytes;//= File.ReadAllBytes(pesistPath);
                string text = File.ReadAllText(pesistPath);
                //bytes = MessagePackSerializer.FromJson(text);
                //pesistDatabase = MessagePackSerializer.Deserialize<PesistDatabase>(bytes);
                pesistDatabase = JsonConvert.DeserializeObject<PesistDatabase>(text);
                pesistDatabase.OnAfterDeserialize();
#else
                using(FileStream stream = System.IO.File.OpenRead(pesistPath))
                    pesistDatabase = MessagePackSerializer.Deserialize<PesistDatabase>(stream);
#endif
                List<string> deleteList = new List<string>();
                foreach(var it in pesistDatabase.MetaInfo)
                {
                    string filePath = it.FilePath;
                    string mataFile = it.FilePath + ".meta";
                    if(it.IsDir)
                    {
                        if(Directory.Exists(filePath))
                        {
                            if(System.IO.File.Exists(mataFile))
                            {
                                AssetImporter m = JsonConvert.DeserializeObject<AssetImporter>(System.IO.File.ReadAllText(mataFile));
                                if(m != null)
                                {
                                    metaInfos_[filePath] = m;
                                }
                                else
                                {
                                    Log.Error("Load meta failed, delete: " + mataFile);
                                    if(System.IO.File.Exists(mataFile))
                                    {
                                        System.IO.File.Delete(mataFile);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log.Error("Empty meta file, delete: " + mataFile);
                            if(System.IO.File.Exists(mataFile))
                            {
                                System.IO.File.Delete(mataFile);
                            }
                            deleteList.Add(filePath);

                        }
                    }
                    else
                    {
                        if(System.IO.File.Exists(filePath))
                        {
                            if(System.IO.File.Exists(mataFile))
                            {
                                AssetImporter m = JsonConvert.DeserializeObject<AssetImporter>(System.IO.File.ReadAllText(mataFile));
                                if(m != null)
                                {
                                    metaInfos_[filePath] = m;
                                }
                                else
                                {
                                    Log.Error("Load meta failed, delete: " + mataFile);
                                    if(System.IO.File.Exists(mataFile))
                                    {
                                        System.IO.File.Delete(mataFile);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Log.Error("Empty meta file, delete: " + mataFile);
                            if(System.IO.File.Exists(mataFile))
                            {
                                System.IO.File.Delete(mataFile);
                            }
                            deleteList.Add(filePath);
                        }
                    }
                    

                }

                foreach(string file in deleteList)
                    pesistDatabase.Remove(file);
            }
            else
            {
                pesistDatabase = new PesistDatabase();
            }

        }

        static void SavePesistInfo()
        {
#if USE_JSON
            File.WriteAllText(pesistPath, JsonConvert.SerializeObject(pesistDatabase, Formatting.Indented));
            //File.WriteAllText(pesistPath, MessagePackSerializer.ToJson<PesistDatabase>(pesistDatabase));
#else
            byte[] bytes = MessagePackSerializer.Serialize(pesistDatabase);
            System.IO.File.WriteAllBytes(pesistPath, bytes);
#endif
        }

        static Type GetTypeByExt(string ext)
        {
            foreach (var fileTypes in fileExtensions_)
            {
                String[] exts = fileTypes.Item2;
                foreach (String ex in exts)
                {
                    if (string.Compare(ex, ext, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return fileTypes.Item1;
                    }
                }

            }

            return null;// typeof(AssetImporter);
        }


        static void ProcessFile(FileInfo fileInfo, DirectoryInfo dataDirInfo)
        {
            if(string.Compare(fileInfo.Extension, ".meta", true) == 0)
            {
                return;
            }

            string path = FileUtil.StandardlizeFile(fileInfo.FullName.Substring(dataDirInfo.Parent.FullName.Length + 1));
            AssetImporter importer;
            if (metaInfos_.TryGetValue(path, out importer))
            {
                if(importer.AssetTimeStamp < fileInfo.LastWriteTime.Ticks)
                {
                    importer.Import();
                }

                return;
            }

            string ext = fileInfo.Extension;
            Type type = GetTypeByExt(ext);
            if (type != null)
            {
                importer = Activator.CreateInstance(type) as AssetImporter;
                importer.FileFormatVersion = importer.GetFileFormatVersion();
                importer.Guid = Guid.NewGuid();
                importer.AssetTimeStamp = fileInfo.LastWriteTime.Ticks;           

                string json = JsonConvert.SerializeObject(importer, Formatting.Indented);
                System.IO.File.WriteAllText(fileInfo.FullName + ".meta", json);
                metaInfos_.Add(path, importer);
                pesistDatabase.Add(AssetType.Meta, path, importer.Guid);
            }

        }

        static void ProcessDirectory(DirectoryInfo dirInfo, DirectoryInfo dataDirInfo)
        {
            string path = FileUtil.StandardlizeFile(dirInfo.FullName.Substring(dataDirInfo.Parent.FullName.Length + 1));
            if(metaInfos_.TryGetValue(path, out AssetImporter importer))
            {
                return;
            }

            importer = new AssetImporter();
            importer.FileFormatVersion = importer.GetFileFormatVersion();
            importer.Guid = Guid.NewGuid();
            importer.AssetTimeStamp = dirInfo.LastWriteTime.Ticks;

            string json = JsonConvert.SerializeObject(importer, Formatting.Indented);
            System.IO.File.WriteAllText(dirInfo.FullName + ".meta", json);
            metaInfos_.Add(path, importer);
            pesistDatabase.Add(AssetType.Dir, path, importer.Guid);
        }

        public static AssetImporter GetAtPath(string path)
        {
            if(metaInfos_.TryGetValue(path, out AssetImporter importer ))
            {
                return importer;
            }

            return null;
        }
    }

}
