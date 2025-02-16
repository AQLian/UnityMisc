using System;
using System.Collections.Generic;
using System.IO;

namespace IFix.Core
{
    public class SubPatchProcessor
    {
        public struct PatchHeaderEntry
        {
            public int Offset;
            public int Length;
        }

        public void ProcessBigPatchFile(string filePath, Action<BinaryReader> processSubPatch)
        {
            using (FileStream fs = File.OpenRead(filePath))
            using (BinaryReader mainReader = new BinaryReader(fs))
            {
                // 1. Verify Header
                byte[] fileMagic = reader.ReadBytes(4);
                if (!fileMagic.SequenceEqual(PatchCombiner.MAGIC))
                {
                    throw new InvalidDataException("Not a valid patch file (invalid magic)");
                }

                ushort numSubPatches = mainReader.ReadUInt16();
                List<PatchHeaderEntry> subPatches = new List<PatchHeaderEntry>();
                for (int i = 0; i < numSubPatches; i++)
                {
                    subPatches.Add(new PatchHeaderEntry
                    {
                        Offset = mainReader.ReadInt32(), 
                        Length = mainReader.ReadInt32()
                    });
                }

                // 2. Process each sub-patch
                foreach (var patch in subPatches)
                {
                    // Create a substream limited to the sub-patch's data section
                    using (var subStream = new SubStream(fs, patch.Offset, patch.Length))
                    using (BinaryReader patchReader = new BinaryReader(subStream))
                    {
                        ProcessSubPatch(patchReader);
                        processSubPatch?.Invoke(patchReader);
                    }
                }
            }
        }

        private void ProcessSubPatch(BinaryReader reader)
        {
            // byte[] magic = reader.ReadBytes(4);
            // Console.WriteLine($"Sub-patch magic: {BitConverter.ToString(magic)}");
        }

        // Custom stream to limit reading to a specific offset + length
        public class SubStream : Stream
        {
            private readonly Stream _baseStream;
            private readonly long _originalPosition;
            private readonly long _length;
            private long _position;

            public SubStream(Stream baseStream, long offset, long length)
            {
                _baseStream = baseStream;
                _originalPosition = baseStream.Position;
                _baseStream.Seek(offset, SeekOrigin.Begin);
                _length = length;
                _position = 0;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                long remaining = _length - _position;
                if (remaining <= 0) return 0;
                if (count > remaining) count = (int)remaining;

                int bytesRead = _baseStream.Read(buffer, offset, count);
                _position += bytesRead;
                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override void Flush() => _baseStream.Flush();
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _length;
            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                _baseStream.Seek(_originalPosition, SeekOrigin.Begin);
                base.Dispose(disposing);
            }
        }
    }
}