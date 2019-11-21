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
            this.resource = resource;
        }

        public ResourceRef(Type type, string filePath, Resource resource = null)
        {
            Type = type.Name;
            FilePath = filePath;
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

}
