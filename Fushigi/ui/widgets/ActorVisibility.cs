using Fushigi.course;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    internal class ActorVisibility
    {

        private static Vector2 icon_size = new Vector2(25 * MainWindow.dpiScale, 25 * MainWindow.dpiScale);
        public static List<string> HiddenAll = new();

        private static readonly Dictionary<string, string> ViewingSettings = new Dictionary<string, string>()
        {
            {"Rails", "No Model" },
            {"BgUnits", "" },
            {"ForegroundActors", "Dropdown" },
            {"BackgroundActors", "Dropdown" }
        };

        private static readonly Dictionary<string, string> ForegroundActors = new Dictionary<string, string>()
        {
            {"Camera", "No Model"},
            {"Area", "No Model" },
            {"MapObj", "" },
            {"Enemy", "" },
            {"Object", ""},
        };

        private static readonly Dictionary<string, string> BackgroundActors = new Dictionary<string, string>()
        {
            {"DV", "" },
            {"Cloud", "" }
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
                    DrawVisibilityToggle(objects.Key, ObjectName, "Model");
                }
            }
        }

        public static void DrawParentIcon(Dictionary<string, string> DropDownObjects, string ObjectName)
        {
            ImGui.TableSetColumnIndex(1);
            var icon = IconUtil.ICON_EYE;

            if (HiddenAll.Contains(ObjectName + "_tag"))
                icon = IconUtil.ICON_EYE_SLASH;

            if (ImGui.Button($"{icon}##{ObjectName}_Parent_Tag", icon_size))
            {
                AddToList(ObjectName + "_tag", HiddenAll);
                foreach (var objects in DropDownObjects)
                    AddToList(objects.Key, LevelViewport.HiddenActors);
            }

            icon = IconUtil.ICON_EYE;
            if (HiddenAll.Contains(ObjectName + "_model"))
                icon = IconUtil.ICON_EYE_SLASH;

            ImGui.TableSetColumnIndex(2);
            if (ImGui.Button($"{icon}##{ObjectName}_Parent_Model", icon_size))
            {
                AddToList(ObjectName + "_model", HiddenAll);
                foreach (var objects in DropDownObjects)
                    if (objects.Value != "No Model")
                        AddToList(objects.Key, LevelViewport.HiddenModels);
            }
            ImGui.TableSetColumnIndex(0);
        }

        public static void DrawVisibilityToggle(string objectName, string parentObjectName, string listToUse)
        {
            var visibilityList = LevelViewport.HiddenActors;

            if (listToUse == "Model")
                visibilityList = LevelViewport.HiddenModels;

            bool contains = visibilityList.Contains(objectName) || HiddenAll.Contains(objectName);
            var icon = IconUtil.ICON_EYE;

            if (contains)
                icon = IconUtil.ICON_EYE_SLASH;

            if (ImGui.Button($"{icon}##EyeButton_{objectName}_{listToUse}", icon_size))
            {
                switch (parentObjectName)
                {
                    case "Rails":
                        AddToList("Rails", HiddenAll);
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
            if (ImGui.Begin("Visibility Options", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoNavInputs))
            {
                if (ImGui.Button("Close"))
                    continueDisplay = false;

                if(ImGui.BeginTable("ActorVisibility", 3, ImGuiTableFlags.BordersInnerV))
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

                    var i = 0;
                    foreach (var setting in ViewingSettings)
                    {
                        if (setting.Value == "Dropdown")
                        {
                            ImGui.TableNextRow();

                            switch (setting.Key)
                            {
                                case "ForegroundActors":
                                    DrawParentIcon(ForegroundActors, "ForegroundActor");
                                    break;
                                case "BackgroundActors":
                                    DrawParentIcon(BackgroundActors, "BackgroundActor");
                                    break;

                            }

                            ImGui.TableSetColumnIndex(0);
                            if (ImGui.TreeNode($"{setting.Key}##Node{i}"))
                            {
                                switch (setting.Key)
                                {
                                    case "ForegroundActors":
                                        DrawDropdown(ForegroundActors, "Actor");
                                        break;
                                    case "BackgroundActors":
                                        DrawDropdown(BackgroundActors, "Actor");
                                        break;

                                }

                                ImGui.TreePop();
                            }
                            i++;
                        }
                        else
                        {
                            DrawFirstColumn(setting.Key);
                            ImGui.TableSetColumnIndex(1);
                            DrawVisibilityToggle(setting.Key, setting.Key, "Tag");

                            if (setting.Value != "No Model")
                            {
                                ImGui.TableSetColumnIndex(2);
                                DrawVisibilityToggle(setting.Key, setting.Key, "Model");
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
