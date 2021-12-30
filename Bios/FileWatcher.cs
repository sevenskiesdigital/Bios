
using System;
using System.IO;
using System.Diagnostics;

namespace Bios
{
    public class FileWatcher
    {
        public FileWatcher()
        {
            Debug.WriteLine("Here..");
            // Cek Folder Images
            using var watcher = new FileSystemWatcher(@"D:\bios\Bios\Bios\images");
            watcher.NotifyFilter = NotifyFilters.Attributes
                             | NotifyFilters.CreationTime
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.LastAccess
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;

            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.Filter = "*.txt";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            Debug.WriteLine("There..");
        }
        

        static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            Debug.WriteLine($"Changed: {e.FullPath}");
        }

        static void OnCreated(object sender, FileSystemEventArgs e)
        {
            string value = $"Created: {e.FullPath}";
            Debug.WriteLine(value);
        }

        static void OnDeleted(object sender, FileSystemEventArgs e) =>
            Debug.WriteLine($"Deleted: {e.FullPath}");

        static void OnRenamed(object sender, RenamedEventArgs e)
        {
            Debug.WriteLine($"Renamed:");
            Debug.WriteLine($"    Old: {e.OldFullPath}");
            Debug.WriteLine($"    New: {e.FullPath}");
        }

        static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Debug.WriteLine($"Message: {ex.Message}");
                Debug.WriteLine("Stacktrace:");
                Debug.WriteLine(ex.StackTrace);
                PrintException(ex.InnerException);
            }
        }
    }
}
