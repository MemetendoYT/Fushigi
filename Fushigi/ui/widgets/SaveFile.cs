using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fushigi.ui.widgets
{
    public class SaveFile
    {
        public bool IsLoaded = false;

        public static string _Path = "progress.sav";
        public static byte[] _Data;
        public static void WriteSaveFile()
        {
            File.WriteAllBytes(_Path, _Data);
        }

        public void CreateBackup()
        {
            File.Copy(_Path, _Path + ".backup", true);
        }

        public int FindBytePatternOffset(byte[] pattern)
        {
            for (int i = 0; i <= _Data.Length - pattern.Length; i++)
            {
                bool match = true;

                for (int j = 0; j < pattern.Length; j++)
                {
                    if (_Data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    return i + pattern.Length;
                }
            }
            return -1;
        }

        // Read/Write
        public static bool ReadBool(int offset)
        {
            return BitConverter.ToBoolean(_Data, offset);

        }

        public static void WriteBool(int offset, bool value)
        {
            _Data[offset] = value ? (byte)0x01 : (byte)0x00;

        }

        public byte GetByte(int offset)
        {
            return _Data[offset];
        }

        public static int ReadInt(int offset)
        {
            return BitConverter.ToInt32(_Data, offset);
        }

        public static void WriteInt(int offset, int value)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            Array.Copy(valueBytes, 0, _Data, offset, sizeof(int));
        }

        public uint ReadUInt32(int offset)
        {
            return BitConverter.ToUInt32(_Data, offset);
        }

        public void WriteUInt32(int offset, uint value)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            Array.Copy(valueBytes, 0, _Data, offset, sizeof(int));
        }

        public float ReadFloat(int offset)
        {
            return BitConverter.ToSingle(_Data, offset);
        }

        public void WriteFloat(int offset, float value)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            Array.Copy(valueBytes, 0, _Data, offset, sizeof(float));
        }

        public string ReadString(int offset)
        {
            byte[] stringData = new byte[0x40];
            Array.Copy(_Data, offset, stringData, 0, 0x40);

            return Encoding.UTF8.GetString(stringData).Replace("\x00", ""); ;
        }

        public void WriteString(int offset, string value)
        {
            if (value == "None") return;

            byte[] byteData = new byte[0x40];
            byte[] stringBytes = Encoding.UTF8.GetBytes(value);

            Array.Copy(stringBytes, 0, byteData, 0, Math.Min(stringBytes.Length, byteData.Length));
            Array.Copy(byteData, 0, _Data, offset, 0x40);
        }

        ////public static void WriteCouseClearNormal(Dictionary<string, LevelInfo> courses, int startOffset, string WriteLocation)
        ////{
        ////    int offset = startOffset;

        ////    foreach (var entry in courses)
        ////    {
        ////        LevelInfo info = entry.Value;

        ////        if (!info.Name.Contains("Break Time") && !info.Name.Contains("Badge") && !info.Name.Contains("Palace") && !info.Name.Contains("Arena"))
        ////        {


        ////            DecideCourse(info, WriteLocation, offset);

        ////            offset += 4;
        ////        }
        ////    }
        ////}
        ////public static void WriteElse(Dictionary<string, LevelInfo> courses, int startOffset, string WriteLocation, string Search)
        ////{
        ////    int offset = startOffset;

        ////    foreach (var entry in courses)
        ////    {

        ////        LevelInfo info = entry.Value;

        ////        if (info.Name.Contains(Search))
        ////        {
        ////            DecideCourse(info, WriteLocation, offset);
        ////        }
        ////    }
        ////}

        ////public static void WriteCouseClearBadge(Dictionary<string, LevelInfo> courses, int startOffset, string WriteLocation)
        ////{
        ////    int offset = startOffset;

        ////    foreach (var entry in courses)
        ////    {
        ////        LevelInfo info = entry.Value;
        ////        if (info.Name.Contains("Badge"))
        ////        {
        ////            DecideCourse(info, WriteLocation, offset);
        ////            offset += 4;
        ////        }

        ////    }
        ////}

        ////public static void WriteCouseClearBreaktime(Dictionary<string, LevelInfo> courses, int startOffset, string WriteLocation)
        ////{
        ////    int offset = startOffset;
        ////    foreach (var entry in courses)
        ////    {

        ////        LevelInfo info = entry.Value;

        ////        if (info.Name.Contains("Break Time"))
        ////        {
        ////            DecideCourse(info, WriteLocation, offset);
        ////            offset += 4;
        ////        }


        ////    }
        ////}



        ////public static void DecideCourse(LevelInfo info, string WriteLocation, int offset)
        ////{
        ////    switch (WriteLocation)
        ////    {
        ////        case "CourseClear":
        ////            info.CourseClearHex = offset;
        ////            break;
        ////        case "GoalSeed":
        ////            if (info.GoalWonderSeed != -1)

        ////            {
        ////                info.GoalWonderSeed = offset;
        ////            }
        ////            break;
        ////        case "WonderSeed":
        ////            if (info.WonderSeed != -1)
        ////            {
        ////                info.WonderSeed = offset;
        ////            }
        ////            break;
        ////        case "PurpleCoin":
        ////            if (info.PurpleCoinOffset != -1)
        ////            {
        ////                info.PurpleCoinOffset = offset;
        ////            }
        ////            break;
        ////        case "ClapperGate":
        ////            if (info.ClapperGate != -1)
        ////            {
        ////                info.ClapperGate = offset;
        ////            }
        ////            break;

        ////    }
        //}
    }
}
