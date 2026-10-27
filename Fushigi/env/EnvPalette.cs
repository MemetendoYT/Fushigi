using Fushigi.agl;
using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.Byml.Writer;
using Fushigi.gl;
using Fushigi.Logger;
using Fushigi.rstb;
using Fushigi.util;
using ImGuiNET;
using Microsoft.Msagl.Drawing;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.env.EnvPalette;
using static Fushigi.gl.Bfres.GsysEnvironment;

namespace Fushigi.env
{
    [Serializable]
    public class EnvPalette : BymlObject
    {  
        [BymlIgnore]
        public string Name { get; set; }

        [BymlProperty("$parent")]
        public string parent { get; set; }

        public EnvBloom Bloom { get; set; }

        public EnvMapList EnvMap { get; set; }
        public EnvDepthOfField DOF { get; set; }

        public EnvGlobalIllumination GI { get; set; }
        public EnvColorList EnvColor { get; set; }
        public EnvEmission Emission { get; set; }
        public EnvShadow Shadow { get; set; }
        public EnvRim Rim { get; set; }

        public EnvSky Sky { get; set; }
        public EnvFogList Fog { get; set; }
        public EnvLightList CharLight { get; set; }
        public EnvLightList CloudLight { get; set; }
        public EnvLightList DvLight { get; set; }
        public EnvLightList FieldLight { get; set; }
        public EnvLightList ObjLight { get; set; }
        public EnvInfo Info { get; set; }
        public bool IsApplyBloom { get; set; }
        public bool IsApplyCharLight { get; set; }
        public bool IsApplyCloudLight { get; set; }
        public bool IsApplyDOF { get; set; }
        public bool IsApplyDvLight { get; set; }
        public bool IsApplyEmission { get; set; }
        public bool IsApplyEnvColor { get; set; }
        public bool IsApplyEnvMap { get; set; }
        public bool IsApplyFieldLight { get; set; }
        public bool IsApplyFog { get; set; }
        public bool IsApplyGI { get; set; }
        public bool IsApplyObjLight { get; set; }
        public bool IsApplyRim { get; set; }
        public bool IsApplyShadow { get; set; }
        public bool IsApplySky { get; set; }
        public bool IsApplyInfo { get; set; }

        public string ParentPath { get; set; }




        public EnvPalette()
        {

        }

        public void Normalize()
        {
            if (EnvColor != null)
            {
                if (EnvColor.RoughnessMul == 0f)
                    EnvColor.RoughnessMul = 1f;
            }
        }
        private void CleanupMissingBlocks(BymlHashTable table)
        {
            var props = this.GetType().GetProperties();

            foreach (var prop in props)
            {
                if (!prop.PropertyType.IsClass || prop.PropertyType == typeof(string))
                    continue;

                string key = prop.Name;

                if (!table.ContainsKey(key))
                {
                    prop.SetValue(this, null);
                }
            }
        }

        public new void Load(BymlHashTable table)
        {

            base.Load(table); 

            CleanupMissingBlocks(table);
        }

        public EnvPalette(string name)
        {
            Load(name);
        }

        public void Load(string name)
        {
            Name = name;

            string local_path = Path.Combine("Gyml", "Gfx", "EnvPaletteParam", $"{name}.game__gfx__EnvPaletteParam.bgyml");
            string file_path = FileUtil.FindContentPath(local_path);
            if (!File.Exists(file_path))
            {
                //Debug.Fail(null);
                Name = null; 
                return;
            }

            var byml = new Byml.Byml(new MemoryStream(File.ReadAllBytes(file_path)));
            this.Load((BymlHashTable)byml.Root);
            Normalize();

        }
        bool IsValidPalette(EnvPalette p)
        {
            return p != null &&
                   p.Fog != null &&
                   p.Fog.Main != null &&
                   p.Fog.MainWorld != null &&
                   p.Fog.Cloud != null &&
                   p.Fog.CloudWorld != null &&
                   p.CharLight != null &&
                   p.CloudLight != null &&
                   p.FieldLight != null &&
                   p.DvLight != null &&
                   p.ObjLight != null &&
                   p.Rim != null &&
                   p.Emission != null &&
                   p.Shadow != null &&
                   p.EnvColor != null &&
                   p.Sky != null;
        }

        public void Lerp(EnvPalette prevPalette, EnvPalette nextPalette, float ratio)
        {
            if (!IsValidPalette(prevPalette) || !IsValidPalette(nextPalette))
            {
                return;
            }

            void LerpFog(EnvFog fogDst, EnvFog fogA, EnvFog fogB)
            {

                fogDst.Start = MathUtil.Lerp(fogA.Start, fogB.Start, ratio);
                fogDst.End = MathUtil.Lerp(fogA.End, fogB.End, ratio);
                fogDst.Damp = MathUtil.Lerp(fogA.Damp, fogB.Damp, ratio);
                fogDst.Color = Color.Lerp(fogA.Color, fogB.Color, ratio);
            }

            void LerpRim(EnvRim a, EnvRim b)
            {
                if (a == null || b == null)
                    return;

                this.Rim.Color = Color.Lerp(a.Color, b.Color, ratio);
                this.Rim.Width = MathUtil.Lerp(a.Width, b.Width, ratio);
                this.Rim.Power = MathUtil.Lerp(a.Power, b.Power, ratio);
                this.Rim.IntensityFieldWall = MathUtil.Lerp(a.IntensityFieldWall, b.IntensityFieldWall, ratio);
                this.Rim.IntensityFieldBand = MathUtil.Lerp(a.IntensityFieldBand, b.IntensityFieldBand, ratio);
                this.Rim.IntensityFieldDeco = MathUtil.Lerp(a.IntensityFieldDeco, b.IntensityFieldDeco, ratio);
                this.Rim.IntensityObject = MathUtil.Lerp(a.IntensityObject, b.IntensityObject, ratio);
                this.Rim.IntensityPlayer = MathUtil.Lerp(a.IntensityPlayer, b.IntensityPlayer, ratio);
                this.Rim.IntensityEnemy = MathUtil.Lerp(a.IntensityEnemy, b.IntensityEnemy, ratio);
                this.Rim.IntensityDV = MathUtil.Lerp(a.IntensityDV, b.IntensityDV, ratio);
                this.Rim.IntensityCloud = MathUtil.Lerp(a.IntensityCloud, b.IntensityCloud, ratio);
            }

            void LerpHemiLight(EnvLightHemisphere dst, EnvLightHemisphere a, EnvLightHemisphere b)
            {
                if (a == null || b == null)
                    return;

                dst.Ground = Color.Lerp(a.Ground, b.Ground, ratio);
                dst.Sky = Color.Lerp(a.Sky, b.Sky, ratio);
                dst.Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, ratio);
            }

            void LerpLight(EnvLightDirectional dst, EnvLightDirectional a, EnvLightDirectional b)
            {
                if (a == null || b == null)
                    return;

                dst.Color = Color.Lerp(a.Color, b.Color, ratio);
                dst.Latitude = MathUtil.Lerp(a.Latitude, b.Latitude, ratio);
                dst.Longitude = MathUtil.Lerp(a.Longitude, b.Longitude, ratio);
                dst.Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, ratio);
            }


            void LerpSkyLut(EnvSkyLut dst, EnvSkyLut a, EnvSkyLut b)
            {
                if (a == null || b == null)
                    return;

                dst.ColorBegin = Color.Lerp(a.ColorBegin, b.ColorBegin, ratio);
                dst.ColorMiddle = Color.Lerp(a.ColorMiddle, b.ColorMiddle, ratio);
                dst.ColorEnd = Color.Lerp(a.ColorEnd, b.ColorEnd, ratio);
                dst.Intensity = MathUtil.Lerp(a.Intensity, b.Intensity, ratio);
                if (ratio == 0.5f)
                {
                    dst.Curve = b.Curve;
                    dst.UseMiddleColor = b.UseMiddleColor;
                }
            }
            void LerpSky(EnvSky a, EnvSky b)
            {
                if (a == null || b == null)
                    return;

                LerpSkyLut(this.Sky.LutTexLeft, a.LutTexLeft, b.LutTexLeft);
                LerpSkyLut(this.Sky.LutTexLeftTop, a.LutTexLeftTop, b.LutTexLeftTop);
                LerpSkyLut(this.Sky.LutTexTop, a.LutTexTop, b.LutTexTop);
                LerpSkyLut(this.Sky.LutTexRightTop, a.LutTexRightTop, b.LutTexRightTop);
            }

            void LerpLightList(EnvLightList dst, EnvLightList a, EnvLightList b)
            {
           
                if (a == null || b == null)
                    return;


                LerpHemiLight(dst.Hemi, a.Hemi, b.Hemi);
                LerpLight(dst.Main, a.Main, b.Main);
                LerpLight(dst.SubDiff0, a.SubDiff0, b.SubDiff0);
                LerpLight(dst.SubDiff1, a.SubDiff1, b.SubDiff1);
                LerpLight(dst.SubDiff2, a.SubDiff2, b.SubDiff2);
                LerpLight(dst.SubSpec0, a.SubSpec0, b.SubSpec0);
                LerpLight(dst.SubSpec1, a.SubSpec1, b.SubSpec1);
            }

            if (prevPalette == null || nextPalette == null)
                return;


            //try
            //{
                LerpLightList(this.CharLight, prevPalette.CharLight, nextPalette.CharLight);
                LerpLightList(this.CloudLight, prevPalette.CloudLight, nextPalette.CloudLight);
                LerpLightList(this.FieldLight, prevPalette.FieldLight, nextPalette.FieldLight);
                LerpLightList(this.DvLight, prevPalette.DvLight, nextPalette.DvLight);
                LerpLightList(this.ObjLight, prevPalette.ObjLight, nextPalette.ObjLight);

                LerpFog(this.Fog.Main, prevPalette.Fog.Main, nextPalette.Fog.Main);
                LerpFog(this.Fog.MainWorld, prevPalette.Fog.MainWorld, nextPalette.Fog.MainWorld);
                LerpFog(this.Fog.Cloud, prevPalette.Fog.Cloud, nextPalette.Fog.Cloud);
                LerpFog(this.Fog.CloudWorld, prevPalette.Fog.CloudWorld, nextPalette.Fog.CloudWorld);

                LerpRim(prevPalette.Rim, nextPalette.Rim);

                this.Emission.Color = Color.Lerp(prevPalette.Emission.Color, nextPalette.Emission.Color, ratio);
                this.Shadow.AOColor = Color.Lerp(prevPalette.Shadow.AOColor, nextPalette.Shadow.AOColor, ratio);
                this.Shadow.Longitude = MathUtil.Lerp(prevPalette.Shadow.Longitude, nextPalette.Shadow.Longitude, ratio);
                this.Shadow.Latitude = MathUtil.Lerp(prevPalette.Shadow.Latitude, nextPalette.Shadow.Latitude, ratio);
                this.EnvColor.Color0 = Color.Lerp(prevPalette.EnvColor.Color0, nextPalette.EnvColor.Color0, ratio);
                this.EnvColor.Color1 = Color.Lerp(prevPalette.EnvColor.Color1, nextPalette.EnvColor.Color1, ratio);
                this.EnvColor.Color2 = Color.Lerp(prevPalette.EnvColor.Color2, nextPalette.EnvColor.Color2, ratio);
                this.EnvColor.Color3 = Color.Lerp(prevPalette.EnvColor.Color3, nextPalette.EnvColor.Color3, ratio);
                this.EnvColor.Color4 = Color.Lerp(prevPalette.EnvColor.Color4, nextPalette.EnvColor.Color4, ratio);
                this.EnvColor.Color5 = Color.Lerp(prevPalette.EnvColor.Color5, nextPalette.EnvColor.Color5, ratio);
                this.EnvColor.Color6 = Color.Lerp(prevPalette.EnvColor.Color6, nextPalette.EnvColor.Color6, ratio);
                this.EnvColor.Color7 = Color.Lerp(prevPalette.EnvColor.Color7, nextPalette.EnvColor.Color7, ratio);

                LerpSky(prevPalette.Sky, nextPalette.Sky);
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("Palette transition failed: " + ex);
            //}

        }

        [Serializable]
        public class EnvBloom
        {
            public float Intensity { get; set; }
            public float MaskEnd { get; set; }
            public float MaskRatio { get; set; }
            public float Threshold { get; set; }
            public float MaskColorPower { get; set; }

            public BymlHashTable Serialize()
            {
                BymlHashTable bloom = new();

                if(Intensity != 0f)
                    bloom.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Intensity), "Intensity");

                if(MaskEnd != 0f)
                    bloom.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(MaskEnd), "MaskEnd");
                if(MaskRatio != 0f)
                    bloom.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(MaskRatio), "MaskRatio");

               if(Threshold != 0f)
                    bloom.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Threshold), "Threshold");

               if(MaskColorPower != 0f)
                    bloom.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(MaskColorPower), "MaskColorPower");
                return bloom;
            }
        }

        public class EnvMapList
        {
            public EnvLightMap Ground0 { get; set; }
            public EnvLightMap Ground1 { get; set; }
            public EnvLightMap Horizon { get; set; }
            public EnvLightMap Sky0 { get; set; }
            public EnvLightMap Sky1 { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                if (Ground0 != null)
                    table.AddNode(BymlNodeId.Hash, Ground0.Serialize(), "Ground0");

                if (Ground1 != null)
                    table.AddNode(BymlNodeId.Hash, Ground1.Serialize(), "Ground1");

                if (Horizon != null)
                    table.AddNode(BymlNodeId.Hash, Horizon.Serialize(), "Horizon");

                if (Sky0 != null)
                    table.AddNode(BymlNodeId.Hash, Sky0.Serialize(), "Sky0");

                if (Sky1 != null)
                    table.AddNode(BymlNodeId.Hash, Sky1.Serialize(), "Sky1");

                return table;
            }
        }

        public class EnvLightMap
        {
            public Color Color { get; set; }
            public float Intensity { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();


                if (!Color.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Color.Serialize(), "Color");

                if(Intensity != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Intensity), "Intensity");

                return table;
            }
        }

        public class EnvRim
        {
            public Color Color { get; set; }

            public float IntensityCloud { get; set; }
            public float IntensityDV { get; set; }
            public float IntensityEnemy { get; set; }
            public float IntensityFieldBand { get; set; }
            public float IntensityFieldDeco { get; set; }
            public float IntensityFieldWall { get; set; }
            public float IntensityObject { get; set; }
            public float IntensityPlayer { get; set; }

            public float Power { get; set; }
            public float Width { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();


                if (!Color.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Color.Serialize(), "Color");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityCloud), "IntensityCloud");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityDV), "IntensityDV");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityEnemy), "IntensityEnemy");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityFieldBand), "IntensityFieldBand");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityFieldDeco), "IntensityFieldDeco");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityFieldWall), "IntensityFieldWall");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityObject), "IntensityObject");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(IntensityPlayer), "IntensityPlayer");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Power), "Power");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Width), "Width");
                return table;
            }

        }

        public class EnvShadow
        {
            public Color AOColor { get; set; }
            public bool EnableDynamicDepthShadow { get; set; }
            public float Latitude { get; set; }
            public float Longitude { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();


                if (!AOColor.IsDefault())
                    table.AddNode(BymlNodeId.Hash, AOColor.Serialize(), "AOColor");
                table.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(EnableDynamicDepthShadow), "EnableDynamicDepthShadow");

                if(Latitude != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Latitude), "Latitude");

                if(Longitude != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Longitude), "Longitude");
                return table;
            }
        }


        [Serializable]
        public class EnvDepthOfField
        {
            public bool Enable { get; set; }
            public float End { get; set; }
            public float MipLevelMax { get; set; }
            public float Start { get; set; }

            public BymlHashTable Serialize()
            {
                BymlHashTable envDepthOfField = new();

                envDepthOfField.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(Enable), "Enable");
                envDepthOfField.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Start), "Start");
                envDepthOfField.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(End), "End");
                envDepthOfField.AddNode(BymlNodeId.Int, BymlUtil.CreateNode(MipLevelMax), "MipLevelMax");

                return envDepthOfField;
            }
        }

        public class EnvEmission
        {
            public Color Color { get; set; }
            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                if (!Color.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Color.Serialize(), "Color");

                return table;
            }

        }

        public class EnvColorList
        {
            public EnvColorList()
            {
                RoughnessMul = 1f;
            }

            public float RoughnessMul { get; set; }
            public Color Color0 { get; set; }
            public Color Color1 { get; set; }
            public Color Color2 { get; set; }
            public Color Color3 { get; set; }
            public Color Color4 { get; set; }
            public Color Color5 { get; set; }
            public Color Color6 { get; set; }
            public Color Color7 { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(RoughnessMul), "RoughnessMul");
                table.AddNode(BymlNodeId.Hash, Color0.Serialize(), "Color0");
                table.AddNode(BymlNodeId.Hash, Color1.Serialize(), "Color1");
                table.AddNode(BymlNodeId.Hash, Color2.Serialize(), "Color2");
                table.AddNode(BymlNodeId.Hash, Color3.Serialize(), "Color3");
                table.AddNode(BymlNodeId.Hash, Color4.Serialize(), "Color4");
                table.AddNode(BymlNodeId.Hash, Color5.Serialize(), "Color5");
                table.AddNode(BymlNodeId.Hash, Color6.Serialize(), "Color6");
                table.AddNode(BymlNodeId.Hash, Color7.Serialize(), "Color7");
                return table;
            }
        }

        public class EnvGlobalIllumination
        {
            public uint BlurLevel { get; set; }
            public float Intensity { get; set; } 
            public float Ratio { get; set; }
            public float RimIntensity { get; set; } 
            public float RimPow { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                table.AddNode(BymlNodeId.Int, BymlUtil.CreateNode(BlurLevel), "BlurLevel");
                if(Intensity != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Intensity), "Intensity");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Ratio), "Ratio");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(RimIntensity), "RimIntensity");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(RimPow), "RimPow");
                return table;
            }
        }
        public class EnvFogList
        {
            public EnvFog Cloud { get; set; } = new EnvFog();
            public EnvFog CloudWorld { get; set; } = new EnvFog();
            public EnvFog Main { get; set; } = new EnvFog();
            public EnvFog MainWorld { get; set; } = new EnvFog();
            public EnvFogOption Option { get; set; } = new EnvFogOption();

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();
                if (Cloud != null && !Cloud.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Cloud.Serialize(), "Cloud");
                if (CloudWorld != null && !CloudWorld.IsDefault())
                    table.AddNode(BymlNodeId.Hash, CloudWorld.Serialize(), "CloudWorld");
                if (Main != null && !Main.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Main.Serialize(), "Main");
                if (MainWorld != null && !MainWorld.IsDefault())
                    table.AddNode(BymlNodeId.Hash, MainWorld.Serialize(), "MainWorld");
                if (Option != null)
                    table.AddNode(BymlNodeId.Hash, Option.Serialize(), "Option");
                return table;
            }
        }

        public class EnvFog
        {
            public Color Color { get; set; }
            public float Damp { get; set; }
            public float End { get; set; }
            public float Start { get; set; }

            public bool IsDefault()
            {
                return Damp == 0f && Start == 0f && End == 0f;
            }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();


                if (!Color.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Color.Serialize(), "Color");

                if(Damp != 0f)
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Damp), "Damp");
                if(End != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(End), "End");
                if(Start != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Start), "Start");
                return table;
            }
        }

        public class EnvFogOption
        {
            public float LightenRatio { get; set; }
            public NoiseMoveSpeed NoiseMoveSpeed { get; set; } = new NoiseMoveSpeed();
            public float NoiseRatio { get; set; }
            public float SkyColorRatio { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(LightenRatio), "LightenRatio");
                table.AddNode(BymlNodeId.Hash, NoiseMoveSpeed.Serialize(), "NoiseMoveSpeed");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(NoiseRatio), "NoiseRatio");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(SkyColorRatio), "SkyColorRatio");

                return table;
            }
        }
        public class EnvInfo
        {
            public object LocationType { get; set; }

            public string WeatherType { get; set; }

            public string WonderType { get; set; }


            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();
                if(LocationType != null)
                    table.AddNode(BymlNodeId.String, BymlUtil.CreateNode(LocationType), "LocationType");
                if(WeatherType != null)
                    table.AddNode(BymlNodeId.String, BymlUtil.CreateNode(WeatherType), "WeatherType");
                if(WonderType != null)
                    table.AddNode(BymlNodeId.String, BymlUtil.CreateNode(WonderType), "WonderType");
                return table;
            }

        }
            public class EnvLightList
        {
            public EnvLightHemisphere Hemi { get; set; }
            public EnvLightDirectional Main { get; set; }
            public EnvLightDirectional PlayerSubDiff0 { get; set; }
            public EnvLightDirectional StageSubDiff0 { get; set; }
            public EnvLightDirectional StageSubDiff1 { get; set; }
            public EnvLightDirectional StageSubSpec0 { get; set; }
            public EnvLightDirectional StageSubSpec1 { get; set; }
            public EnvLightDirectional SubDiff0 { get; set; }
            public EnvLightDirectional SubDiff1 { get; set; }
            public EnvLightDirectional SubDiff2 { get; set; }
            public EnvLightDirectional SubSpec0 { get; set; }
            public EnvLightDirectional SubSpec1 { get; set; }
            public EnvLightDirectional SubSpec2 { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                if (Hemi != null)
                    table.AddNode(BymlNodeId.Hash, Hemi.Serialize(), "Hemi");

                if (Main != null)
                    table.AddNode(BymlNodeId.Hash, Main.Serialize(), "Main");

                if (PlayerSubDiff0 != null)
                    table.AddNode(BymlNodeId.Hash, PlayerSubDiff0.Serialize(), "PlayerSubDiff0");

                if (StageSubDiff0 != null)
                    table.AddNode(BymlNodeId.Hash, StageSubDiff0.Serialize(), "StageSubDiff0");

                if (StageSubDiff1 != null)
                    table.AddNode(BymlNodeId.Hash, StageSubDiff1.Serialize(), "StageSubDiff1");

                if (StageSubSpec0 != null)
                    table.AddNode(BymlNodeId.Hash, StageSubSpec0.Serialize(), "StageSubSpec0");

                if (StageSubSpec1 != null)
                    table.AddNode(BymlNodeId.Hash, StageSubSpec1.Serialize(), "StageSubSpec1");

                if (SubDiff0 != null)
                    table.AddNode(BymlNodeId.Hash, SubDiff0.Serialize(), "SubDiff0");

                if (SubDiff1 != null)
                    table.AddNode(BymlNodeId.Hash, SubDiff1.Serialize(), "SubDiff1");

                if (SubDiff2 != null)
                    table.AddNode(BymlNodeId.Hash, SubDiff2.Serialize(), "SubDiff2");

                if (SubSpec0 != null)
                    table.AddNode(BymlNodeId.Hash, SubSpec0.Serialize(), "SubSpec0");

                if (SubSpec1 != null)
                    table.AddNode(BymlNodeId.Hash, SubSpec1.Serialize(), "SubSpec1");

                if (SubSpec2 != null)
                    table.AddNode(BymlNodeId.Hash, SubSpec2.Serialize(), "SubSpec2");

                return table;
            }
        }


        public class EnvLightDirectional
        {
            public Color Color { get; set; }
            public float Intensity { get; set; }
            public float Latitude { get; set; } //degrees
            public float Longitude { get; set; } //degrees

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                if(!Color.IsDefault())
                    table.AddNode(BymlNodeId.Hash, Color.Serialize(), "Color");

                if(Intensity != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Intensity), "Intensity");

                if(Latitude != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Latitude), "Latitude");

                if(Longitude != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Longitude), "Longitude");

                return table;
            }
        }

        public class EnvLightHemisphere
        {
            public Color Ground { get; set; }
            public float Intensity { get; set; }
            public Color Sky { get; set; }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                table.AddNode(BymlNodeId.Hash, Ground.Serialize(), "Ground");
                if(Intensity != 0f)
                    table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Intensity), "Intensity");
                table.AddNode(BymlNodeId.Hash, Sky.Serialize(), "Sky");

                return table;
            }
        }

        public class EnvSky
        {
            public float HorizontalOffset { get; set; } //Updates skybox mat params
            public float RotDegLeftTop { get; set; } //Updates skybox mat params
            public float RotDegRightTop { get; set; } //Updates skybox mat params

            public EnvSkyLut LutTexLeft { get; set; }
            public EnvSkyLut LutTexLeftTop { get; set; }
            public EnvSkyLut LutTexRightTop { get; set; }
            public EnvSkyLut LutTexTop { get; set; }

            public BymlHashTable Serialize()
            {
                BymlHashTable sky = new();

       
                sky.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(HorizontalOffset), "HorizontalOffset");
                sky.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(RotDegLeftTop), "RotDegLeftTop");
                sky.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(RotDegRightTop), "RotDegRightTop");

                if (LutTexLeft != null)
                    sky.AddNode(BymlNodeId.Hash, LutTexLeft.Serialize(), "LutTexLeft");
                if (LutTexLeftTop != null)
                    sky.AddNode(BymlNodeId.Hash, LutTexLeftTop.Serialize(), "LutTexLeftTop");
                if(LutTexRightTop != null)
                    sky.AddNode(BymlNodeId.Hash, LutTexRightTop.Serialize(), "LutTexRightTop");
                if (LutTexTop != null)
                    sky.AddNode(BymlNodeId.Hash, LutTexTop.Serialize(), "LutTexTop");

                return sky;
            }
        }

        public class EnvSkyLut
        {
            public Color ColorBegin { get; set; }
            public Color ColorEnd { get; set; }
            public Color ColorMiddle { get; set; }
            public float Intensity { get; set; }
            public bool UseMiddleColor { get; set; }
            public EnvSkyLutCurve Curve { get; set; }

            public BymlHashTable Serialize()
            {
                BymlHashTable lut = new();

                lut.AddNode(BymlNodeId.Hash, ColorBegin.Serialize(), "ColorBegin");
                lut.AddNode(BymlNodeId.Hash, ColorEnd.Serialize(), "ColorEnd");
                lut.AddNode(BymlNodeId.Hash, ColorMiddle.Serialize(), "ColorMiddle");

                if(Intensity != 0f)
                    lut.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Intensity), "Intensity");
                lut.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(UseMiddleColor), "UseMiddleColor");

                if(Curve != null)
                    lut.AddNode(BymlNodeId.Hash, Curve.Serialize(), "Curve");

                return lut;
            }

            public static byte[] Lerp(EnvSkyLut previous, EnvSkyLut next, float t)
            {
                byte[] data_bytes = new byte[64 * 4];

                float[] rgba32_1 = previous.ComputeRgba32(64);
                float[] rgba32_2 = next.ComputeRgba32(64);

                for (int i = 0; i < rgba32_1.Length; i++)
                {
                    float v = MathUtil.Lerp(rgba32_1[i], rgba32_2[i], t);
                    data_bytes[i] = (byte)(v * 255);
                }
                return data_bytes;
            }

            public byte[] ComputeRgba8(int width = 64)
            {
                byte[] data_bytes = new byte[width * 4];

                float[] rgba32 = ComputeRgba32(width);
                for (int i = 0; i < rgba32.Length; i++)
                    data_bytes[i] = (byte)(rgba32[i] * 255);

                return data_bytes;
            }

            public float[] ComputeRgba32(int width = 64)
            {
                if (Curve == null) // Always means constant?
                {
                    var color = ColorEnd.ToVector4();
                    var tmp = new float[width * 4];
                    for (int i = 0; i < width * 4; i += 4)
                    {
                        tmp[i + 0] = Math.Clamp(color.X, 0, 1f);
                        tmp[i + 1] = Math.Clamp(color.Y, 0, 1f);
                        tmp[i + 2] = Math.Clamp(color.Z, 0, 1f);
                        tmp[i + 3] = Math.Clamp(color.W, 0, 1f);
                    }

                    return tmp;
                }

                var type = Curve.GetCurveType();
                var data = Curve.Data.ToArray();

                Vector4[] colors = UseMiddleColor ?
                    new Vector4[] { ColorBegin.ToVector4(), ColorMiddle.ToVector4(), ColorEnd.ToVector4() } :
                    new Vector4[] { ColorBegin.ToVector4(), ColorEnd.ToVector4(), ColorEnd.ToVector4() };

                float[] buffer = new float[width * 4];

                Vector4 LerpBetweenColors(Vector4 a, Vector4 b, Vector4 c, float t)
                {
                    if (t < 0.5f) // Bottom to middle
                        return Vector4.Lerp(a, b, t / 0.5f);
                    else // Middle to top
                        return Vector4.Lerp(b, c, (t - 0.5f) / 0.5f);
                }

                for (int i = 0; i < width * 4; i += 4)
                {
                    int index = i / 4;
                    float time = index / (width - 1f);
                    float x = MathF.Min(AglCurve.Interpolate(data, type, time), Curve.MaxX);

                    //Use "X" to lerp between the colors to get
                    Vector4 color = LerpBetweenColors(colors[0], colors[1], colors[2], x);

                    buffer[i + 0] = Math.Clamp(color.X, 0, 1f);
                    buffer[i + 1] = Math.Clamp(color.Y, 0, 1f);
                    buffer[i + 2] = Math.Clamp(color.Z, 0, 1f);
                    buffer[i + 3] = Math.Clamp(color.W, 0, 1f);
                }
                return buffer;
            }
        }

        public class EnvSkyLutCurve
        {
            public List<float> Data { get; set; }
            public float MaxX { get; set; }
            public string Type { get; set; } 

            public BymlHashTable Serialize()
            {
                BymlHashTable curve = new();

                curve.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(MaxX), "MaxX");
                curve.AddNode(BymlNodeId.String, BymlUtil.CreateNode(Type), "Type");
                BymlArrayNode dataArray = new();

                if (Data is not null)
                {
                    foreach (float f in Data)
                        dataArray.AddNodeToArray(BymlUtil.CreateNode<float>(f));
                }

                curve.AddNode(BymlNodeId.Array, dataArray, "Data");

                return curve;
            }

            public AglCurve.CurveType GetCurveType()
            {
                if (Enum.TryParse(Type, out AglCurve.CurveType curveType))
                    return curveType;
                else
                    throw new ArgumentException("Invalid CurveType");
            }
        }

        public struct Color
        {
            public float R { get; set; }
            public float G { get; set; }
            public float B { get; set; }
            public float A { get; set; }

            public Vector4 ToVector4() => new Vector4(R, G, B, A);

            public BymlHashTable Serialize()
            {
                BymlHashTable color = new();

                color.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(R), "R");
                color.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(G), "G");
                color.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(B), "B");
                color.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(A), "A");

                return color;
            }

            public bool IsDefault()
            {
                return R == 0f && G == 0f && B == 0f && A == 0f;
            }

            public override string ToString()
            {
                return $"R {R} G {G} B {B} A {A}";
            }

            public Color() { }

            public Color(Vector4 v)
            {
                R = v.X;
                G = v.Y;
                B = v.Z;
                A = v.W;
            }

            public static Color Lerp(Color a, Color b, float t)
            {
                return new Color(Vector4.Lerp(a.ToVector4(), b.ToVector4(), t));
            }


        }

        public class NoiseMoveSpeed
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public NoiseMoveSpeed() { }

            public NoiseMoveSpeed(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public BymlHashTable Serialize()
            {
                var table = new BymlHashTable();

                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(X), "X");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Y), "Y");
                table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode(Z), "Z");

                return table;
            }
        }

        public void Save(RSTB resource_table, string folder)
        {

            // Build BYML root
            BymlHashTable root = new BymlHashTable();

            if(parent != null)
                root.AddNode(BymlNodeId.String, BymlUtil.CreateNode(parent), "$parent");

            if (Bloom != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyBloom), "IsApplyBloom");
                root.AddNode(BymlNodeId.Hash, Bloom.Serialize(), "Bloom");
            }

            if (CharLight != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyCharLight), "IsApplyCharLight");
                root.AddNode(BymlNodeId.Hash, CharLight.Serialize(), "CharLight");
            }

            if (CloudLight != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyCloudLight), "IsApplyCloudLight");
                root.AddNode(BymlNodeId.Hash, CloudLight.Serialize(), "CloudLight");
            }

            if (DOF != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyDOF), "IsApplyDOF");
                root.AddNode(BymlNodeId.Hash, DOF.Serialize(), "DOF");
            }

            if (DvLight != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyDvLight), "IsApplyDvLight");
                root.AddNode(BymlNodeId.Hash, DvLight.Serialize(), "DvLight");
            }

            if (Emission != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyEmission), "IsApplyEmission");
                root.AddNode(BymlNodeId.Hash, Emission.Serialize(), "Emission");
            }

            if (EnvColor != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyEnvColor), "IsApplyEnvColor");
                root.AddNode(BymlNodeId.Hash, EnvColor.Serialize(), "EnvColor");
            }

            if (EnvMap != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyEnvMap), "IsApplyEnvMap");
                root.AddNode(BymlNodeId.Hash, EnvMap.Serialize(), "EnvMap");
            }

            if (FieldLight != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyFieldLight), "IsApplyFieldLight");
                root.AddNode(BymlNodeId.Hash, FieldLight.Serialize(), "FieldLight");
            }

            if (Fog != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyFog), "IsApplyFog");
                root.AddNode(BymlNodeId.Hash, Fog.Serialize(), "Fog");
            }

            if (GI != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyGI), "IsApplyGI");
                root.AddNode(BymlNodeId.Hash, GI.Serialize(), "GI");
            }

            if (ObjLight != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyObjLight), "IsApplyObjLight");
                root.AddNode(BymlNodeId.Hash, ObjLight.Serialize(), "ObjLight");
            }

            if (Rim != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyRim), "IsApplyRim");
                root.AddNode(BymlNodeId.Hash, Rim.Serialize(), "Rim");
            }

            if (Shadow != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyShadow), "IsApplyShadow");
                root.AddNode(BymlNodeId.Hash, Shadow.Serialize(), "Shadow");
            }

            if (Sky != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplySky), "IsApplySky");
                root.AddNode(BymlNodeId.Hash, Sky.Serialize(), "Sky");
            }

            if (Info != null)
            {
                if (parent == null)
                    root.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode(IsApplyInfo), "IsApplyInfo");
                root.AddNode(BymlNodeId.Hash, Info.Serialize(), "Info");
            }

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;



            // Ensure directory exists
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Write file
            string levelPath = Path.Combine(folder, $"{Name}.game__gfx__EnvPaletteParam.bgyml");
            File.WriteAllBytes(levelPath, mem.ToArray());
            resource_table.SetResource($"Gyml/Gfx/EnvPaletteParam/{Name}.game__gfx__EnvPaletteParam.bgyml", decomp_size);
        }
    }

    }