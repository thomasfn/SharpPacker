using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpPacker
{
    /// <summary>
    /// Represents a file that holds several others
    /// </summary>
    public class PackFile
    {
        private class FileEntry
        {
            public string Name;
            public int Length;
            public int Offset;

            public byte[] CachedData;
            public bool Dirty;
        }

        private List<FileEntry> entries;
        private Dictionary<string, FileEntry> entrymap;

        private string filename;

        public const string HeaderString = "SHARPPACKER";
        public const ushort HeaderVersion = 0x0001;

        private enum PackFlags : uint
        {
            None = 0
        }

        private PackFlags flags;

        private int contentlocation;

        private bool removedfiles;

        /// <summary>
        /// Initialises a new instance of this PackFile
        /// </summary>
        /// <param name="filename">The filename of the packfile</param>
        public PackFile(string filename)
        {
            this.filename = filename;
            entries = new List<FileEntry>();
            entrymap = new Dictionary<string, FileEntry>();
        }

        private Stream OpenRead()
        {
            if (!File.Exists(filename)) return null;
            return File.OpenRead(filename);
        }

        /// <summary>
        /// Gets the number of files stored in this packfile
        /// </summary>
        public int FileCount
        {
            get
            {
                return entries.Count;
            }
        }

        /// <summary>
        /// Loads this pack file
        /// </summary>
        public void Load()
        {
            // Get the stream
            using (Stream strm = OpenRead())
            {
                // Validate
                if (strm == null) throw new FileNotFoundException("File not found", filename);

                // Read and verify header
                BinaryReader rdr = new BinaryReader(strm);
                string header = Encoding.ASCII.GetString(rdr.ReadBytes(HeaderString.Length));
                if (header != HeaderString) throw new Exception("Invalid header");
                ushort version = rdr.ReadUInt16();
                if (version != HeaderVersion) throw new Exception("Invalid version");
                int numentries = rdr.ReadInt32();
                flags = (PackFlags)rdr.ReadUInt32();
                if (numentries < 0) throw new Exception("Invalid entry count");

                // Read all entries
                entries = new List<FileEntry>(numentries);
                entrymap = new Dictionary<string, FileEntry>();
                for (uint i = 0; i < numentries; i++)
                {
                    FileEntry entry = new FileEntry();
                    entry.Name = rdr.ReadString();
                    entry.Length = rdr.ReadInt32();
                    entry.Offset = rdr.ReadInt32();
                    entry.Dirty = false;
                    entry.CachedData = null;
                    if (entrymap.ContainsKey(entry.Name))
                        throw new Exception(string.Format( "Duplicate entry ({0})", entry.Name));
                    entries.Add(entry);
                    entrymap.Add(entry.Name, entry);
                }

                // Record the content location
                contentlocation = (int)strm.Position;
            }
        }

        /// <summary>
        /// Returns if the specified file exists in this packfile or not
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True if the file exists</returns>
        public bool FileExists(string filename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Check for 0 length (means marked for removal)
            if (entry.Length <= 0) return false;

            // It exists
            return true;
        }

        /// <summary>
        /// Returns a stream to the specified file
        /// It is the responsibility of the consumer to close the stream after usage and to not overrun
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>The file data as a stream</returns>
        public Stream GetFile(string filename, out int length)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry))
            {
                length = 0;
                return null;
            }

            // Output length
            length = entry.Length;

            // Is it cached?
            if (entry.CachedData != null)
            {
                // Return a memory stream
                return new MemoryStream(entry.CachedData, false);
            }

            // Open a stream
            Stream strm = OpenRead();
            strm.Seek(contentlocation + entry.Offset, SeekOrigin.Begin);

            // Return it
            return strm;
        }

        /// <summary>
        /// Returns a byte array containing the contents of the specified file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="length"></param>
        /// <param name="cache">Whether or not to cache the file after loading it</param>
        /// <returns>The file data as a byte array</returns>
        public byte[] GetFileRaw(string filename, out int length, bool cache = false)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry))
            {
                length = 0;
                return null;
            }

            // Output length
            length = entry.Length;

            // Check cache
            if (entry.CachedData != null) return entry.CachedData;

            // Load it
            byte[] data;
            using (Stream strm = OpenRead())
            {
                strm.Seek(contentlocation + entry.Offset, SeekOrigin.Begin);
                data = new byte[entry.Length];
                strm.Read(data, 0, entry.Length);
            }

            // Cache if requested
            if (cache) entry.CachedData = data;

            // Return it
            return data;
        }

        /// <summary>
        /// Caches the specified file in memory
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True if successful</returns>
        public bool CacheFile(string filename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Is it already cached?
            if (entry.CachedData != null) return false;

            // Load it
            byte[] data;
            using (Stream strm = OpenRead())
            {
                strm.Seek(contentlocation + entry.Offset, SeekOrigin.Begin);
                data = new byte[entry.Length];
                strm.Read(data, 0, entry.Length);
            }

            // Cache it
            entry.CachedData = data;

            // Done
            return true;
        }

        /// <summary>
        /// Uncaches the specified file from memory
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True if successful</returns>
        public bool UncacheFile(string filename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Is it even cached?
            if (entry.CachedData == null) return false;

            // Remove reference
            entry.CachedData = null;

            // Done
            return true;
        }

        /// <summary>
        /// Returns if the specified file is cached or not
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool FileCached(string filename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Return
            return entry.CachedData != null;
        }

        /// <summary>
        /// Returns the length of the specified file, 0 if file not found
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public int FileLength(string filename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return 0;

            // Return
            return entry.Length;
        }

        /// <summary>
        /// Adds a file to this packfile
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>True if successful</returns>
        public bool AddFile(string filename, byte[] data)
        {
            // Check if it already exists
            if (entrymap.ContainsKey(filename)) return false;

            // Find an offset for us (at the end of the file, or unallocated)
            int offset = 0;
            foreach (FileEntry fentry in entries)
                if (fentry.Offset == -1)
                    offset = -1;
                else
                    offset = Math.Max(offset, fentry.Offset + fentry.Length);

            // Create a new entry
            FileEntry entry = new FileEntry();
            entry.Name = filename;
            entry.Length = data.Length;
            entry.Offset = offset;
            entry.CachedData = data;
            entry.Dirty = true;
            entrymap.Add(filename, entry);
            entries.Add(entry);

            // Success
            return true;
        }

        /// <summary>
        /// Updates the specified file with new data
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="newdata"></param>
        /// <returns></returns>
        public bool UpdateFile(string filename, byte[] newdata)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Set new data
            entry.CachedData = newdata;
            entry.Dirty = true;

            // If the new data is larger, we'll need to allocate a new offset
            if (newdata.Length > entry.Length)
                entry.Offset = -1;
            entry.Length = newdata.Length;

            // Success
            return true;
        }

        /// <summary>
        /// Renames a file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="newfilename"></param>
        /// <returns></returns>
        public bool MoveFile(string filename, string newfilename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Check new filename isn't taken
            if (FileExists(newfilename)) return false;

            // Remove old entry
            entrymap.Remove(filename);
            entrymap.Add(newfilename, entry);

            // Rename and make it dirty
            entry.Name = newfilename;
            entry.Dirty = true;

            // Done
            return true;
        }

        /// <summary>
        /// Marks a file for removal
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool RemoveFile(string filename)
        {
            // Get the entry
            FileEntry entry;
            if (!entrymap.TryGetValue(filename, out entry)) return false;

            // Scrap it
            entrymap.Remove(filename);
            entries.Remove(entry);
            removedfiles = true;

            // Done
            return true;
        }

        /// <summary>
        /// Gets the filenames of all files stored within this packfile
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFiles()
        {
            return entrymap.Keys;
        }

        /// <summary>
        /// Saves any changes or additions to this packfile to file
        /// </summary>
        public void Save()
        {
            // Figure out what we need to do
            bool offsetreallocation = false;
            bool dirtyfiles = false;
            foreach (FileEntry entry in entries)
            {
                if (entry.Offset == -1)
                    offsetreallocation = true;
                if (entry.Dirty)
                    dirtyfiles = true;
            }

            // Do we need to do any work?
            if (!offsetreallocation && !dirtyfiles && !removedfiles) return;

            // Prepare a temporary file to write into
            string tmp = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_temp" + Path.GetExtension(filename));
            if (File.Exists(tmp)) File.Delete(tmp);

            // Open the files
            using (Stream srcstrm = OpenRead())
            {
                using (Stream dststrm = File.OpenWrite(tmp))
                {
                    // Open writer
                    BinaryWriter wtr = new BinaryWriter(dststrm);

                    // Write header
                    WriteHeader(wtr);

                    // Store accum offset
                    int offset = 0;

                    // Store old offsets
                    Dictionary<FileEntry, int> oldoffsets = new Dictionary<FileEntry, int>();

                    // Loop each entry
                    foreach (FileEntry entry in entries)
                    {
                        // Store the old offset
                        oldoffsets.Add(entry, entry.Offset);

                        // Compute offset
                        entry.Offset = offset;
                        offset += entry.Length;

                        // Write the entry
                        wtr.Write(entry.Name);
                        wtr.Write(entry.Length);
                        wtr.Write(entry.Offset);
                    }

                    // Record the new content location
                    int newcontentlocation = (int)dststrm.Length;

                    // Loop each entry
                    foreach (FileEntry entry in entries)
                    {
                        // Get the entry data
                        int entryoffset = oldoffsets[entry];
                        byte[] data;
                        if (entry.CachedData != null)
                            data = entry.CachedData;
                        else if (entryoffset == -1)
                            throw new Exception(string.Format("No data found when trying to write file {0}", entry.Name));
                        else
                        {
                            srcstrm.Seek(contentlocation + entryoffset, SeekOrigin.Begin);
                            data = new byte[entry.Length];
                            srcstrm.Read(data, 0, entry.Length);
                        }

                        // Write into file
                        wtr.Write(data);
                    }
                }
            }

            // Delete the old file and move temp file in
            File.Delete(filename);
            File.Move(tmp, filename);
        }

        private void WriteHeader(BinaryWriter wtr)
        {
            wtr.Write(Encoding.ASCII.GetBytes(HeaderString));
            wtr.Write(HeaderVersion);
            wtr.Write(entries.Count);
            wtr.Write((uint)flags);
        }

    }
}
