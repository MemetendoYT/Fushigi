using Fushigi.course;
using Fushigi.ui.modal;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui.widgets
{
    internal class WorldMapSettings
    {

        public static void Draw(ref bool continueDisplay, IPopupModalHost modalHost, WorldMapInfo worldMapInfo)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);

            // Window
            if (ImGui.Begin("Course Settings", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {
                // Close Button
                if (ImGui.Button("Close"))
                {
                    continueDisplay = false;
                }


                if (ImGui.BeginTabBar("Fart", ImGuiTabBarFlags.None))
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

        private static void DrawWorldMapAppearanceSettings(WorldMapInfo worldMapInfo)
        {
            if (ImGui.BeginTable("##WorldMapSettings", 2))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                foreach(var course in worldMapInfo.Courses)
                {
                    ImGui.Text($"Course: {course.Key}");
                    ImGui.TableNextRow();
                }
            }
            ImGui.EndTable();
        }

    }
}
