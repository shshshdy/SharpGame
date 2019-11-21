using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    [DataContract]
    public class ResourceRef : DisposeBase
    {
        [DataMember]
        public string Type { get => type?.Name; set => type = Resource.GetType(value); }

        [DataMember]
        public string FilePath;

        [DataMember]
        public Guid FileID;

        [IgnoreDataMember]
        public Type type;

        [IgnoreDataMember]
        public Resource resource;
        
        public ResourceRef()
        {
        }

        public ResourceRef(Type type, string filePath, Resource resource = null)
        {
            Type = type.Name;
            FilePath = filePath;
            this.resource = resource;
        }

        public T Load<T>() where T : Resource
        {
            return Resources.Instance.Load(type, FilePath) as T;
        }

        public static ResourceRef Create<T>(string file)
        {
            return new ResourceRef(typeof(T), file, null);
        }

    }

}
