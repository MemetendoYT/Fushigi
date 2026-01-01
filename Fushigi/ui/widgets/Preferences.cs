using Fushigi.course;
using Fushigi.gl;
using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.util;
using ImGuiNET;
using System;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fushigi.ui.widgets
{
    class Preferences
    {
        static readonly Vector4 errCol = new Vector4(1f, 0, 0, 1);
        static bool romfsTouched = false;
        static bool modRomfsTouched = false;
        static string curTheme = null;
        private static readonly string[] ShaderDescriptions =
            {
            "All Actors",
            "Vanilla Actors Only",
            "Vanilla Actors Except DV",
            "Vanilla Actors Except DV and Tiles",
            "Tilesets Only"
            };
        public static void Draw(ref bool continueDisplay, GLTaskScheduler glTaskScheduler,
     IPopupModalHost modalHost)
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

            var romfs = UserSettings.GetRomFSPath();
            var mod = UserSettings.GetModRomFSPath();
            var useGameShaders = UserSettings.UseGameShaders();
            var enableHalfTile = UserSettings.GetEnableHalfTile();
            var enableTranslation = UserSettings.GetEnableTranslation();
            var backupFreqMinutes = UserSettings.GetBackupFreqMinutes();
            curTheme = UserSettings.GetTheme();
            switch (curTheme)
            {
                case "Dark (Default)": ImGui.StyleColorsDark(); break;
                case "Classic": ImGui.StyleColorsClassic(); break;
                case "Light": ImGui.StyleColorsLight(); break;
            }
            ImGui.Indent();

            if (PathSelector.Show(
                "Wonder Dump Directory",
                ref romfs,
                RomFS.IsValidRoot(romfs))
                )
            {
                romfsTouched = true;

                UserSettings.SetRomFSPath(romfs);

                if (!RomFS.IsValidRoot(romfs))
                {
                    return;
                }

                Task.Run(async () =>
                {
                    await ProgressBarDialog.ShowDialogForAsyncAction(modalHost,
                    $"Preloading Thumbnails",
                    async (p) =>
                    {
                        await glTaskScheduler.Schedule(gl => RomFS.SetRoot(romfs, gl));
                    });

                    ChildActorParam.Load();

                    /* if our parameter database isn't set, set it */
                    if (!ParamDB.sIsInit)
                    {
                        await MainWindow.LoadParamDBWithProgressBar(modalHost);
                    }
                });

            }

            Tooltip.Show("The game files which are stored under the romfs folder.\nIf you are using v1.0.1 of Super Mario Bros. Wonder, use a RomFS Game Path with v65536 files in it.");

            if (romfsTouched && !RomFS.IsValidRoot(romfs))
            {
                ImGui.TextColored(errCol,
                    "The path you have selected is invalid. Please select a RomFS path that contains your full Wonder dump.");
            }

            if (PathSelector.Show("Modded Directory", ref mod, !string.IsNullOrEmpty(mod)))
            {
                modRomfsTouched = true;

                UserSettings.SetModRomFSPath(mod);
                Console.WriteLine("Mod RomFS Path set to: " + mod);

                UserSettings.SetRomfsReload(true);

            }

            Tooltip.Show("The save output where to save modified romfs files");

            if (modRomfsTouched && string.IsNullOrEmpty(mod))
            {
                ImGui.TextColored(errCol,
                    "The path you have selected is invalid. Directory must not be empty.");
            }

            if (ImGui.Checkbox("Use Game Shaders", ref useGameShaders))
            {
                UserSettings.SetGameShaders(useGameShaders);
            }

            Tooltip.Show("Displays models using the shaders present in the game. This may cause a performance drop but will look more visually accurate.");

            if (ImGui.Checkbox("Enable Half Tile Editing", ref enableHalfTile))
                UserSettings.SetEnableHalfTile(enableHalfTile);

            Tooltip.Show("Enable half tile editng for BGUnits, also affects the placement of rails as well.");

            if (ImGui.Checkbox("Enable Actor Translation", ref enableTranslation))
            {
                UserSettings.SetEnableTranslation(enableTranslation);
                CourseScene.refreshTranslation = true;
            }

            Tooltip.Show("Translates all the actor names to English");

            if (ImGui.InputFloat("Backup Frequency (in minutes)", ref backupFreqMinutes))
                UserSettings.SetBackupFreqMinutes(backupFreqMinutes);

            Tooltip.Show("How long between each backup, in minutes.\nBackups are stored wherever Fushigi is installed to.");


            if (ImGui.BeginCombo("Themes", curTheme))
            {
                if (ImGui.Selectable("Dark (Default)", curTheme == "Dark (Default)"))
                {
                    ImGui.StyleColorsDark();
                    curTheme = "Dark (Default)";
                    UserSettings.SetTheme(curTheme);
                }
                if (ImGui.Selectable("Classic", curTheme == "Classic"))
                {
                    ImGui.StyleColorsClassic();
                    curTheme = "Classic";
                    UserSettings.SetTheme(curTheme);
                }
                if (ImGui.Selectable("Light", curTheme == "Light"))
                {
                    ImGui.StyleColorsLight();
                    curTheme = "Light";
                    UserSettings.SetTheme(curTheme);
                }
                ImGui.EndCombo();
            }

            Tooltip.Show("Change the UI theme.");
        }

        private static void DrawAdvancedSettings(IPopupModalHost modalHost)
        {

            var renderCustomModels = UserSettings.GetRenderCustomModels();
            var useAstcTextureCache = UserSettings.UseAstcTextureCache();
            var hideDeletingLinkedActorsPopup = UserSettings.HideDeletingLinkedActorsPopup();
            var useNewCamera = UserSettings.GetUseNewCamera();
            var privateDRPC = UserSettings.GetPrivateDRPC();
            var toggleRomfsReload = UserSettings.GetAllowRomfsReload();

            ImGui.Indent();

            Tooltip.Show("Displays models using the shaders present in the game. This may cause a performance drop but will look more visually accurate.");

            if (ImGui.Checkbox("Render Models from mod RomFS", ref renderCustomModels))
            {
                UserSettings.SetRenderCustomModels(renderCustomModels);
            }

            Tooltip.Show("Uses the models from the mod directory. WARNING: Rendering of custom models using game shaders is broken.");

            if (ImGui.Checkbox("Use Astc Texture Cache", ref useAstcTextureCache))
            {
                UserSettings.SetAstcTextureCache(useAstcTextureCache);
            }

            Tooltip.Show("Saves ASTC textures to disk which takes up disk space, but improves loading times and ram usage significantly.");

            if (ImGui.Checkbox("Hide Deleting Linked Actors Popup", ref hideDeletingLinkedActorsPopup))
            {
                UserSettings.SetHideDeletingLinkedObjectsPopup(hideDeletingLinkedActorsPopup);
            }

            Tooltip.Show("Hides the warning popup when you delete actors with links.");

            if (ImGui.Checkbox("Use New Camera [BETA!]", ref useNewCamera))
                UserSettings.SetUseNewCamera(useNewCamera);

            Tooltip.Show("Uses a new camera system that aims to be more accurate.\nWARNING: in beta and might cause some issues");

            if (ImGui.Checkbox("Enable ROMFS Reload", ref toggleRomfsReload))
                UserSettings.SetAllowRomfsReload(toggleRomfsReload);

            Tooltip.Show("When switching to a different modded romfs everything will reload.");

            if (ImGui.Checkbox("Hide Activity", ref privateDRPC))
                UserSettings.SetPrivateDRPC(privateDRPC);

            Tooltip.Show("Whether or not to hide information about the course in the Discord RPC.\nReload required to work.");


            int shaderSettings = UserSettings.GetShaders();

            if (ImGui.BeginCombo("Shader Settings [EXPERIMENTAL]", ShaderDescriptions[shaderSettings]))
            {
                if (ImGui.Selectable(ShaderDescriptions[0]))
                {
                    UserSettings.SetShaders(0);
                }
                if (ImGui.Selectable(ShaderDescriptions[1]))
                {
                    UserSettings.SetShaders(1);
                }
                if (ImGui.Selectable(ShaderDescriptions[2]))
                {
                    UserSettings.SetShaders(2);
                }
                if (ImGui.Selectable(ShaderDescriptions[3]))
                {
                    UserSettings.SetShaders(3);
                }
                if (ImGui.Selectable(ShaderDescriptions[4]))
                {
                    UserSettings.SetShaders(4);
                }

                ImGui.EndCombo();
            }
            Tooltip.Show("Disable custom shaders on custom actors. NOTE: This only works on new custom actors and not model swaps");
        }
    }
}