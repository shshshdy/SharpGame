using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGame
{

    /// Resource reloading started.
    public struct ReloadStarted { }

    /// Resource reloading finished successfully.
    public struct ReloadFinished { }

    /// Resource reloading failed.
    public struct ReloadFailed { }

    /// Tracked file changed in the resource directories.
    public struct FileChanged
    {
        public string FileName { get; }
        public string ResourceName { get; }
        public FileChanged(string fileName, string resourceName)
        {
            FileName = fileName;
            ResourceName = resourceName;
        }
    }

    /// Resource loading failed.
    public struct LoadFailed
    {
        public string ResourceName { get; }
        public LoadFailed(string resourceName)
        {
            ResourceName = resourceName;
        }
    }

    /// Resource not found.
    public struct ResourceNotFound
    {
        public string ResourceName { get; }
        public ResourceNotFound(string resourceName)
        {
            ResourceName = resourceName;
        }
    }

    /// Unknown resource type.
    public struct UnknownResourceType
    {
        public Type ResourceType { get; }
        public UnknownResourceType(Type resourceType)
        {
            ResourceType = resourceType;
        }
    }

    /// Resource background loading finished.
    public struct ResourceBackgroundLoaded
    {
        public string ResourceName { get; }
        public bool Success { get; }
        public Resource Resource { get; }

        public ResourceBackgroundLoaded(string resourceName, bool success, Resource resource)
        {
            ResourceName = resourceName;
            Success = success;
            Resource = resource;
        }
        
    }

    /// Language changed.
    public struct ChangeLanguage { }

}
