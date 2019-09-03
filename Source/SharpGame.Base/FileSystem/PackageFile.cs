using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{

    /// %File entry within the package file.
    public struct PackageEntry
    {
        /// Offset from the beginning.
        public int offset_;
        /// File size.
        public int size_;
        /// File checksum.
        public int checksum_;
    }

    public class PackageFile : Object
    {
        Dictionary<string, PackageEntry> entries_ = new Dictionary<string, PackageEntry>();
        /// File name.
        string fileName_;
        /// Package file total size.
        int totalSize_ = 0;
        /// Total data size in the package using each entry's actual size if it is a compressed package file.
        int totalDataSize_ = 0;
        /// Package file checksum.
        int checksum_ = 0;
        /// Compressed flag.
        bool compressed_ = false;

        /// Return all file entries.
        public Dictionary<string, PackageEntry> Entries => entries_;

        /// Return the package file name.
        public string Name => fileName_;

        /// Return number of files.
        public int NumFiles => entries_.Count;

        /// Return total size of the package file.
        public int GetTotalSize => totalSize_;

        /// Return total data size from all the file entries in the package file.
        public int GetTotalDataSize => totalDataSize_;

        /// Return checksum of the package file contents.
        public int GetChecksum => checksum_;

        /// Return whether the files are compressed.
        public bool IsCompressed => compressed_;

        /// Return list of file names in the package.
        public ICollection<string> EntryNames => entries_.Keys;

        public PackageFile()
        {
        }

        public PackageFile(string fileName, int startOffset = 0)
        {
            Open(fileName, startOffset);
        }

        public bool Exists(string fileName)
        {
            bool found = entries_.ContainsKey(fileName);
            return found;
        }

        internal PackageEntry? GetEntry(string fileName)
        {
            if(entries_.TryGetValue(fileName, out PackageEntry ret))
            {
                return ret;
            }

            return null;
        }

        public bool Open(string fileName, int startOffset = 0)
        {
            File file = new File(System.IO.File.OpenRead(fileName));

            // Check ID, then read the directory
            file.Seek(startOffset);

            string id = file.ReadCString();
            if(id != "UPAK" && id != "ULZ4")
            {
                // If start offset has not been explicitly specified, also try to read package size from the end of file
                // to know how much we must rewind to find the package start
                if(startOffset == 0)
                {
                    int fileSize = (int)file.Length;
                    file.Seek((fileSize - sizeof(uint)));
                    int newStartOffset = fileSize - file.Read<int>();
                    if(newStartOffset < fileSize)
                    {
                        startOffset = newStartOffset;
                        file.Seek(startOffset);
                        id = file.ReadCString();
                    }
                }

                if(id != "UPAK" && id != "ULZ4")
                {
                    Log.Error(fileName + " is not a valid package file");
                    return false;
                }
            }

            fileName_ = fileName;
            totalSize_ = (int)file.Length;
            compressed_ = id == "ULZ4";

            int numFiles = file.Read<int>();
            checksum_ = file.Read<int>();

            for(int i = 0; i < numFiles; ++i)
            {
                string entryName = file.ReadCString();
                PackageEntry newEntry;
                newEntry.offset_ = file.Read<int>() + startOffset;
                totalDataSize_ += (newEntry.size_ = file.Read<int>());
                newEntry.checksum_ = file.Read<int>();
                if(!compressed_ && newEntry.offset_ + newEntry.size_ > totalSize_)
                {
                    Log.Error("File entry " + entryName + " outside package file");
                    return false;
                }
                else
                    entries_[entryName] = newEntry;
            }

            return true;
        }

    }
}
