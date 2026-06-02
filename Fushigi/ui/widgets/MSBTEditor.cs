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
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ZstdSharp.Unsafe;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    public class MsbtEditor
    {

        private bool init = false;
        List<(string name, Dictionary<string, string> messages)> gameMsgFiles = new();
        private Sarc sarc;
        private string[] gameMsg;

        private Dictionary<string, string> replacements = new Dictionary<string, string>
        {
            ["0E 01 04 02 13 C3 8D"] = "{Icon_Bowser}",
            ["0E 00 03 02 00 00"] = "{Blue_Text_Start}",
            ["0E 00 03 02 C3 BF C3 BF"] = "{Color_Text_End}",
            ["0E 00 04 00"] = "{Go to next box}\n",
            ["0E 05 02 02"] = "FF",
            ["0E 0A 16 00"] = "{Character Icon}",
            ["0E 01 04 02 01 C3 8D"] = "{Wonder Seed W1}",
            ["0E 01 04 02 02 C3 8D"] = "{Wonder Seed W2}",
            ["0E 01 04 02 03 C3 8D"] = "{Wonder Seed W3}",
            ["0E 01 04 02 04 C3 8D"] = "{Wonder Seed W4}",
            ["0E 01 04 02 05 C3 8D"] = "{Wonder Seed W5}",
            ["0E 01 04 02 06 C3 8D"] = "{Wonder Seed W6}",
            ["0E 01 04 02 07 C3 8D"] = "{Wonder Seed W7}",
            ["0E 01 04 02 0A C3 8D"] = "{Purple Coin}",
            ["0E 00 03 02 13 00"] = "{Purple Text}",
            ["0E 00 02 02 4B 00 57 31"] = "{W1 Icon}",
            ["0E 00 02 02 4B 00 57 32"] = "{W2 Icon}",
            ["0E 00 02 02 4B 00 57 33"] = "{W3 Icon}",
            ["0E 00 02 02 4B 00 57 34"] = "{W4 Icon}",
            ["0E 00 02 02 4B 00 57 35"] = "{W5 Icon}",
            ["0E 00 02 02 4B 00 57 36"] = "{W6 Icon}",
            ["0E 00 02 02 4B 00 57 37"] = "{W7 Icon}",
            ["0E 00 02 02 64 00 20"] = "{I have no fucking idea}",
            ["EE 83 A8"] = "{R}",
            ["EE 83 A9"] = "{ZR}",
            ["EE 84 80"] = "{Left Stick}",
            ["EE 84 81"] = "{Stick}",
            ["0E 0A 18 02 00 01"] = "{Y}",
            ["0E 0A 18 02 00 00"] = "{Left Button}",
            ["0E 0A 18 02 01 01"] = "{B Button}",
            ["0E 0A 18 02 01 01"] = "{B Button}",
            ["0E 0A 18 02 01 00"] = "{Down Button}"

        };


        public void initMSBT()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string malsMod = Path.Combine(UserSettings.GetModRomFSPath(), "Mals");
            string malsRoot = Path.Combine(RomFS.GetRoot(), "Mals");
            
            string path = null;

            if (Directory.Exists(malsMod))
                path = Directory.GetFiles(malsMod, "USen.Product.*.sarc.zs").FirstOrDefault();
            else
                path = Directory.GetFiles(malsRoot, "USen.Product.*.sarc.zs").FirstOrDefault();

            if (path == null)
                return;


            if (File.Exists(path))
            {
                sarc = Sarc.FromBinary(new(FileUtil.DecompressFile(path)));
                gameMsg = sarc.Keys
                .Where(k => k.StartsWith("GameMsg"))
                .ToArray();

                Array.Sort(gameMsg, StringComparer.OrdinalIgnoreCase);

                foreach (var file in gameMsg)
                {
                    //Console.WriteLine(file);
                    if (!file.Contains('/'))
                        return;

                    var fileName = file.Split('/')[1];

                    var segment = sarc[file];
                    var ms = new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
                    var sarcFile = new MsbtFile(ms).Messages;

                }

            }
        }

        public string icons(string msbt)
        {
            string hex = BitConverter.ToString(Encoding.UTF8.GetBytes(msbt)).Replace("-", " ");
            Console.WriteLine(hex);
            foreach (var check in replacements)
            {
                if (hex.Contains(check.Key))
                {
                    var replacement = BitConverter.ToString(Encoding.UTF8.GetBytes(check.Value)).Replace("-", " ");
                    hex = hex.Replace(check.Key, replacement);
                }
            }
            return hex;

        }
        public void Draw()
        {
            ImGui.Begin("MSBT");

            if (ImGui.Button("Save MSBT"))
            {
            
                    foreach (var (name, messages) in gameMsgFiles)
                    {
                        var segment = sarc[name];
                        var ms = new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);

                        var msbt = new MsbtFile(ms);

                        foreach (var kv in messages)
                            msbt.Messages[kv.Key] = kv.Value;

                        using var outStream = new MemoryStream();
                        msbt.Save(outStream);


                        sarc[name] = new ArraySegment<byte>(outStream.ToArray());
                    }

                    // Save entire SARC
                    using var output = File.Create("modified.sarc");
                    sarc.Write(output);

            }

            if (!init)
            {
                initMSBT();
                init = true;
            }

            foreach (var name in gameMsg)
            {
                if (!name.Contains('/'))
                    return;

                var fileName = name.Split("/")[1];
                if (ImGui.TreeNode(fileName))
                {

                    if (!gameMsgFiles.Any(x => x.name == name))
                    {
                        var segment = sarc[name];
                        var ms = new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
                        var sarcFile = new MsbtFile(ms).Messages;
     
                        gameMsgFiles.Add((name, sarcFile));
                        foreach (var (sarcName, sarc) in gameMsgFiles)
                        {
                            foreach (var (key, value) in sarc.OrderBy(x => x.Key))
                            {

                                string hex = icons(value);

                                string[] parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                byte[] bytes = parts.Select(p => Convert.ToByte(p, 16)).ToArray();
                                string newText = Encoding.UTF8.GetString(bytes);
                                sarc[key] = newText;
                                Console.WriteLine(sarc[key]);
                            }
                        }

                    }

                    //var sarcFile = new MsbtFile(new MemoryStream(sarc.OpenFile(name))).Messages;
                    foreach (var (sarcName, sarc) in gameMsgFiles)
                    {
                        if (sarcName == name)
                        {
                            foreach (var (key, value) in sarc.OrderBy(x => x.Key))
                            {
                                ImGui.BeginTable("ActorVisibility", 2, ImGuiTableFlags.BordersInnerV);
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                                
                                ImGui.Text(key);
                                ImGui.TableSetColumnIndex(1);
                                var text = value;
                                if (ImGui.InputTextMultiline($"##{key}", ref text, 2048, new Vector2(300, 80)))
                                    sarc[key] = text;

                                sarc[key] = text;
                                ImGui.EndTable();
                            }
                        }
                    }
                }
                ImGui.Separator();
            }
            ImGui.End();
        }
    }
}