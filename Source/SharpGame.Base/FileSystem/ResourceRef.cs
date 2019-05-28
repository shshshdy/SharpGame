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
        public string type;
        [DataMember]
        public Guid fileID;

        [IgnoreDataMember]
        public Resource resource;

        public readonly static ResourceRef Null = new ResourceRef();

        public ResourceRef(string type, Guid guid, Resource resource = null)
        {
            this.type = type;
            this.fileID = guid;
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
    public struct ResourceRefList
    {
        [DataMember]
        public string type;
        [DataMember]
        public List<Guid> fileIDs;

        public readonly static ResourceRefList Null = new ResourceRefList();

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
