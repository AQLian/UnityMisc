using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IFix.Core
{
    public static class PatchCombiner
    {
        // [Header]
        // 4 bytes: Magic "PATC"
        // 2 bytes: Version
        // 2 bytes: Entry count
        // N entries (each 8 bytes):
        //     4 bytes: Offset to patch data
        //     4 bytes: Length of patch data

        // [Data]
        // Concatenated patch files

        private static readonly byte[] MAGIC = Encoding.ASCII.GetBytes("PATC");
        private const ushort VERSION = 1;

        public static void Create(string[] patchFiles, string outputPath)
        {
            List<(byte[] data, uint length)> patches = new List<(byte[], uint)>();
            List<(uint offset, uint length)> entries = new List<(uint, uint)>();

            // Read all patch files and their lengths
            foreach (var file in patchFiles)
            {
                var data = File.ReadAllBytes(file);
                patches.Add((data, (uint)data.Length));
            }

            // Calculate header size: magic(4) + version(2) + count(2) + (offset(4) + length(4)) * count
            uint headerSize = (uint)(MAGIC.Length + 
                                    sizeof(ushort) + 
                                    sizeof(ushort) + 
                                    (sizeof(uint) * 2) * patches.Count);
            uint currentOffset = headerSize;

            // Calculate offsets and lengths for each patch
            foreach (var patch in patches)
            {
                entries.Add((currentOffset, patch.length));
                currentOffset += patch.length;
            }

            // Write output file
            using (var stream = new FileStream(outputPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // Write header
                writer.Write(MAGIC);
                writer.Write(VERSION);
                writer.Write((ushort)patches.Count);
                
                // Write offset table (offset + length for each entry)
                foreach (var entry in entries)
                {
                    writer.Write(entry.offset);
                    writer.Write(entry.length);
                }

                // Write patch data
                foreach (var patch in patches)
                {
                    writer.Write(patch.data);
                }
            }
        }
    }
}