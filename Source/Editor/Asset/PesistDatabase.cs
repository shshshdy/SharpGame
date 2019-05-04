using MessagePack;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace UniqueEditor
{
    [DataContract]
    public enum AssetType
    {
        Serialized,    
        Meta,
        Dir,
    }

    [DataContract]
    public class AssetMeta
    {
        [DataMember(Order = 0)]
        public AssetType Type { get; set; }

        [DataMember(Order = 1)]
        public string FilePath { get; set; }

        [DataMember(Order = 2)]
        public Guid Guid { get; set; }

        [IgnoreDataMember]
        public bool IsDir
        {
            get { return Type == AssetType.Dir; }
        }
    }

    [DataContract]
    public class PesistDatabase : IMessagePackSerializationCallbackReceiver
    {
        [DataMember(Order = 0)]
        public List<AssetMeta> MetaInfo { get; set; } = new List<AssetMeta>();

        Dictionary<Guid, AssetMeta> guidToMeta_ = new Dictionary<Guid, AssetMeta>();
        Dictionary<string, Guid> pathToGuid_ = new Dictionary<string, Guid>();

        
        public void Add(AssetType type, string key, Guid val)
        {
            AssetMeta meta = new AssetMeta() { FilePath = key, Guid = val, Type = type };
            MetaInfo.Add(meta);
            pathToGuid_.Add(key, val);
            guidToMeta_.Add(val, meta);
        }

        public void Remove(string file)
        {
            Remove(PathToGUID(file));
        }

        public void Remove(Guid guid)
        {
            if(guidToMeta_.TryGetValue(guid, out AssetMeta meta))
            {
                guidToMeta_.Remove(guid);
                pathToGuid_.Remove(meta.FilePath);
            }

            MetaInfo.RemoveAll((item) => item.Guid == guid);
        }

        public void Update(Guid guid, string filePath)
        {
            if(guidToMeta_.TryGetValue(guid, out AssetMeta meta))
            {
                if(meta.FilePath != filePath)
                {
                    pathToGuid_.Remove(meta.FilePath);
                    meta.FilePath = filePath;
                    pathToGuid_[filePath] = guid;
                }

            }

        }

        public Guid PathToGUID(string path)
        {
            if(pathToGuid_.TryGetValue(path, out Guid guid))
            {
                return guid;
            }

            return Guid.Empty;
        }

        public string GUIDToPath(Guid guid)
        {
            if(guidToMeta_.TryGetValue(guid, out AssetMeta meta))
            {
                return meta.FilePath;
            }

            return string.Empty;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            foreach(var v in MetaInfo)
            {
                guidToMeta_.Add(v.Guid, v);
                pathToGuid_.Add(v.FilePath, v.Guid);
            }
        }
    }
}
