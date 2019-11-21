using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace SharpGame
{
    [DataContract]
    public struct ResourceRef
    {
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string FilePath;
        [DataMember]
        public Guid FileID;
        [IgnoreDataMember]
        public Resource resource;

        public readonly static ResourceRef Null = new ResourceRef();
        
        public ResourceRef(Type type, string filePath, Resource resource = null)
        {
            Type = type.Name;
            FilePath = filePath;
            this.resource = resource;
        }

        public T Load<T>() where T : Resource
        {
            var type = Resource.GetType(Type);
            return Resources.Instance.Load(type, FilePath) as T;
        }

        public static ResourceRef Create<T>(string file)
        {
            return new ResourceRef(typeof(T), file, null);
        }

        public bool Equals(in ResourceRef other)
        {
            return Type == other.Type && FileID == other.FileID;
        }

        public override bool Equals(object obj)
        {
            if(obj is ResourceRef)
            {
                return this.Equals((ResourceRef)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 1084822076;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Type);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
            hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(FileID);
            return hashCode;
        }

        public static bool operator ==(ResourceRef lhs, ResourceRef rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ResourceRef lhs, ResourceRef rhs)
        {
            return !lhs.Equals(rhs);
        }
    }

}
