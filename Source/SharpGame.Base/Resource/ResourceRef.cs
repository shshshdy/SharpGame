using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    [DataContract]
    public class ResourceRef
    {
        [DataMember]
        public string Type { get => type.Name; set => type = Resource.GetType(value); }
        [DataMember]
        public Guid FileID;
        [DataMember]
        public string FilePath;

        [IgnoreDataMember]
        public Type type;
        [IgnoreDataMember]
        public Resource resource;
        
        public ResourceRef()
        {
        }

        public ResourceRef(Type type, Guid guid, Resource resource = null)            
        {
            Type = type.Name;
            FileID = guid;
            this.resource = resource;
        }

        public ResourceRef(Type type, string filePath, Resource resource = null)
        {
            Type = type.Name;
            FilePath = filePath;
            FileID = Resources.Instance.GetGuid(filePath);
            this.resource = resource;
        }

        public Resource Load()
        {
            return Resources.Instance.Load(this);
        }

        public static ResourceRef Create<T>(string file)
        {
            return new ResourceRef(typeof(T), file, null);
        }

    }

    [DataContract]
    public class ResourceRefList
    {
        [DataMember]
        public string type;
        [DataMember]
        public List<Guid> fileIDs;
        
        public ResourceRefList()
        {
        }

        public ResourceRefList(string type, params Guid[] guids)
        {
            this.type = type;
            this.fileIDs = new List<Guid>(guids);
        }

        public ResourceRefList(Type type, params Guid[] guids)
            : this(type.Name, guids)
        {
        }

        public ResourceRefList(string type, params string[] files)
        {  
            this.type = type;
            this.fileIDs = new List<Guid>();

            foreach(var file in files)
            {
                this.fileIDs.Add(Resources.Instance.GetGuid(file));
            }
        }
    }
}
