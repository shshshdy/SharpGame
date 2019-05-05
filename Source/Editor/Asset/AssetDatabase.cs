using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;

namespace SharpGame.Editor
{
    public class AssetDatabase : Object
    {
        ConcurrentQueue<FileSystemEventArgs> changeFiles_ = new ConcurrentQueue<FileSystemEventArgs>();
        public AssetDatabase()
        {
            //AssetImporter.Init();

            //WatcherStart(FileSystem.DataPath, "*.*");

            SubscribeToEvent<BeginFrame>(HandleBeginFrame);

        }
        

        private void WatcherStart(string path, string filter)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();

            watcher.Path = path;
            watcher.Filter = filter;

            watcher.Changed += (o, e) => changeFiles_.Enqueue(e);
            watcher.Created += (o, e) => changeFiles_.Enqueue(e);
            watcher.Deleted += (o, e) => changeFiles_.Enqueue(e);
            watcher.Renamed += (o, e) => changeFiles_.Enqueue(e);

            watcher.EnableRaisingEvents = true;
            watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
            watcher.IncludeSubdirectories = true;
        }

        private void HandleBeginFrame(ref BeginFrame e)
        {
            while(!changeFiles_.IsEmpty)
            {
                if(changeFiles_.TryDequeue(out var fileEvent))
                {
                    switch(fileEvent)
                    {
                        case RenamedEventArgs renameEvent:
                            OnRenamed(renameEvent);
                            break;
                        default:
                            if(fileEvent.ChangeType == WatcherChangeTypes.Created)
                            {
                                OnCreated(fileEvent);
                            }
                            else if(fileEvent.ChangeType == WatcherChangeTypes.Deleted)
                            {
                                OnDeleted(fileEvent);

                            }
                            else if(fileEvent.ChangeType == WatcherChangeTypes.Changed)
                            {
                                OnChanged(fileEvent);
                            }
                            break;
                    }

                }

            }

        }

        
        private void OnCreated(FileSystemEventArgs e)
        {
            Console.WriteLine("File Create {0}  {1}  {2}", e.ChangeType, e.FullPath, e.Name);
        }

        private void OnChanged(FileSystemEventArgs e)
        {
            string path = FileUtil.StandardlizeFile(e.Name);
            if(path.EndsWith(".vs") || path.EndsWith(".ps") || path.EndsWith(".cs"))
            {
                int idx = path.LastIndexOf('/');
                path = path.Substring(0, idx) + ".shader";
            }

            ResourceCache cache = Get<ResourceCache>();
            Resource obj = cache.GetExistingResource(null, path);
            if(obj)
                obj.Modified = true;
        }

        private void OnDeleted(FileSystemEventArgs e)
        {
            Console.WriteLine("File Delete {0}  {1}   {2}", e.ChangeType, e.FullPath, e.Name);
        }

        private void OnRenamed(RenamedEventArgs e)
        {
            Console.WriteLine("File Rename {0}  {1}  {2}", e.ChangeType, e.FullPath, e.Name);
        }
    }
}
