using Fushigi.course;
using Fushigi.ui.helpers;
using Fushigi.ui.modal;
using ImGuiNET;
using System.Numerics;

namespace Fushigi.ui.widgets
{
    class AreaSettings
    {
        static LevelViewport Viewport;
        public static void Draw(ref bool continueDisplay, IPopupModalHost modalHost, AreaParam areaParam, LevelViewport viewport)
        {
            Viewport = viewport;
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);

            // Window
            if (ImGui.Begin("Area Settings", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoCollapse))
            {

                // Close Button
                if (ImGui.Button("Close"))
                {
                    continueDisplay = false;

                    //  from Transition palettes
                    var transPal = areaParam.EnvPaletteSetting.TransPaletteList;
                    if (transPal != null)
                    {
                        for (int i = 0; i < transPal.Count; i++)
                            if (transPal[i] == "") transPal.Remove(transPal[i]);

                        if (transPal.Count == 0) transPal = null;
                    }
                    areaParam.EnvPaletteSetting.TransPaletteList = transPal;

                    //  from Wonder palettes
                    var wonderPal = areaParam.EnvPaletteSetting.WonderPaletteList;
                    if (wonderPal != null)
                    {
                        for (int i = 0; i < wonderPal.Count; i++)
                            if (wonderPal[i] == "") wonderPal.Remove(wonderPal[i]);

                        if (wonderPal.Count == 0) wonderPal = null;
                    }
                    areaParam.EnvPaletteSetting.WonderPaletteList = wonderPal;

                    //  from Event palettes
                    var eventPal = areaParam.EnvPaletteSetting.EventPaletteList;
                    if (eventPal != null)
                    {
                        for (int i = 0; i < eventPal.Count; i++)
                            if (eventPal[i] == "") eventPal.Remove(eventPal[i]);

                        if (eventPal.Count == 0) eventPal = null;
                    }
                    areaParam.EnvPaletteSetting.EventPaletteList = eventPal;
                }

                // Setting Tabs
                if (ImGui.BeginTabBar("AreaSettingsTypes", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem("Appearance"))
                    {
                        DrawAppearanceSettings(areaParam);
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Music & Sound"))
                    {
                        DrawMusicSettings(areaParam);
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Misc."))
                    {
                        DrawMiscSettings(areaParam);
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }

                if (ImGui.IsAnyItemActive() && ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                    CourseSave.saveStatus = false;
                
                ImGui.End();
            }

        }

        private static void DrawAppearanceSettings(AreaParam areaParam)
        {
            ImGui.SeparatorText("Terrain Settings");
            if (ImGui.BeginTable("SkinParam", 2))
            {
                AreaParam.AreaSkinParam skinParam = areaParam.SkinParam;
                string value = "";

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);


                if (Ounce.isOunce)
                   StringParamList.Tilesets.TryAdd("Bellabel Park (Underground)", "NewWorldChika");

                string fieldA = (string)skinParam.FieldA ?? "";
                if (ImguiHelper.DrawDropdown("Tileset A", ref fieldA, StringParamList.Tilesets))
                {
                    skinParam.FieldA = fieldA;
                    LevelViewport.updateSkinA = true;
                }

                string fieldB = (string)skinParam.FieldB ?? "";
                if (ImguiHelper.DrawDropdown("Tileset B", ref fieldB, StringParamList.Tilesets))
                {
                    skinParam.FieldB = fieldB;
                    LevelViewport.updateSkinB = true;
                }

                string objectField = (string)skinParam.Object ?? "";
                if (ImguiHelper.DrawDropdown("Object Appearance", ref objectField, StringParamList.Tilesets))
                    skinParam.Object = objectField;

                bool disableBgUnitDecoA = skinParam.DisableBgUnitDecoA;
                if (ImGui.Checkbox("Disable Decorations on Field A", ref disableBgUnitDecoA))
                    skinParam.DisableBgUnitDecoA = disableBgUnitDecoA;


                ImGui.EndTable();
            }

            ImGui.SeparatorText("Level Palettes");
            var initPal = areaParam.EnvPaletteSetting.InitPaletteBaseName ?? "";
            if (ImGui.TreeNode("Initial Palette"))
            {
                if (ImGui.InputText("##InitialPalette", ref initPal, 1024))
                {
                    string oldPalette = areaParam.EnvPaletteSetting.InitPaletteBaseName;
                    areaParam.EnvPaletteSetting.InitPaletteBaseName = initPal;
                    string palette = areaParam.EnvPaletteSetting.InitPaletteBaseName;
                    Viewport.EnvironmentData.TransitionEnvPalette(oldPalette, palette);
                  
                  
                }
                ImGui.TreePop();
            }

            List<string> transPal = areaParam.EnvPaletteSetting.TransPaletteList;
            if (ImGui.TreeNode("Transition Palettes"))
            {
                if (ImGui.Button("Add Palette"))
                {
                    if (transPal is null) transPal = new List<string>();
                    transPal.Add("");
                    areaParam.EnvPaletteSetting.TransPaletteList = transPal;
                }

                if (transPal is not null)
                {
                    for (int i = 0; i < transPal.Count; i++)
                    {
                        var palette = transPal[i];
                        palette = parsePalette(palette);
                        string oldPalette = palette;
                        string[] parts = palette.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (ImGui.InputText($"##TransitionPalette{i}", ref palette, 1024))
                        {
  
                            Viewport.EnvironmentData.TransitionEnvPalette(oldPalette, palette);
                            palette = revertPalette(palette);
                            transPal[i] = palette;
                        }
                    }
                }

                ImGui.TreePop();
            }

            List<string> wonderPal = areaParam.EnvPaletteSetting.WonderPaletteList;
            if (ImGui.TreeNode("Wonder Palettes"))
            {
                if (ImGui.Button("Add Palette"))
                {
                    if (wonderPal is null) {
                    wonderPal = new List<string>(); }
                    wonderPal.Add("");
                    areaParam.EnvPaletteSetting.WonderPaletteList = wonderPal;
                }

                if (wonderPal is not null)
                {
                    for (int i = 0; i < wonderPal.Count; i++)
                    {
                        var palette = wonderPal[i];
                        palette = parsePalette(palette);
                        string oldPalette = palette;
                        if (ImGui.InputText($"##WonderPalette{i}", ref palette, 1024))
                        {
                            Viewport.EnvironmentData.TransitionEnvPalette(oldPalette, palette);
                            palette = revertPalette(palette);
                            wonderPal[i] = palette;
                        }
                    }
                }

                ImGui.TreePop();
            }

            List<string> eventPal = areaParam.EnvPaletteSetting.EventPaletteList;
            if (ImGui.TreeNode("Event Palettes"))
            {
                if (ImGui.Button("Add Palette"))
                {
                    if (eventPal is null) eventPal = new List<string>();
                    eventPal.Add("");
                    areaParam.EnvPaletteSetting.EventPaletteList = eventPal;
                }

                if (eventPal is not null)
                {
                    for (int i = 0; i < eventPal.Count; i++)
                    {
                        var palette = eventPal[i];
                        palette = parsePalette(palette);
                        string oldPalette = palette;
                        if (ImGui.InputText($"##EventPalette{i}", ref palette, 1024))
                        {
                            Viewport.EnvironmentData.TransitionEnvPalette(oldPalette, palette);
                            palette = revertPalette(palette);
                            eventPal[i] = palette;

                        }
                    }
                }

                ImGui.TreePop();
            }
        }

        private static void DrawMusicSettings(AreaParam areaParam)
        {
            // TODO: Make this better by maybe loading these from a json
            ImGui.SeparatorText("Normal Music");
            {
                if (ImGui.BeginTable("##NormalMusicParam", 2))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    // BgmType
                    string bgmType = areaParam.BgmType ?? "";
                    if (ImguiHelper.DrawDropdown("Background Track", ref bgmType, StringParamList.BgmType))
                        areaParam.BgmType = bgmType;

                    // EnvironmentSound
                    string environmentSound = areaParam.EnvironmentSound ?? "";
                    if (ImguiHelper.DrawDropdown("Environment Sound", ref environmentSound, StringParamList.EnvironmentSound))
                        areaParam.EnvironmentSound = environmentSound;

                    // EnvironmentSoundEfx
                    string environmentSoundEfx = areaParam.EnvironmentSoundEfx ?? "";
                    if (ImguiHelper.DrawDropdown("Environment Sound Effects", ref environmentSoundEfx, StringParamList.EnvironmentSoundEfx))
                        areaParam.EnvironmentSoundEfx = environmentSoundEfx;

                    ImGui.EndTable();
                }

                // BgmInterlock
                var bgmInterlock = areaParam.BgmInterlock;
                if (ImGui.Checkbox("Background Music Interlock", ref bgmInterlock))
                    areaParam.BgmInterlock = bgmInterlock;
            }

            ImGui.SeparatorText("Wonder Music");
            {
                if (ImGui.BeginTable("##WonderMusic", 2))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    // WonderBgmType
                    string wonderBgmType = areaParam.WonderBgmType ?? "Delete";
                    if (ImguiHelper.DrawDropdown("Wonder Background Track", ref wonderBgmType, StringParamList.WonderBgmType))
                        areaParam.WonderBgmType = wonderBgmType;

                    // WonderBgmStartOffset
                    ImGui.Text("Wonder Background Track Starting Delay");
                    ImGui.TableNextColumn();
                    var wonderBgmStartOffset = areaParam.WonderBgmStartOffset;
                    if (ImGui.InputFloat("##WonderBgmStartOffset", ref wonderBgmStartOffset))
                        areaParam.WonderBgmStartOffset = wonderBgmStartOffset;

                    ImGui.TableNextColumn();

                    // WonderEnvironmentSound
                    string wonderEnvironmentSound = areaParam.WonderEnvironmentSound ?? "";
                    if (ImguiHelper.DrawDropdown("Wonder Environment Sound", ref wonderEnvironmentSound, StringParamList.WonderEnvironmentSound))
                        areaParam.WonderEnvironmentSound = wonderEnvironmentSound;

                    // WonderEnvironmentSoundEfx
                    string wonderEnvironmentSoundEfx = areaParam.WonderEnvironmentSoundEfx ?? "";
                    if (ImguiHelper.DrawDropdown("Wonder Environment Sound Effects", ref wonderEnvironmentSoundEfx, StringParamList.WonderEnvironmentSoundEfx))
                        areaParam.WonderEnvironmentSoundEfx = wonderEnvironmentSoundEfx;

                    // WonderBgmEfx
                    string wonderBgmEfx = areaParam.WonderBgmEfx ?? "";
                    if (ImguiHelper.DrawDropdown("Wonder Background Music Effect", ref wonderBgmEfx, StringParamList.WonderBgmEfx))
                        areaParam.WonderBgmEfx = wonderBgmEfx == "" ? null : wonderBgmEfx;

                    // WonderSEKeyForTag
                    string wonderSEKeyForTag = areaParam.WonderSEKeyForTag ?? "";
                    if (ImguiHelper.DrawDropdown("Wonder Sound Effect Key for Tag", ref wonderSEKeyForTag, StringParamList.WonderSEKeyForTag))
                        areaParam.WonderSEKeyForTag = wonderSEKeyForTag;

                    ImGui.EndTable();
                }

                // BgmInterlockOfWonder
                var bgmInterlockOfWonder = areaParam.BgmInterlockOfWonder;
                if (ImGui.Checkbox("Wonder Background Music Interlock", ref bgmInterlockOfWonder))
                    areaParam.BgmInterlockOfWonder = bgmInterlockOfWonder;
            }

            ImGui.SeparatorText("Misc. Sound Settings");
            {
                if (ImGui.BeginTable("##MiscMusic", 2))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    // BgmString
                    string bgmString = areaParam.BgmString ?? "";
                    if (ImguiHelper.DrawDropdown("Background Music Settings String", ref bgmString, StringParamList.BgmString))
                        areaParam.BgmString = bgmString;

                    // ExternalSoundAsset
                    string externalSoundAsset = areaParam.ExternalSoundAsset ?? "";
                    if (ImguiHelper.DrawDropdown("External Sound Asset", ref externalSoundAsset, StringParamList.ExternalSoundAsset))
                        areaParam.ExternalSoundAsset = externalSoundAsset;

                    // DynamicResolutionQuality
                    string dynamicResolutionQuality = areaParam.DynamicResolutionQuality ?? "";
                    if (ImguiHelper.DrawDropdown("Dynamic Resolution Quality", ref dynamicResolutionQuality, StringParamList.DynamicResolutionQuality))
                        areaParam.DynamicResolutionQuality = dynamicResolutionQuality;

                    // PlayerRhythmJumpTiming
                    string playerRhythmJumpTiming = areaParam.PlayerRhythmJumpTiming ?? "";
                    if (ImguiHelper.DrawDropdown("Player Rhythm Jump Timing", ref playerRhythmJumpTiming, StringParamList.PlayerRhythmJumpTiming))
                        areaParam.PlayerRhythmJumpTiming = playerRhythmJumpTiming;

                    // PlayerRhythmJumpBadgeTiming
                    string playerRhythmJumpBadgeTiming = areaParam.PlayerRhythmJumpBadgeTiming ?? "";
                    if (ImguiHelper.DrawDropdown("Player Rhythm Jump Badge Timing", ref playerRhythmJumpBadgeTiming, StringParamList.PlayerRhythmJumpBadgeTiming))
                        areaParam.PlayerRhythmJumpBadgeTiming = playerRhythmJumpBadgeTiming;

                    // ExternalRhythmPatternSet
                    string externalRhythmPatternSet = areaParam.ExternalRhythmPatternSet ?? "";
                    if (ImguiHelper.DrawDropdown("External Rhythm Pattern Set", ref externalRhythmPatternSet, StringParamList.ExternalRhythmPatternSet))
                        areaParam.ExternalRhythmPatternSet = externalRhythmPatternSet == "" ? null : externalRhythmPatternSet;

                    ImGui.EndTable();
                }

                // UseMetalicPlayerSoundAsset
                var useMetalicPlayerSoundAsset = areaParam.UseMetalicPlayerSoundAsset;
                if (ImGui.Checkbox("Use Metalic Player Sound Asset", ref useMetalicPlayerSoundAsset))
                    areaParam.UseMetalicPlayerSoundAsset = useMetalicPlayerSoundAsset;

                // IsNeedCallWaterInSE
                var isNeedCallWaterInSE = areaParam.IsNeedCallWaterInSE;
                if (ImGui.Checkbox("Water-In Sound Effect Call needed", ref isNeedCallWaterInSE))
                    areaParam.IsNeedCallWaterInSE = isNeedCallWaterInSE;

                // IsNeedCallWaterInSE
                var isNotCallWaterEnvSE = areaParam.IsNotCallWaterEnvSE;
                if (ImGui.Checkbox("Don't call Water Environment Sound Effect", ref isNotCallWaterEnvSE))
                    areaParam.IsNotCallWaterEnvSE = isNotCallWaterEnvSE;
            }
        }
        public static string parsePalette(string palette)
        {
            palette = palette.Replace("Work/Gyml/Gfx/EnvPaletteParam/", "");
            palette = palette.Replace(".game__gfx__EnvPaletteParam.gyml", "");
            return palette;
        }

        public static string revertPalette(string palette)
        {
            palette = "Work/Gyml/Gfx/EnvPaletteParam/" + palette + ".game__gfx__EnvPaletteParam.gyml";
            return palette;
        }
        public static readonly Dictionary<uint, string> BackGroundAreaTypeNames = new()
        {
            { 1097715313, "WithScroll" },
            { 3069107721, "None" },
        };

        private static void DrawMiscSettings(AreaParam areaParam)
        {
            if (ImGui.BeginTable("##MiscSettings", 2))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                // BadgeMedleyEquipBadgeId
                string badgeMedleyEquipBadgeId = areaParam.BadgeMedleyEquipBadgeId ?? "";
                if (ImguiHelper.DrawDropdown("Badge Medley Equip Badge ID", ref badgeMedleyEquipBadgeId, StringParamList.BadgeMedleyEquipBadgeId))
                    areaParam.BadgeMedleyEquipBadgeId = badgeMedleyEquipBadgeId;

                // BackGroundAreaType
                var backGroundAreaType = "";
                if (areaParam.BackGroundAreaType is uint)
                    backGroundAreaType = BackGroundAreaTypeNames[(uint)areaParam.BackGroundAreaType];
                else if (areaParam.BackGroundAreaType != null)
                    backGroundAreaType = areaParam.BackGroundAreaType.ToString();

                if (ImguiHelper.DrawDropdown("Background Area Type", ref backGroundAreaType, StringParamList.BackGroundAreaType))
                    areaParam.BackGroundAreaType = backGroundAreaType;

                // EnvSetName
                string envSetName = areaParam.EnvSetName ?? "";
                if (ImguiHelper.DrawDropdown("Environment Set Name", ref envSetName, StringParamList.EnvSetName))
                    areaParam.EnvSetName = envSetName;

                ImGui.EndTable();
            }

            // IsWaterArea
            var isWaterArea = areaParam.IsWaterArea;
            if (ImGui.Checkbox("Is Water Area", ref isWaterArea))
                areaParam.IsWaterArea = isWaterArea;

            // IsKoopaJr04Area
            var isKoopaJr04Area = areaParam.IsKoopaJr04Area;
            if (ImGui.Checkbox("Is Bowser Jr. 04 Fight Area", ref isKoopaJr04Area))
                areaParam.IsKoopaJr04Area = isKoopaJr04Area;

            // IsInvisibleDeadLine
            var isInvisibleDeadLine = areaParam.IsInvisibleDeadLine;
            if (ImGui.Checkbox("Is Invisible Death Line", ref isInvisibleDeadLine))
                areaParam.IsInvisibleDeadLine = isInvisibleDeadLine;

            // IsResetMarkerFlag
            var isResetMarkerFlag = areaParam.IsResetMarkerFlag;
            if (ImGui.Checkbox("Is Reset Marker Flag", ref isResetMarkerFlag))
                areaParam.IsResetMarkerFlag = isResetMarkerFlag;

            // IsVisibleOnlySameWonderPlayer
            var isVisibleOnlySameWonderPlayer = areaParam.IsVisibleOnlySameWonderPlayer;
            if (ImGui.Checkbox("Is Visible Only Same Wonder Player", ref isVisibleOnlySameWonderPlayer))
                areaParam.IsVisibleOnlySameWonderPlayer = isVisibleOnlySameWonderPlayer;

            // IsVisibleOnlySameWonderPlayer
            var isSetListenerCenter = areaParam.IsSetListenerCenter;
            if (ImGui.Checkbox("Is Set Listener Center", ref isSetListenerCenter))
                areaParam.IsSetListenerCenter = isSetListenerCenter;
        }
    }
}
