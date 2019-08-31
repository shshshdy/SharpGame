using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{
    public interface IResourceReader
    {
        Type ResourceType { get; }
        Resource LoadResource(string name);
    }
    
    public class ResourceReader<T> : IResourceReader where T : Resource
    {
        public Type ResourceType => typeof(T);

        protected FileSystem FileSystem => FileSystem.Instance;
        protected string extension = "";
        protected string loadingFile;
        public ResourceReader(string ext)
        {
            extension = ext;
        }

        protected bool MatchExtension(string name)
        {
            if(string.IsNullOrEmpty(extension))
            {
                return true;
            }

            string ext = FileUtil.GetExtension(name);
            return extension.IndexOf(ext, StringComparison.CurrentCultureIgnoreCase) != -1;
        }

        public T Load(string name) => LoadResource(name) as T;

        public virtual Resource LoadResource(string name)
        {
            loadingFile = name;

            if (!MatchExtension(name))
            {
                return null;
            }
            
            // Attempt to load the resource
            File stream = FileSystem.Instance.GetFile(name);
            if(stream == null)
                return null;

            T resource = Activator.CreateInstance<T>();        
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
