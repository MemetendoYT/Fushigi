using Fushigi.gl;
using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    class Preferences
    {
        static readonly Vector4 errCol = new Vector4(1f, 0, 0, 1);
        static bool romfsTouched = false;
        static bool modRomfsTouched = false;
        private static readonly string[] ShaderDescriptions =
        {
            "All Actors",
            "Vanilla Actors Only",
            "Vanilla Actors Except DV",
            "Vanilla Actors Except DV and Tiles",
            "Tilesets Only"
        };
        public static void Draw(ref bool continueDisplay, GLTaskScheduler glTaskScheduler, IPopupModalHost modalHost)
        {
            ImGui.SetNextWindowSize(new Vector2(700, 300), ImGuiCond.Once);

            if (ImGui.Begin("Fushigi Settings", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                if (ImGui.Button("Close"))
                    continueDisplay = false;
                ImGui.PopStyleColor(1);

                if (ImGui.BeginTabBar("FushigiSettingsTab", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("Basic Settings"))
                    {
                        DrawBasicSettings(modalHost, glTaskScheduler);
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Advanced Settings"))
                    {
                        DrawAdvancedSettings(modalHost);
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }


        private static void DrawBasicSettings(IPopupModalHost modalHost, GLTaskScheduler glTaskScheduler)
        {
            switch (UserSettings.Theme)
            {
                case "Dark (Default)": ImGui.StyleColorsDark(); break;
                case "Classic": ImGui.StyleColorsClassic(); break;
                case "Light": ImGui.StyleColorsLight(); break;
            }
            ImGui.Indent();

            if (PathSelector.Show("Wonder Dump Directory", ref UserSettings.RefRomFSPath, RomFS.IsValidRoot(UserSettings.RomFSPath)))
            {
                romfsTouched = true;

                UserSettings.RomFSPath = UserSettings.RefRomFSPath;

                if (!RomFS.IsValidRoot(UserSettings.RomFSPath))
                    return;

                Task.Run(async () =>
                {
                    await ProgressBarDialog.ShowDialogForAsyncAction(modalHost, $"Preloading Thumbnails", async (p) =>
                    {
                        await glTaskScheduler.Schedule(gl => RomFS.SetRoot(UserSettings.RomFSPath, gl));
                    });

                    ChildActorParam.Load();

                    /* if our parameter database isn't set, set it */
                    if (!ParamDB.sIsInit)
                        await MainWindow.LoadParamDBWithProgressBar(modalHost);
                });

            }

            Tooltip.Show("The game files which are stored under the romfs folder.\nIf you are using v1.0.1 of Super Mario Bros. Wonder, use a RomFS Game Path with v65536 files in it.");

            if (romfsTouched && !RomFS.IsValidRoot(UserSettings.RomFSPath))
                ImGui.TextColored(errCol, "The path you have selected is invalid. Please select a RomFS path that contains your full Wonder dump.");

            if (PathSelector.Show("Modded Directory", ref UserSettings.RefModRomFSPath, !string.IsNullOrEmpty(UserSettings.ModRomFSPath)))
            {
                modRomfsTouched = true;

                UserSettings.ModRomFSPath = UserSettings.RefModRomFSPath;
                Console.WriteLine("Mod RomFS Path set to: " + UserSettings.ModRomFSPath);

                UserSettings.RomFSReload = true;
            }

            Tooltip.Show("The save output where to save modified romfs files");

            if (modRomfsTouched && string.IsNullOrEmpty(UserSettings.ModRomFSPath))
                ImGui.TextColored(errCol, "The path you have selected is invalid. Directory must not be empty.");

            Checkbox("Use Game Shaders", ref UserSettings.RefUseGameShaders, (v) => UserSettings.UseGameShaders = v,
                "Displays models using the shaders present in the game. This may cause a performance drop but will look more visually accurate.");

            Checkbox("Enable Half Tile Editing", ref UserSettings.RefEnableHalfTile, (v) => UserSettings.EnableHalfTile = v,
                "Enable half tile editng for BGUnits, also affects the placement of rails as well.");

            Checkbox("Enable Actor Name Translation", ref UserSettings.RefEnableTranslation, (v) => UserSettings.EnableTranslation = v,
                "Translates all the actor names to English.");

            InputFloat("Backup Frequency (in minutes)", ref UserSettings.RefBackupFreqMinutes, (v) => UserSettings.BackupFreqMinutes = v,
                "How long between each backup, in minutes.\nBackups are stored to wherever Fushigi is installed.");

            if (ImGui.BeginCombo("Themes", UserSettings.Theme))
            {
                if (ImGui.Selectable("Dark (Default)", UserSettings.Theme == "Dark (Default)"))
                {
                    ImGui.StyleColorsDark();
                    UserSettings.Theme = "Dark (Default)";
                }
                if (ImGui.Selectable("Classic", UserSettings.Theme == "Classic"))
                {
                    ImGui.StyleColorsClassic();
                    UserSettings.Theme = "Classic";
                }
                if (ImGui.Selectable("Light", UserSettings.Theme == "Light"))
                {
                    ImGui.StyleColorsLight();
                    UserSettings.Theme = "Light";
                }
                ImGui.EndCombo();
            }

            Tooltip.Show("Change the UI theme.");
        }

        private static void DrawAdvancedSettings(IPopupModalHost modalHost)
        {
            ImGui.Indent();

            Tooltip.Show("Displays models using the shaders present in the game. This may cause a performance drop but will look more visually accurate.");

            Checkbox("Render Models from mod RomFS", ref UserSettings.RefRenderCustomModels, (v) => UserSettings.RenderCustomModels = v,
                "Uses the models from the mod directory. WARNING: Rendering of custom models using game shaders is broken.");

            Checkbox("Use Astc Texture Cache", ref UserSettings.RefUseAstcTextureCache, (v) => UserSettings.UseAstcTextureCache = v,
                "Saves ASTC textures to disk which takes up disk space, but improves loading times and ram usage significantly.");

            Checkbox("Hide Deleting Linked Actors Popup", ref UserSettings.RefHideDeletingLinkedActorsPopup, (v) => UserSettings.UseAstcTextureCache = v,
                "Hides the warning popup when you delete actors with links.");

            Checkbox("Use New Camera [BETA!]", ref UserSettings.RefUseNewCamera, (v) => UserSettings.UseNewCamera = v,
                "Uses a new camera system that aims to be more accurate.\nWARNING: in beta and might cause some issues");

            Checkbox("Enable RomFS Reload", ref UserSettings.RefAllowRomFSReload, (v) => UserSettings.AllowRomFSReload = v,
                "When switching to a different modded romfs everything will reload.");
            
            Checkbox("Enable Discord Rich Presence", ref UserSettings.RefSetEnableDRPC, (v) => UserSettings.EnableDRPC = v,
                "Whether or not to enable Discord Rich Presence.\nReload required to work.");

            Checkbox("Hide Rich Presence Activity", ref UserSettings.RefSetPrivateDRPC, (v) => UserSettings.PrivateDRPC = v,
                "Whether or not to hide information about the course in the Discord RPC.\nReload required to work.");
            
            Checkbox("Show FPS Counter", ref UserSettings.RefFPSCounter, (v) => UserSettings.FPSCounter = v,
                "Displays an FPS counter on the top-left of the editor viewport.");

            Checkbox("Show Advanced Debug Info", ref UserSettings.RefAdvancedDebugSettings, (v) => UserSettings.AdvancedDebugSettings = v,
                "Displays debugging information on the top-left of the editor viewport.");

            Checkbox("Enable VSync", ref UserSettings.RefVSync, (v) => UserSettings.VSync = v,
                "Enable VSync to cap framerate to your monitor's refresh rate,\n or disable to unlimit.");

            if (ImGui.BeginCombo("Shader Settings [EXPERIMENTAL]", ShaderDescriptions[UserSettings.ShaderSettings]))
            {
                for (int i = 0; i < 5; i++)
                    if (ImGui.Selectable(ShaderDescriptions[i]))
                        UserSettings.ShaderSettings = i;

                ImGui.EndCombo();
            }
            Tooltip.Show("Disable custom shaders on custom actors. NOTE: This only works on new custom actors and not model swaps");
        }

        private static void Checkbox(string name, ref bool reference, Action<bool> value, string tooltip)
        {
            if (ImGui.Checkbox(name, ref reference))
                value(reference);
            Tooltip.Show(tooltip);
        }
        
        private static void InputFloat(string name, ref float reference, Action<float> value, string tooltip)
        {
            if (ImGui.InputFloat(name, ref reference))
                value(reference);
            Tooltip.Show(tooltip);
        }
    }
}