using DiscordRPC.Message;
using Fasterflect;
using Fushigi.course;
using Fushigi.env;
using Fushigi.gl;
using Fushigi.Msbt;
using Fushigi.ui.modal;
using Fushigi.util;
using FuzzySharp.Edits;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using OpenAbility.ImGui.Nodes;
using SarcLibrary;
using Silk.NET.Core;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ZstdSharp.Unsafe;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{

    public class LevelInfo(string name) : SaveFile
    {
        public string Name = name;
        public int CourseClear { get; set; }
        public bool CourseClearVal { get; set; }
        public int GoalWonderSeed { get; set; }
        public bool GoalWonderSeedVal { get; set; }
        public int WonderSeed { get; set; }
        public bool WonderSeedVal { get; set; }
        public int PurpleCoin { get; set; }
        public bool PurpleCoin1 { get; set; }
        public bool PurpleCoin2 { get; set; }
        public bool PurpleCoin3 { get; set; }
        public int ClapperGate { get; set; }
        public bool ClapperGateVal { get; set; }
        public int PurpleCoinOffset { get; set; }
        public bool SkipOffset { get; set; }

    }
    public static class Byte_Patterns
    {
        //Game Progression
        public static byte[] COMPLETE_GAME = new byte[] { 0xB4, 0xC9, 0x3E, 0x5D }; // Medal on Intro Screen
        public static byte[] INTRO_CUTSCENE_COMPLETED = new byte[] { 0x52, 0xCC, 0xF1, 0x89 }; // Has the intro been played
        public static byte[] GRAND_SEED_WORLD1 = new byte[] { 0x59, 0x58, 0x81, 0x55 }; // Seed for World 1
        public static byte[] GRAND_SEED_WORLD2 = new byte[] { 0x86, 0xBA, 0xAB, 0x49 }; // Seed for World 2
        public static byte[] GRAND_SEED_WORLD3 = new byte[] { 0xD6, 0xD8, 0x50, 0xB5 }; // Seed for World 3
        public static byte[] GRAND_SEED_WORLD4 = new byte[] { 0x6E, 0x7F, 0xCF, 0x1D }; // Seed for World 4
        public static byte[] GRAND_SEED_WORLD5 = new byte[] { 0x00, 0x3E, 0x5A, 0x0D }; // Seed for World 5
        public static byte[] GRAND_SEED_WORLD6 = new byte[] { 0x2B, 0x0D, 0x66, 0xD4 }; // Seed for World 6
        public static byte[] COINS_PATTERN = new byte[] { 0x21, 0xBB, 0xF0, 0x17 }; // Coins
        public static byte[] PURPLE_COINS_PATTERN = new byte[] { 0x27, 0x68, 0xEE, 0xF4 }; // Purple Coins
        public static int LIVES = 0x167C;

    }
    public class RegOffset : SaveFile
    {
        public int OffsetValue;
        public int newValue;
        public int maxValue;
        public bool useNext;
        public RegOffset(byte[] offset, bool useNextOffset, int max)
        {
            maxValue = max;
            OffsetValue = FindBytePatternOffset(offset);
            useNext = useNextOffset;

            if (useNextOffset)
            {
                newValue = _Data[OffsetValue] | (_Data[OffsetValue + 1] << 8);
            }
            else
            {
                newValue = BitConverter.ToInt32(_Data, OffsetValue);
            }
        }


        public RegOffset(int offset, int max)
        {
            maxValue = max;
            OffsetValue = offset;
            newValue = _Data[offset];
        }
    }

    public class SaveEditor
    {
        private static readonly Dictionary<string, int> W1CourseClearOffsets = new Dictionary<string, int>()
        {
            { "0", 0x43F0 },
            { "15", 0x4460 },
            { "20", 0x4404 },
            { "25", 0x43F0 },
            { "3", 0x4438 },
            { "4", 0x44B0 },
        };

        private static readonly Dictionary<string, int> W1GoalSeedOffsets = new Dictionary<string, int>()
        {
            { "0", 0x3348 },
            { "25",0x33E0 },
            { "3", 0x3390 },
            { "4", 0x3408 }
        };

        private static readonly Dictionary<string, int> W1WonderSeedOffsets = new Dictionary<string, int>()
        {
            { "0", 0x3AF8 },
        };

        private static readonly Dictionary<string, int> W1GateOffsets = new Dictionary<string, int>()
        {
            { "15", 0x0CD3 },
        };

        private static readonly Dictionary<string, int> W1PurpleCoinOffsets = new Dictionary<string, int>()
        {
            { "0", 0x1718 },
            { "15", 0x1788 },
            { "25", 0x17B0 },
            { "3", 0x1760 },
        };

        private Dictionary<string, RegOffset> MiscInfo = new();
        private static Dictionary<string, LevelInfo> World1Levels = new();
        private static Dictionary<string, LevelInfo> World2Levels = new();
        private Dictionary<string, LevelInfo>[] Worlds =
        {
            World1Levels,
            World2Levels
        };
        private bool hasInit = false;

        public void InitializeOffsets()
        {
            MiscInfo["Coin Count"] = new RegOffset(Byte_Patterns.COINS_PATTERN, false, 99);
            MiscInfo["Purple Coin Count"] = new RegOffset(Byte_Patterns.PURPLE_COINS_PATTERN, true, 999);
            MiscInfo["Lives"] = new RegOffset(Byte_Patterns.LIVES, 99);
       
            World1Levels["COURSE_001"] = new LevelInfo("Welcome to the Flower Kingdom!");
            World1Levels["COURSE_002"] = new LevelInfo("Piranha Plants on Parade");
            World1Levels["COURSE_004"] = new LevelInfo("Scram, Skedaddlers!");
            World1Levels["COURSE_005"] = new LevelInfo("Bulrush Coming Through!");
            World1Levels["COURSE_003"] = new LevelInfo("Here Come the Hoppos");
            World1Levels["COURSE_200"] = new LevelInfo("Wiggler Race Mountaineering!");
            World1Levels["COURSE_013"] = new LevelInfo("Rolla Koopa Derby")
            {
                SkipOffset = true
            };
            World1Levels["COURSE_007"] = new LevelInfo("Swamp Pipe Crawl");
            World1Levels["COURSE_008"] = new LevelInfo("Angry Spikes and Sinkin' Pipes");
            World1Levels["COURSE_009"] = new LevelInfo("Bulrush Express");
            World1Levels["COURSE_006"] = new LevelInfo("Sproings in the Twilight Forest");
            World1Levels["COURSE_010"] = new LevelInfo("Cosmic Hoppos");
            World1Levels["COURSE_300"] = new LevelInfo("Badge Challenge Parachute Cap I");
            World1Levels["COURSE_304"] = new LevelInfo("Badge Challenge Wall-Climb Jump I");
            World1Levels["COURSE_316"] = new LevelInfo("Expert Badge Challenge Jet Run I");
            World1Levels["COURSE_418"] = new LevelInfo("Break Time! Hurry, Hurry");
            World1Levels["COURSE_411"] = new LevelInfo("Break Time! Wonder Token Tunes");
            World1Levels["COURSE_400"] = new LevelInfo("Break Time! Pop Up, Hoppo!");
            World1Levels["COURSE_150"] = new LevelInfo("Pipe-Rock Plateau Palace");
            World1Levels["COURSE_250"] = new LevelInfo("KO Arena Pipe-Rock Rumble");

            WriteOffset(World1Levels, W1CourseClearOffsets, "CourseClear");
            WriteOffset(World1Levels, W1WonderSeedOffsets, "WonderSeed");
            WriteOffset(World1Levels, W1GoalSeedOffsets, "GoalWonderSeed");
            WriteOffset(World1Levels, W1GateOffsets, "ClapperGate");
            WriteOffset(World1Levels, W1PurpleCoinOffsets, "PurpleCoin");
        }

        private void WriteOffset(Dictionary<string, LevelInfo> Levels, Dictionary<string, int> offsets, string property)
        {
            foreach ((var CourseValue, var startOffset) in offsets)
            {
                int offset = startOffset;

                var prop = typeof(LevelInfo).GetProperty(property);
                var propVal = typeof(LevelInfo).GetProperty(property + "Val");

                foreach ((var name, var level) in Levels)
                {
                    var split = name.Split("COURSE_")[1];;
                    if (split.StartsWith(CourseValue))
                    {
                        if (level.SkipOffset)
                            offset += 4;

                            prop.SetValue(level, offset);

                        if (property == "PurpleCoin")
                        {
                            ReadBigCoin(offset, level);
                        }
                        else
                        {
                            bool value = SaveFile.ReadBool(offset);
                            propVal.SetValue(level, value);
                        }
                        offset += 4;

                    }
                }
            }
        }

        public void ReadBigCoin(int value, LevelInfo level)
        {
            byte newValue = 0;
            if (value != 0)
                newValue = Convert.ToByte(SaveFile.ReadInt(value));

            level.PurpleCoin1 = (newValue & 1) != 0;
            level.PurpleCoin2 = ((newValue >> 1) & 1) != 0;
            level.PurpleCoin3 = ((newValue >> 2) & 1) != 0;
        }
        public void Draw()
        {
            ImGui.Begin("Save Editor");


            if (ImGui.Button("Read Savefile"))
            {
                FileDialog dlg = new FileDialog();


                if (dlg.ShowDialog())
                {
                    SaveFile._Path = dlg.SelectedPath;
                    SaveFile._Data = File.ReadAllBytes(SaveFile._Path);
                    InitializeOffsets();
                    hasInit = true;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Write Savefile"))
            {
                foreach (var file in MiscInfo)
                {
                    var data = file.Value;

                    int value = data.newValue;

                    if (data.useNext)
                    {
                        byte lowByte = (byte)(value & 0xFF);
                        byte highByte = (byte)((value >> 8) & 0xFF);
                        SaveFile._Data[data.OffsetValue] = lowByte;
                        SaveFile._Data[data.OffsetValue + 1] = highByte;
                        Console.WriteLine(" uejihst uiehtghiesgh");
                    }
                    else
                    {
                        SaveFile.WriteInt(data.OffsetValue, data.newValue);
                    }
                }

                foreach (var world in Worlds)
                {
                    foreach (var file in world)
                    {
                        var level = file.Value;

                        if (level.CourseClear > 0)
                            SaveFile.WriteBool(level.CourseClear, level.CourseClearVal);

                        if (level.GoalWonderSeed > 0)
                            SaveFile.WriteBool(level.GoalWonderSeed, level.GoalWonderSeedVal);

                        if (level.WonderSeed > 0)
                            SaveFile.WriteBool(level.WonderSeed, level.WonderSeedVal);

                        if (level.ClapperGate > 0)
                            SaveFile.WriteBool(level.ClapperGate, level.ClapperGateVal);

                        if (level.PurpleCoin > 0)
                        {
                            int coinVal =
                                Convert.ToInt32(level.PurpleCoin1) +
                                Convert.ToInt32(level.PurpleCoin2) * 2 +
                                Convert.ToInt32(level.PurpleCoin3) * 4;

                            SaveFile.WriteInt(level.PurpleCoin, coinVal);
                        }
                        Console.WriteLine(file.Key + " " + level.CourseClear + " " + level.CourseClearVal);
                    }
                }
                SaveFile.WriteSaveFile();
            }

            if (!hasInit)
                return;

            if (ImGui.BeginTabBar("SaveData", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    if (ImGui.BeginTable("##General", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        foreach ((string label, RegOffset offset) in MiscInfo)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);

                            ImGui.Text(label);
                            ImGui.TableNextColumn();

                            var val = offset.newValue;
                            ImGui.DragInt($"##{label}", ref val, 1f, 0, offset.maxValue);
                            offset.newValue = val;
                        }
                        ImGui.EndTable();
                    }
                    ImGui.EndTabItem();
                }

                for (int i = 0; i < Worlds.Length; i++)
                {
                    if (ImGui.BeginTabItem($"World {i + 1}"))
                    {
                        if (ImGui.BeginTable($"##World{i + 1}", 8, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            ImGui.TableSetupColumn("Level Name");
                            ImGui.TableSetupColumn("Course Clear");
                            ImGui.TableSetupColumn("Wonder Seed");
                            ImGui.TableSetupColumn("Goal Wonder Seed");
                            ImGui.TableSetupColumn("Gate");
                            ImGui.TableSetupColumn("BigPurple1");
                            ImGui.TableSetupColumn("BigPurple2");
                            ImGui.TableSetupColumn("BigPurple3");
                            ImGui.TableHeadersRow();
                            foreach ((string label, LevelInfo offset) in Worlds[i])
                            {
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);

                                ImGui.Text(offset.Name);
                                ImGui.TableNextColumn();

                                var val = offset.CourseClearVal;
                                ImGui.Checkbox($"##Clear{label}", ref val);
                                offset.CourseClearVal = val;

                                ImGui.TableNextColumn();
                                if (offset.WonderSeed > 0)
                                {
                                    val = offset.WonderSeedVal;
                                    ImGui.Checkbox($"##WonderSeed{label}", ref val);
                                    offset.WonderSeedVal = val;
                                }

                                ImGui.TableNextColumn();
                                if (offset.GoalWonderSeed > 0)
                                {
                                    val = offset.GoalWonderSeedVal;
                                    ImGui.Checkbox($"##GoalSeed{label}", ref val);
                                    offset.GoalWonderSeedVal = val;
                                }

                                ImGui.TableNextColumn();
                                if (offset.ClapperGate > 0)
                                {
                                    val = offset.ClapperGateVal;
                                    ImGui.Checkbox($"##Gate{label}", ref val);
                                    offset.ClapperGateVal = val;
                                }

                                ImGui.TableNextColumn();
                                if (offset.PurpleCoin > 0)
                                {
                                    val = offset.PurpleCoin1;
                                    ImGui.Checkbox($"##P1{label}", ref val);
                                    offset.PurpleCoin1 = val;
                                }

                                ImGui.TableNextColumn();
                                if (offset.PurpleCoin > 0)
                                {
                                    val = offset.PurpleCoin2;
                                    ImGui.Checkbox($"##P2{label}", ref val);
                                    offset.PurpleCoin2 = val;
                                }

                                if (offset.PurpleCoin > 0)
                                {
                                    ImGui.TableNextColumn();
                                    val = offset.PurpleCoin3;
                                    ImGui.Checkbox($"##P3{label}", ref val);
                                    offset.PurpleCoin3 = val;
                                }

                            }
                            ImGui.EndTable();
                        }
                        ImGui.EndTabItem();
                    }
                }
                    ImGui.EndTabBar();
                
            }
            ImGui.End();
        }
    }
}

//private void WriteOffset(Dictionary<string, LevelInfo> Levels, int startOffset, string property, int max = -1)
//{

//    int offset = startOffset;

//    var prop = typeof(LevelInfo).GetProperty(property);
//    var propVal = typeof(LevelInfo).GetProperty(property + "Val");

//    int i = 1;
//    foreach (var level in Levels.Values)
//    {
//        if (i == max)
//        {
//            Console.WriteLine("retruning");
//            return;
//        }

//        int current = (int)prop.GetValue(level);

//        if (current == 0)
//        {
//            prop.SetValue(level, offset);

//            if (property == "WonderSeed")
//            {
//                Console.WriteLine(offset);
//            }
//            bool value = SaveFile.ReadBool(offset);
//            propVal.SetValue(level, value);

//            offset += 4;
//        }

//        if(current == -2)
//            offset += 4;

//        i++;
//    }
//}