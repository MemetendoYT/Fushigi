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

        private static Vector2 icon_size = new Vector2(25 * MainWindow.dpiScale, 25 * MainWindow.dpiScale);

        private static readonly Dictionary<string, string> ViewingSettings = new Dictionary<string, string>()
        {
            {"Rails", "No Model" },
            {"BgUnits", "" },
            {"Actors", "Dropdown" },
        };

        private static readonly Dictionary<string, string> ForegroundActors = new Dictionary<string, string>()
        {
            {"Camera", "No Model"},
            {"Area", "No Model" },
            {"DV", "" },
            {"MapObj", "" },
            {"Enemy", "" },
        };

        private static readonly Dictionary<string, string> BackgroundActors = new Dictionary<string, string>()
        {
            {"DV", "" },
        };

        private static readonly Dictionary<string, string> Actors = new Dictionary<string, string>()
        {
            {"Camera", "No Model"},
            {"Area", "No Model" },
            {"MapObj", "" },
            {"Enemy", "" },
            {"DV", "" }
        };

        public static void DrawDropdown(Dictionary<string, string> DropDownObjects, string ObjectName)
        {
            foreach (var objects in DropDownObjects)
            {
                DrawFirstColumn(objects.Key);
                ImGui.TableSetColumnIndex(1);
                DrawVisibilityToggle(objects.Key, ObjectName, "Tag");

                if (objects.Value != "No Model")
                {
                    ImGui.TableSetColumnIndex(2);
                    ImGui.SameLine();
                    DrawVisibilityToggle(objects.Key, ObjectName, "Model");
                }
            }
        }

        public static void DrawVisibilityToggle(string objectName, string parentObjectName, string listToUse)
        {
            var visibilityList = LevelViewport.HiddenActors;

            if(listToUse == "Model")
                visibilityList = LevelViewport.HiddenModels;

            bool contains = visibilityList.Contains(objectName);
            var icon = IconUtil.ICON_EYE;

            if (contains)
                icon = IconUtil.ICON_EYE_SLASH;

                if (ImGui.Button($"{icon}##EyeButton_{objectName}_{listToUse}", icon_size))
                {
                    switch (parentObjectName)
                    {
                        case "Rails":
                            LevelViewport.ShowRails = !LevelViewport.ShowRails;
                            break;
                        case "Actor":
                            AddToList(objectName, visibilityList);
                            break;
                    }
                }
        }

        public static void AddToList(string name, List<string> list)
        {
            if (list.Contains(name))
                list.Remove(name);
            else
                list.Add(name);
        }

        public static void DrawFirstColumn(string name)
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
                    ImGui.Text("Object");
                    ImGui.Separator();
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text("Tag Visibility");
                    ImGui.Separator();
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text("Model Visibility");
                    ImGui.Separator();

                    foreach (var setting in ViewingSettings)
                    {
                        DrawFirstColumn(setting.Key);

                        if (setting.Value == "Dropdown")
                        {
                            ImGui.SameLine();
                            if (ImGui.TreeNode(""))
                            {
                                switch (setting.Key)
                                {
                                    case "Actors":
                                        DrawDropdown(Actors, "Actor");
                                        break;
                                }

                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            ImGui.TableSetColumnIndex(1);
                            DrawVisibilityToggle(setting.Key, setting.Key, "Tag");

                            if (setting.Value != "No Model")
                            {
                                ImGui.TableSetColumnIndex(2);
                                ImGui.SameLine();
                                DrawVisibilityToggle(setting.Key, setting.Key, "Model");
                            }
                        }
                        //ImGui.TableSetColumnIndex(0);
                        //ImGui.Separator();
                    }
                    ImGui.EndTable();
                }
                ImGui.End();
            }
        }
    }
}
