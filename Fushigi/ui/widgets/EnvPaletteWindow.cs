using Fasterflect;
using Fushigi.agl;
using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.course;
using Fushigi.env;
using Fushigi.gl.Bfres.AreaData;
using Fushigi.rstb;
using Fushigi.ui.modal;
using Fushigi.util;
using FuzzySharp.Edits;
using ImGuiNET;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Newtonsoft.Json.Linq;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Fushigi.env.EnvPalette;
using static Fushigi.gl.Bfres.EnvironmentBlockExtended;
using static Fushigi.gl.Bfres.GsysEnvironment;
using static Microsoft.Msagl.Layout.Incremental.KDTree;
using static System.Net.WebRequestMethods;

namespace Fushigi.ui.widgets
{
    public class EnvPaletteWindow
    {
        private AreaParam AreaParam;
        private EnvPalette EnvPalette;
        private string Label;

        public EnvPaletteWindow() { }

        public static bool hasInitialized = false;
        private GL _gl;
        bool showTop = false;
        bool showBottom = false;
        private string activeCurveEditor = null;
        public EnvPalette.EnvSkyLut CurrentLut;

        private static readonly Dictionary<string, string> LocationType = new Dictionary<string, string>()
        {
            { "None", "None" },
            { "Desert", "Desert" },
            { "White Desert", "WhiteDesert" },
            { "UnderWater", "UnderWater" },
            { "UnderWaterArea", "UnderWaterArea" },
            { "UnderWaterFixCamera", "UnderWaterFixCamera" },
            { "Grassy Field", "GrassField" },
            { "Forest_Spore", "Forest_Spore" },
            { "Forest Godrays", "Forest_Sunbeams" },
            { "Cave", "CaveDust" },
            { "Leaves", "Pasture" },
            { "Green_Spore", "Green_Spore" },
            { "FirePlace", "FirePlace" },
            { "Lava", "Lava" },
            { "ScrollFireSpark", "ScrollFireSpark" },
            { "LensFlareMoon", "LensFlareMoon" },
            { "GoldWaterFall", "GoldWaterFall" },
            { "Wetlands", "Wetlands" },
            { "LastBossCastle", "LastBossCastle" },
            { "EDDemoTimeLineA", "EDDemoTimeLineA" },
            { "EDDemoTimeLineB", "EDDemoTimeLineB" },
            { "Credits", "StaffRollSpotLight" }
        };

        private static readonly Dictionary<string, string> WeatherType = new Dictionary<string, string>()
        {
            { "None", "None" },
            { "Rain", "Rain" },
            { "Honey Drip", "HoneyRain" },
            { "Slime Drip", "GelRain" },
            { "Snow", "Snow" },
            { "SnowBirdEye", "SnowBirdEye" },
            { "Blizzard (Right to Left)", "BlizzardRtoL" },
            { "Blizzard", "Blizzard" },
            { "Lens Flare", "LensFlare" },
            { "LastKoopaAppear01", "LastKoopaAppear01" },
            { "LastKoopaAppear02", "LastKoopaAppear02" },
            { "LastKoopaAppear03", "LastKoopaAppear03" },
            { "LastKoopaBattle00", "LastKoopaBattle00" },
            { "LastKoopaBattle01", "LastKoopaBattle01" },
            { "LastKoopaBattle02", "LastKoopaBattle02" },
            { "LastKoopaBattle03", "LastKoopaBattle03" }
        };

        private static readonly Dictionary<string, string> WonderType = new Dictionary<string, string>()
        {
            { "None", "None" },
            { "Default", "Default" },
            { "LastWonderKoppaJr", "LastWonderKoppaJr" },
            { "OPDemoKoopaCastleWonder", "OPDemoKoopaCastleWonder" },
            { "OPDemoBeautifulWonder", "OPDemoBeautifulWonder" },
            { "Wonder Default Flower", "WonderDefaultFlower" },
            { "Wonder Default Flowerdust", "WonderDefaultFlowerdust" },
            { "WonderDefaultTwinkle", "WonderDefaultTwinkle" },
            { "WonderDarkFlower", "WonderDarkFlower" },
            { "Goomba Wonder", "WonderKuribo" },
            { "WonderWater", "WonderWater" },
            { "WonderMusic", "WonderMusic" },
            { "WonderMusicDeep", "WonderMusicDeep" },
            { "WonderCaution", "WonderCaution" },
            { "Wonder Blizzard", "WonderSnow" },
            { "Wonder Dot", "WonderDot" },
            { "Wonder Star", "WonderStar" },
            { "Wonder Stripe", "WonderStripe" },
            { "Quiz Wonder", "WonderQuiz" },
            { "WonderQuick", "WonderQuick" },
            { "WonderSlow", "WonderSlow" },
            { "WonderSpeed", "WonderSpeed" },
            { "Wonderflash", "Wonderflash" },
            { "WonderGradation", "WonderGradation" },
            { "WonderLotion", "WonderLotion" },
            { "WonderFlyingItem", "WonderFlyingItem" },
            { "WonderWaterball", "WonderWaterball" },
            { "WonderOverlookMove", "WonderOverlookMove" },
            { "WonderBowser", "WonderBowser" },
            { "WonderBowserLast", "WonderBowserLast" }
        };

        public void Load(GL gl, AreaParam areaParam, EnvPalette envPalette)
        {
            _gl = gl;
            AreaParam = areaParam;
            EnvPalette = envPalette;
            Update();
      
        }


        public void Update()
        {
            AreaResourceManager.ActiveArea.ReloadPalette(_gl, EnvPalette);
        }

        public void Draw(ref bool continueDisplay, IPopupModalHost modalHost)
        {
            ImGui.SetNextWindowSize(new Vector2(900 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);

            bool open = ImGui.Begin("Palette Window", ImGuiWindowFlags.NoCollapse);
           
            if (open)
            {
                if (ImGui.Button("Close"))
                    continueDisplay = false;

                PaletteDropdown();

                if (ImGui.BeginTabBar("EnvTabs"))
                {

                    if (ImGui.BeginTabItem("Components"))
                    {
                        RenderToggleUI();
                        ImGui.EndTabItem();
                    }


                    if (ImGui.BeginTabItem("Post Processing"))
                    {
                        if (ImGui.CollapsingHeader("Bloom", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderBloomUI();
                        }

                        if (ImGui.CollapsingHeader("Depth of Field", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderDOFUI();
                        }

                        if (ImGui.CollapsingHeader("Environment Effects", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderInfoUI();
                        }
                        if (ImGui.CollapsingHeader("Fog", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderFogUI();
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Environment"))
                    {
                        if (ImGui.CollapsingHeader("Skybox", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderSkyboxUI();
                        }
                        if (ImGui.CollapsingHeader("Enviornment Color", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderEnvColorUI();
                        }
                        if (ImGui.CollapsingHeader("Environment Map", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderEnvMapUI();
                        }
                        ImGui.EndTabItem();
                    }

 
                    if (ImGui.BeginTabItem("Lighting"))
                    {
                        if (ImGui.CollapsingHeader("Directional and Hemi Lighting", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderLightsUI();
                        }
                        if (ImGui.CollapsingHeader("Global Illumination", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderGIUI();
                        }
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Shading"))
                    {
                        if (ImGui.CollapsingHeader("Emission", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderEmissionUI();
                        }
                        if (ImGui.CollapsingHeader("Shadow", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderShadowUI();
                        }
                        if (ImGui.CollapsingHeader("Rim", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            RenderRimUI();
                        }
                        ImGui.EndTabItem();
                    }


                    ImGui.EndTabBar();
                }
            }

            ImGui.End(); 
        }

        private void RenderBloomUI()
        {
            if (EnvPalette.Bloom == null)
            {
                if (ImGui.Button("Add Bloom"))
                    EnvPalette.Bloom = new EnvPalette.EnvBloom();
            }
            else
            {
                if (ImGui.Button("Remove Bloom"))
                {
                    EnvPalette.Bloom = null;
                    return;
                }


                ImGui.Columns(2);
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGui.Indent();
                DrawFloatSlider("Intensity", EnvPalette.Bloom, "Intensity", 0, 1f);
                DrawFloat("Mask End", EnvPalette.Bloom, "MaskEnd");
                DrawFloatSlider("Mask Ratio", EnvPalette.Bloom, "MaskRatio", 0, 1f);
                DrawFloatSlider("Threshold", EnvPalette.Bloom, "Threshold", 0, 1f);
                DrawFloatSlider("MaskColorPower", EnvPalette.Bloom, "MaskColorPower", 0, 1f);
                ImGui.Unindent();
                ImGui.Columns(1);

            }


        }
        private void RenderEnvMapUI()
        {
            if (EnvPalette.EnvMap == null)
            {
                if (ImGui.Button("Add EnvMap"))
                {
                    EnvPalette.EnvMap = new EnvPalette.EnvMapList();
                    EnvPalette.EnvMap.Ground0 = new EnvPalette.EnvLightMap();
                    EnvPalette.EnvMap.Ground1 = new EnvPalette.EnvLightMap();
                    EnvPalette.EnvMap.Horizon = new EnvPalette.EnvLightMap();
                    EnvPalette.EnvMap.Sky0 = new EnvPalette.EnvLightMap();
                    EnvPalette.EnvMap.Sky1 = new EnvPalette.EnvLightMap();
                }
            }
            else
            {
                if (ImGui.Button("Remove EnvMap"))
                {
                    EnvPalette.EnvMap = null;
                    return;
                }

                if (ImGui.TreeNode($"Ground0"))
                {
                    RenderMapUI("Ground0", EnvPalette.EnvMap.Ground0);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($"Ground1"))
                {
                    RenderMapUI("Ground1", EnvPalette.EnvMap.Ground1);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($"Horizon"))
                {
                    RenderMapUI("Horizon", EnvPalette.EnvMap.Horizon);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($"Sky0"))
                {
                    RenderMapUI("Sky0", EnvPalette.EnvMap.Sky0);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode($"Sky1"))
                {
                    RenderMapUI("Sky1", EnvPalette.EnvMap.Sky1);
                    ImGui.TreePop();
                }

            }
        }


        public void RenderMapUI(string label, EnvPalette.EnvLightMap LightMap)
        {
            if (LightMap == null)
                return;

            ImGui.Columns(2);

            DrawColor($"{label} Color", LightMap, "Color");
            DrawFloat($"{label} Intensity", LightMap, "Intensity");

            ImGui.Columns(1);
        }

        private void RenderEmissionUI()
        {
            if (EnvPalette.Emission == null)
            {
                if (ImGui.Button("Add Emission"))
                    EnvPalette.Emission = new EnvPalette.EnvEmission();
            }
            else
            {
                if (ImGui.Button("Remove Emission"))
                {
                    EnvPalette.Emission = null;
                    return;
                }



                ImGui.Columns(2);
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGui.Indent();
                DrawColor("Color", EnvPalette.Emission, "Color");
                ImGui.Unindent();
                ImGui.Columns(1);

            }


        }

        private void RenderDOFUI()
        {
            if (EnvPalette.DOF == null)
            {
                if (ImGui.Button("Add DOF"))
                    EnvPalette.DOF = new EnvPalette.EnvDepthOfField();
            }
            else
            {
                if (ImGui.Button("Remove DOF"))
                {
                    EnvPalette.DOF = null;
                    return;
                }

                ImGui.Columns(2);
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGui.Indent();
                DrawCheckbox("Enable", EnvPalette.DOF, "Enable");
                DrawFloat("End", EnvPalette.DOF, "End");
                DrawIntSlider("Mip Map", EnvPalette.DOF, "MipLevelMax", 0, 8);
                DrawFloat("Start", EnvPalette.DOF, "Start");
                ImGui.Unindent();
                ImGui.Columns(1);

            }

        }

        private void RenderGIUI()
        {
            if (EnvPalette.GI == null)
            {
                if (ImGui.Button("Add GI"))
                    EnvPalette.GI = new EnvPalette.EnvGlobalIllumination();
            }
            else
            {
                if (ImGui.Button("Remove GI"))
                {
                    EnvPalette.GI = null;
                    return;
                }


                ImGui.Columns(2);
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGui.Indent();

                DrawIntSliderU("Blur Level", EnvPalette.GI, "BlurLevel", 0, 10);
                DrawFloatSlider("Intensity", EnvPalette.GI, "Intensity", 0f, 1f);
                DrawFloatSlider("Ratio", EnvPalette.GI, "RimIntensity", 0f, 1f);
                DrawFloat("RimIntensity", EnvPalette.GI, "RimIntensity");
                DrawFloat("RimPow", EnvPalette.GI, "RimPow");
                ImGui.Columns(1);

            }
        }


        private void RenderShadowUI()
        {
            if (EnvPalette.Shadow == null)
            {
                if (ImGui.Button("Add Shadow"))
                    EnvPalette.Shadow = new EnvPalette.EnvShadow();
            }
            else
            {
                if (ImGui.Button("Remove Shadow"))
                {
                    EnvPalette.Shadow = null;
                    return;
                }
            

            ImGui.Columns(2);
            ImGui.NextColumn();
            ImGui.NextColumn();

            ImGui.Indent();
            DrawCheckbox("Dynamic Depth Shadows", EnvPalette.Shadow, "EnableDynamicDepthShadow");
            DrawFloat("Latitude", EnvPalette.Shadow, "Latitude");
            DrawFloat("Longitude", EnvPalette.Shadow, "Longitude");
            DrawColor("AOColor", EnvPalette.Shadow, "AOColor");
            ImGui.Unindent();
            ImGui.Columns(1);

            }
        }

        private void PaletteDropdown()
        {
            void SelectPalette(string name, string palette)
            {
                if (string.IsNullOrEmpty(palette))
                    return;

                palette = palette.Replace("Work/Gyml/Gfx/EnvPaletteParam/", "");
                palette = palette.Replace(".game__gfx__EnvPaletteParam.gyml", "");

                //  Work / Gyml / Gfx / EnvPaletteParam / AW_Hajimari_Sougen_HajimariDokan.game__gfx__EnvPaletteParam.gyml

                bool selected = this.EnvPalette?.Name == palette;
                if (ImGui.Selectable($"{name} : {palette}", selected))
                {
                    if (CanTransition(EnvPalette?.Name, palette))
                    {
                        AreaResourceManager.ActiveArea.TransitionEnvPalette(EnvPalette?.Name, palette);
                        EnvPalette.Load(palette);
                    }
                    else
                    {
                        EnvPalette.Load(palette);
                        this.Update();
                    }
                }
                if (selected)
                    ImGui.SetItemDefaultFocus();
            }

            if (ImGui.BeginCombo("Area Palette List", EnvPalette?.Name, ImGuiComboFlags.HeightLarge))
            {
                SelectPalette($"Default Palette", this.AreaParam.EnvPaletteSetting.InitPaletteBaseName);

                if (this.AreaParam.EnvPaletteSetting.WonderPaletteList != null)
                {
                    foreach (var palette in this.AreaParam.EnvPaletteSetting.WonderPaletteList)
                        SelectPalette($"Wonder Palette", palette);
                }
                if (this.AreaParam.EnvPaletteSetting.TransPaletteList != null)
                {
                    foreach (var palette in this.AreaParam.EnvPaletteSetting.TransPaletteList)
                        SelectPalette($"Transition Palette", palette);
                }
                if (this.AreaParam.EnvPaletteSetting.EventPaletteList != null)
                {
                    foreach (var palette in this.AreaParam.EnvPaletteSetting.EventPaletteList)
                        SelectPalette($"Event Palette", palette);
                }
                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("EnvPalette", EnvPalette?.Name, ImGuiComboFlags.HeightLarge))
            {
                var dir = Path.Combine(UserSettings.GetRomFSPath(), "Gyml", "Gfx", "EnvPaletteParam");
                foreach (var file in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(file).Replace(".game__gfx__EnvPaletteParam.bgyml", "");
                    bool select = EnvPalette?.Name == name;
                    if (ImGui.Selectable(name, select))
                    {
                        EnvPalette.Load(name);
                        this.Update();
                        AreaParam.EnvPaletteSetting.InitPaletteBaseName = name;
                    }
                    if (select)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

        }

        bool CanTransition(string fromName, string toName)
        {

            var temp = new EnvPalette(toName);

            if (temp.Emission == null || EnvPalette.Emission != null)
                return false;

            if (temp.Rim == null || EnvPalette.Rim != null)
                return false;

            return true;
        }

        private void RenderInfoUI()
        {
            string value = "";
            if (EnvPalette.Info == null)
            {
                if (ImGui.Button("Add Info"))
                    EnvPalette.Info = new EnvPalette.EnvInfo();
            }
            else
            {
                if (ImGui.Button("Remove Info"))
                {
                    EnvPalette.Info = null;
                    return;
                }



                if (EnvPalette.Info.LocationType == null)
                {
                    value = "None";
                    EnvPalette.Info.LocationType = "None";
                }
                else
                    value = EnvPalette.Info.LocationType.ToString();

                int index = LocationType.Values.ToList().IndexOf(value);
                ImGui.Text("Location Type");
                if (ImGui.Combo("##LocationType", ref index, LocationType.Keys.ToArray(), LocationType.Count(), 10))
                    EnvPalette.Info.LocationType = LocationType.Values.ToArray()[index];


                if (EnvPalette.Info.WeatherType == null)
                {
                    value = "None";
                    EnvPalette.Info.WeatherType = "None";
                }
                else
                    value = EnvPalette.Info.WeatherType;

                index = WeatherType.Values.ToList().IndexOf(value);
                ImGui.Text("Weather Type");
                if (ImGui.Combo("##WeatherType", ref index, WeatherType.Keys.ToArray(), WeatherType.Count(), 10))
                    EnvPalette.Info.WeatherType = WeatherType.Values.ToArray()[index];

                if (EnvPalette.Info.WonderType == null)
                {
                    value = "None";
                    EnvPalette.Info.WonderType = "None";
                }
                else
                    value = EnvPalette.Info.WonderType;


                index = WonderType.Values.ToList().IndexOf(value);
                ImGui.Text("Wonder Type");
                if (ImGui.Combo("##WonderType", ref index, WonderType.Keys.ToArray(), WonderType.Count(), 10))
                    EnvPalette.Info.WonderType = WonderType.Values.ToArray()[index];

            }
        }
  

        public void RenderRimUI()
        {
            if (EnvPalette.Rim == null)
            {
                if (ImGui.Button("Add Rim"))
                    EnvPalette.Rim = new EnvPalette.EnvRim();
            }
            else
            {
                if (ImGui.Button("Remove Rim"))
                {
                    EnvPalette.Rim = null;
                    return;
                }

                ImGui.Columns(2);


                ImGui.AlignTextToFramePadding();
                ImGui.NextColumn();
                ImGui.NextColumn();

                ImGui.Indent();
                DrawColor("Rim Color", EnvPalette.Rim, "Color");
                DrawFloat("Rim Width", EnvPalette.Rim, "Width");
                DrawFloat("Rim Power", EnvPalette.Rim, "Power");
                //ImGui.Text("Rim Amount:");
                //ImGui.NextColumn();
                //ImGui.NextColumn();
                DrawFloatSlider("Object", EnvPalette.Rim, "IntensityObject", 0, 1f);
                DrawFloatSlider("Player", EnvPalette.Rim, "IntensityPlayer", 0, 1f);
                DrawFloatSlider("Enemy", EnvPalette.Rim, "IntensityEnemy", 0, 1f);

                DrawFloatSlider("Field Band", EnvPalette.Rim, "IntensityFieldBand", 0, 1f);
                DrawFloatSlider("Field Wall", EnvPalette.Rim, "IntensityFieldWall", 0, 1f);
                DrawFloatSlider("Field Deco", EnvPalette.Rim, "IntensityFieldDeco", 0, 1f);

                DrawFloatSlider("Cloud", EnvPalette.Rim, "IntensityCloud", 0, 1f);
                DrawFloatSlider("DV", EnvPalette.Rim, "IntensityDV", 0, 1f);

                ImGui.Columns(1);

            }
        }

        public void RenderEnvColorUI()
        {
            if (EnvPalette.EnvColor == null)
            {
                if (ImGui.Button("Add EnvColor"))
                    EnvPalette.EnvColor = new EnvPalette.EnvColorList();
            }
            else
            {
                if (ImGui.Button("Remove EnvColor"))
                {
                    EnvPalette.EnvColor = null;
                    return;
                }

                ImGui.Columns(2);
                DrawFloatSlider("Roughness Mul", EnvPalette.EnvColor, "RoughnessMul", 0, 1f);
                for (int i = 0; i < 8; i++)
                    DrawColor($"Color {i}", EnvPalette.EnvColor, $"Color{i}");
                ImGui.Columns(1);
            }
        }

        public void RenderFogUI()
        {
            if (EnvPalette.Fog == null)
            {
                if (ImGui.Button("Add Fog"))
                    EnvPalette.Fog = new EnvPalette.EnvFogList();
            }
            else
            {
                if (ImGui.Button("Remove Fog"))
                {
                    EnvPalette.Fog = null;
                    return;
                }

                if (ImGui.TreeNode("Main"))
                {
                    RenderFogUI("Main", EnvPalette.Fog.Main);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("MainWorld"))
                {
                    RenderFogUI("MainWorld", EnvPalette.Fog.MainWorld);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Cloud"))
                {
                    RenderFogUI("Cloud", EnvPalette.Fog.Cloud);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("CloudWorld"))
                {
                    RenderFogUI("CloudWorld", EnvPalette.Fog.CloudWorld);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Option"))
                {
                    RenderFogOptionUI(EnvPalette.Fog.Option);
                    ImGui.TreePop();
                }


                }
        }

        public void RenderFogOptionUI(EnvFogOption opt)
        {
            if (opt == null)
                return;

            ImGui.Columns(2);

            DrawFloat("Lighten Ratio", opt, nameof(opt.LightenRatio));

            DrawFloat("NoiseMoveSpeed X", opt.NoiseMoveSpeed, nameof(opt.NoiseMoveSpeed.X));
            DrawFloat("NoiseMoveSpeed Y", opt.NoiseMoveSpeed, nameof(opt.NoiseMoveSpeed.Y));
            DrawFloat("NoiseMoveSpeed Z", opt.NoiseMoveSpeed, nameof(opt.NoiseMoveSpeed.Z));

            DrawFloat("Noise Ratio", opt, nameof(opt.NoiseRatio));
            DrawFloat("Sky Color Ratio", opt, nameof(opt.SkyColorRatio));

            ImGui.Columns(1);
        }
        public void RenderToggleUI()
        {
            DrawToggle("Enable Bloom", EnvPalette.Bloom, EnvPalette, nameof(EnvPalette.IsApplyBloom));
            DrawToggle("Enable Char Light", EnvPalette.CharLight, EnvPalette, nameof(EnvPalette.IsApplyCharLight));
            DrawToggle("Enable Cloud Light", EnvPalette.CloudLight, EnvPalette, nameof(EnvPalette.IsApplyCloudLight));
            DrawToggle("Enable DOF", EnvPalette.DOF, EnvPalette, nameof(EnvPalette.IsApplyDOF));
            DrawToggle("Enable DV Light", EnvPalette.DvLight, EnvPalette, nameof(EnvPalette.IsApplyDvLight));
            DrawToggle("Enable Emission", EnvPalette.Emission, EnvPalette, nameof(EnvPalette.IsApplyEmission));
            DrawToggle("Enable Env Color", EnvPalette.EnvColor, EnvPalette, nameof(EnvPalette.IsApplyEnvColor));
            DrawToggle("Enable Env Map", EnvPalette.EnvMap, EnvPalette, nameof(EnvPalette.IsApplyEnvMap));
            DrawToggle("Enable Field Light", EnvPalette.FieldLight, EnvPalette, nameof(EnvPalette.IsApplyFieldLight));
            DrawToggle("Enable Fog", EnvPalette.Fog, EnvPalette, nameof(EnvPalette.IsApplyFog));
            DrawToggle("Enable GI", EnvPalette.GI, EnvPalette, nameof(EnvPalette.IsApplyGI));
            DrawToggle("Enable Obj Light", EnvPalette.ObjLight, EnvPalette, nameof(EnvPalette.IsApplyObjLight));
            DrawToggle("Enable Rim", EnvPalette.Rim, EnvPalette, nameof(EnvPalette.IsApplyRim));
            DrawToggle("Enable Shadow", EnvPalette.Shadow, EnvPalette, nameof(EnvPalette.IsApplyShadow));
            DrawToggle("Enable Sky", EnvPalette.Sky, EnvPalette, nameof(EnvPalette.IsApplySky));
            DrawToggle("Enable Info", EnvPalette.Info, EnvPalette, nameof(EnvPalette.IsApplyInfo));

        }
        private void DrawToggle(string label, object subsystem, EnvPalette palette, string property)
        {
 
            if (subsystem == null)
            {
                ImGui.TextDisabled($"{label} (Not Added)");
                return;
            }

            var prop = palette.GetType().GetProperty(property);
            bool v = (bool)prop.GetValue(palette);

            if (ImGui.Checkbox(label, ref v))
                prop.SetValue(palette, v);
        }

        public void RenderFogUI(string label, EnvPalette.EnvFog fog)
        {
            if (fog == null)
                return;

            ImGui.Columns(2);

            DrawColor($"{label} Color", fog, "Color");
            DrawFloat($"{label} Start", fog, "Start");
            DrawFloat($"{label} End", fog, "End");
            DrawFloat($"{label} Damp", fog, "Damp");

            ImGui.Columns(1);
        }



        public void RenderLightsUI()
        {

            if (ImGui.TreeNode("ObjLight"))
            {
                EnvPalette.ObjLight = RenderLightsUI("ObjLight", EnvPalette.ObjLight);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("CharLight"))
            {
                EnvPalette.CharLight = RenderLightsUI("CharLight", EnvPalette.CharLight);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("FieldLight"))
            {
                EnvPalette.FieldLight = RenderLightsUI("FieldLight", EnvPalette.FieldLight);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("DvLight"))
            {
                EnvPalette.DvLight = RenderLightsUI("DvLight", EnvPalette.DvLight);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("CloudLight"))
            {
                EnvPalette.CloudLight = RenderLightsUI("CloudLight", EnvPalette.CloudLight);
                ImGui.TreePop();
            }

        }

        public void RenderLightsHemiUI(string label, EnvPalette.EnvLightList list)
        {
            if (list == null)
                return;

            ImGui.Columns(4);

            RenderHemiLights($"{label} Hemi", list.Hemi);

            ImGui.Columns(1);
        }

        public EnvPalette.EnvLightList RenderLightsUI(string label, EnvPalette.EnvLightList list)
        {
            if (list == null)
            {
                if (ImGui.Button($"Add {label}"))
                {
                    list = new EnvLightList();
                    list.Main = new EnvLightDirectional();
                    list.SubDiff0 = new EnvLightDirectional();
                    list.SubDiff1 = new EnvLightDirectional();
                    list.SubDiff2 = new EnvLightDirectional();
                    list.SubSpec0 = new EnvLightDirectional();
                    list.SubSpec1 = new EnvLightDirectional();
                    list.SubSpec2 = new EnvLightDirectional();
                    list.PlayerSubDiff0 = new EnvLightDirectional();
                    return list;
                }
                return null;
            }

            if (ImGui.Button($"Remove {label}"))
                return null;

            ImGui.Columns(6);

            ImGui.Text("Name"); ImGui.NextColumn();
            ImGui.Text("Color"); ImGui.NextColumn();
            ImGui.Text("Intensity"); ImGui.NextColumn();
            ImGui.Text("Longitude"); ImGui.NextColumn();
            ImGui.Text("Latitude"); ImGui.NextColumn();
            ImGui.Text("Dir"); ImGui.NextColumn();

            RenderDirectionalLights($"{label} Main", list.Main);
            RenderDirectionalLights($"{label} SubDiff0", list.SubDiff0);
            RenderDirectionalLights($"{label} SubDiff1", list.SubDiff1);
            RenderDirectionalLights($"{label} SubDiff2", list.SubDiff2);
            RenderDirectionalLights($"{label} SubSpec0", list.SubSpec0);
            RenderDirectionalLights($"{label} SubSpec1", list.SubSpec1);
            RenderDirectionalLights($"{label} SubSpec2", list.SubSpec2);
            RenderDirectionalLights($"{label} PlayerSubDiff0", list.PlayerSubDiff0);

            ImGui.Columns(1);
            ImGui.Spacing();

            if (list.Hemi == null)
            {
                if (ImGui.Button($"Add {label} Hemi"))
                    list.Hemi = new EnvPalette.EnvLightHemisphere();
            }
            else
            {
                if (ImGui.TreeNodeEx($"{label} Hemi", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.Button($"Remove {label} Hemi"))
                        list.Hemi = null;
                    else
                        RenderHemiLights($"{label} Hemi", list.Hemi);

                    ImGui.TreePop();
                }
            }

            return list;
        }

        public void RenderHemiLights(string label, EnvPalette.EnvLightHemisphere hemi)
        {
            if (hemi == null)
                return;

            var sky_color = hemi.Sky.ToVector4();
            var ground_color = hemi.Ground.ToVector4();
            float intensity = hemi.Intensity;

            ImGui.Columns(3);

            ImGui.Text("Sky");
            ImGui.NextColumn();
            if (ImGui.ColorEdit4($"##{label}_Sky", ref sky_color, ImGuiColorEditFlags.NoInputs))
            {
                hemi.Sky = new EnvPalette.Color(sky_color);
                this.Update();
            }
            ImGui.NextColumn();
            ImGui.Text(""); 
            ImGui.NextColumn();

            // GROUND
            ImGui.Text("Ground");
            ImGui.NextColumn();
            if (ImGui.ColorEdit4($"##{label}_Ground", ref ground_color, ImGuiColorEditFlags.NoInputs))
            {
                hemi.Ground = new EnvPalette.Color(ground_color);
                this.Update();
            }
            ImGui.NextColumn();
            ImGui.Text("");
            ImGui.NextColumn();

            ImGui.Text("Intensity");
            ImGui.NextColumn();
            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);
            if (ImGui.DragFloat($"##{label}_Intensity", ref intensity))
            {
                hemi.Intensity = intensity;
                this.Update();
            }
            ImGui.PopItemWidth();
            ImGui.NextColumn();

            ImGui.Columns(1);
        }

        public void RenderDirectionalLights(string label, EnvPalette.EnvLightDirectional dir)
        {
            if (dir == null)
                return;

            var color = dir.Color.ToVector4();
            float intensity = dir.Intensity;
            float longitude = dir.Longitude;
            float latitude = dir.Latitude;

            ImGui.Text(label);
            ImGui.NextColumn();

            if (ImGui.ColorEdit4($"##Color{label}dir", ref color, ImGuiColorEditFlags.NoInputs))
            {
                dir.Color = new EnvPalette.Color(color);
                this.Update();
            }
            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.DragFloat($"##Intensity{label}inten", ref intensity))
            {
                dir.Intensity = intensity;
                this.Update();
            }
            ImGui.PopItemWidth();

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.DragFloat($"##Longitude{label}long", ref longitude))
            {
                dir.Longitude = longitude;
                this.Update();
            }

            ImGui.PopItemWidth();

            ImGui.NextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - 2);

            if (ImGui.DragFloat($"##Latitude{label}lat", ref latitude))
            {
                dir.Latitude = latitude;
                this.Update();
            }

            ImGui.PopItemWidth();

            ImGui.NextColumn();

            var d = GetDirectionalVector(latitude, longitude);

            ImGui.Text($"{MathF.Round(d.X, 5)} {MathF.Round(d.Y, 5)} {MathF.Round(d.Z, 5)}");

            ImGui.NextColumn();
        }

        static System.Numerics.Vector3 GetDirectionalVector(float latitude, float longitude)
        {
            // Convert latitude and longitude from degrees to radians
            float latRad = latitude * MathUtil.Deg2Rad;
            float lonRad = longitude * MathUtil.Deg2Rad;

            float x = MathF.Cos(latRad) * MathF.Sin(lonRad);
            float y = MathF.Sin(latRad);
            float z = MathF.Cos(latRad) * MathF.Cos(lonRad);

            var dir = new System.Numerics.Vector3(x, y, z);
            return System.Numerics.Vector3.Normalize(-dir);
        }

        public void RenderPostEffectUI()
        {
        }
        public void RenderCurveEditor(string label)
        {
            ImGui.SetNextWindowSize(new Vector2(500 * MainWindow.dpiScale, 500 * MainWindow.dpiScale), ImGuiCond.Once);
            var io = ImGui.GetIO();
            if (ImGui.Begin("Curve Editor - " + label, ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Button("Close"))
                    activeCurveEditor = null;

                CurveEditors[label].Render(400);
            }
            Update();
            ImGui.End();
        }
        public void RenderSkyboxUI()
        {

            if (EnvPalette.Sky == null)
            {
                if (ImGui.Button($"Add Sky"))
                    EnvPalette.Sky = new EnvPalette.EnvSky();
            }
            else
            {
                if (ImGui.Button("Remove Sky"))
                {
                    EnvPalette.Sky = null;
                    Update();
                    return;
                }

                ImGui.Separator();

                ImGui.Columns(1);

                EnvPalette.Sky.LutTexTop = RenderSkyboxLUTUI("Top", EnvPalette.Sky.LutTexTop);
                EnvPalette.Sky.LutTexLeft = RenderSkyboxLUTUI("Left", EnvPalette.Sky.LutTexLeft);
                EnvPalette.Sky.LutTexLeftTop = RenderSkyboxLUTUI("Top Left", EnvPalette.Sky.LutTexLeftTop);
                EnvPalette.Sky.LutTexRightTop = RenderSkyboxLUTUI("Top Right", EnvPalette.Sky.LutTexRightTop);
                ImGui.Columns(1);
            }
    
        }

        private bool open_popup = false;
        private string popup_lut = "";
        private Dictionary<string, AglCurveEditor> CurveEditors = new();



        public EnvPalette.EnvSkyLut RenderSkyboxLUTUI(string label, EnvPalette.EnvSkyLut lut)
        {
            if (lut == null)
            {
                if (ImGui.Button($"Add {label}"))
                {
                    lut = new EnvPalette.EnvSkyLut
                    {
                        ColorBegin = new EnvPalette.Color(new Vector4(0f, 0f, 0f, 1f)),
                        ColorMiddle = new EnvPalette.Color(new Vector4(0f, 0f, 0f, 1f)),
                        ColorEnd = new EnvPalette.Color(new Vector4(0f, 0f, 0f, 1f)),
                        UseMiddleColor = false,
                        Curve = new EnvSkyLutCurve
                        {
                            Type = "Linear",
                            MaxX = 1f,
                            Data = new List<float> { 0f, 1f }
                        }
                    };
                }
                }
            else
            {
                if (ImGui.Button($"Remove {label}"))
                {
                    if (activeCurveEditor == label)
                    {
                        activeCurveEditor = null;
                        CurrentLut = null;
                    }

                    CurveEditors.Remove(label);
                    return null;
                }
            }

            if (lut != null)
            {
                if (ImGui.Button($"{IconUtil.ICON_EDIT}##{label}", new Vector2(30, 30)))
                {
                    activeCurveEditor = label;

                    if (!CurveEditors.TryGetValue(label, out var editor))
                    {
                        editor = new AglCurveEditor();
                        CurveEditors[label] = editor;
                    }

                    editor.Load(lut, label, EnvPalette);
                    CurrentLut = lut;
                }

                ImGui.SameLine();

                if (ImGui.BeginChild($"Grad{label}", new Vector2(ImGui.GetColumnWidth() - 2, 30)))
                {
                    var screenPos = ImGui.GetCursorScreenPos();
                    float width = ImGui.GetContentRegionAvail().X;

                    CalculateGradient(lut, screenPos, (int)width, 30);
                }
                ImGui.EndChild();

                ImGui.Separator();

                if (activeCurveEditor == label)
                {
                    if (CurrentLut != lut)
                    {
                        var editor = CurveEditors[label];
                        editor.Load(lut, label, EnvPalette);
                        CurrentLut = lut;
                    }

                    RenderCurveEditor(label);
                }

                return lut;
            }

            return lut;
        }


        public void CalculateGradient(EnvPalette.EnvSkyLut lut, Vector2 screenPos, int width, float height)
        {
            var pos = screenPos;

            if (lut == null)
                return;

            var type = AglCurve.CurveType.Linear;
            if (lut.Curve != null)
            {
                type = lut.Curve.GetCurveType();
            }
            var data = new float[] { 0f, 1f };
            if(lut.Curve != null)
                data = lut.Curve.Data.ToArray();

            Vector4[] colors = lut.UseMiddleColor ?
                new Vector4[] { lut.ColorBegin.ToVector4(), lut.ColorMiddle.ToVector4(), lut.ColorEnd.ToVector4() } :
                new Vector4[] { lut.ColorBegin.ToVector4(), lut.ColorEnd.ToVector4(), lut.ColorEnd.ToVector4() };

            Vector4 LerpBetweenColors(Vector4 a, Vector4 b, Vector4 c, float t)
            {
                //bottom to top
                if (!lut.UseMiddleColor)
                    return Vector4.Lerp(a, b, t);

                if (t < 0.5f) // Bottom to middle
                    return Vector4.Lerp(a, b, t / 0.5f);
                else // Middle to top
                    return Vector4.Lerp(b, c, (t - 0.5f) / 0.5f);
            }

            for (int i = 0; i < width; i++)
            {
                float time = i / (width - 1f);
                float x = 1;
                if(lut.Curve != null)
                    x = MathF.Min(AglCurve.Interpolate(data, type, time), lut.Curve.MaxX);

                //Use "X" to lerp between the colors to get
                Vector4 color = LerpBetweenColors(colors[0], colors[1], colors[2], x);

                ImGui.GetWindowDrawList().AddRectFilled(
                    screenPos + new Vector2(time * width - 1, 0),
                    screenPos + new Vector2(time * width + 1, height), ImGui.ColorConvertFloat4ToU32(color));
            }
        }

        private void DrawColor(string label, object obj, string property)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var color = (EnvPalette.Color)prop.GetValue(obj);
            var vec = color.ToVector4();

            if (ImGui.ColorEdit4($"##{label}", ref vec, ImGuiColorEditFlags.NoInputs))
            {
                prop.SetValue(obj, new EnvPalette.Color(vec));
                Update();
            }
            ImGui.NextColumn();
        }
        private void DrawCheckbox(string label, object obj, string property)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var v = (bool)prop.GetValue(obj);
            if (ImGui.Checkbox($"##{label}", ref v))
            {
                prop.SetValue(obj, v);
                Update();
            }
            ImGui.NextColumn();
        }
        private void DrawFloat(string label, object obj, string property)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var v = (float)prop.GetValue(obj);
            if (ImGui.DragFloat($"##{label}", ref v))
            {
                prop.SetValue(obj, v);
                Update();
            }
            ImGui.NextColumn();
        }
        public void SavePalette(RSTB resource_table, string folder)
        {
            if(EnvPalette.Name.StartsWith(".")) {
                return;
            }

            if (CurveEditors.Values.Count == 0)
            {
                if (EnvPalette == null)
                {
                    EnvPalette = new EnvPalette();
                }

                EnvPalette.Save(resource_table, folder);
                return;
            }

            foreach (var editor in CurveEditors.Values)
            {
                editor.Save(resource_table, folder);
            }
        }
        public void SavePalette(RSTB resource_table)
        {
            string modRoot = UserSettings.GetModRomFSPath();
            string outPath = Path.Combine(
                modRoot,
                "Gyml",
                "Gfx",
                "EnvPaletteParam"
            );
            SavePalette(resource_table, outPath);


        }
        private void DrawIntSlider(string label, object obj, string property, int min, int max)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            float f = (float)prop.GetValue(obj);
            int v = (int)f;
            if (ImGui.SliderInt($"##{label}", ref v, min, max))
            {
                prop.SetValue(obj, v);
                Update();
            }
            ImGui.NextColumn();
        }

        private void DrawIntSliderU(string label, object obj, string property, int min, int max)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);

            uint f = (uint)prop.GetValue(obj);
            int v = (int)f;

            if (ImGui.SliderInt($"##{label}", ref v, min, max))
            {
                prop.SetValue(obj, (uint)v); 
                Update();
            }

            ImGui.NextColumn();
        }

        private void DrawFloatSlider(string label, object obj, string property, float min, float max)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
            ImGui.NextColumn();

            var prop = obj.GetType().GetProperty(property);
            var v = (float)prop.GetValue(obj);
            if (ImGui.SliderFloat($"##{label}", ref v, min, max))
            {
                prop.SetValue(obj, v);
                Update();
            }
            ImGui.NextColumn();
        }
    }
}