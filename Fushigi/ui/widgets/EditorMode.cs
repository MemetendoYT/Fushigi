using DiscordRPC.Exceptions;
using Fushigi.course;
using Fushigi.env;
using Fushigi.ui.modal;
using ImGuiNET;
using OpenAbility.ImGui.Nodes;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    public class EditorMode
    {

        public static string editMode = "";
        public static bool initLevelEditor = false;
        public void Load(GL gl, AreaParam areaParam, EnvPalette envPalette)
        {

        }

        public static void Draw(MainWindow mainWindow)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 300 * MainWindow.dpiScale), ImGuiCond.Once);

            ImGuiWindowFlags flags =
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse;

            bool open = ImGui.Begin("Editor Mode", flags);

            if (open)
            {
                editMode = "";
                if (ImGui.Button("Level Editor"))
                {
                    editMode = "Level Editor";
                    if (!initLevelEditor)
                    {
                        _ = mainWindow.StartupRoutine();
                        initLevelEditor = true;
                    }
                }

                if (ImGui.Button("AINB Node Editor"))
                {
                    editMode = "AINB";
                }

                if (ImGui.Button("Collision Editor"))
                {
                    editMode = "Collision";
                }

                if (ImGui.Button("MSBT Editor"))
                {
                    editMode = "MSBT";
                }

            }

            ImGui.End();
        }

    }
}