using Fushigi.course;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Fushigi.ui.widgets
{
    internal class WorldMapSettings
    {
        private static WorldMapInfo.CourseTable courseToRemove;

        public static void Draw(ref bool continueDisplay, IPopupModalHost modalHost, WorldMapInfo worldMapInfo)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);

            if (ImGui.Begin("Course Settings", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Button("Close"))
                {
                    continueDisplay = false;
                }

                if (ImGui.BeginTabBar("WorldMap", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("WorldMap Settings"))
                    {
                        DrawWorldMapAppearanceSettings(worldMapInfo);
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }

        public static bool TrashIcon(string id)
        {
            float rowHeight = ImGui.GetFrameHeight();
            float deleteButtonWidth = rowHeight * 1.6f;

            Vector2 deletePos = ImGui.GetCursorScreenPos();
            bool clicked = ImGui.InvisibleButton(id, new Vector2(deleteButtonWidth, rowHeight));

            string deleteIcon = IconUtil.ICON_TRASH_ALT;
            Vector2 iconSize = ImGui.CalcTextSize(deleteIcon);
            Vector2 iconPos = deletePos + new Vector2(
                (deleteButtonWidth - iconSize.X) * 0.5f,
                (rowHeight - iconSize.Y) * 0.5f
            );

            uint color = ImGui.GetColorU32(ImGuiCol.Text);
            if (!ImGui.IsItemHovered())
                color = (color & 0xFFFFFF) | ((uint)((color >> 24) * 0.5f) << 24);

            ImGui.GetWindowDrawList().AddText(iconPos, color, deleteIcon);
            ImGui.SetItemTooltip("Delete Prefab");

            return clicked;
        }

        public static string parseStage(string stage)
        {
            stage = stage.Replace("Work/Stage/StageParam/", "");
            stage = stage.Replace(".game__stage__StageParam.gyml", "");
            return stage;
        }
        public static string revertStage(string stage)
        {
            stage = "Work/Stage/StageParam/" + stage + ".game__stage__StageParam.gyml";
            return stage;
        }
        public static void DrawText(string Text)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text(Text);
            ImGui.TableSetColumnIndex(1);
        }

        private static void DrawWorldMapAppearanceSettings(WorldMapInfo worldMapInfo)
        {
            int i = 0;
            if (ImGui.TreeNode($"Courses"))
            {
                foreach (var course in worldMapInfo.Courses)
                {
                    float rowHeight = ImGui.GetFrameHeight();
                    float deleteButtonWidth = rowHeight * 1.6f;
                    float fullWidth = ImGui.GetContentRegionAvail().X;
                    Vector2 rowStart = ImGui.GetCursorScreenPos();

                    ImGui.SetNextItemAllowOverlap();
                    bool open = ImGui.TreeNodeEx($"Course_{i}", ImGuiTreeNodeFlags.SpanFullWidth);

                    ImGui.SetCursorScreenPos(new Vector2(rowStart.X + fullWidth - deleteButtonWidth, rowStart.Y));
                    if (TrashIcon($"##delete_{i}"))
                        courseToRemove = course;

                    if (open)
                    {
                        if (ImGui.BeginTable($"CourseTable_{i}", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            DrawText("Is Route Lock Course");
                            bool RouteLock = course.IsRouteLockCourse;
                            ImGui.Checkbox($"##Course_{i}_RouteLock", ref RouteLock);
                            course.IsRouteLockCourse = RouteLock;

                            DrawText("Get Grand Wonder Seed");
                            bool GrandSeed = course.GetGrandSeed;
                            ImGui.Checkbox($"##Course_{i}_GrandSeed", ref RouteLock);
                            course.GetGrandSeed = GrandSeed;

                            DrawText("Key");
                            var key = course.Key;
                            ImGui.InputText($"##Key_{i}", ref key, 256);
                            course.Key = key;

                            DrawText("Stage Path");
                            var StagePath = parseStage(course.StagePath);
                            ImGui.InputText($"##StagePath_{i}", ref StagePath, 256);
                            course.StagePath = revertStage(StagePath);

                            DrawText("Appear Bonus Course");
                            bool bonus = course.GoldenFlower;
                            ImGui.Checkbox($"##Course_{i}_Bonus", ref bonus);
                            course.GoldenFlower = bonus;

                            DrawText("Demo Type");
                            string DemoType = course.CourserDemoType;
                            ImGui.InputText($"##DemoType_{i}", ref DemoType, 256);
                            course.CourserDemoType = DemoType;

                            ImGui.EndTable();
                        }
                        ImGui.TreePop();
                    }

                    i++;
                }
                ImGui.TreePop();

                if (courseToRemove != null)
                {
                    worldMapInfo.Courses.Remove(courseToRemove);
                    courseToRemove = null;
                }

                if (ImGui.Button("Add New Course"))
                    worldMapInfo.Courses.Add(new WorldMapInfo.CourseTable());
            }

            if (ImGui.TreeNode($"Gates"))
            {
                foreach (var gate in worldMapInfo.Gates)
                {
                    if (ImGui.TreeNodeEx($"Gate_{gate.GateNo}", ImGuiTreeNodeFlags.SpanFullWidth))
                    {
                        if (ImGui.BeginTable($"GateTable_{gate.GateNo}", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            DrawText("Gate No");
                            ImGui.Text($"{gate.GateNo}");

                            DrawText("Initial Message");
                            var msg = gate.BalloonMsgLabel;
                            ImGui.InputText($"##Msg_{gate.GateNo}", ref msg, 256);
                            gate.BalloonMsgLabel = msg;

                            DrawText("Price");
                            int price = gate.Price;
                            ImGui.DragInt($"##Price_{gate.GateNo}", ref price);
                            gate.Price = price;

                            ImGui.EndTable();
                        }
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
        }
    }
}