namespace Fushigi.Bfres
{
    [Serializable]
    public class MaterialAnim : IResData, INamed
    {
        public string Name { get; set; }
        public int FrameCount { get; set; }
        public Dictionary<string, MaterialAnimConfigs> MaterialConfigs { get; set; }

        public void Read(BinaryReader reader)
        {
            long start = reader.BaseStream.Position;

            reader.SeekBegin(start + 0x08);
            Name = reader.ReadStringOffset(reader.ReadUInt64());

            reader.SeekBegin(start + 0x58);
            FrameCount = reader.ReadInt32();

            read_fmaa(reader, start);
            reader.SeekBegin(start + 0x70);
        }

        public void read_fmaa(BinaryReader reader, long start)
        {
            reader.SeekBegin(start + 0x28);
            var data_offsets = reader.ReadUInt64();

            reader.SeekBegin(start + 0x62);
            int mat_count = reader.ReadUInt16();

            MaterialConfigs = new Dictionary<string, MaterialAnimConfigs>();
            for (int i = 0; i < mat_count; i++)
            {
                long matStart = (long)data_offsets + i * 0x40;

                reader.SeekBegin(matStart);
                var mat = new MaterialAnimConfigs();
                mat.Name = reader.ReadStringOffset(reader.ReadUInt64());

                reader.SeekBegin(matStart + 0x08);
                long paramOfs = (long)reader.ReadUInt64();

                reader.SeekBegin(matStart + 0x18);
                long curveOfs = (long)reader.ReadUInt64();

                reader.SeekBegin(matStart + 0x32);
                int paramCount = reader.ReadUInt16();

                for (int p = 0; p < paramCount; p++)
                {
                    reader.SeekBegin(paramOfs + p * 0x18);
                    string paramName = reader.ReadStringOffset(reader.ReadUInt64());
                    int firstCurve = reader.ReadUInt16();
                    int curveCount = reader.ReadUInt16();

                    var colors = new float[FrameCount + 1][];

                    for (int frame = 0; frame <= FrameCount; frame++)
                    {
                        float[] color = { 1, 1, 1, 1 };

                        for (int c = firstCurve; c < firstCurve + curveCount; c++)
                        {
                            long curve = curveOfs + c * 0x30;

                            reader.SeekBegin(curve + 0x08);
                            long keys = (long)reader.ReadUInt64();

                            reader.SeekBegin(curve + 0x14);
                            int channel = reader.ReadInt32() / 4;

                            reader.SeekBegin(curve + 0x10);
                            int step = ((reader.ReadUInt16() >> 4) & 7) == 0 ? 16 : 8;   // Kin = 16, Yama = 8

                            reader.SeekBegin(keys + frame * step);
                            if (channel < 4)
                                color[channel] = reader.ReadSingle();
                        }

                        colors[frame] = color;
                    }

                    mat.Colors[paramName] = colors;
                }

                MaterialConfigs[mat.Name] = mat;
            }
        }


        // 1. every frame: match anim groups to materials by name


        [Serializable]
        public class MaterialAnimConfigs
        {
            public string Name { get; set; }
            public Dictionary<string, float[][]> Colors { get; set; } = new();
        }
    }
}

