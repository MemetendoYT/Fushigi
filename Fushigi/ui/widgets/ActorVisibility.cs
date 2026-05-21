using Fushigi.course;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    internal class ActorVisibility
    {

        private static LevelViewport viewport;
        private static readonly Dictionary<string, string> ViewingSettings = new Dictionary<string, string>()
        {
            {"Rails", "No Model" },
            {"BgUnits", "" },
            {"Actors", "Dropdown" },
        };

        private static readonly Dictionary<string, string> Actors = new Dictionary<string, string>()
        {
            {"DV", "" },
            {"Camera", "No Model"},
            {"MapObj", "" },
            {"Enemy", "" },
            {"Area", "" }
        };

        public static void DrawThingy(Dictionary<string, string> actors, string name)
        {
            foreach (var actor in actors)
            {
                TableJargon(actor.Key);
                ImGui.TableSetColumnIndex(1);
                Bazinga(actor.Key, name, "Tag");

                if (actor.Value != "No Model")
                {
                    ImGui.TableSetColumnIndex(2);
                    ImGui.SameLine();
                    Bazinga(actor.Key, actor.Key, "Model");
                }
            }

        }

        public static void Bazinga(string name, string parent, string dictionary)
        {
            var thing = LevelViewport.HiddenActors;
            if(dictionary == "Model")
            {
                thing = LevelViewport.HiddenModels;
            }

                Vector2 icon_size = new Vector2(25 * MainWindow.dpiScale, 25 * MainWindow.dpiScale);
            if (ImGui.Button($"{IconUtil.ICON_EYE}##EyeButton_{name}", icon_size))
            {
                switch (parent)
                {
                    case "Rails":
                        LevelViewport.ShowRails = !LevelViewport.ShowRails;
                        break;
                    case "Actor":
                        AddToList(name, thing);
                        break;
                }

            }
        }

        public static void AddToList(string name, List<string> list)
        {
            Console.WriteLine(name);
            if (list.Contains(name))
                list.Remove(name);
            else
                list.Add(name);
        }
        public static void TableJargon(string name)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text(name + ":");
        }
        public static void Draw(ref bool continueDisplay, IPopupModalHost modalHost)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);
            // Window
            if (ImGui.Begin("Visibility Options", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {
                // Close Button
                if (ImGui.Button("Close"))
                    continueDisplay = false;

                if (ImGui.BeginTable("ActorVisibility", 3, ImGuiTableFlags.BordersInnerV))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Thingy");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text("Show Tag");
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text("Show Model");

                    foreach (var setting in ViewingSettings)
                    {
                        TableJargon(setting.Key);

                        if (setting.Value == "Dropdown")
                        {
                            ImGui.SameLine();
                            if (ImGui.TreeNode(""))
                            {
                                switch (setting.Key)
                                {
                                    case "Actors":
                                        DrawThingy(Actors, "Actor");
                                        break;
                                }
                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            ImGui.TableSetColumnIndex(1);
                            Bazinga(setting.Key, setting.Key, "Tag");

                            if (setting.Value != "No Model")
                            {
                                ImGui.TableSetColumnIndex(2);
                                ImGui.SameLine();
                                Bazinga(setting.Key, setting.Key, "Model");
                            }
                        }
                       
                    }
                    ImGui.EndTable();
                }
                ImGui.End();
            }
        }
    }
}
