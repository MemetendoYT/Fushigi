using Fushigi.Byml;
using Fushigi.env;
using Fushigi.rstb;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using Microsoft.Msagl.Core.ProjectionSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Fushigi.course.CourseComment;

namespace Fushigi.course
{
    public class CourseArea
    {

        public CourseArea(string areaName, bool overrideVanilla)
        {
            mAreaName = areaName;
            if (areaName == "BlankStage")
                Load(true, false, false);
            else
                Load(false, false, overrideVanilla);
        }


        public void Load(bool loadTemplate, bool disableBGUnit, bool overrideVanilla)
        {
            string areaParamPath = "";

            if (overrideVanilla)
            {
                loadTemplate = true;

                string phiveDir = Path.Combine(
                           UserSettings.GetModRomFSPath(),
                           "Phive",
                           "StaticCompoundBody"
                       );

                string normalDir = Path.Combine(
                           UserSettings.GetRomFSPath(),
                           "Phive",
                           "StaticCompoundBody"
                       );

                if (!Directory.Exists(phiveDir))
                    Directory.CreateDirectory(phiveDir);


                string phiveFileDir = Path.Combine(
                    phiveDir,
                    $"{mAreaName}.Nin_NX_NVN.bphsc.zs"
                );

                string phiveNormalDir = Path.Combine(
                    normalDir,
                    $"{mAreaName}.Nin_NX_NVN.bphsc.zs"
                );
                bool phiveFound = File.Exists(phiveFileDir) || File.Exists(phiveNormalDir);

                if (!phiveFound)
                {
                    File.Copy(
                        Path.Combine(AppContext.BaseDirectory, "res", "BlankStaticCompoundBody.bphsc.zs"),
                        phiveFileDir,
                        overwrite: true
                    );
                }
            }

            if (!loadTemplate)
            {
                areaParamPath = FileUtil.FindContentPath(
                Path.Combine("Stage", "AreaParam", $"{mAreaName}.game__stage__AreaParam.bgyml")
                );
            }
            else
            {
                areaParamPath = "res/template.game__stage__AreaParam.bgyml";
            }

            if (!File.Exists(areaParamPath))
            {
                areaParamPath = "res/template.game__stage__AreaParam.bgyml";
                loadTemplate = true;
            }

            mAreaParams = new AreaParam(new Byml.Byml(new MemoryStream(File.ReadAllBytes(areaParamPath))));

            //Load env settings
            try
            {
                if (mAreaParams.EnvPaletteSetting != null &&
                    mAreaParams.EnvPaletteSetting.InitPaletteBaseName != null)
                {

                    mInitEnvPalette = new EnvPalette(
                        mAreaParams.EnvPaletteSetting.InitPaletteBaseName
                    );
                }
                else
                {
                    mInitEnvPalette = new EnvPalette();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            string levelPath = "";
            if (!loadTemplate)
            {
                levelPath = FileUtil.FindContentPath(
                    Path.Combine("BancMapUnit", $"{mAreaName}.bcett.byml.zs")
                    );
            }
            else
            {
                levelPath = "res/template.bcett.byml.zs";
            }
            byte[] levelBytes = FileUtil.DecompressFile(levelPath);
            var byml = new Byml.Byml(new MemoryStream(levelBytes));

            BymlHashTable? root = byml.Root as BymlHashTable;

            mRootHash = BymlUtil.GetNodeData<uint>(root["RootAreaHash"]);
            mStageParams = BymlUtil.GetNodeData<string>(root["StageParam"]);

            if (root.ContainsKey("Actors"))
            {
                BymlArrayNode actorsArray = (BymlArrayNode)root["Actors"];
                mActorHolder = new CourseActorHolder(actorsArray);
            }
            else
            {
                mActorHolder = new();
            }

            if (root.ContainsKey("Comments"))
            {
                BymlArrayNode commentsArray = (BymlArrayNode)root["Comments"];
                mCommentHolder = new CourseCommentHolder(commentsArray);
            }
            else
            {
                mCommentHolder = new();
            }

            if (root.ContainsKey("Rails"))
            {
                BymlArrayNode railsArray = (BymlArrayNode)root["Rails"];
                mRailHolder = new(railsArray);
            }
            else
            {
                mRailHolder = new();
            }

            if (root.ContainsKey("ActorToRailLinks"))
            {
                BymlArrayNode? actorLinksArray = root["ActorToRailLinks"] as BymlArrayNode;
                mRailLinksHolder = new CourseActorToRailLinksHolder(actorLinksArray, mActorHolder, mRailHolder);
            }
            else
            {
                mRailLinksHolder = new();
            }

            if (root.ContainsKey("Links"))
            {
                BymlArrayNode? linksArray = root["Links"] as BymlArrayNode;
                mLinkHolder = new(linksArray);
            }
            else
            {
                mLinkHolder = new();
            }

            if (root.ContainsKey("SimultaneousGroups"))
            {
                BymlArrayNode? groupsArray = root["SimultaneousGroups"] as BymlArrayNode;
                mGroupsHolder = new CourseGroupHolder(groupsArray);
            }
            else
            {
                mGroupsHolder = new();
            }

            if (root.ContainsKey("BgUnits") && !disableBGUnit)
            {
                BymlArrayNode? unitsArray = root["BgUnits"] as BymlArrayNode;
                mUnitHolder = new CourseUnitHolder(unitsArray);
            }
            else if (!disableBGUnit)
            {
                mUnitHolder = new();
            }
        }

        public BymlArrayNode LoadPrefab(string prefabName, byte[] levelBytes, string objType)
        {
            var byml = new Byml.Byml(new MemoryStream(levelBytes));

            BymlHashTable? root = byml.Root as BymlHashTable;
            BymlArrayNode objArray = null;
            if (root.ContainsKey(objType))
                objArray = (BymlArrayNode)root[objType];
 
            return objArray;
        }

        public CourseActor? GetActorByHash(ulong hash)
        {
            return mActorHolder.mActors.FirstOrDefault(a => a.mHash == hash);
        }

        public void SaveStageParam(RSTB resource_table, string dir)
        {
            if (Course.IsOneAreaCourse)
                return;

            string fileName = mAreaName;
            var stageParamFilePath = Path.Combine(dir, "Stage", "StageParam", $"{fileName}.game__stage__StageParam.bgyml");
            bool noFileFound = !File.Exists(stageParamFilePath);

            if (noFileFound)
            {
                string phiveDir = Path.Combine(dir, "Phive", "StaticCompoundBody");

                string normalDir = Path.Combine(UserSettings.GetRomFSPath(), "Phive", "StaticCompoundBody");

                if (!Directory.Exists(phiveDir))
                    Directory.CreateDirectory(phiveDir);

                string phiveFileDir = Path.Combine(
                    phiveDir,
                    $"{fileName}.Nin_NX_NVN.bphsc.zs"
                );

                string phiveNormalDir = Path.Combine(
                    normalDir,
                    $"{fileName}.Nin_NX_NVN.bphsc.zs"
                );

                bool phiveFound = File.Exists(phiveFileDir) || File.Exists(phiveNormalDir);

                if (!phiveFound)
                {
                    File.Copy(
                        Path.Combine(AppContext.BaseDirectory, "res", "BlankStaticCompoundBody.bphsc.zs"),
                        phiveFileDir,
                        overwrite: true
                    );
                }

                var templateParamFilePath = "res/template.game__stage__StageParam.bgyml";

                Byml.Byml stageParam = new Byml.Byml(
                    new MemoryStream(File.ReadAllBytes(templateParamFilePath))
                );

                BymlHashTable stageParamRoot = new();

                BymlHashTable components = new();

                string AreaParamPath =
                    $"Work/Stage/AreaParam/{fileName}.game__stage__AreaParam.gyml";
                string Mumap =
                    $"Work/MapUnit/Map/{fileName}.mumap";
                string StaticCompoundBody =
                    $"Work/Phive/StaticCompoundBody/{fileName}.phive__StaticCompoundBodySourceParam.gyml";

                components.AddNode(BymlNodeId.String, BymlUtil.CreateNode(AreaParamPath), "AreaParam");
                components.AddNode(BymlNodeId.String, BymlUtil.CreateNode(Mumap), "Mumap");
                components.AddNode(BymlNodeId.String, BymlUtil.CreateNode(StaticCompoundBody), "StaticCompoundBodySourceParam");


                stageParamRoot.AddNode(BymlNodeId.Hash, components, "Components");
                //stageParamRoot.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(Course.Catergory), "Catergory");


                stageParam.Root = stageParamRoot;

                string outPath = stageParamFilePath;

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                using (var ms = new MemoryStream())
                {
                    var decomp_size = (uint)ms.Length;
                    stageParam.Save(ms);
                    File.WriteAllBytes(outPath, ms.ToArray());
                    resource_table.SetResource($"Stage/StageParam/{mAreaName}.game__stage__StageParam.bgyml", decomp_size);
                }
            }
        }
        public void SaveActorsToPrefab(List<CourseActor> copiedActors, List<CourseActor> actors, string prefabName, List<CourseRail> copiedRails, List<CourseRail> rails)
        {
            string prefabFolder = Path.Combine(UserSettings.SettingsDir, "prefabs");

            if (!Directory.Exists(prefabFolder))
                Directory.CreateDirectory(prefabFolder);

            BymlHashTable root = new();
            root.AddNode(BymlNodeId.Array, mActorHolder.SerializePrefab(copiedActors, mLinkHolder), "Actors");
            root.AddNode(BymlNodeId.Array, mLinkHolder.SerializePrefab(actors), "Links");
            root.AddNode(BymlNodeId.Array, mRailHolder.SerializePrefab(copiedRails), "Rails");
            root.AddNode(BymlNodeId.Array, mRailLinksHolder.SerializePrefab(actors, rails), "ActorToRailLinks");

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;

            string prefabPath = Path.Combine(prefabFolder, $"{prefabName}.bcett.byml.zs");
            File.WriteAllBytes(prefabPath, FileUtil.CompressData(mem.ToArray()));

        }

        public void Save(RSTB resource_table, string folder, bool saveTemplate)
        {
            if (!saveTemplate)
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }

            BymlHashTable root = new();
            root.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mRootHash), "RootAreaHash");
            root.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mStageParams), "StageParam");
            root.AddNode(BymlNodeId.Array, mRailLinksHolder.SerializeToArray(), "ActorToRailLinks");
            root.AddNode(BymlNodeId.Array, mActorHolder.SerializeToArray(mLinkHolder), "Actors");

            if (mCommentHolder.mComments.Count != 0)
                root.AddNode(BymlNodeId.Array, mCommentHolder.SerializeToArray(), "Comments");

            if (mUnitHolder != null)
                root.AddNode(BymlNodeId.Array, mUnitHolder.SerializeToArray(), "BgUnits");

            root.AddNode(BymlNodeId.Array, mLinkHolder.SerializeToArray(), "Links");
            root.AddNode(BymlNodeId.Array, mRailHolder.SerializeToArray(), "Rails");
            root.AddNode(BymlNodeId.Array, mGroupsHolder.SerializeToArray(), "SimultaneousGroups");

            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);

            var decomp_size = (uint)mem.Length;


            //Compress and save the course area           
            string levelPath = Path.Combine(folder, $"{mAreaName}.bcett.byml.zs");
           
            if (saveTemplate)
            {
                string paramPath = "res/template.game__stage__AreaParam.bgyml";
                levelPath = "res/template.bcett.byml.zs";
                File.WriteAllBytes(levelPath, FileUtil.CompressData(mem.ToArray()));
                Load(true, true, false);

            }
            else
            {

                levelPath = Path.Combine(folder, $"{mAreaName}.bcett.byml.zs");
                File.WriteAllBytes(levelPath, FileUtil.CompressData(mem.ToArray()));

                //Update resource table
                // filePath is a key not an actual path so we cannot use Path.Combine
                resource_table.SetResource($"BancMapUnit/{mAreaName}.bcett.byml", decomp_size);
            }
        }


        public string GetName()
        {
            return mAreaName;
        }

        public IReadOnlyList<CourseActor> GetActors()
        {
            return mActorHolder.mActors;
        }

        public IReadOnlyList<CourseRail> GetRails()
        {
            return mRailHolder.mRails;
        }

        public IReadOnlyList<CourseComment> GetComments()
        {
            return mCommentHolder.mComments;
        }

        public IReadOnlyList<CourseActor> GetSortedActors()
        {
            if (!mActorHolder.mActors.TrueForAll(mActorHolder.mSortedActors.Contains) || !mActorHolder.mSortedActors.TrueForAll(mActorHolder.mActors.Contains))
            {
                mActorHolder.mSortedActors = new List<CourseActor>(mActorHolder.mActors);
                mActorHolder.mSortedActors.Sort((x, y) => x.mTranslation.Z.CompareTo(y.mTranslation.Z));
            }
            return mActorHolder.mSortedActors;
        }

        public string mAreaName;
        public uint mRootHash;
        string mStageParams;
        public AreaParam mAreaParams;
        public CourseActorHolder mActorHolder;
        public CourseCommentHolder mCommentHolder;
        public CourseRailHolder mRailHolder;
        public CourseActorToRailLinksHolder mRailLinksHolder;
        public CourseLinkHolder mLinkHolder;
        public CourseGroupHolder mGroupsHolder;
        public CourseUnitHolder mUnitHolder;
        public EnvPalette mInitEnvPalette;
        public bool hasPickCompleted;
        internal bool hasStartedPick;
    }
}
