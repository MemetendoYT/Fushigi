using Fasterflect;
using Fushigi.gl;
using Fushigi.ui.modal;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Numerics;
using System.Text;

namespace Fushigi.ui
{
    public partial class MainWindow
    {
        class WelcomeMessage : OkDialog<WelcomeMessage>
        {
            protected override string Title => "Welcome";
            int pageNumb = 0;
            protected override void DrawBody(Promise<Void> promise)
            {
                var imgSize = new Vector2(128, 128);
                float windowWidth = ImGui.GetContentRegionAvail().X;
                float centerX = ImGui.GetCursorPosX() + (windowWidth - imgSize.X) * 0.5f;
                if (pageNumb == 0)
                {
                    ImGui.SetCursorPosX(centerX);
                    ImGui.Image((IntPtr)FushigiLogo.ID, imgSize);
                    ImGui.Dummy(new Vector2(0, 30));
                    ImGui.SetCursorPosX(centerX);
                    ImGui.Text("Welcome to Fushigi");
                    ImGui.SetCursorPosX(centerX);
                    ImGui.Text("The Super Mario Bros. Wonder Level Editor");
                }
                else if (pageNumb == 1)
                {
                    ImGui.Text("Setup your romfs mr stinky fart");
                    Preferences.DrawSettings(mModalHost, mGLTaskScheduler);
                }

                float buttonHeight = ImGui.GetFrameHeight();
                float footerY = ImGui.GetWindowHeight() - buttonHeight - ImGui.GetStyle().WindowPadding.Y;
                ImGui.SetCursorPosY(footerY);
                bool showBack = pageNumb > 0;
                if (showBack)
                {
                    ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
                    if (ImGui.Button("Go Back"))
                        pageNumb--;
                }

                var nextSize = new Vector2(80, 0);
                float nextWidth = nextSize.X > 0 ? nextSize.X : ImGui.CalcTextSize("Next").X + ImGui.GetStyle().FramePadding.X * 2;
                ImGui.SetCursorPosY(footerY);
                ImGui.SetCursorPosX(windowWidth + ImGui.GetStyle().WindowPadding.X - nextWidth);

                bool isDisabled = string.IsNullOrEmpty(RomFS.GetRoot()) ||
                    string.IsNullOrEmpty(UserSettings.GetModRomFSPath());

                if (isDisabled && pageNumb == 1)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.4f, 0.4f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.4f, 0.4f, 1f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1f));
                    ImGui.Button("Next", nextSize);
                    ImGui.PopStyleColor(3);
                }
                else if (pageNumb == 2)
                {
                    ImGui.Text("Shit your pants");
                    if (ImGui.Button("Next", nextSize))
                        promise.SetResult(new Void());
                }
                else
                {
                    if (ImGui.Button("Next", nextSize))
                        pageNumb++;
                }
            }
        }
    }
}