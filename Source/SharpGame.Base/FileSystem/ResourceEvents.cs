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
        public String FileName { get; }
        public String ResourceName { get; }
        public FileChanged(String fileName, String resourceName)
        {
            FileName = fileName;
            ResourceName = resourceName;
        }
    }

    /// Resource loading failed.
    public struct LoadFailed
    {
        public String ResourceName { get; }
        public LoadFailed(String resourceName)
        {
            ResourceName = resourceName;
        }
    }

    /// Resource not found.
    public struct ResourceNotFound
    {
        public String ResourceName { get; }
        public ResourceNotFound(String resourceName)
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
        public String ResourceName { get; }
        public bool Success { get; }
        public Resource Resource { get; }

        public ResourceBackgroundLoaded(String resourceName, bool success, Resource resource)
        {
            ResourceName = resourceName;
            Success = success;
            Resource = resource;
        }
        
    }

    /// Language changed.
    public struct ChangeLanguage { }

}
