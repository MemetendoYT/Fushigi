using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    internal class Shortcuts
    {


        public static void Draw(ref bool continueDisplay)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 250 * MainWindow.dpiScale), ImGuiCond.Once);
            if (ImGui.Begin("Keyboard Shortcuts", ref continueDisplay, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoNavInputs))
            {
                if (ImGui.BeginTable("Shortcuts", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Action");
                    ImGui.TableSetupColumn("Keyboard Input");
                    ImGui.TableHeadersRow();
                    DrawTableRow("Copy", "Ctrl-C");
                    DrawTableRow("Paste", "Ctrl-V");
                    DrawTableRow("Delete", "Del | Shift + Backspace");
                    DrawTableRow("Hide Objects", "H");
                    DrawTableRow("Reveal Objects", "Alt-H");
                    DrawTableRow("Select All Rail Points", "Ctrl-A");
                    DrawTableRow("Pivot", "R");
                    ImGui.EndTable();
                }
            }
            ImGui.End();
        }

        public static void DrawTableRow(string textA, string textB)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(textA);
            ImGui.TableNextColumn();
            ImGui.Text(textB);
        }
    }
}