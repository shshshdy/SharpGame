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
        public string Type { get => type.Name; set => type = Resource.nameToType[value]; }
        [DataMember]
        public Guid FileID;

        [IgnoreDataMember]
        public Type type;
        [IgnoreDataMember]
        public Resource resource;
        
        public ResourceRef()
        {
        }

        public ResourceRef(string type, Guid guid, Resource resource = null)
        {
            this.Type = type;
            this.FileID = guid;
            this.resource = resource;
        }

        public ResourceRef(Type type, Guid guid, Resource resource = null)
            : this(type.Name, guid, resource)
        {
        }

        public ResourceRef(string type, string file)
            : this(type, Resources.Instance.GetGuid(file), null)
        {
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
