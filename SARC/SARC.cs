using System.Runtime.InteropServices;
using System.Text;

namespace Fushigi.SARC
{
    public class SARC
    {
        [StructLayout(LayoutKind.Sequential, Size = 0x14)]
        public struct SARCHeader
        {
            public uint Magic;
            public ushort HeaderSize;
            public ushort BOM;
            public uint FileSize;
            public uint DataOffset;
            public ushort Version;
            public ushort Padding;
        }

        public struct SFATHeader
        {
            public uint Magic;
            public ushort HeaderSize;
            public ushort NodeCount;
            public uint HashKey;
        }

        public struct SARCFile
        {
            public uint EntryOffs;
            public uint NameHash;
            public uint NameOffs;
            public uint Offs;
            public uint Size;
            public string FileName;
        }

        Dictionary<string, byte[]> ChangedFiles = new Dictionary<string, byte[]>();

        public void SetFile(string path, byte[] data)
        {
            ChangedFiles[path] = data;
        }

        SARCHeader Header;
        SFATHeader SFAT_Header;
        Dictionary<string, SARCFile> Files = new Dictionary<string, SARCFile>();
        MemoryStream Stream;

        public SARC(MemoryStream stream)
        {
            Stream = stream;
            stream.Read(Utils.AsSpan(ref Header));

            if (Header.Magic != 0x43524153)
            {
                throw new InvalidDataException("SARC::SARC() -- Invalid magic.");
            }

            long sFatOffset = stream.Position;

            stream.Read(Utils.AsSpan(ref SFAT_Header));

            if (SFAT_Header.Magic != 0x54414653)
            {
                throw new InvalidDataException("SARC::SARC() -- Invalid SFAT magic.");
            }

            long sFNTOffset = sFatOffset + 0xC + (SFAT_Header.NodeCount * 0x10);

            for (uint i = 0; i < SFAT_Header.NodeCount; i++)
            {
                stream.Seek((long)(sFatOffset + 0xC + (i * 0x10)), SeekOrigin.Begin);
                SARCFile file = new SARCFile();
                file.EntryOffs = (uint)stream.Position;
                file.NameHash = stream.AsBinaryReader().ReadUInt32();
                file.NameOffs = (stream.AsBinaryReader().ReadUInt32() & 0xFFFFFF) << 2;
                file.Offs = stream.AsBinaryReader().ReadUInt32();
                file.Size = stream.AsBinaryReader().ReadUInt32() - file.Offs;

                stream.Seek((long)(sFNTOffset + 0x8 + file.NameOffs), SeekOrigin.Begin);
                file.FileName = Utils.ReadString(stream.AsBinaryReader());

                Files.Add(file.FileName, file);
            }
        }

        public void Write(MemoryStream Stream)
        {
            var writer = new BinaryWriter(Stream);

            writer.Write("SARC"u8);
            writer.Write((ushort)0x14);
            writer.Write((ushort)0xFEFF);
            writer.Write(0xFFFFFFFFu);
            writer.Write(0xFFFFFFFFu);
            writer.Write((ushort)0x100);
            writer.Write((ushort)0);

            var sorted_hashes = Files.Keys.OrderBy(name => Files[name].NameHash).ToList();

            writer.Write("SFAT"u8);
            writer.Write((ushort)0xC);
            writer.Write((ushort)Files.Count);
            writer.Write(SFAT_Header.HashKey);

            uint string_offset = 0;
            uint data_offset = 0;
            foreach (var name in sorted_hashes)
            {
                writer.Write(Files[name].NameHash);
                writer.Write(0x01000000 | (string_offset >> 2));
                data_offset = AlignUp(data_offset, 8);
                writer.Write(data_offset);
                byte[] fileData = OpenFile(name);
                data_offset += (uint)fileData.Length;
                writer.Write(data_offset);
                string_offset += AlignUp((uint)name.Length + 1, 4);
            }

            writer.Write("SFNT"u8);
            writer.Write((ushort)8);
            writer.Write((ushort)0);

            foreach (var name in sorted_hashes)
            {
                writer.Write(Encoding.ASCII.GetBytes(name));
                writer.Write((byte)0);
                Stream.Position = AlignUp((uint)Stream.Position, 4);
            }

            Stream.Position = AlignUp((uint)Stream.Position, 8);
            uint data_start = (uint)Stream.Position;
            foreach (var name in sorted_hashes)
            {
                Stream.Position = AlignUp((uint)Stream.Position, 8);
                writer.Write(OpenFile(name));
            }

            Stream.Position = 0x08;
            writer.Write((uint)Stream.Length);
            Stream.Position = 0x0C;
            writer.Write(data_start);
        }

        public static uint AlignUp(uint n, uint alignment)
        {
            return (n + alignment - 1) & ~(alignment - 1);
        }

        public bool DirectoryExists(string path)
        {
            foreach (string file in Files.Keys)
            {
                if (file.StartsWith(path))
                {
                    return true;
                }
            }

            return false;
        }

        public bool FileExists(string path)
        {
            foreach (string file in Files.Keys)
            {
                if (file == path)
                {
                    return true;
                }
            }

            return false;
        }

        public string[] GetFiles(string path)
        {
            List<string> files = new List<string>();

            foreach(string file in Files.Keys)
            {
                if (file.StartsWith(path))
                {
                    files.Add(file);
                }
            }

            return files.ToArray();
        }

        public byte[] OpenFile(string path)
        {
            if (ChangedFiles.ContainsKey(path))
            {
                return ChangedFiles[path];
            }

            if (!FileExists(path))
            {
                return null;
            }

            SARCFile file = Files[path];

            Stream.Seek(Header.DataOffset + file.Offs, SeekOrigin.Begin);
            return Utils.ReadBytes(Stream.AsBinaryReader(), file.Size);
        }
    }
}
