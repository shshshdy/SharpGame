using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public interface IResourceReader
    {
        Type ResourceType { get; }
        Resource Load(string name);
    }
    
    public class ResourceReader<T> : IResourceReader where T : Resource
    {
        public Type ResourceType => typeof(T);

        public virtual Resource Load(string name)
        {
            // Attempt to load the resource
            File stream = FileSystem.Instance.OpenFile(name);
            if(stream == null)
                return null;

            T resource = Activator.CreateInstance<T>();

            if (!resource)
            {
                Log.Error("Could not load unknown resource type " + ResourceType.ToString());
                return null;
            }
         
            if (!OnLoad(resource, stream))
            {
                stream.Dispose();
                resource.Dispose();
                return null;               
            }

            stream.Dispose();
            return resource;
        }

        /// <summary>
        /// Standard load
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual bool OnLoad(T resource, File stream)
        {
            return resource.Load(stream);
        }

    }
}
