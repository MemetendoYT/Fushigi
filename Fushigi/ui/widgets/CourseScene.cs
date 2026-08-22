using Fasterflect;
using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.course;
using Fushigi.env;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.param;
using Fushigi.rstb;
using Fushigi.SARC;
using Fushigi.ui.helpers;
using Fushigi.ui.modal;
using Fushigi.ui.SceneObjects;
using Fushigi.ui.SceneObjects.bgunit;
using Fushigi.ui.undo;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using static DiscordRPC.User;
using static Fushigi.course.CourseComment;
using NumVec = System.Numerics.Vector3;


namespace Fushigi.ui.widgets
{
    class CourseScene
    {
        private CourseScene(Course course, GLTaskScheduler glScheduler, IPopupModalHost popupModalHost)
        {
            this.course = course;
            this.mPopupModalHost = popupModalHost;
            selectedArea = course.GetArea(0);
            undoWindow = new UndoWindow();
            envPaletteWindow = new EnvPaletteWindow();
            activeViewport = null!;
            LevelViewport._courseScene = this;
            
            SkinTable ??= BymlSerialize.Deserialize<DefaultBgUnitSkinConfigTable>(
            RomFS.GetOrLoadBootUpPack().OpenFile(
            "System/CombinationDataTableData/DefaultBgUnitSkinConfigTable.pp__CombinationDataTableData.bgyml"));
            UpdateDRPC();
        }


        #region Variables
        readonly Dictionary<CourseArea, LevelViewport> viewports = [];
        readonly Dictionary<CourseArea, object?> lastSavedAction = new();
        readonly Dictionary<CourseArea, CourseAreaScene> areaScenes = [];
        Dictionary<CourseArea, LevelViewport>? lastCreatedViewports;
        public LevelViewport activeViewport;
        readonly UndoWindow undoWindow;
        readonly EnvPaletteWindow envPaletteWindow;
        NumVec camSave;
        public static bool saveStatus = true;
        private bool areaSwitched;
        private LevelViewport pendingViewport;
        private string globalLinkType;
        private CourseActor globalSource;
        private CourseActor pendingSource = null;
        private bool runGlobalPicker;
        private bool runGlobal;
        private CourseAreaEditContext pendingEditContext;
        public static bool bypassSelection;
        private string startingArea;
        (object? courseObj, FullPropertyCapture capture)
           propertyCapture = (null,
            FullPropertyCapture.Empty);

        public Course course;
        readonly IPopupModalHost mPopupModalHost;
        public CourseArea selectedArea;

        public static readonly Dictionary<string, bool> mLayersVisibility = [];
        bool mHasFilledLayers = false;
        bool mAllLayersVisible = true;

        bool showAreaSettings = false;
        bool showTalkingFlower = false;
        bool showCourseSettings = false;
        bool showWorldMapSettings = false;
        bool showPaletteWindow = false;
        public bool hasOpened;
        public static bool showGlobalLinkWindow = false;
        private int linkNumb;
        CourseLink gLink;
        public static uint oldAreaParamSize;
        public static uint areaParamSize;
        public static uint oldCourseInfoSize;
        public static uint courseInfoSize;
        public Vector2 sizeToUse = new Vector2(0, 0);
        public static Dictionary<string, List<ulong>> mCopiedLinks = [];
        public string previousWord = "";
        public static bool refreshTranslation = false;
        private List<string> filteredActors = new();
        private List<string> filteredLayers = new();
        private List<string> translatedActors = new();
        private CourseArea areaToFocus = null;
        public static bool leftClickStartedInsideViewport = false;
        public static bool insideViewport = false;
        public static CourseArea currentArea;
        public static DefaultBgUnitSkinConfigTable SkinTable;
        // this is a very bad fix bc im waiting
        // to work on jupahe's editor instead of
        // fushigi.
        public static bool HideWalls;
        public static bool reloadUnit = false;
        private int count = 0;
        private float offset = 0;
        private float radius = 0;
        private bool doRotate = true;
        List<CourseActor> topLinks;
        CourseActor? selected;
        private CourseActor? storedGlobalActor;
        public static bool killPicker;
        private CancellationTokenSource globalPickToken;
        private bool isPickingSecondArea;
        private string secondPickArea;
        private bool startedPicker;
        public static bool pickingComplete;
        private LevelViewport oldViewport;
        private bool reverseGlobalLink;
        private float previousDelta;
        private BymlArrayNode actorsArray;
        private CourseActorHolder prefabActors;
        private Dictionary<ulong, ulong> hashMapActors = new();
        private string mActorSearchAll = "";
        private string prevSearch;
        private CourseAreaEditContext mEditContext;
        private CourseActor pickActor;

        string mActorSearchText = "";

        CourseLink? mSelectedGlobalLink = null;

        readonly string[] mViewMode = [
            "View All Actors",
            "View Normal Actors",
            "View Wonder Actors"];

        readonly string[] mLinkTypes = [
            "BasicSignal",
            "Create",
            "CreateRelativePos",
            "CreateAfterDied",
            "Delete",
            "Reference",
            "NextGoTo",
            "NextGoToParallel",
            "Bind",
            "Bind_NoRot",
            "Connection",
            "Follow",
            "PopUp",
            "Contents",
            "NoticeDeath",
            "Relocation",
            "ParamRefForChild",
            "CullingReference",
            "EventJoinMember",
            "EventGuest_04",
            "EventGuest_05",
            "EventGuest_06",
            "EventGuest_08",
            "EventGuest_09",
            "EventGuest_10",
            "EventGuest_11",
            "AreaDirCorres_Up",
            "AreaDirCorres_Down",
            "AreaDirCorres_Left",
            "AreaDirCorres_Right",
        ];

        public static readonly string[] LayerTypes = [
            "None",
            "DvScreen",
            "DvNear2",
            "DvNear1",
            "DecoAreaFront",
            "BgFront",
            "PlayArea",
            "DecoArea",
            "DvPlayArea",
            "DvMiddle1",
            "DvMiddle2",
            "DvFar1",
            "DvFar2",
            "DvFar3",
            "DvFar4",
            "DvFar5",
            "DvFar6",
            "DvFar7",
            "DvFar8",
            "DvFar9",
            "DvFar10"
        ];

        public static readonly string[] BackgroundLayerTypes = [
            "DvScreen",
            "DvNear2",
            "DvNear1",
            "BgFront",
            "DvPlayArea",
            "DvMiddle1",
            "DvMiddle2",
            "DvFar1",
            "DvFar2",
            "DvFar3",
            "DvFar4",
            "DvFar5",
            "DvFar6",
            "DvFar7",
            "DvFar8",
            "DvFar9",
            "DvFar10"
        ];

        public static readonly string[] RailTypes = [
            "Default",
            "NextGoTo",
            "BadgeChallenge",
            "CRing",
            "Scroll",
            "SectionFrame",
            "ShabonMove",
            "SwitchON",
            "SwitchOFF",
        ];

        public static readonly Regex NumberRegex = new(@"\d+");

        public static List<string> layerSortTypes = [
             "DvScreen",
             "DvNear",
             "DecoAreaFront",
             "PlayArea",
             "DvPlayArea",
             "DecoArea",
             "DvMiddle",
             "DvFar"
        ];
        #endregion

        public Course GetCourse()
        {
            return course;
        }

        public class LayerSorter : IComparer<string>
        {
            private static readonly Regex NumberRegex = new Regex(@"\d+$");

            public int Compare(string x, string y)
            {
                string baseX = NumberRegex.Replace(x, "");
                string baseY = NumberRegex.Replace(y, "");

                int idX = layerSortTypes.IndexOf(baseX);
                int idY = layerSortTypes.IndexOf(baseY);

                int typeCompare = idX.CompareTo(idY);
                if (typeCompare != 0)
                    return typeCompare;

                int numX = ExtractNumber(x);
                int numY = ExtractNumber(y);

                return numX.CompareTo(numY);
            }

            private int ExtractNumber(string s)
            {
                var match = NumberRegex.Match(s);
                return match.Success ? int.Parse(match.Value) : 0;
            }
        }

        public async Task PrepareResourcesLoad(GLTaskScheduler glScheduler,
            IProgress<(string operationName, float? progress)> progress)
        {
            //Check what files are needed to load/unload by area
            List<string> resourceFiles = new List<string>();
            foreach (var area in course.GetAreas())
            {
                foreach (var actor in area.GetActors())
                {
                    if (actor.mActorPack != null)
                        resourceFiles.Add(actor.mActorPack.GetModelFileName());

                }
            }
            //All resource files to load
            resourceFiles = resourceFiles.Distinct().Where(x => !string.IsNullOrEmpty(x)).ToList();
            //Unload any unused resources in the cache
            //BfresTextureRender.clearTextures();
            List<string> removed = [];
            foreach (var bfres in BfresCache.Cache)
            {
                //Not currently used by area, dispose
                if (!resourceFiles.Contains(bfres.Key))
                {
                    if (bfres.Value.IsCompleted)
                        bfres.Value.Result?.Dispose();

                    removed.Add(bfres.Key);

                }
            }

            foreach (var bfres in removed)
                BfresCache.Cache.Remove(bfres);

            //Load all used resources
            for (int i = 0; i < resourceFiles.Count; i++)
            {
                string? file = resourceFiles[i];
                progress.Report(($"Loading models", i / (float)resourceFiles.Count));
                Logger.Logger.LogMessage("CourseScene", $"Loading {file}");
                await BfresCache.LoadAsync(glScheduler, file);
                Logger.Logger.LogMessage("CourseScene", $"Loaded {file}");
            }
            Logger.Logger.LogMessage("CourseScene", $"Finished loading models");
        }

        public void PreventFurtherRendering()
        {
            foreach (var v in viewports.Values) v.PreventFurtherRendering();
        }
        void UpdateDRPC()
        {
            string sCourseID = course.GetName().Split("_")[0].Replace("Course", "");
            if (int.TryParse(sCourseID, out int courseID))
            {
                if (RomFS.CourseNames.TryGetValue(courseID, out string? courseName))
                {
                    RomFS.CourseWorlds.TryGetValue(courseID, out int worldID);
                    DRPC.SetEditingCourse(selectedArea.GetName(), courseName, worldID);
                    Program.MainWindow.SetWindowIcon(worldID);
                }
                else
                    Logger.Logger.LogWarning("CourseScene", $"Failed to get course name for {course.GetName()}");

                return;
            }

            Logger.Logger.LogWarning("CourseScene", $"Failed to get course ID for {course.GetName()}");
        }

        #region Area
        readonly LayerSorter layerSort = new();
        public void overwriteLevel(CourseArea currentArea, GLTaskScheduler GLTaskScheduler)
        {
            CourseArea blank = new CourseArea("BlankStage", false);
            currentArea.mAreaParams = blank.mAreaParams;
            currentArea.mInitEnvPalette = blank.mInitEnvPalette;
            currentArea.mActorHolder = blank.mActorHolder;
            currentArea.mRailHolder = blank.mRailHolder;
            currentArea.mUnitHolder = blank.mUnitHolder;
            currentArea.mLinkHolder = blank.mLinkHolder;
            currentArea.mGroupsHolder = blank.mGroupsHolder;
            currentArea.mRailLinksHolder = blank.mRailLinksHolder;
            currentArea.mRootHash = blank.mRootHash;
            reloadUnit = true;

            RebuildAreaData(GLTaskScheduler, currentArea);
        }
        private static void AreaParameters(AreaParam area)
        {
            ParamHolder areaParams = ParamLoader.GetHolder("AreaParam");
            var pos = ImGui.GetCursorScreenPos();
            ImGui.SetNextWindowPos(pos, ImGuiCond.Appearing);
            ImGui.SetNextWindowContentSize(new Vector2(400, 800));

            if (ImGui.BeginPopup($"AreaParams", ImGuiWindowFlags.NoMove))
            {
                ImGui.SeparatorText("Area Parameters");

                if (ImGui.BeginTable("AreaParms", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    foreach (string key in areaParams.Keys)
                    {

                        string paramType = areaParams[key];

                        //if (!area.ContainsParam(key))
                        //{
                        //    continue;
                        //}

                        ImGui.Text(key);
                        ImGui.TableNextColumn();

                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - 5);

                        switch (paramType)
                        {
                            case "String":
                                {
                                    string value = "";
                                    if (area.ContainsParam(key))
                                    {
                                        value = (string)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.InputText($"##{key}", ref value, 1024);
                                    break;
                                }
                            case "Bool":
                                {
                                    bool value = false;
                                    if (area.ContainsParam(key))
                                    {
                                        value = (bool)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.Checkbox($"##{key}", ref value);
                                    break;
                                }
                            case "Int":
                                {
                                    int value = 0;
                                    if (area.ContainsParam(key))
                                    {
                                        //value = (int)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.InputInt($"##{key}", ref value);
                                    break;
                                }
                            case "Float":
                                {
                                    float value = 0.0f;
                                    if (area.ContainsParam(key))
                                    {
                                        value = (float)area.GetParam(area.GetRoot(), key, paramType);
                                    }
                                    ImGui.InputFloat($"##{key}", ref value);
                                    break;
                                }
                            default:
                                Console.WriteLine(key);
                                break;
                        }
                        ImGui.PopItemWidth();
                        ImGui.TableNextColumn();
                    }
                    ImGui.EndTable();
                }
                ImGui.EndPopup();
            }
        }

        public void removeArea()
        {
            MainWindow.removeCurrentArea = true;
            ImGui.CloseCurrentPopup();
        }
        public async Task RebuildAreaData(GLTaskScheduler scheduler, CourseArea newArea)
        {
            uint hash = (uint)(new Random().NextDouble() * uint.MaxValue);
            newArea.mRootHash = hash;
            var hashMap = new Dictionary<ulong, ulong>();


            foreach (var actor in newArea.GetActors())
            {
                ulong oldHash = actor.mHash;
                ulong newHash = (ulong)(new Random().NextDouble() * ulong.MaxValue);

                hashMap[oldHash] = newHash;

                actor.mHash = newHash;
                actor.mAreaHash = newArea.mRootHash;


            }


            foreach (CourseLink link in newArea.mLinkHolder.mLinks)
            {
                if (hashMap.TryGetValue(link.mSource, out ulong newSrc))
                    link.mSource = newSrc;

                if (hashMap.TryGetValue(link.mDest, out ulong newDst))
                    link.mDest = newDst;
            }

            foreach (CourseGroup group in newArea.mGroupsHolder.mGroups)
            {
                for (int i = 0; i < group.mActors.Count; i++)
                {
                    ulong oldActorHash = group.mActors[i];

                    if (hashMap.TryGetValue(oldActorHash, out ulong newActorHash))
                        group.mActors[i] = newActorHash;
                }

            }


            foreach (CourseActorToRailLink rail in newArea.mRailLinksHolder.mLinks)
            {
                if (hashMap.TryGetValue(rail.mSourceActor, out ulong newSrc))
                    rail.mSourceActor = newSrc;

            }

            var areaScene = new CourseAreaScene(newArea, new CourseAreaSceneRoot(newArea));
            areaScenes[newArea] = areaScene;

            var viewport = await scheduler.Schedule(gl => new LevelViewport(newArea, gl, areaScene));
            viewports[newArea] = viewport;

            lastSavedAction[newArea] = null;

            var localAreaScene = areaScene;

            viewport.ObjectDeletionRequested += (objs) =>
            {
                if (objs.Count > 0)
                    _ = DeleteObjectsWithWarningPrompt(
                        objs,
                        localAreaScene.EditContext,
                        "Delete objects"
                    );
            };
        }

        public void DeleteAreaFiles(string areaName)
        {
            string modRoot = UserSettings.GetModRomFSPath();
            string areaParam = Path.Combine(modRoot, "Stage/AreaParam", $"{areaName}.game__stage__AreaParam.bgyml");
            string phive = Path.Combine(modRoot, "Phive/StaticCompoundBody", $"{areaName}.phive__StaticCompoundBodySourceParam.gyml");
            string bcett = Path.Combine(modRoot, "BancMapUnit", $"{areaName}.bcett.byml.zs");

            TryDelete(areaParam);
            TryDelete(phive);
            TryDelete(bcett);
        }

        private void TryDelete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        public static async Task<CourseScene> Create(Course course,
            GLTaskScheduler glScheduler,
            IPopupModalHost popupModalHost,
            IProgress<(string operationName, float? progress)> progress)
        {
            var cs = new CourseScene(course, glScheduler, popupModalHost);

            //if (UserSettings.GetEnableTranslation()) 
            Translate.LoadEnglishNames();

            foreach (var area in course.GetAreas())
            {
                var areaScene = new CourseAreaScene(area, new CourseAreaSceneRoot(area));
                cs.areaScenes[area] = areaScene;

                var viewport = await glScheduler.Schedule(gl => new LevelViewport(area, gl, areaScene));
                cs.viewports[area] = viewport;
                cs.lastSavedAction[area] = null;

                var localAreaScene = areaScene;

                viewport.ObjectDeletionRequested += (objs) =>
                {

                    if (objs.Count > 0)
                    {
                        _ = cs.DeleteObjectsWithWarningPrompt(
                            objs,
                            localAreaScene.EditContext,
                            "Delete objects"
                        );
                    }
                };
            }

            oldAreaParamSize = 0;
            foreach (var area in course.GetAreas())
            {
                BymlHashTable old_root = area.mAreaParams.Serialize();
                var old_byml = new Byml.Byml(old_root);
                var old_size = AreaParam.GetDecompressedSize(old_byml);
                oldAreaParamSize += old_size;
            }

            if (!Course.IsWorldMap)
            {
                BymlHashTable old_croot = course.mCourseInfo.Serialize();
                var old_cbyml = new Byml.Byml(old_croot);
                var old_csize = AreaParam.GetDecompressedSize(old_cbyml);
                oldCourseInfoSize = old_csize;
            }

            cs.activeViewport = cs.viewports[cs.selectedArea];

            await cs.PrepareResourcesLoad(glScheduler, progress);
            return cs;
        }

        #endregion

        #region History
        public void Undo() => areaScenes[selectedArea].EditContext.Undo();
        public void Redo() => areaScenes[selectedArea].EditContext.Redo();
        void UndoHistoryPanel()
        {
            undoWindow.Render(areaScenes[selectedArea].EditContext);
        }

        #endregion

        #region UI

        private void CourseUnitView(CourseUnitHolder unitHolder)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            BGUnitRailSceneObj GetRailSceneObj(object courseObject)
            {
                if (!areaScenes[selectedArea].TryGetObjFor(courseObject, out var sceneObj))
                    return null;
                return (BGUnitRailSceneObj)sceneObj;
            }

            ImGui.Text("Select a Wall");
            ImGui.Text("Alt + Left Click to add point");
            ImGui.Text("Delete to remove point");
            ImGui.Text("Right Click to add Internal Rails");

            ImGui.Checkbox("Hide Walls", ref HideWalls);

            if (ImGui.Button("Add Tile Unit", new Vector2(100 * MainWindow.dpiScale, 25 * MainWindow.dpiScale)))
            {
                editContext.AddBgUnit(new CourseUnit());
            }

            List<CourseUnit> removed_tile_units = new List<CourseUnit>();

            foreach (var unit in unitHolder.mUnits)
            {
                var tree_flags = ImGuiTreeNodeFlags.None;
                string name = $"Tile Unit {unitHolder.mUnits.IndexOf(unit)}";

                ImGui.AlignTextToFramePadding();
                bool expanded = ImGui.TreeNodeEx($"##{name}", ImGuiTreeNodeFlags.DefaultOpen);

                ImGui.SameLine();
                //ImGui.SetNextItemAllowOverlap();
                //if (ImGui.Checkbox($"##Visible{name}", ref unit.Visible))
                //{
                //    foreach (var wall in unit.Walls)
                //    {
                //        //ImGui.Text("wall");
                //        var railObj = wall.ExternalRail;
                //        if (railObj == null)
                //            continue;

                //        //railObj.Visible = unit.Visible;
                //        //foreach (var rail in wall.InternalRails)
                //        //{
                //        //    GetRailSceneObj(rail).Visible = unit.Visible;
                //        //}
                //    }
                //    //editContext.DeselectAll();
                //    //editContext.Select(unit);
                ////}
                ////ImGui.SameLine();

                if (ImGui.Selectable(name, editContext.IsSelected(unit)))
                {
                    editContext.DeselectAll();
                    editContext.Select(unit);
                }
                if (expanded)
                {
                    void RailListItem(string type, BGUnitRail rail, int id)
                    {
                        bool isSelected = editContext.IsSelected(rail);
                        string wallname = $"{type} {id}";

                        ImGui.Indent();

                        var railObj = rail;
                        if (railObj == null)
                            return;

                        //if (ImGui.Checkbox($"##Visible{wallname}", ref railObj.Visible))
                        //{

                        //}


                        if (ImGui.BeginTable("Rails", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);

                            void SelectRail()
                            {
                                editContext.DeselectAll();
                                editContext.Select(rail);
                            }

                            ImGui.Checkbox($"##{name}{wallname}_visibility", ref rail.Visible);
                            ImGui.SameLine();

                            if (ImGui.Selectable($"##{name}{wallname}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                SelectRail();
                            }
                            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                            {
                                SelectRail();
                                ImGui.OpenPopup("WallMenu");
                            }

                            ImGui.SameLine();
                            ImGui.Text(wallname);

                            ImGui.TableNextColumn();

                            ImGui.TextDisabled($"(Num Points: {rail.Points.Count})");

                            ImGui.EndTable();
                        }

                        ImGui.Unindent();
                    }
 
                    if (editContext.IsSelected(unit))
                    {
                        if (ImGui.BeginPopupContextWindow("RailMenu", ImGuiPopupFlags.MouseButtonRight))
                        {
                            if (ImGui.MenuItem("Add Wall"))
                            {
                                editContext.AddWall(unit, new Wall(unit));
                                editContext.DeselectAll();
                                editContext.Select(unit);
                            }

                            if (ImGui.MenuItem($"Remove {name}"))
                            {
                                removed_tile_units.Add(unit);
                            }

                            ImGui.EndPopup();
                        }
                    }

                    if (unit.mModelType is not CourseUnit.ModelType.Bridge)
                    {
                        if (ImGui.Button("Add Wall"))
                        {
                            editContext.AddWall(unit, new Wall(unit));
                        }
                        ImGui.SameLine();

                        if (ImGui.Button("Remove Wall") || (ImGui.IsKeyPressed(ImGuiKey.Delete) && ImGui.GetIO().KeyShift))
                        {
                            var batchAction = mEditContext.BeginBatchAction();
                            foreach (var rail in mEditContext.GetSelectedObjects<BGUnitRail>())
                            {
                                mEditContext.CommitAction(new TileRebuildRevertable(rail.mCourseUnit));
                                editContext.DeleteWall(unit, unit.Walls[rail.mIndex]);
                                BGUnitRailSceneObj.rebuildUnit(rail.mCourseUnit);
                            }
                            batchAction.Commit("RAARGH");
                        }

                        for (int iWall = 0; iWall < unit.Walls.Count; iWall++)
                        {
                            Wall wall = unit.Walls[iWall];
                            if (editContext.IsSelected(wall.ExternalRail))
                            {
                                if (ImGui.BeginPopupContextWindow("WallMenu", ImGuiPopupFlags.MouseButtonRight))
                                {
                                    if (ImGui.MenuItem("Select All Rail Points"))
                                    {
                                        foreach (var point in wall.ExternalRail.Points)
                                        {
                                            editContext.Select(point);
                                        }
                                        foreach (var iRail in wall.InternalRails)
                                        {
                                            editContext.Select(iRail);
                                            foreach (var point in iRail.Points)
                                            {
                                                editContext.Select(point);
                                            }
                                        }
                                    }

                                    ImGui.Separator();

                                    if (ImGui.MenuItem("Add Internal Rail"))
                                    {
                                        editContext.AddInternalRail(wall, new BGUnitRail(unit) { IsInternal = true });
                                        reloadUnit = true;

                                    }

                                    if (ImGui.MenuItem("Reverse Rail Points"))
                                        editContext.ReverseBgUnitRailPoints(wall);

                                    if (ImGui.MenuItem("Remove Rail"))
                                    {
                                        editContext.DeleteWall(unit, wall);
                                        reloadUnit = true;

                                    }

                                    ImGui.EndPopup();
                                }
                            }

                            if (wall.InternalRails.Count > 0)
                            {
                                ImGui.Unindent();
                                bool ex = ImGui.TreeNodeEx($"##{name}Wall{iWall}", ImGuiTreeNodeFlags.DefaultOpen);
                                ImGui.SameLine();

                                RailListItem("Wall", wall.ExternalRail, unit.Walls.IndexOf(wall));

                                ImGui.Indent();

                                if (ex)
                                {
                                    for (int iInternal = 0; iInternal < wall.InternalRails.Count; iInternal++)
                                    {
                                        BGUnitRail? rail = wall.InternalRails[iInternal];
                                        if (editContext.IsSelected(rail))
                                        {
                                            if (ImGui.BeginPopupContextWindow("WallMenu", ImGuiPopupFlags.MouseButtonRight))
                                            {
                                                if (ImGui.MenuItem($"Remove Internal Rail {iInternal}"))
                                                    editContext.DeleteInternalRail(wall, rail);

                                                ImGui.EndPopup();
                                            }
                                        }
                                        RailListItem("Internal Rail", rail, iInternal);
                                    }
                                }

                                ImGui.TreePop();
                            }
                            else
                            {
                                RailListItem("Wall", wall.ExternalRail, iWall);
                            }
                        }
                    }

                    if (unit.mModelType is CourseUnit.ModelType.SemiSolid or CourseUnit.ModelType.Bridge)
                    {
                        if (ImGui.Button("Add Belt"))
                        {
                            editContext.AddBeltRail(unit, new BGUnitRail(unit) { IsClosed = false});
                            editContext.DeselectAll();
                            editContext.Select(unit);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Remove Belt"))
                        {
                            foreach (var rail in mEditContext.GetSelectedObjects<BGUnitRail>())
                            {
                                editContext.DeleteBeltRail(unit, unit.mBeltRails[rail.mIndex]);
                            }
                        }

                        for (int iBeltRail = 0; iBeltRail < unit.mBeltRails.Count; iBeltRail++)
                        {
                            BGUnitRail beltRail = unit.mBeltRails[iBeltRail];
                            RailListItem("Belt", beltRail, iBeltRail);
                        }
                    }
                    ImGui.TreePop();
                }
            }

            if (removed_tile_units.Count > 0)
            {
                foreach (var tile in removed_tile_units)
                {
                    areaScenes[selectedArea].RemoveSceneObjectsForUnit(tile);
                    editContext.DeleteBgUnit(tile);
                }

                removed_tile_units.Clear();

                foreach (var unit in selectedArea.mUnitHolder.mUnits)
                    BGUnitRailSceneObj.rebuildUnit(unit);

                BGUnitRailSceneObj.rebuildTiles = true;

                editContext.DeselectAll();
            }
        }

        interface IToolWindow
        {
            void Draw(ref bool windowOpen);
        }
        public void DrawUI(GL gl, double deltaSeconds)
        {
            if (blankLevel)
            {
                blankLevel = false;
                currentArea = selectedArea;
                MainWindow.reloadLevel = true;
            }

            mEditContext = areaScenes[selectedArea].EditContext;

            UndoHistoryPanel();

            ActorsPanel();

            if (!Course.IsWorldMap)
                CommentsPanel();

            SelectionParameterPanel();

            RailsPanel();

            if (!Course.IsWorldMap)
                GlobalLinksPanel();

            LocalLinksPanel();

            SelectActorAndLayerPanel();

            if (!Course.IsWorldMap)
            {
                SimultaneousGroupPanel();

                BGUnitPanel();

                CourseMiniView();
            }

            backupTime += deltaSeconds;
            if (backupTime >= UserSettings.GetBackupFreqMinutes() * 60)
            {
                Save(true);
                backupTime = 0;
            }


            if (showTalkingFlower)
            {
                var ctx = areaScenes[selectedArea].EditContext;
                TalkingFlower.Draw(ref showTalkingFlower, mPopupModalHost, ctx);
            }

            if (showPaletteWindow)
                envPaletteWindow.Draw(ref showPaletteWindow, mPopupModalHost);


            if (showGlobalLinkWindow)
                DrawGlobalLinksPanel(ref showGlobalLinkWindow, mPopupModalHost, gLink, linkNumb);

            ulong selectionVersionBefore = areaScenes[selectedArea].EditContext.SelectionVersion;

            bool status = ImGui.Begin("Viewports", ImGuiWindowFlags.NoNav);

            if (ImGui.BeginTabBar("ViewportTabs"))
            {
                for (int i = 0; i < course.GetAreaCount(); i++)
                {
                    var area = course.GetArea(i);

                    if (!viewports.TryGetValue(area, out var viewport))
                        continue;

                    if (ImGui.BeginTabItem(area.GetName()))
                    {

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                            ImGui.OpenPopup($"AreaTabMenu_{i}");

                        if (ImGui.BeginPopup($"AreaTabMenu_{i}"))
                        {
                            if (ImGui.MenuItem("Remove Area"))
                                removeArea();

                            ImGui.EndPopup();
                        }

                        if ((ImGui.GetIO().KeyCtrl) && ImGui.IsKeyPressed(ImGuiKey.W))
                            removeArea();

                        if ((ImGui.GetIO().KeyCtrl) && ImGui.IsKeyPressed(ImGuiKey.T))
                        {
                            MainWindow.addNewArea = true;
                            saveStatus = false;
                        }

                        //if (areaToFocus == area)
                        //{
                        //    ImGui.SetItemDefaultFocus();
                        //    activeViewport = viewport;
                        //    if (selectedArea != area)
                        //    {
                        //        mHasFilledLayers = false;
                        //    }

                        //    selectedArea = area;
                        //    currentArea = selectedArea;
                        //    areaToFocus = null;
                        //}


                        if (selectedArea != area)
                        {
                            mHasFilledLayers = false;
                            areaSwitched = true;
                            regenerateLayersList = true;
                        }

                        activeViewport = viewport;
                        selectedArea = area;

                        if (isPickingSecondArea && selectedArea.GetName() != secondPickArea && !pickingComplete)
                        {
                            startedPicker = true;
                            oldViewport.CancelPick();
                        }

                        if (startedPicker && selectedArea.GetName() != startingArea && !pickingComplete)
                        {
                            RunGlobalPicker();
                        }

                        envPaletteWindow.Load(gl, area.mAreaParams, area.mInitEnvPalette);

                        ViewportMenuBar(area, viewport);

                        var io = ImGui.GetIO();

                        bool viewportActive = ImGui.IsWindowFocused(ImGuiFocusedFlags.ChildWindows);

                        if (viewportActive)
                            io.ConfigFlags &= ~ImGuiConfigFlags.NavEnableKeyboard;
                        else
                            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

                        var size = ImGui.GetContentRegionAvail();
                        sizeToUse = size;

                        var drawPos = ImGui.GetCursorScreenPos();
                        ImGui.SetCursorScreenPos(drawPos);

                        if (!Course.IsWorldMap)
                            viewport.Draw(size, deltaSeconds, mLayersVisibility);
                        else
                            viewport.DrawWM(size, deltaSeconds, mLayersVisibility);

                        viewport.vpMin = ImGui.GetItemRectMin();
                        viewport.vpMax = ImGui.GetItemRectMax();
                        insideViewport = ImGui.IsMouseHoveringRect(viewport.vpMin, viewport.vpMax);
                        viewport.DrawOverlay();


                        DrawComments(viewport, mEditContext, area);
                        

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            leftClickStartedInsideViewport = insideViewport;
                        }

                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            leftClickStartedInsideViewport = false;
                        }

                        bool placingActor =
                            viewport.mObjectPickingRequest != null ||
                            viewport.mPositionPickingRequest != null;

                        KeyboardModifier modifiers = KeyboardModifier.None;
                        if (ImGui.GetIO().KeyShift) modifiers |= KeyboardModifier.Shift;
                        if (ImGui.GetIO().KeyAlt) modifiers |= KeyboardModifier.Alt;
                        if (OperatingSystem.IsMacOS() ? ImGui.GetIO().KeySuper : ImGui.GetIO().KeyCtrl)
                            modifiers |= KeyboardModifier.CtrlCmd;


                        bool viewportWindowHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.None);

                        AreaParameters(area.mAreaParams);

                        ImGui.EndChild();     // End ViewportContent
                        ImGui.EndTabItem();   // End tab
                    }
                }

                if (ImGui.TabItemButton("+"))
                {
                    MainWindow.addNewArea = true;
                    saveStatus = false;
                }

                ImGui.EndTabBar();
            }


            if (lastCreatedViewports != viewports)
            {
                for (int i = 0; i < course.GetAreaCount(); i++)
                {
                    var area = course.GetArea(i);
                    var playerLocator = area.mActorHolder.mActors.Find(x => x.mPackName == "PlayerLocator");

                    if (playerLocator is not null)
                    {
                        areaToFocus = area;
                        viewports[area].FrameSelectedActor(playerLocator);
                        break;
                    }
                }

                lastCreatedViewports = viewports;
            }

            if (status)
                ImGui.End();
        }
        private void SimultaneousGroupPanel()
        {
            ImGui.Begin("Simultaneous Groups");

            var editContext = areaScenes[selectedArea].EditContext;
            var areaGroups = selectedArea.mGroupsHolder.mGroups;

            List<CourseGroup> groupsToRemove = new List<CourseGroup>();

            if (ImGui.Button("Add Group", new Vector2(120 * MainWindow.dpiScale, 30 * MainWindow.dpiScale)))
                editContext.AddGroup(new CourseGroup());

            ImGui.SameLine();

            if (ImGui.Button("Remove Group", new Vector2(120 * MainWindow.dpiScale, 30 * MainWindow.dpiScale))
                || ImGui.IsKeyPressed(ImGuiKey.Delete))
            {
                foreach (var group in areaGroups)
                {
                    if (editContext.IsSelected(group))
                        groupsToRemove.Add(group);
                }
            }

            for (int j = 0; j < areaGroups.Count; j++)
            {
                var group = areaGroups[j];
                string name = $"Simultaneous Group {j}";

                ImGui.AlignTextToFramePadding();
                bool expanded = ImGui.TreeNodeEx($"##{name}");

                ImGui.SameLine();

                if (ImGui.Selectable(name, editContext.IsSelected(group),
                    ImGuiSelectableFlags.None,
                    new Vector2(150 * MainWindow.dpiScale, 22 * MainWindow.dpiScale)))
                {
                    editContext.DeselectAll();
                    editContext.Select(group);
                }

                ImGui.SameLine(ImGui.GetColumnWidth() - (80 * MainWindow.dpiScale));

                if (ImGui.Button($"Add Actor ##{j}", new Vector2(80 * MainWindow.dpiScale, 25 * MainWindow.dpiScale)))
                {
                    KeyboardModifier modifier;
                    ImGui.SetWindowFocus(selectedArea.GetName());

                    Task.Run(async () =>
                    {
                        do
                        {
                            using var tokenSource = new CancellationTokenSource();
                            (var picked, modifier) = await activeViewport.PickObject(
                                "Select the actor you wish to add to this group. -- Hold SHIFT to add multiple",
                                x => x is CourseActor, tokenSource);

                            if (picked is null)
                                return;

                            editContext.AddActorToGroup(group, picked as CourseActor);

                        } while ((modifier & KeyboardModifier.Shift) > 0);
                    });
                }

                if (expanded)
                {
                    List<CourseActor> actorsToRemove = new List<CourseActor>();

                    for (int i = 0; i < group.mActors.Count; i++)
                    {
                        var actorHash = group.mActors[i];
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);

                        selectedArea.mActorHolder.TryGetActor(actorHash, out CourseActor? actor);

                        if (actor != null)
                        {
                            float dpi = MainWindow.dpiScale;

                            float fullWidth = ImGui.GetContentRegionAvail().X;

                            float deleteButtonWidth = ImGui.GetFrameHeight() * 3.2f * dpi;
                            float actorButtonWidth = fullWidth - deleteButtonWidth;


                            if (ImGui.Button(actor.mName, new Vector2(actorButtonWidth, 0)))
                            {
                                activeViewport.SelectedActor(actor);
                                activeViewport.Camera.Target.X = actor.mTranslation.X;
                                activeViewport.Camera.Target.Y = actor.mTranslation.Y;
                            }
                            ImGui.SetItemTooltip($"{actor.mPackName}\n{actor.mName}");

                            ImGui.SameLine();

                            Vector2 deletePos = ImGui.GetCursorScreenPos();
                            bool clicked = ImGui.InvisibleButton(
                                $"##DeleteActor{i}FromGroup",
                                new Vector2(deleteButtonWidth, ImGui.GetFrameHeight())
                            );

                            string deleteIcon = IconUtil.ICON_TRASH_ALT;
                            Vector2 iconSize = ImGui.CalcTextSize(deleteIcon);
                            Vector2 iconPos = deletePos + new Vector2(
                                (deleteButtonWidth - iconSize.X) * 0.5f,
                                (ImGui.GetFrameHeight() - iconSize.Y) * 0.5f
                            );

                            uint color = ImGui.GetColorU32(ImGuiCol.Text);
                            if (!ImGui.IsItemHovered())
                                color = (color & 0xFFFFFF) | ((uint)((color >> 24) * 0.5f) << 24);

                            ImGui.GetWindowDrawList().AddText(iconPos, color, deleteIcon);
                            ImGui.SetItemTooltip("Delete Actor from Group");

                            if (clicked)
                                actorsToRemove.Add(actor);
                        }

                        if (actorsToRemove.Count > 0)
                        {
                            foreach (var a in actorsToRemove)
                                editContext.RemoveActorFromGroup(group, a);

                            actorsToRemove.Clear();
                        }
                    }

                    ImGui.TreePop();
                }
            }

            if (groupsToRemove.Count > 0)
            {
                foreach (var g in groupsToRemove)
                    editContext.DeleteGroup(g);

                groupsToRemove.Clear();
            }

            ImGui.End();
        }

        private void CourseMiniView()
        {
            var area = selectedArea;
            var editContext = areaScenes[area].EditContext;
            var view = viewports[area];
            bool status = ImGui.Begin("Minimap", ImGuiWindowFlags.NoNav);

            var widgetTopLeft = ImGui.GetCursorScreenPos();

            var widgetSize = ImGui.GetContentRegionAvail();
            ImGui.InvisibleButton("MiniMapWidget", widgetSize);
            bool isActive = ImGui.IsItemActive();
            bool isHovered = ImGui.IsItemHovered();

            var cam = view.Camera;
            var camSize = view.GetCameraSizeIn2DWorldSpace();

            BoundingBox2D bb = BoundingBox2D.Empty;

            foreach (var actor in area.GetActors())
            {
                if (actor.mPackName == "GlobalAreaInfoActor")
                    continue;

                bb.Include(new Vector2(actor.mTranslation.X, actor.mTranslation.Y));
            }

            foreach (var unit in area.mUnitHolder.mUnits)
            {
                foreach (var subUnit in unit.mTileSubUnits)
                {
                    var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);

                    foreach (var tile in subUnit.GetTiles(new Vector2(float.NegativeInfinity), new Vector2(float.PositiveInfinity)))
                    {
                        var pos = tile.pos + origin2D;
                        bb.Include(new BoundingBox2D(pos, pos + Vector2.One));
                    }
                }
            }
            var levelSize = bb.Max - bb.Min;

            var ratio = widgetSize.X / levelSize.X < widgetSize.Y / levelSize.Y ? widgetSize.X / levelSize.X : widgetSize.Y / levelSize.Y;
            var lvlRectSize = levelSize * ratio;
            var miniCamPos = new Vector2(cam.Target.X - bb.Min.X, -cam.Target.Y + bb.Min.Y) * ratio;
            var miniCamSize = camSize * ratio;
            var miniCamSave = new Vector2(camSave.X - bb.Min.X, -camSave.Y + bb.Min.Y) * ratio;
            var padding = (widgetSize - lvlRectSize) / 2;

            var lvlRectTopLeft = widgetTopLeft + padding;

            var col = ImGuiCol.Button;

            if (ImGui.IsMouseDown(ImGuiMouseButton.Right) && !ImGui.IsMouseDown(ImGuiMouseButton.Left) &&
            (isHovered || (isActive && ImGui.IsMouseReleased(ImGuiMouseButton.Left))) && camSave == default)
            {
                camSave = cam.Target;
            }

            if ((ImGui.IsMouseDown(ImGuiMouseButton.Left) ||
            ImGui.IsMouseDown(ImGuiMouseButton.Right)) &&
            isHovered)
            {
                if (camSave != default)
                {
                    col = ImGuiCol.TextDisabled;
                    ImGui.GetWindowDrawList().AddRect(lvlRectTopLeft + miniCamSave - miniCamSize / 2 + new Vector2(0, lvlRectSize.Y),
                        lvlRectTopLeft + miniCamSave + miniCamSize / 2 + new Vector2(0, lvlRectSize.Y),
                        ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Button]), 6, 0, 3);
                }

                var pos = ImGui.GetMousePos();
                cam.Target = new((pos.X - lvlRectTopLeft.X) / ratio + bb.Min.X,
                    (-pos.Y + lvlRectTopLeft.Y + lvlRectSize.Y) / ratio + bb.Min.Y, cam.Target.Z);
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && camSave != default)
            {
                if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                    cam.Target = camSave;

                camSave = default;
            }

            var dl = ImGui.GetWindowDrawList();

            Vector2 MapPointPixelAligned(Vector2 pos) => new Vector2(
                MathF.Round(lvlRectTopLeft.X + (pos.X - bb.Min.X) / (bb.Max.X - bb.Min.X) * lvlRectSize.X),
                MathF.Round(lvlRectTopLeft.Y + (pos.Y - bb.Max.Y) / (bb.Min.Y - bb.Max.Y) * lvlRectSize.Y)
                );

            var backgroundSubUnits = area.mUnitHolder.mUnits
                .Where(x => x.mModelType == CourseUnit.ModelType.NoCollision)
                .SelectMany(x => x.mTileSubUnits);

            foreach (var subUnit in backgroundSubUnits)
            {
                var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);

                foreach (var tile in subUnit.GetTiles(bb.Min - origin2D, bb.Max - origin2D))
                {
                    var pos = tile.pos + origin2D;
                    dl.AddRectFilled(
                        MapPointPixelAligned(pos),
                        MapPointPixelAligned(pos + Vector2.One),
                        0xFF666688);
                }
            }

            var foregroundTileUnits = area.mUnitHolder.mUnits
                .Where(x => x.mModelType != CourseUnit.ModelType.NoCollision);

            var foregroundSubUnits = foregroundTileUnits
                .SelectMany(x => x.mTileSubUnits)
                .OrderBy(x => x.mOrigin.Z);

            foreach (var subUnit in foregroundSubUnits)
            {
                var type = foregroundTileUnits.First(x => x.mTileSubUnits.Contains(subUnit)).mModelType;
                var unitColor = 0xFF999999;
                var edgeColor = 0xFFEEEEEE;

                switch (type)
                {
                    case CourseUnit.ModelType.Solid:
                        unitColor = 0xFFBB9999;
                        edgeColor = 0xFFFFEEEE;
                        break;
                    case CourseUnit.ModelType.SemiSolid:
                        unitColor = 0xFF99BB99;
                        edgeColor = 0xFFEEFFEE;
                        break;
                }

                var origin2D = new Vector2(subUnit.mOrigin.X, subUnit.mOrigin.Y);
                foreach (var tile in subUnit.GetTiles(bb.Min - origin2D, bb.Max - origin2D))
                {
                    var pos = tile.pos + origin2D;
                    dl.AddRectFilled(
                        MapPointPixelAligned(pos),
                        MapPointPixelAligned(pos + Vector2.One),
                        unitColor);
                }
                if (subUnit == foregroundSubUnits.Last(x => x.mOrigin.Z == subUnit.mOrigin.Z))
                {
                    foreach (var wall in foregroundTileUnits
                        .SelectMany(x => x.Walls)
                        .Where(x => x.ExternalRail.Points.FirstOrDefault()?.mTranslation.Z == subUnit.mOrigin.Z))
                    {
                        var rail = wall.ExternalRail;

                        var pos = rail.Points.Select(x => MapPointPixelAligned(new(x.mTranslation.X, x.mTranslation.Y))).ToArray();
                        dl.AddPolyline(ref pos[0],
                            rail.Points.Count,
                            edgeColor,
                            rail.IsClosed ? ImDrawFlags.Closed : ImDrawFlags.None,
                            1.5f);
                    }
                }
            }

            dl.AddRect(lvlRectTopLeft,
                lvlRectTopLeft + lvlRectSize,
                ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]), 6, 0, 3);

            dl.AddRect(lvlRectTopLeft + miniCamPos - miniCamSize / 2 + new Vector2(0, lvlRectSize.Y),
                lvlRectTopLeft + miniCamPos + miniCamSize / 2 + new Vector2(0, lvlRectSize.Y),
                ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)col]), 6, 0, 3);

            if (status)
                ImGui.End();

        }
        public void ViewportMenuBar(CourseArea area, LevelViewport viewport)
        {
            ImGui.BeginChild(
                            "ViewportContent",
                            ImGui.GetContentRegionAvail(),
                            ImGuiChildFlags.None,
                            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
                            );


            if (ImGui.BeginChild("viewport_menu_bar", new Vector2(ImGui.GetWindowWidth(), 30 * MainWindow.dpiScale)))
            {
                Vector2 icon_size = new Vector2(25 * MainWindow.dpiScale, 25 * MainWindow.dpiScale);

                ImGui.PushStyleColor(ImGuiCol.Button, 0);

                if (!Course.IsWorldMap)
                {
                    if (ImGui.Button(IconUtil.ICON_ARCHIVE, icon_size))
                        showCourseSettings = true;
                    ImGui.SetItemTooltip("Edit Course Settings");

                }
                else
                {
                    if (ImGui.Button(IconUtil.ICON_ARCHIVE, icon_size))
                        showWorldMapSettings = true;
                    ImGui.SetItemTooltip("Edit World Map Settings");
                }
                ImGui.SameLine();

                if (ImGui.Button(IconUtil.ICON_FILE_IMPORT, icon_size))
                    showAreaSettings = true;
                ImGui.SetItemTooltip("Edit Area Settings");

                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), "|");
                ImGui.SameLine();

                if (ImGui.Button(viewport.PlayAnimations ? IconUtil.ICON_STOP : IconUtil.ICON_PLAY, icon_size))
                    viewport.PlayAnimations = !viewport.PlayAnimations;

                ImGui.SameLine();

                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_BORDER_ALL, viewport.ShowGrid, icon_size))
                    viewport.ShowGrid = !viewport.ShowGrid;

                ImGui.SameLine();

                string current_palette = area.mInitEnvPalette == null ? "" : area.mInitEnvPalette.Name;

                void SelectPalette(string name, string palette)
                {
                    if (string.IsNullOrEmpty(palette))
                        return;

                    palette = palette.Replace("Work/Gyml/Gfx/EnvPaletteParam/", "");
                    palette = palette.Replace(".game__gfx__EnvPaletteParam.gyml", "");

                    bool selected = current_palette == name;
                    if (ImGui.Selectable($"{name} : {palette}", selected))
                        viewport.EnvironmentData.TransitionEnvPalette(current_palette, palette);

                    if (selected)
                        ImGui.SetItemDefaultFocus();
                }


                if (showAreaSettings)
                    AreaSettings.Draw(ref showAreaSettings, mPopupModalHost, area.mAreaParams, viewport);

                if (showCourseSettings)
                    CourseSettings.Draw(ref showCourseSettings, mPopupModalHost, course.mCourseInfo, course.mMapAnalysisInfo, course.mStageLoadInfo);

                if (showWorldMapSettings)
                    WorldMapSettings.Draw(ref showWorldMapSettings, mPopupModalHost, course.mWorldInfo);

                if (showActorVisibility)
                    ActorVisibility.Draw(ref showActorVisibility, mPopupModalHost);


                ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(1, 1, 1, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(1, 1, 1, 0.15f));
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);

                var flags = ImGuiComboFlags.NoArrowButton | ImGuiComboFlags.WidthFitPreview;
                if (ImGui.BeginCombo($"##EnvPalette", $"{IconUtil.ICON_PALETTE}", flags))
                {
                    SelectPalette($"Default Palette", area.mAreaParams.EnvPaletteSetting.InitPaletteBaseName);
                    if (area.mAreaParams.EnvPaletteSetting.WonderPaletteList != null)
                        foreach (var palette in area.mAreaParams.EnvPaletteSetting.WonderPaletteList)
                            SelectPalette($"Wonder Palette", palette);
                    if (area.mAreaParams.EnvPaletteSetting.TransPaletteList != null)
                        foreach (var palette in area.mAreaParams.EnvPaletteSetting.TransPaletteList)
                            SelectPalette($"Transition Palette", palette);
                    if (area.mAreaParams.EnvPaletteSetting.EventPaletteList != null)
                        foreach (var palette in area.mAreaParams.EnvPaletteSetting.EventPaletteList)
                            SelectPalette($"Event Palette", palette);
                    ImGui.EndCombo();
                }

                ImGui.PopStyleVar();
                ImGui.PopStyleColor(4);

                ImGui.SameLine();


                bool useGameShaders = UserSettings.UseGameShaders();
                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_ADJUST, useGameShaders, icon_size))
                {
                    useGameShaders = !useGameShaders;
                    UserSettings.SetGameShaders(useGameShaders);
                }
                ImGui.SetItemTooltip("Use Game Shaders");

                ImGui.SameLine();

                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_CAMERA, viewport.ScreenshotMode, icon_size))
                    viewport.ScreenshotMode = !viewport.ScreenshotMode;
                ImGui.SetItemTooltip("Screenshot Mode");

                ImGui.SameLine();

                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_IMAGE, viewport.ShowBackground, icon_size))
                {
                    viewport.ShowBackground = !viewport.ShowBackground;
                    foreach (var layer in mLayersVisibility.Keys)
                        if (BackgroundLayerTypes.Contains(layer))
                            mLayersVisibility[layer] = viewport.ShowBackground;
                }
                ImGui.SetItemTooltip("Hide/Show Background Layers");

                ImGui.SameLine();

                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_ICONS, viewport.ShowActors, icon_size))
                {
                    viewport.ShowActors = !viewport.ShowActors;
                    foreach (var layer in mLayersVisibility.Keys)
                        if (!BackgroundLayerTypes.Contains(layer))
                            mLayersVisibility[layer] = viewport.ShowActors;
                }
                ImGui.SetItemTooltip("Hide/Show Foreground Actors");


                ImGui.SameLine();

                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_SYNC, true, icon_size))
                {
                    BfresCache.Clear();
                }
                ImGui.SetItemTooltip("Reload Models");

                ImGui.SameLine();

                if (ImguiHelper.DrawTextToggle(IconUtil.ICON_PAINT_BRUSH, true, icon_size))
                {
                    showPaletteWindow = true;
                    hasOpened = true;
                }

                ImGui.SetItemTooltip("Palette Editor [Experimental]");

                ImGui.SameLine();

                if (ImGui.Button(IconUtil.ICON_LIST, icon_size))
                    showTalkingFlower = true;
                ImGui.SetItemTooltip("View Talking Flower Voice Lines");

                ImGui.SameLine();

                if (ImGui.Button(IconUtil.ICON_EYE, icon_size))
                    showActorVisibility = true;
                ImGui.SetItemTooltip("Edit Actor Visibility");

                ImGui.PopStyleColor(1);
                ImGui.EndChild();
            }

        }

        #endregion

        #region Rails

        private async void CourseRailsView(CourseRailHolder railHolder)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            ImGui.Text("Select a Rail");
            ImGui.Text("Alt + Left Click to add point");
            ImGui.Text("Double click to add/remove a curve point");
            ImGui.Text("Delete to remove point");

            ImGui.SetNextWindowSize(ImGui.GetContentRegionAvail());
            if (ImGui.BeginCombo("##Add Rail", "Add Rail"))
            {
                foreach (string type in RailTypes)
                {
                    ImGui.Selectable(type);

                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                        editContext.AddRail(new CourseRail(this.selectedArea.mRootHash, type));
                }

                ImGui.EndCombo();
            }
            ImGui.SameLine();

            if (ImGui.Button("Remove Rail"))
            {
                if (railHolder.mRails.Count > 0)
                {
                    var selected = editContext.GetSelectedObjects<CourseRail>();
                    foreach (var rail in selected)
                        editContext.DeleteRail(rail);
                }

            }

            CourseRail railToDelete = null;
            bool removeRail = false;
            foreach (CourseRail rail in railHolder.mRails)
            {
                var rail_node_flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;
                if (editContext.IsSelected(rail) &&
                    !editContext.IsAnySelected<CourseRail.CourseRailPoint>())
                {
                    rail_node_flags |= ImGuiTreeNodeFlags.Selected;

                    if (ImGui.BeginPopupContextWindow("WallMenu", ImGuiPopupFlags.MouseButtonRight))
                    {
                        if (ImGui.MenuItem("Select All Rail Points"))
                        {
                            foreach (var point in rail.mPoints)
                            {
                                editContext.Select(point);
                            }
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Reverse Rail Points"))
                            editContext.ReverseRailPoints(rail);

                        if (ImGui.MenuItem("Remove Rail"))
                        {
                            railToDelete = rail;
                            removeRail = true;
                        }

                        ImGui.EndPopup();
                    }
                }

                bool expanded = ImGui.TreeNodeEx($"Rail {railHolder.mRails.IndexOf(rail)}", rail_node_flags);
                if (ImGui.IsItemHovered(0) && ImGui.IsMouseClicked(0))
                {
                    editContext.DeselectAll();
                    editContext.Select(rail);
                }

                if (expanded)
                {
                    foreach (CourseRail.CourseRailPoint pnt in rail.mPoints)
                    {
                        var flags = ImGuiTreeNodeFlags.Leaf;
                        if (editContext.IsSelected(pnt))
                            flags |= ImGuiTreeNodeFlags.Selected;

                        if (ImGui.TreeNodeEx($"Point {rail.mPoints.IndexOf(pnt)}", flags))
                            ImGui.TreePop();

                        if (ImGui.IsItemHovered(0) && ImGui.IsMouseClicked(0))
                        {
                            editContext.DeselectAll();
                            editContext.Select(pnt);
                        }
                    }

                    ImGui.TreePop();
                }
            }
            if (removeRail)
            {
                editContext.DeleteRail(railToDelete);
                removeRail = false;
            }
        }
        public void deleteEmptyRails()
        {
            CourseAreaEditContext ctx;
            var toDelete = new List<(CourseArea area, CourseRail rail)>();


            foreach (var area in course.GetAreas())
            {
                var rails = area.mRailHolder.mRails;
                foreach (CourseRail rail in rails)
                {
                    if (rail.mPoints.Count == 0)
                        toDelete.Add((area, rail));
                }
            }


            foreach (var (area, rail) in toDelete)
            {
                ctx = areaScenes[area].EditContext;
                ctx.DeleteRail(rail);
            }
        }

        private void RailsPanel()
        {
            ImGui.Begin("Rails");

            CourseRailHolder railArray = selectedArea.mRailHolder;

            CourseRailsView(railArray);

            ImGui.End();
        }

        #endregion

        #region Actors
        private void ActorsPanel()
        {
            ImGui.Begin("Current");

            if (ImGui.Button("Delete Actor"))
            {
                var ctx = areaScenes[selectedArea].EditContext;
                var actors = ctx.GetSelectedObjects<CourseActor>().ToList();

                if (actors.Count > 0)
                    _ = DeleteObjectsWithWarningPrompt(actors,
                        ctx, "Delete actors");

            }

            ImGui.SameLine();

            ImGui.AlignTextToFramePadding();
            ImGui.Text(IconUtil.ICON_SEARCH.ToString());
            ImGui.SameLine();

            ImGui.InputText($"##Search", ref mActorSearchText, 0x100);

            // actors are in an array
            CourseActorHolder actorArray = selectedArea.mActorHolder;

            //CourseActorsTreeView(actorArray);
            CourseActorsLayerView(actorArray);

            ImGui.End();
        }

        private static void PlacementNode(CourseActor actor)
        {
            static void EditFloat3RadAsDeg(string label, ref System.Numerics.Vector3 rad, float speed)
            {
                float RadToDeg(float rad)
                {
                    double deg = 180 / Math.PI * rad;
                    return (float)deg;
                }

                float DegToRad(float deg)
                {
                    double rad = Math.PI / 180 * deg;
                    return (float)rad;
                }

                ImGui.AlignTextToFramePadding();
                ImGui.Text(label);
                ImGui.TableNextColumn();

                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                var deg = new System.Numerics.Vector3(RadToDeg(rad.X), RadToDeg(rad.Y), RadToDeg(rad.Z));

                if (ImGui.DragFloat3($"##{label}", ref deg, speed))
                {
                    rad.X = DegToRad(deg.X);
                    rad.Y = DegToRad(deg.Y);
                    rad.Z = DegToRad(deg.Z);
                }

                ImGui.PopItemWidth();
            }

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                if (ImGui.BeginTable("Trans", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Scale");
                    ImGui.TableNextColumn();

                    ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                    ImGui.DragFloat3("##Scale", ref actor.mScale, 0.25f, 0, float.MaxValue);
                    ImGui.PopItemWidth();

                    ImGui.TableNextColumn();

                    EditFloat3RadAsDeg("Rotation", ref actor.mRotation, 0.25f);

                    ImGui.TableNextColumn();

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Translation");
                    ImGui.TableNextColumn();

                    ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                    ImGui.DragFloat3("##Translation", ref actor.mTranslation, 0.25f);
                    ImGui.PopItemWidth();

                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }
        }

        private void DynamicParamNode(CourseActor actor)
        {
            if (ImGui.CollapsingHeader("Dynamic", ImGuiTreeNodeFlags.DefaultOpen))
            {
                List<string> actorParams = null;
                try
                {
                    actorParams = ParamDB.GetActorComponents(actor.mPackName);
                }
                catch
                {
                    return;
                }

                if (actorParams != null)
                {
                    foreach (string param in actorParams)
                    {
                        Dictionary<string, ParamDB.ComponentParam> dict = ParamDB.GetComponentParams(param);

                        if (dict.Keys.Count == 0 && dict != null)
                        {
                            continue;
                        }
                        ImGui.Indent();

                        ImGui.Text(param);
                        ImGui.Separator();
                        ImGui.Indent();

                        if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);
                            ActorParams.DrawActorParams(actor, param);
                            ImGui.EndTable();
                        }

                        //if (param == "WorldMapCoursePointGate")
                        //{
                        //    if (ImGui.BeginTable("FixedProps", 2,
                        //        ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        //    {
                        //        ImGui.TableNextRow();
                        //        ImGui.TableSetColumnIndex(0);
                        //        ImGui.AlignTextToFramePadding();

                        //        if (gateID <= course.mWorldInfo.Gates.Count && gateID > 0)
                        //        {
                        //            ImGui.Text("Price");

                        //            ImGui.TableSetColumnIndex(1);
                        //            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                        //            var Gate = course.mWorldInfo.Gates[gateID - 1];
                        //            var price = Gate.Price;
                        //            if (ImGui.InputInt("##price", ref price))
                        //            {
                        //                course.mWorldInfo.Gates[gateID - 1].Price = price;
                        //            }
                        //        }
                        //        else
                        //        {
                        //            if (ImGui.Button("makey new gatey"))
                        //            {
                        //                course.mWorldInfo.Gates.Add(new WorldMapInfo.GateTable
                        //                {
                        //                    GateNo = course.mWorldInfo.Gates.Count + 1
                        //                });
                        //            }
                        //        }
                        //        ImGui.PopItemWidth();

                        //        ImGui.EndTable();
                        //    }
                        //}
                        ImGui.Unindent();
                        ImGui.Unindent();

                    }
                }
            }
        }

        private string mSelectedActor;
        public string mSelectedLayer;
        private bool regenerateLayersList;

        #endregion

        #region Prefabs
        public List<CourseActor> CreatePrefab(NumVec placement, string prefab, byte[] levelBytes, uint areaHash)
        {
            var actorsArray = selectedArea.LoadPrefab(prefab, levelBytes, "Actors");
            var linksArray = selectedArea.LoadPrefab(prefab, levelBytes, "Links");

            prefabActors = new CourseActorHolder(actorsArray);
            var prefabLinks = new CourseLinkHolder(linksArray);

            foreach (CourseActor actor in prefabActors.mActors)
            {
                ulong oldHash = actor.mHash;
                ulong newHash = RandomUtil.GetRandom();

                hashMapActors[oldHash] = newHash;
                actor.mHash = newHash;
                actor.mAreaHash = areaHash;
                actor.mTranslation += placement;
            }

            var links = selectedArea.mLinkHolder.mLinks;
            foreach (CourseLink link in prefabLinks.mLinks)
            {
                if (hashMapActors.TryGetValue(link.mSource, out ulong newSrc))
                    link.mSource = newSrc;

                if (hashMapActors.TryGetValue(link.mDest, out ulong newDst))
                    link.mDest = newDst;

                links.Add(link);
            }

            return prefabActors.mActors;
        }

        public List<CourseRail> CreatePrefabRail(NumVec placement, string prefab, byte[] levelBytes, uint areaHash)
        {
            var railsArray = selectedArea.LoadPrefab(prefab, levelBytes, "Rails");
            var prefabRails = new CourseRailHolder(railsArray);
            var railLinksArray = selectedArea.LoadPrefab(prefab, levelBytes, "ActorToRailLinks");
            var prefabRailLinks = new CourseActorToRailLinksHolder(railLinksArray, prefabActors, prefabRails);

            Dictionary<ulong, ulong> hashMap = new();
            Dictionary<ulong, ulong> hashMapPoint = new();

            foreach (CourseRail rail in prefabRails.mRails)
            {
                ulong oldHash = rail.mHash;
                ulong newHash = RandomUtil.GetRandom();

                hashMap[oldHash] = newHash;
                rail.mHash = newHash;
                rail.mAreaHash = areaHash;
                foreach (var point in rail.mPoints)
                {
                    ulong oldHashPoint = point.mHash;
                    ulong newHashPoint = RandomUtil.GetRandom();
                    hashMapPoint[oldHashPoint] = newHashPoint;

                    point.mHash = newHashPoint;
                    point.mTranslation.X += placement.X;
                    point.mTranslation.Y += placement.Y;

                    if (point.mIsCurve)
                    {
                        point.mControl.mTranslation.X += placement.X;
                        point.mControl.mTranslation.Y += placement.Y;
                    }
                }
            }

            var links = selectedArea.mRailLinksHolder.mLinks;
            foreach (CourseActorToRailLink link in prefabRailLinks.mLinks)
            {
                if (hashMapActors.TryGetValue(link.mSourceActor, out ulong newSrc))
                    link.mSourceActor = newSrc;

                if (hashMap.TryGetValue(link.mDestRail, out ulong newDst))
                    link.mDestRail = newDst;

                if (hashMapPoint.TryGetValue(link.mDestPoint, out ulong newPointDst))
                    link.mDestPoint = newPointDst;

                links.Add(link);
            }

            return prefabRails.mRails;

        }
        public async void LoadPrefab(string prefab, string directory)
        {
            var viewport = activeViewport;
            var area = selectedArea;
            var ctx = areaScenes[selectedArea].EditContext;

            NumVec? pos;

            var areaHash = selectedArea.mRootHash;
            var areaLinks = selectedArea.mLinkHolder;

            KeyboardModifier modifier;
            mSelectedLayer = mSelectedLayer ?? "PlayArea1";

            if (!mLayersVisibility.ContainsKey(mSelectedLayer))
            {
                mSelectedLayer = "PlayArea";
                AddSelectedLayer();
                mSelectedLayer = "PlayArea1";
            }

            using var tokenSource = new CancellationTokenSource();
            {
                ImGui.SetWindowFocus(area.mAreaName);
                (pos, modifier) = await viewport.PickPosition(
                    $"Placing Prefab", mSelectedLayer, tokenSource);

                if (!pos.TryGetValue(out var posVec))
                {
                    mSelectedLayer = null;
                    return;
                }
                ctx.DeselectAll();

                NumVec placement;

                placement.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                placement.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                placement.Z = 0.0f;

                string prefabPath = Path.Combine(directory, $"{prefab}.bcett.byml.zs");
                byte[] levelBytes = FileUtil.DecompressFile(prefabPath);

                var actorsArray = selectedArea.LoadPrefab(prefab, levelBytes, "Actor");
                var linksArray = selectedArea.LoadPrefab(prefab, levelBytes, "Links");

                var prefabActors = CreatePrefab(placement, prefab, levelBytes, areaHash);

                var batch = ctx.BeginBatchAction();
                foreach (var actor in prefabActors)
                {
                    var i = 0;
                    do
                    {
                        i++;
                    } while (area.GetActors().Any(x => x.mName == $"{actor.mPackName}{i}"));
                    actor.mName = $"{actor.mPackName}{i}";
                    mSelectedLayer = actor.mLayer;
                    ctx.AddActor(actor);
                    AddLayerFromFile();
                }

                List<CourseRail>? prefabRails = CreatePrefabRail(placement, prefab, levelBytes, areaHash);
                if (prefabRails != null)
                {
                    foreach (var rail in prefabRails)
                        ctx.AddRail(rail);
                }
                batch.Commit($"{IconUtil.ICON_PASTE} Added {prefab} Prefab");
            }

        }

        private void PrefabsView()
        {
            string appDataPath = Path.Combine(UserSettings.SettingsDir, "prefabs");
            string resPath = "res/prefabs";
            string[] resPrefab = Directory.GetFiles(resPath, "*.bcett*");

            if (ImGui.BeginTabBar("PrefabTabs"))
            {
                if (ImGui.BeginTabItem("User Prefabs"))
                {
                    if (Directory.Exists(appDataPath))
                    {
                        string[] files = Directory.GetFiles(appDataPath, "*.bcett*");
                        prefabList(files, true, appDataPath);
                    }
                    else
                        ImGui.Text(noPrefabsText);
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Default Prefabs"))
                {
                    prefabList(resPrefab, false, resPath);
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        public void prefabList(string[] files, bool canDelete, string directory)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                ImGui.SetWindowFocus(null);

            ImGui.BeginChild("PrefabScroll", ImGui.GetContentRegionAvail());
            float rowHeight = ImGui.GetFrameHeight();
            float deleteButtonWidth = rowHeight * 1.6f;

            if (files.Length == 0)
                ImGui.Text(noPrefabsText);

            foreach (string file in files)
            {

                string prefab = Path.GetFileName(file).Split(".bcett")[0];

                ImGui.PushID(prefab);

                float fullWidth = ImGui.GetContentRegionAvail().X;
                float nameWidth = fullWidth - deleteButtonWidth - 8;

                ImGui.BeginGroup();
                ImGui.PushItemWidth(nameWidth);

                if (ImGui.Selectable(prefab, false, ImGuiSelectableFlags.None, new Vector2(nameWidth, rowHeight)))
                {
                    LoadPrefab(prefab, directory);
                }

                ImGui.PopItemWidth();
                ImGui.EndGroup();

                ImGui.SameLine();

                Vector2 deletePos = ImGui.GetCursorScreenPos();
                bool clicked = ImGui.InvisibleButton("##delete", new Vector2(deleteButtonWidth, rowHeight));

                if (canDelete)
                {
                    string deleteIcon = IconUtil.ICON_TRASH_ALT;
                    Vector2 iconSize = ImGui.CalcTextSize(deleteIcon);
                    Vector2 iconPos = deletePos + new Vector2(
                        (deleteButtonWidth - iconSize.X) * 0.5f,
                        (rowHeight - iconSize.Y) * 0.5f
                    );


                    uint color = ImGui.GetColorU32(ImGuiCol.Text);
                    if (!ImGui.IsItemHovered())
                        color = (color & 0xFFFFFF) | ((uint)((color >> 24) * 0.5f) << 24);

                    ImGui.GetWindowDrawList().AddText(iconPos, color, deleteIcon);
                    ImGui.SetItemTooltip("Delete Prefab");

                    if (clicked)
                    {
                        DeletePrefabPopup(file);
                    }

                    ImGui.PopID();
                }
            }
            ImGui.EndChild();
        }
        public async Task DeletePrefabPopup(string file)
        {
            var result = await RemoveAreaConfirmationDialog.ShowDialog(MainWindow.mModalHost, "Remove Prefab", "Do you want to remove this prefab?\nThis action cannot be undone!");

            if (result == RemoveAreaConfirmationDialog.DialogResult.Yes)
            {
                File.Delete(file);
            }

        }
        #endregion

        static void UpdateWonderVisibility(CourseActor actor, Dictionary<string, List<ulong>> links, CourseArea area)
        {
            foreach (var link in links)
            {
                var reLinks = area.GetActors().Where(x => link.Value.Contains(x.mHash));
                if (!link.Key.Contains("CreateRelative") &&
                    (link.Key.Contains("Create") ||
                    link.Key.Contains("PopUp") ||
                    link.Key.Contains("Delete") ||
                    link.Key.Contains("BasicSignal")))
                {
                    foreach (CourseActor linkActor in reLinks)
                    {
                        if ((actor.mPackName == "ObjectWonderTag" || actor.mWonderView == WonderViewType.WonderOnly) &&
                        (!link.Key.Contains("BasicSignal") || (linkActor.mActorPack?.Category.Contains("Tag") ?? false)))
                        {
                            if (link.Key.Contains("Delete"))
                                linkActor.mWonderView = WonderViewType.WonderOff;
                            else
                                linkActor.mWonderView = WonderViewType.WonderOnly;
                        }
                        else
                            linkActor.mWonderView = WonderViewType.Normal;
                    }
                }
            }
        }


        private static bool ToggleButton(string id, string textOn, string textOff, ref bool value, Vector2 size = default)
        {
            var textOnSize = ImGui.CalcTextSize(textOn) * 1.2f;
            var textOffSize = ImGui.CalcTextSize(textOff) * 1.2f;

            if (size.X <= 0 || size.Y <= 0)
            {

                size.X = MathF.Max(textOffSize.X, textOnSize.X) + ImGui.GetStyle().FramePadding.X * 2;
                size.Y = MathF.Max(textOffSize.Y, textOnSize.Y) + ImGui.GetStyle().FramePadding.Y * 2;
            }

            Vector2 cp = ImGui.GetCursorScreenPos();
            bool clicked = ImGui.InvisibleButton(id, size);
            if (clicked)
                value = !value;

            float alpha = value ? 1f : 0.5f;

            if (!ImGui.IsItemHovered())
                alpha -= 0.2f;

            ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), ImGui.GetFontSize() * 1.2f,
                cp + (size - (value ? textOnSize : textOffSize)) / 2,
                (ImGui.GetColorU32(ImGuiCol.Text) & 0xFF_FF_FF) | (uint)(0xFF * alpha) << 24,
                value ? textOn : textOff
                );

            return clicked;
        }

        #region Add Actor
        private void SelectActorAndLayerPanel()
        {
            ImGui.Begin("Add");

            ImGui.BeginTabBar("SelectActorAndLayerWindow");

            if (ImGui.BeginTabItem("Add Actor"))
            {

                if (ImGui.IsItemClicked())
                    regenerateLayersList = true;

                ActorSearch();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Add Layer"))
            {
                ImGui.InputText("##LayerSearch", ref mLayerSearch, 0x100);
                ImGui.BeginChild("ActorScroll", ImGui.GetContentRegionAvail());

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                    ImGui.SetWindowFocus(null);


                if (ImGui.BeginTable("##Layers", 1, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    var layers = LayerTypes
                      .Except(mLayersVisibility.Keys)
                      .ToArray();

                    layerSearch(layers, 0);
                    ImGui.EndTable();
                    ImGui.EndChild();
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Add Prefab"))
            {
                PrefabsView();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();

            ImGui.End();
        }
        private void layerSearch(string[] layers, int callFrom)
        {
            bool isSearch = !string.IsNullOrWhiteSpace(mLayerSearch);
            const int MaxLayerCount = 10;
            int layerCount = 0;

            if (prevCallFrom != callFrom)
                regenerateLayersList = true;
            if (prevLayerSearch != mLayerSearch || regenerateLayersList)
            {
                regenerateLayersList = false;
                filteredLayers.Clear();
                prevLayerSearch = mLayerSearch;

                foreach (var layer in layers)
                {
                    bool HasText = layer.IndexOf(mLayerSearch, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isSearch && !HasText)
                        continue;

                    filteredLayers.Add(layer);
                }
                prevCallFrom = callFrom;
            }
            if (filteredLayers.Count == 0)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text("No layers found");
            }

            for (var i = 0; i < filteredLayers.Count; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                var layer = filteredLayers[i];

                if (callFrom == 0)
                {
                    layerCount = mLayersVisibility.Keys
                                .Count(x => x.StartsWith(layer) && NumberRegex.IsMatch(x.AsSpan(layer.Length..)));
                    if (layer == "PlayArea" || layer == "DecoArea")
                        layer += $" ({layerCount}/{MaxLayerCount})";
                }

                ImGui.Selectable(layer);

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    mSelectedLayer = layer;
                    regenerateLayersList = true;
                }

            }

            if (mSelectedLayer != null && mSelectedActor != null)
                AddSelectedActorWithLayer();
            else if (mSelectedLayer != null)
                AddSelectedLayer();
        }
        private void ActorSearch()
        {
            ImGuiTableFlags flags = ImGuiTableFlags.BordersInnerV;

            int columnCount = 1;
            if (UserSettings.GetEnableTranslation())
                columnCount = 2;

            if (mSelectedActor == null)
            {
                ImGui.InputText("##ActorSearch", ref mActorSearchAll, 0x100);
                flags |= ImGuiTableFlags.Resizable;
            }
            else
            {
                ImGui.InputText("##LayerSearch", ref mLayerSearch, 0x100);
                flags &= ~ImGuiTableFlags.Resizable;
                columnCount = 1;
            }

            bool isSearch = !string.IsNullOrWhiteSpace(mActorSearchAll);

            if (mSelectedActor != null && mSelectedLayer != null)
                ImGui.Text($"Placing {mSelectedActor} on {mSelectedLayer}");

            if (prevSearch != mActorSearchAll)
            {
                filteredActors.Clear();
                translatedActors.Clear();
                prevSearch = mActorSearchAll;

                foreach (var actor in ParamDB.GetActors())
                {
                    var actorEnglish = Translate.FetchTranslatedName(actor);
                    bool HasText = actor.IndexOf(mActorSearchAll, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   actorEnglish.IndexOf(mActorSearchAll, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isSearch && !HasText)
                        continue;

                    filteredActors.Add(actor);
                    translatedActors.Add(actorEnglish);
                }

            }

            ImGui.BeginChild("ActorScroll", ImGui.GetContentRegionAvail());
            string enActor = "";

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                ImGui.SetWindowFocus(null);

            if (filteredActors.Count == 0)
                ImGui.Text("No Actors found");

            if (ImGui.BeginTable("##ActorsAndLayers", columnCount, flags))
            {

                if (mSelectedActor == null)
                {
                    int i = 0;
                    foreach (string actor in filteredActors)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        var actorType = CourseActor.GetActorTypeFromGyaml(actor);
                        uint color = CourseActor.CourseActorColors[CourseActorType.None];
                        CourseActor.CourseActorColors.TryGetValue(actorType, out color);
                        ImGui.PushStyleColor(ImGuiCol.Text, CourseActor.ColorShift(color));
                        if (UserSettings.GetEnableTranslation())
                        {
                            enActor = translatedActors[i];
                            ImGui.Selectable(enActor);
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            ImGui.Selectable(actor);
                            ImGui.PopStyleColor();
                        }

                        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                            mSelectedActor = actor;
                        if (UserSettings.GetEnableTranslation())
                        {
                            ImGui.TableSetColumnIndex(1);
                            ImGui.BeginDisabled();
                            ImGui.Text(actor);
                            ImGui.EndDisabled();
                        }
                        i++;
                    }

                }
                else if (mSelectedLayer == null)
                {
                    var layers = mLayersVisibility.Keys.ToArray().ToArray();
                    layerSearch(layers, 1);
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();
        }
        private async Task AddSelectedActorWithLayer()
        {
            var viewport = activeViewport;
            var area = selectedArea;

            NumVec? pos;
            KeyboardModifier modifier;
            using var tokenSource = new CancellationTokenSource();

            string actorName = mSelectedActor;
            if (UserSettings.GetEnableTranslation())
                actorName = Translate.FetchTranslatedName(actorName);

            Console.WriteLine("placing actor");
            do
            {
                ImGui.SetWindowFocus(area.mAreaName);
                (pos, modifier) = await viewport.PickPosition(
                    $"Placing actor {actorName} -- Hold SHIFT to place multiple", mSelectedLayer, tokenSource);
                if (!pos.TryGetValue(out var posVec))
                {
                    break;
                }
                var actor = new CourseActor(mSelectedActor, area.mRootHash, mSelectedLayer);

                posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Z = 0.0f;

                actor.mTranslation = posVec;

                var i = 0;
                do
                {
                    i++;
                } while (area.GetActors().Any(x => x.mName == $"{actor.mPackName}{i}"));
                actor.mName = $"{actor.mPackName}{i}";

                actor = ActorPlacementParams(actor);

                mEditContext.AddActor(actor);
            } while ((modifier & KeyboardModifier.Shift) > 0);
            mSelectedActor = null;
            mSelectedLayer = null;
        }
        #endregion

        #region Comments 

        private void CommentsView(CourseCommentHolder commentHolder)
        {
            if (commentHolder.mComments.Count == 0)
            {
                ImGui.Text("No comments found");
                return;
            }

            CourseComment commentToDelete = null;
            float rowHeight = ImGui.GetFrameHeight();
            float deleteButtonWidth = rowHeight * 1.6f;

            int commentVal = 0;
            int i = 0;
            var ctx = areaScenes[selectedArea].EditContext;
            foreach (CourseComment comment in commentHolder.mComments)
            {
                i++;
                string commentNum = $"Comment {i}";

                ImGui.PushID(commentNum);

                float fullWidth = ImGui.GetContentRegionAvail().X;
                float nameWidth = fullWidth - deleteButtonWidth - 8;

                ImGui.BeginGroup();
                ImGui.PushItemWidth(nameWidth);

                if (ImGui.Selectable(commentNum, false, ImGuiSelectableFlags.None, new Vector2(nameWidth, rowHeight)))
                {
                    ctx.Select(comment);
                    activeViewport.FrameSelectedComment(comment);
                }

                ImGui.PopItemWidth();
                ImGui.EndGroup();

                ImGui.SameLine();

                Vector2 deletePos = ImGui.GetCursorScreenPos();
                bool clicked = ImGui.InvisibleButton("##delete", new Vector2(deleteButtonWidth, rowHeight));

                string deleteIcon = IconUtil.ICON_TRASH_ALT;
                Vector2 iconSize = ImGui.CalcTextSize(deleteIcon);
                Vector2 iconPos = deletePos + new Vector2(
                    (deleteButtonWidth - iconSize.X) * 0.5f,
                    (rowHeight - iconSize.Y) * 0.5f
                );

                uint color = ImGui.GetColorU32(ImGuiCol.Text);
                if (!ImGui.IsItemHovered())
                    color = (color & 0xFFFFFF) | ((uint)((color >> 24) * 0.5f) << 24);

                ImGui.GetWindowDrawList().AddText(iconPos, color, deleteIcon);
                ImGui.SetItemTooltip("Remove Comment");

                if (clicked)
                {
                    commentToDelete = comment;
                    commentVal = i;
                }
                ImGui.PopID();
            }

            if (commentToDelete != null)
            {
                ctx.RemoveComment(commentToDelete, commentVal);
                commentToDelete = null;
            }
        }
        private void CommentsPanel()
        {
            ImGui.Begin("Comments");

            //if (ImGui.Button("Delete Actor"))
            //{
            //    var ctx = areaScenes[selectedArea].EditContext;
            //    var actors = ctx.GetSelectedObjects<CourseActor>().ToList();

            //    if (actors.Count > 0)
            //        _ = DeleteObjectsWithWarningPrompt(actors,
            //            ctx, "Delete actors");
            //}


            CourseCommentHolder commentsArray = selectedArea.mCommentHolder;

            CommentsView(commentsArray);

            ImGui.End();
        }
        private async Task AddComment()
        {
            var viewport = activeViewport;
            var area = selectedArea;
            var ctx = areaScenes[selectedArea].EditContext;

            NumVec? pos;
            KeyboardModifier modifier;
            using var tokenSource = new CancellationTokenSource();

            do
            {
                ImGui.SetWindowFocus(area.mAreaName);
                (pos, modifier) = await viewport.PickPosition(
                    $"Placing comment -- Hold SHIFT to place multiple", mSelectedLayer, tokenSource);
                if (!pos.TryGetValue(out var posVec))
                {
                    break;
                }

                int commentNumb;
                var comment = new CourseComment();

                posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Z = 0.0f;

                //ctx.AddActor(actor);
            } while ((modifier & KeyboardModifier.Shift) > 0);
            //mSelectedActor = null;
            mSelectedLayer = null;
        }

        #endregion

        #region Links

        void DrawGlobalLinksPanel(ref bool continueDisplay, IPopupModalHost modalHost, CourseLink link, int linkNumb)
        {
            ImGui.SetNextWindowSize(new Vector2(550 * MainWindow.dpiScale, 250 * MainWindow.dpiScale), ImGuiCond.Once);

            ImGui.Begin("Global Links Panel", ref continueDisplay, ImGuiWindowFlags.NoCollapse);

            ImGui.AlignTextToFramePadding();
            mSelectedGlobalLink = link;
            if (mSelectedGlobalLink != null)
                ImGui.Text("Link " + linkNumb);
            else
                continueDisplay = false;

            if (ImGui.Button("Delete Link") || ImGui.IsKeyPressed(ImGuiKey.Delete))
            {
                course.RemoveGlobalLink(mSelectedGlobalLink);
                mSelectedGlobalLink = null;
                continueDisplay = false;
            }

            if (mSelectedGlobalLink != null)
            {
                ImGui.Separator();

                if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Source Hash"); ImGui.TableNextColumn();

                        if (LevelViewport.setGlobalSrc)
                        {
                            mSelectedGlobalLink.mSource = LevelViewport.globalHash;
                            LevelViewport.setGlobalSrc = false;
                        }
                        string srcHash = mSelectedGlobalLink.mSource.ToString();
                        if (ImGui.InputText("##Source Hash", ref srcHash, 256, ImGuiInputTextFlags.CharsDecimal))
                        {
                            mSelectedGlobalLink.mSource = ulong.TryParse(srcHash, out var v) ? v : 0;

                        }

                        ImGui.TableNextColumn();
                        ImGui.Text("Destination Hash"); ImGui.TableNextColumn();

                        if (LevelViewport.setGlobalDst)
                        {
                            mSelectedGlobalLink.mDest = LevelViewport.globalHash;
                            LevelViewport.setGlobalDst = false;
                        }
                        string destHash = mSelectedGlobalLink.mDest.ToString();

                        if (ImGui.InputText("##Dest Hash", ref destHash, 256, ImGuiInputTextFlags.CharsDecimal))
                        {
                            mSelectedGlobalLink.mDest = ulong.TryParse(destHash, out var v) ? v : 0;
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text("Link Type"); ImGui.TableNextColumn();

                        List<string> types = mLinkTypes.ToList();
                        int idx = types.IndexOf(mSelectedGlobalLink.mLinkName);
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        if (ImGui.Combo("##Link Type", ref idx, mLinkTypes, mLinkTypes.Length))
                        {
                            mSelectedGlobalLink.mLinkName = mLinkTypes[idx];
                        }

                        ImGui.EndTable();
                    }
                }
            }
            ImGui.End();
        }


        private void RecursiveLinkFind(CourseArea area, CourseLinkHolder links,
            CourseAreaEditContext editContext, float em, IEnumerable<CourseActor> linkList,
            Hashtable parentActors)
        {
            foreach (CourseActor actor in linkList)
            {
                var destLinks = links.GetDestHashesFromSrc(actor.mHash);
                ImGui.TableNextRow();

                string actorName = actor.mPackName;

                if (UserSettings.GetEnableTranslation())
                    actorName = Translate.FetchTranslatedName(actorName);

                string name = actor.mName;

                ulong actorHash = actor.mHash;
                bool isSelected = editContext.IsSelected(actor);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled(name);

                bool expanded = false;
                bool isVisible = true;
                float margin = 1.5f * em;
                float headerHeight = 1.4f * em;
                Vector2 cp = ImGui.GetCursorScreenPos();
                ImGui.TableSetColumnIndex(0);

                ImGuiTreeNodeFlags node_flags = ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.OpenOnArrow;

                if (isSelected)
                    node_flags |= ImGuiTreeNodeFlags.Selected;

                if (!parentActors.ContainsValue(actor) && destLinks.Count > 0)
                    expanded = ImGui.TreeNodeEx($"{actorHash}", node_flags, actorName);
                else
                    expanded = ImGui.Selectable(actorName, isSelected);

                if (ImGui.IsItemClicked())
                {
                    activeViewport.SelectedActor(actor);
                }

                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                {
                    activeViewport.FrameSelectedActor(actor);
                    selected ??= actor;
                }
                if (parentActors.Count == 0 && selected == actor)
                {
                    ImGui.SetScrollHereY();
                    selected = null;
                }

                ImGui.BeginDisabled(!isVisible);

                UpdateWonderVisibility(actor, destLinks, area);

                if (expanded)
                {
                    if (!parentActors.ContainsValue(actor) && destLinks.Count > 0)
                    {
                        foreach (var link in destLinks)
                        {
                            if (ImGui.TreeNodeEx($"{link.Key}##{actorHash}", ImGuiTreeNodeFlags.FramePadding, link.Key))
                            {
                                var parents = new Hashtable(parentActors);
                                parents[actorHash] = actor;
                                var reLinks = area.GetActors().Where(x => link.Value.Contains(x.mHash));
                                RecursiveLinkFind(area, links, editContext, em, reLinks, parents);
                                ImGui.TreePop();
                            }
                        }
                        ImGui.TreePop();
                    }
                }

                ImGui.EndDisabled();
            }
            parentActors.Clear();
        }

        private void AreaLocalLinksView(CourseArea area)
        {
            var links = area.mLinkHolder;
            var editContext = areaScenes[selectedArea].EditContext;

            float em = ImGui.GetFrameHeight();
            var wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            var wcMax = wcMin + ImGui.GetContentRegionAvail();

            topLinks = area.GetActors()
                .Where(x => links.mLinks.Any(y => y.mSource == x.mHash)).ToList();
            if (ImGui.BeginTable("##Links", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
            {
                CourseActor? selected = null;
                RecursiveLinkFind(area, links, editContext, em, topLinks, []);
                ImGui.EndTable();
            }

            ImGui.PopClipRect();

            ImGui.EndChild();
        }
        private void GlobalLinksPanel()
        {
            if (!Course.IsOneAreaCourse || course.GetAreaCount() > 1)
            {
                ImGui.Begin("Global Links");

                if (ImGui.Button("Add Link"))
                {
                    course.AddGlobalLink();
                }

                ImGui.Separator();

                CourseGlobalLinksView(course.GetGlobalLinks());

                ImGui.End();
            }
            else
            {
                ImGui.Begin("Global Links");

                ImGui.TextWrapped("This is a one-area course, so there are no global links to display.");

                ImGui.End();
            }

        }

        private void LocalLinksPanel()
        {
            ImGui.Begin("Local Links");

            ImGui.Separator();

            AreaLocalLinksView(selectedArea);

            ImGui.End();
        }

        private void CourseGlobalLinksView(CourseLinkHolder linkHolder)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            for (int i = 0; i < linkHolder.mLinks.Count; i++)
            {
                CourseLink link = linkHolder.mLinks[i];

                ImGui.Selectable($"Link {i}", editContext.IsSelected(link));

                if (ImGui.IsItemHovered() &&
                    ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    editContext.DeselectAll();
                    editContext.Select(link);

                    showGlobalLinkWindow = true;
                    linkNumb = i;
                    gLink = link;
                }
            }
        }

        #endregion

        #region Selection Parameters

        private CourseActor ActorPlacementParams(CourseActor actor)
        {
            if (actor.mPackName == "ItemWonderHole")
                actor.mActorParameters["ChildActorSelectName"] = "Default";

            if (actor.mPackName.Contains("CameraArea"))
                actor.mScale = new NumVec(27.5f, 15.5f, 1.0f);

            return actor;
        }
        private void SelectionParameterPanel()
        {
            var editContext = areaScenes[selectedArea].EditContext;

            bool status = ImGui.Begin("Selection Parameters", ImGuiWindowFlags.AlwaysVerticalScrollbar);

            if (!mEditContext.IsAnySelected<FushigiCursor>() && activeViewport.cursor != null)
            {
                var cursor = activeViewport.cursor;
                cursor.delta = 0;
                pickActor = null;
            }

            if (mEditContext.IsAnySelected<FushigiCursor>())
                SelectedCursor();
            else if (editContext.IsSingleObjectSelected(out CourseActor? mSelectedActor) || startedPicker)
                SelectedActor(mSelectedActor);
            else if (editContext.IsSingleObjectSelected(out CourseUnit? mSelectedUnit))
                SelectedUnit(mSelectedUnit);
            else if (editContext.IsSingleObjectSelected(out BGUnitRail? mSelectedUnitRail))
                SelectedUnitRail(mSelectedUnitRail);
            else if (editContext.IsSingleObjectSelected(out CourseRail? mSelectedRail))
                SelectedRail(mSelectedRail);
            else if (editContext.IsSingleObjectSelected(out CourseRail.CourseRailPoint? mSelectedRailPoint) ||
                     editContext.IsSingleObjectSelected(out CourseRail.CourseRailPointControl? mSelectedRailPointCont))
                SelectedRailPoint(mSelectedRailPoint);
            else if (editContext.GetObjectCountOfType<CourseActor>() > 1)
                SelectedMultiple();
            else
                NoneSelected();

            if (status)
                ImGui.End();
        }

        private void SelectedMultiple()
        {
            var actors = mEditContext.GetSelectedObjects<CourseActor>().ToArray();
            ImGui.AlignTextToFramePadding();
            ImGui.SameLine();
            if (ImGui.Button($"Paste Source Links") && mCopiedLinks.Count > 0)
            {
                var total = 0;
                var batch = mEditContext.BeginBatchAction();
                foreach ((string linkName, List<ulong> hashArray) in mCopiedLinks)
                {
                    for (int i = 0; i < hashArray.Count; i++)
                    {
                        var link = new CourseLink(linkName)
                        {
                            mSource = 0,
                            mDest = 0
                        };
                        if (actors.Length > 1)
                        {
                            foreach (CourseActor actor in actors)
                            {
                                link = new CourseLink(linkName)
                                {
                                    mSource = actor.mHash,
                                    mDest = hashArray[i]
                                };
                                if (!selectedArea.mLinkHolder.mLinks.Contains(link))
                                {
                                    mEditContext.AddLink(link);
                                    total++;
                                }
                            }
                        }
                        if (!selectedArea.mLinkHolder.mLinks.Contains(link))
                        {
                            mEditContext.AddLink(link);
                            total++;
                        }
                    }
                }
                batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");
            }

            ImGui.SameLine();
            if (ImGui.Button($"Paste Destination Links") && mCopiedLinks.Count > 0)
            {
                var total = 0;
                var batch = mEditContext.BeginBatchAction();
                foreach ((string linkName, List<ulong> hashArray) in mCopiedLinks)
                {
                    for (int i = 0; i < hashArray.Count; i++)
                    {
                        var link = new CourseLink(linkName)
                        {
                            mSource = 0,
                            mDest = 0
                        };
                        if (actors.Length > 1)
                        {
                            foreach (CourseActor actor in actors)
                            {
                                link = new CourseLink(linkName)
                                {
                                    mSource = hashArray[i],
                                    mDest = actor.mHash
                                };
                                if (!selectedArea.mLinkHolder.mLinks.Contains(link))
                                {
                                    mEditContext.AddLink(link);
                                    total++;
                                }
                            }
                        }
                        if (!selectedArea.mLinkHolder.mLinks.Contains(link))
                        {
                            mEditContext.AddLink(link);
                            total++;
                        }
                    }
                }
                batch.Commit($"{IconUtil.ICON_CLIPBOARD_CHECK} Paste {total} Link{(total == 1 ? "" : "s")}");
            }
            ImGui.SetItemTooltip("Paste Source Links");

            ImGui.Text("Layer");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

            if (ImGui.BeginCombo("##Dropdown", "Select Layer"))
            {
                foreach (var layer in mLayersVisibility.Keys.ToArray().ToImmutableList())
                {
                    if (ImGui.Selectable(layer))
                    {
                        //item is selected
                        //Console.WriteLine("Changing " + mSelectedActor.mName + "'s layer from " + mSelectedActor.mLayer + " to " + layer + ".");
                        foreach (var actor in actors)
                            actor.mLayer = layer;
                    }
                }
                ImGui.EndCombo();
            }
            string text = "Multiple Actors Selected";
            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;

            var windowHight = ImGui.GetWindowSize().Y;
            var textHeight = ImGui.CalcTextSize(text).Y;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.SetCursorPosY((windowHight - textHeight) * 0.5f);
            ImGui.BeginDisabled();
            ImGui.Text(text);
            ImGui.EndDisabled();
        }
        private void SelectedRailPoint(CourseRail.CourseRailPoint mSelectedRailPoint)
        {
            if (mEditContext.IsSingleObjectSelected(out CourseRail.CourseRailPointControl? cont))
                mSelectedRailPoint ??= cont.point;
            ImGui.AlignTextToFramePadding();

            CourseRailHolder railArray = selectedArea.mRailHolder;
            var railIndex = CourseRail.findRailNum(railArray, mSelectedRailPoint);
            var childIndex = CourseRail.findPointNum(railArray, mSelectedRailPoint);
            ImGui.Text($"Rail Point {childIndex} from Rail {railIndex}");
            ImGui.NewLine();
            ImGui.Separator();

            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Hash"); ImGui.TableNextColumn();
                    string hash = mSelectedRailPoint.mHash.ToString();
                    if (ImGui.InputText("##Hash", ref hash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mSelectedRailPoint.mHash = Convert.ToUInt64(hash);
                    }

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Translation");

                    ImGui.TableNextColumn();

                    ImGui.DragFloat3("##Translation", ref mSelectedRailPoint.mTranslation, 0.25f);

                    ImGui.TableNextColumn();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("Curve Control");

                    ImGui.TableNextColumn();

                    bool priorCurve = mSelectedRailPoint.mIsCurve;
                    ImGui.Checkbox("##Curved", ref mSelectedRailPoint.mIsCurve);

                    if (priorCurve != mSelectedRailPoint.mIsCurve)
                        activeViewport.undoPointToggleMethod(mSelectedRailPoint, priorCurve);

                    ImGui.SameLine();

                    ImGui.BeginDisabled(!mSelectedRailPoint.mIsCurve);

                    ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                    ImGui.DragFloat3("##Control", ref mSelectedRailPoint.mControl.mTranslation, 0.25f);
                    //if (mSelectedRailPoint.mControl.mTranslate != mSelectedRailPoint.mControl.mStartingTrans && !ImGui.IsMouseDragging(ImGuiMouseButton.Left) && mSelectedRailPoint.mIsCurve)
                    //{
                    //    editContext.CommitAction(new PropertyFieldsSetUndo(
                    //       mSelectedRailPoint.mControl,
                    //        [("mTranslate", mSelectedRailPoint.mControl.GetFieldValue("mStartingTrans"))],
                    //        $"{IconUtil.ICON_ARROWS_ALT} Move Rail Point Control"));
                    //}

                    ImGui.PopItemWidth();

                    ImGui.EndDisabled();

                    ImGui.EndTable();
                }
            }

            if (ImGui.CollapsingHeader("Dynamic Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    foreach (KeyValuePair<string, object> param in mSelectedRailPoint.mParameters)
                    {
                        string type = param.Value.GetType().ToString();
                        ImGui.Text(param.Key);
                        ImGui.TableNextColumn();

                        switch (type)
                        {
                            case "System.UInt32":
                                int uint_val = Convert.ToInt32(param.Value);
                                if (ImGui.InputInt($"##{param.Key}", ref uint_val))
                                {
                                    mSelectedRailPoint.mParameters[param.Key] = Convert.ToUInt32(uint_val);
                                }
                                break;
                            case "System.Int32":
                                int int_val = (int)param.Value;
                                if (ImGui.InputInt($"##{param.Key}", ref int_val))
                                {
                                    mSelectedRailPoint.mParameters[param.Key] = int_val;
                                }
                                break;
                            case "System.Single":
                                float float_val = (float)param.Value;
                                if (ImGui.InputFloat($"##{param.Key}", ref float_val))
                                {
                                    mSelectedRailPoint.mParameters[param.Key] = float_val;
                                }
                                break;
                            case "System.Boolean":
                                bool bool_val = (bool)param.Value;
                                if (ImGui.Checkbox($"##{param.Key}", ref bool_val))
                                {
                                    mSelectedRailPoint.mParameters[param.Key] = bool_val;
                                }
                                break;
                        }
                        ImGui.TableNextColumn();
                    }
                    ImGui.EndTable();
                }
            }
        }
        private void SelectedUnit(CourseUnit mSelectedUnit)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Selected BG Unit");

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Model Type"); ImGui.TableNextColumn();

                    if(ImGui.Combo("##mModelType", ref Unsafe.As<CourseUnit.ModelType, int>(ref mSelectedUnit.mModelType),
                        CourseUnit.ModelTypeNames, CourseUnit.ModelTypeNames.Length))
                    {
                        BGUnitRailSceneObj.rebuildUnit(mSelectedUnit);
                    }

                    ImGui.TableNextColumn();

                    ImGui.Text("Skin Division"); ImGui.TableNextColumn();
                    if (ImGui.Combo("##SkinDivision", ref Unsafe.As<CourseUnit.SkinDivision, int>(ref mSelectedUnit.mSkinDivision),
                        CourseUnit.SkinDivisionNames, CourseUnit.SkinDivisionNames.Length))
                    {
                        BGUnitRailSceneObj.rebuildUnit(mSelectedUnit);
                    }

                    ImGui.EndTable();
                }
            }

            if (mSelectedUnit.mModelType is CourseUnit.ModelType.SemiSolid)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button("Remove all Belts"))
                {
                    var batchAction = mEditContext.BeginBatchAction();

                    for (int i = mSelectedUnit.mBeltRails.Count - 1; i >= 0; i--)
                        mEditContext.DeleteBeltRail(mSelectedUnit, mSelectedUnit.mBeltRails[i]);

                    batchAction.Commit("Remove all Belts from TileUnit");
                }

                ImGui.SameLine();

                if (ImGui.Button("Generate Belts"))
                {
                    var batchAction = mEditContext.BeginBatchAction();

                    for (int i = mSelectedUnit.mBeltRails.Count - 1; i >= 0; i--)
                        mEditContext.DeleteBeltRail(mSelectedUnit, mSelectedUnit.mBeltRails[i]);

                    void ProcessRail(BGUnitRail rail)
                    {
                        if (rail.Points.Count <= 1)
                            return;

                        BGUnitRail? firstBeltRail = null;
                        BGUnitRail? currentBeltRail = null;

                        var lastPoint = new NumVec(float.NaN);

                        for (int i = 0; i < rail.Points.Count; i++)
                        {
                            var point0 = rail.Points[i].mTranslation;
                            var point1 = rail.Points.GetWrapped(i + 1).mTranslation;

                            if (point0.X >= point1.X)
                                continue;

                            if (point0 != lastPoint)
                            {
                                if (currentBeltRail is not null)
                                    mEditContext.AddBeltRail(mSelectedUnit, currentBeltRail);

                                currentBeltRail = new BGUnitRail(mSelectedUnit);
                                currentBeltRail.IsClosed = false;
                                currentBeltRail.Points.Add(new BGUnitRail.RailPoint(currentBeltRail, point0));
                                firstBeltRail ??= currentBeltRail;
                            }

                            currentBeltRail!.Points.Add(new BGUnitRail.RailPoint(currentBeltRail, point1));
                            lastPoint = point1;
                        }

                        var lastBeltRail = currentBeltRail;

                        if (firstBeltRail is not null && lastBeltRail is not null &&
                            firstBeltRail != lastBeltRail &&
                            lastBeltRail.Points[^1].mTranslation == firstBeltRail.Points[0].mTranslation)
                        {
                            //connect first and last rail

                            for (int i = 0; i < lastBeltRail.Points.Count - 1; i++)
                            {
                                var position = lastBeltRail.Points[i].mTranslation;
                                firstBeltRail.Points.Insert(i, new BGUnitRail.RailPoint(firstBeltRail, position));
                            }
                        }
                        else if (lastBeltRail is not null)
                            mEditContext.AddBeltRail(mSelectedUnit, lastBeltRail);
                    }

                    foreach (var wall in mSelectedUnit.Walls)
                    {
                        ProcessRail(wall.ExternalRail);

                        foreach (var internalRail in wall.InternalRails)
                            ProcessRail(internalRail);
                    }

                    batchAction.Commit("Add Belts");
                }
            }
        }
        private void SelectedRail(CourseRail mSelectedRail)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Selected {mSelectedRail.mType} Rail");
            ImGui.NewLine();
            ImGui.Separator();

            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    ImGui.Text("Rail Type"); ImGui.TableNextColumn();
                    if (ImGui.BeginCombo("##Edit Rail Type", mSelectedRail.mType))
                    {
                        foreach (string type in RailTypes)
                        {
                            ImGui.Selectable(type);

                            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0))
                            {
                                mSelectedRail.mType = type;
                                mSelectedRail.RegenerateParameters();
                            }
                        }

                        ImGui.EndCombo();
                    }
                    ImGui.TableNextColumn();
                    ImGui.Text("Hash"); ImGui.TableNextColumn();
                    string hash = mSelectedRail.mHash.ToString();
                    if (ImGui.InputText("##Hash", ref hash, 256, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue))
                    {
                        mSelectedRail.mHash = Convert.ToUInt64(hash);
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text("IsClosed");
                    ImGui.TableNextColumn();
                    ImGui.Checkbox("##IsClosed", ref mSelectedRail.mIsClosed);

                    ImGui.EndTable();
                }
            }

            if (ImGui.CollapsingHeader("Dynamic Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("DynamProps", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ActorParams.DrawRailParams(mSelectedRail);
                    ImGui.EndTable();
                }
            }
        }
        private void SelectedUnitRail(BGUnitRail mSelectedUnitRail)
        {

            ImGui.AlignTextToFramePadding();
            ImGui.Text($"Selected BG Unit Rail");

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("IsClosed"); ImGui.TableNextColumn();
                    if (ImGui.Checkbox("##IsClosed", ref mSelectedUnitRail.IsClosed))
                        mSelectedUnitRail.mCourseUnit.GenerateTileSubUnits();

                    ImGui.TableNextColumn();
                    //Depth editing for bg unit. All points share the same depth, so batch edit the Z point
                    float depth = mSelectedUnitRail.Points.Count == 0 ?
                        mSelectedUnitRail.mCourseUnit.mModelType switch
                        {
                            CourseUnit.ModelType.Solid => 0,
                            CourseUnit.ModelType.SemiSolid => -2,
                            CourseUnit.ModelType.NoCollision => -4,
                            CourseUnit.ModelType.Bridge => -2,
                            _ => 0
                        }
                        : mSelectedUnitRail.Points[0].mTranslation.Z;

                    ImGui.Text("Z Depth"); ImGui.TableNextColumn();
                    if (ImGui.DragFloat("##Depth", ref depth, 0.1f))
                    {
                        //Update depth to all points
                        foreach (var p in mSelectedUnitRail.Points)
                            p.mTranslation = new System.Numerics.Vector3(p.mTranslation.X, p.mTranslation.Y, depth);
                        mSelectedUnitRail.mCourseUnit.GenerateTileSubUnits();
                    }

                    ImGui.EndTable();
                }
            }
        }
        private void SelectedActor(CourseActor mSelectedActor)
        {

            if (mSelectedActor == null && startedPicker)
            {
                mSelectedActor = globalSource;
            }

            //invalidate current action if there has been external changes
            if (propertyCapture.capture.HasChangesSinceLastCheckpoint())
            {
                propertyCapture = (null, FullPropertyCapture.Empty);
            }

            #region Actor UI
            string actorName = mSelectedActor.mPackName;
            string name = mSelectedActor.mName;

            if (ImGui.BeginTable("Props", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                string packName = mSelectedActor.mPackName;
                string englishName = packName;

                if (UserSettings.GetEnableTranslation())
                    englishName = Translate.FetchTranslatedName(englishName);


                ImGui.Text("Actor Name");
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                ImGui.InputText("##Actor Name", ref englishName, 256, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopItemWidth();

                ImGui.TableNextColumn();
                ImGui.Text("Actor Hash");
                ImGui.TableNextColumn();
                string hash = mSelectedActor.mHash.ToString();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                ImGui.InputText("##Actor Hash", ref hash, 256, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopItemWidth();

                ImGui.TableNextColumn();
                ImGui.Text("Area Hash");
                ImGui.TableNextColumn();
                string areaHash = mSelectedActor.mAreaHash.ToString();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
                ImGui.InputText("##Area Hash", ref areaHash, 256, ImGuiInputTextFlags.ReadOnly);
                ImGui.PopItemWidth();

                ImGui.TableNextColumn();
                ImGui.Separator();
                ImGui.TableNextColumn();
                ImGui.Separator();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name");

                ImGui.TableNextColumn();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                if (ImGui.InputText($"##{name}", ref name, 512, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    mSelectedActor.mName = name;
                }

                ImGui.PopItemWidth();

                ImGui.TableNextColumn();
                ImGui.Text("Layer");
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

                if (ImGui.BeginCombo("##Dropdown", mSelectedActor.mLayer))
                {
                    foreach (var layer in mLayersVisibility.Keys.ToArray().ToImmutableList())
                    {
                        if (ImGui.Selectable(layer))
                        {
                            //item is selected
                            Console.WriteLine("Changing " + mSelectedActor.mName + "'s layer from " + mSelectedActor.mLayer + " to " + layer + ".");
                            mSelectedActor.mLayer = layer;
                        }
                    }

                    ImGui.EndCombo();
                }
                ImGui.PopItemWidth();

                ImGui.EndTable();
            }

            PlacementNode(mSelectedActor);

            /* actor parameters are loaded from the dynamic node */
            if (mSelectedActor.mActorParameters.Count > 0)
            {
                DynamicParamNode(mSelectedActor);
            }

            // Links Section
            if (ImGui.CollapsingHeader("Local And Global Links", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Links");
                ImGui.Separator();

                var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(mSelectedActor.mHash);

                var sourceTree = ImGui.TreeNodeEx("Source Links", ImGuiTreeNodeFlags.DefaultOpen);
                ImGui.SameLine();
                if (ImGui.BeginCombo("##Add Link", "Add Link", ImGuiComboFlags.WidthFitPreview))
                {
                    for (int i = 0; i < mLinkTypes.Length; i++)
                    {
                        var linkType = mLinkTypes[i];

                        if (ImGui.Selectable(linkType))
                        {
                            KeyboardModifier modifier;
                            ImGui.SetWindowFocus(selectedArea.GetName());
                            Task.Run(async () =>
                            {
                                do
                                {
                                    (var pickedDest, modifier) = await PickLinkDestInViewportFor(mSelectedActor);
                                    if (pickedDest is null)
                                        return;

                                    var link = new CourseLink(linkType)
                                    {
                                        mSource = mSelectedActor.mHash,
                                        mDest = pickedDest.mHash
                                    };
                                    mEditContext.AddLink(link);
                                } while ((modifier & KeyboardModifier.Shift) > 0);
                            });
                        }
                    }

                    ImGui.EndCombo();
                }
                ImGui.SetItemTooltip("Links this actor is the source of");
                ImGui.SameLine();
                if (ImGui.Button($"{IconUtil.ICON_COPY}"))
                {
                    mCopiedLinks = selectedArea.mLinkHolder.GetDestHashesFromSrc(mSelectedActor.mHash);
                }
                ImGui.SetItemTooltip("Copy Source Links");
                ImGui.SameLine();
                if (ImGui.Button($"{IconUtil.ICON_PASTE}") && mCopiedLinks.Count > 0)
                {
                    var total = 0;
                    var batch = mEditContext.BeginBatchAction();
                    foreach ((string linkName, List<ulong> hashArray) in mCopiedLinks)
                    {
                        for (int i = 0; i < hashArray.Count; i++)
                        {
                            var link = new CourseLink(linkName)
                            {
                                mSource = mSelectedActor.mHash,
                                mDest = hashArray[i]
                            };
                            if (!selectedArea.mLinkHolder.mLinks.Contains(link))
                            {
                                mEditContext.AddLink(link);
                                total++;
                            }
                        }
                    }
                    batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");
                }
                ImGui.SetItemTooltip("Paste Source Links");

                if (sourceTree)
                {
                    ImGui.Indent();
                    foreach ((string linkName, List<ulong> hashArray) in destHashes)
                    {
                        if (ImGui.TreeNodeEx(linkName, ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            for (int i = 0; i < hashArray.Count; i++)
                            {
                                ImGui.PushID($"{hashArray[i].ToString()}_{i}");
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
                                // ImGui.Text("Destination");
                                // ImGui.TableNextColumn();

                                CourseActor? destActor = selectedArea.mActorHolder[hashArray[i]];

                                if (destActor != null)
                                {
                                    if (ImGui.Button(destActor.mName, new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 3.2f, 0)))
                                    {
                                        mSelectedActor = destActor;
                                        activeViewport.SelectedActor(destActor);
                                        activeViewport.Camera.Target.X = destActor.mTranslation.X;
                                        activeViewport.Camera.Target.Y = destActor.mTranslation.Y;
                                    }
                                    ImGui.SetItemTooltip($"{destActor.mPackName}\n{destActor.mName}");
                                }
                                else
                                {
                                    if (ImGui.Button("Actor Not Found"))
                                    {

                                    }
                                }

                                ImGui.SameLine();

                                var cursorSP = ImGui.GetCursorScreenPos();
                                var padding = ImGui.GetStyle().FramePadding;

                                uint WithAlphaFactor(uint color, float factor) => color & 0xFFFFFF | ((uint)((color >> 24) * factor) << 24);

                                float deleteButtonWidth = ImGui.GetFrameHeight() * 1.6f;

                                float columnWidth = ImGui.GetContentRegionAvail().X;

                                ImGui.PushClipRect(cursorSP,
                                    cursorSP + new Vector2(columnWidth - deleteButtonWidth, ImGui.GetFrameHeight()), true);

                                //var cursor = ImGui.GetCursorPos();
                                // ImGui.BeginDisabled();
                                // if (ImGui.Button("Replace"))
                                // {

                                // }
                                // ImGui.EndDisabled();
                                // cursor.X += ImGui.GetItemRectSize().X + 2;

                                //ImGui.SetCursorPos(cursor);
                                if (ImGui.Button(IconUtil.ICON_EYE_DROPPER))
                                {
                                    ImGui.SetWindowFocus(selectedArea.GetName());
                                    Task.Run(async () =>
                                    {
                                        var (pickedDest, _) = await PickLinkDestInViewportFor(mSelectedActor);
                                        if (pickedDest is null)
                                            return;

                                        //TODO rework GetDestHashesFromSrc to return the actual link objects or do it in another way
                                        var link = selectedArea.mLinkHolder.mLinks.Find(
                                            x => x.mSource == mSelectedActor.mHash &&
                                            x.mLinkName == linkName &&
                                            x.mDest == destActor!.mHash);

                                        link.mDest = pickedDest.mHash;
                                    });
                                }
                                ImGui.SetItemTooltip("Replace");

                                ImGui.PopClipRect();
                                cursorSP.X += columnWidth - deleteButtonWidth;
                                ImGui.SetCursorScreenPos(cursorSP);

                                ImGui.SameLine();

                                bool clicked = ImGui.InvisibleButton("##Delete Link", new Vector2(deleteButtonWidth, ImGui.GetFrameHeight()));
                                string deleteIcon = IconUtil.ICON_TRASH_ALT;
                                ImGui.GetWindowDrawList().AddText(cursorSP + new Vector2((deleteButtonWidth - ImGui.CalcTextSize(deleteIcon).X) / 2, padding.Y),
                                    WithAlphaFactor(ImGui.GetColorU32(ImGuiCol.Text), ImGui.IsItemHovered() ? 1 : 0.5f),
                                    deleteIcon);

                                ImGui.SetItemTooltip("Delete Link");

                                if (clicked)
                                    mEditContext.DeleteLink(linkName, mSelectedActor.mHash, hashArray[i]);

                                ImGui.PopID();
                            }
                            ImGui.TreePop();
                        }

                        ImGui.Separator();
                    }
                    ImGui.Unindent();
                    ImGui.TreePop();
                }

                var sourceHashes = selectedArea.mLinkHolder.GetSrcHashesFromDest(mSelectedActor.mHash);

                var destTree = ImGui.TreeNodeEx("Destination Links", ImGuiTreeNodeFlags.DefaultOpen);
                ImGui.SameLine();
                if (ImGui.BeginCombo("##Add Link", "Add Link", ImGuiComboFlags.WidthFitPreview))
                {
                    for (int i = 0; i < mLinkTypes.Length; i++)
                    {
                        var linkType = mLinkTypes[i];

                        if (ImGui.Selectable(linkType))
                        {
                            KeyboardModifier modifier;
                            ImGui.SetWindowFocus(selectedArea.GetName());
                            Task.Run(async () =>
                            {
                                do
                                {
                                    (var pickedDest, modifier) = await PickLinkDestInViewportFor(mSelectedActor);
                                    if (pickedDest is null)
                                        return;

                                    var link = new CourseLink(linkType)
                                    {
                                        mSource = pickedDest.mHash,
                                        mDest = mSelectedActor.mHash
                                    };
                                    mEditContext.AddLink(link);
                                } while ((modifier & KeyboardModifier.Shift) > 0);
                            });
                        }
                    }

                    ImGui.EndCombo();
                }
                ImGui.SetItemTooltip("Links this actor is the destination of");

                ImGui.SameLine();
                if (ImGui.Button($"{IconUtil.ICON_COPY}"))
                {
                    mCopiedLinks = selectedArea.mLinkHolder.GetSrcHashesFromDest(mSelectedActor.mHash);
                }
                ImGui.SetItemTooltip("Copy Destination Links");
                ImGui.SameLine();
                if (ImGui.Button($"{IconUtil.ICON_PASTE}") && mCopiedLinks.Count > 0)
                {
                    var total = 0;
                    var batch = mEditContext.BeginBatchAction();

                    foreach ((string linkName, List<ulong> hashArray) in mCopiedLinks)
                    {
                        for (int i = 0; i < hashArray.Count; i++)
                        {
                            var link = new CourseLink(linkName)
                            {
                                mSource = hashArray[i],
                                mDest = mSelectedActor.mHash
                            };

                            if (!selectedArea.mLinkHolder.mLinks.Contains(link))
                            {
                                mEditContext.AddLink(link);
                                total++;
                            }
                        }
                    }

                    batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");
                }
                ImGui.SetItemTooltip("Paste Destination Links");
                if (destTree)
                {
                    ImGui.Indent();
                    foreach ((string linkName, List<ulong> hashArray) in sourceHashes)
                    {
                        if (ImGui.TreeNodeEx(linkName, ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            for (int i = 0; i < hashArray.Count; i++)
                            {
                                ImGui.PushID($"{hashArray[i].ToString()}_{i}");
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
                                // ImGui.Text("Destination");
                                // ImGui.TableNextColumn();

                                CourseActor? srcActor = selectedArea.mActorHolder[hashArray[i]];

                                if (srcActor != null)
                                {
                                    if (ImGui.Button(srcActor.mName, new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 1.6f, 0)))
                                    {
                                        mSelectedActor = srcActor;
                                        activeViewport.SelectedActor(srcActor);
                                        activeViewport.Camera.Target.X = srcActor.mTranslation.X;
                                        activeViewport.Camera.Target.Y = srcActor.mTranslation.Y;
                                    }
                                    ImGui.SetItemTooltip($"{srcActor.mPackName}\n{srcActor.mName}");
                                }
                                else
                                {
                                    if (ImGui.Button("Actor Not Found"))
                                    {

                                    }
                                }
                                ImGui.SameLine();

                                var cursorSP = ImGui.GetCursorScreenPos();
                                var padding = ImGui.GetStyle().FramePadding;

                                uint WithAlphaFactor(uint color, float factor) => color & 0xFFFFFF | ((uint)((color >> 24) * factor) << 24);

                                float deleteButtonWidth = ImGui.GetFrameHeight() * 1.6f;

                                float columnWidth = ImGui.GetContentRegionAvail().X;

                                ImGui.PushClipRect(cursorSP,
                                    cursorSP + new Vector2(columnWidth - deleteButtonWidth, ImGui.GetFrameHeight()), true);

                                ImGui.PopClipRect();
                                cursorSP.X += columnWidth - deleteButtonWidth;
                                ImGui.SetCursorScreenPos(cursorSP);

                                bool clicked = ImGui.InvisibleButton("##Delete Link", new Vector2(deleteButtonWidth, ImGui.GetFrameHeight()));
                                string deleteIcon = IconUtil.ICON_TRASH_ALT;
                                ImGui.GetWindowDrawList().AddText(cursorSP + new Vector2((deleteButtonWidth - ImGui.CalcTextSize(deleteIcon).X) / 2, padding.Y),
                                    WithAlphaFactor(ImGui.GetColorU32(ImGuiCol.Text), ImGui.IsItemHovered() ? 1 : 0.5f),
                                    deleteIcon);

                                ImGui.SetItemTooltip("Delete Link");

                                if (clicked)
                                    mEditContext.DeleteLink(linkName, hashArray[i], mSelectedActor.mHash);

                                ImGui.PopID();
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.Unindent();
                    ImGui.TreePop();
                }

                if (!Course.IsWorldMap)
                {
                    // Global Links Section
                    ImGui.AlignTextToFramePadding();

                    ImGui.Text("Global Links");
                    ImGui.Separator();

                    if (!Course.IsOneAreaCourse || course.GetAreaCount() > 1)
                    {

                        var glDestHashes = course.GetGlobalLinks().GetDestHashesFromSrc(mSelectedActor.mHash);

                        var glDestIDs = course.GetGlobalLinks().GetIndicesOfLinksWithSrc_ForDelete(mSelectedActor.mHash);

                        var glSourceTree = ImGui.TreeNodeEx("Global Source Links", ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.SameLine();
                        if (ImGui.BeginCombo("##Add Global Link Src", "Add Global Link", ImGuiComboFlags.WidthFitPreview))
                        {
                            for (int i = 0; i < mLinkTypes.Length; i++)
                            {
                                var linkType = mLinkTypes[i];

                                if (ImGui.Selectable(linkType))
                                {
                                    reverseGlobalLink = false;
                                    pickingComplete = false;
                                    globalLinkType = linkType;
                                    globalSource = mSelectedActor;
                                    startingArea = selectedArea.GetName();
                                    if (globalSource != null)
                                    {
                                        startedPicker = true;
                                        RunGlobalPicker();
                                    }
                                }
                            }

                            ImGui.EndCombo();
                        }

                        ImGui.SetItemTooltip("Global Links this actor is the source of");
                        if (glSourceTree)
                        {
                            ImGui.Indent();

                            var destIDs = glDestIDs.ToList();

                            foreach ((string linkName, List<ulong> hashArray) in glDestHashes)
                            {
                                if (ImGui.TreeNodeEx(linkName, ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    for (int idx = 0; idx < hashArray.Count; idx++)
                                    {
                                        int linkId = destIDs[idx];
                                        ulong destHash = hashArray[idx];

                                        CourseActor? destActor = course.ResolveActorByHash(destHash);
                                        actorName = destActor?.mName ?? "Missing Actor";

                                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);


                                        if (ImGui.Button($"Link {linkId}: {actorName}",
                                            new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 1.6f, 0)))
                                        {
                                            showGlobalLinkWindow = true;
                                            gLink = course.GetGlobalLinks().mLinks[linkId];
                                            linkNumb = linkId;
                                        }

                                        ImGui.SameLine();

                                        var cursorSP = ImGui.GetCursorScreenPos();
                                        var padding = ImGui.GetStyle().FramePadding;

                                        float deleteButtonWidth = ImGui.GetFrameHeight() * 1.6f;

                                        bool clicked = ImGui.InvisibleButton(
                                            $"##DeleteLink_{linkId}",
                                            new Vector2(deleteButtonWidth, ImGui.GetFrameHeight())
                                        );

                                        string deleteIcon = IconUtil.ICON_TRASH_ALT;

                                        uint WithAlphaFactor(uint color, float factor)
                                            => color & 0xFFFFFF | ((uint)((color >> 24) * factor) << 24);

                                        ImGui.GetWindowDrawList().AddText(
                                            cursorSP + new Vector2(
                                                (deleteButtonWidth - ImGui.CalcTextSize(deleteIcon).X) / 2,
                                                padding.Y),
                                            WithAlphaFactor(
                                                ImGui.GetColorU32(ImGuiCol.Text),
                                                ImGui.IsItemHovered() ? 1 : 0.5f),
                                            deleteIcon
                                        );

                                        ImGui.SetItemTooltip("Delete Link");

                                        if (clicked)
                                            course.RemoveGlobalLink(course.GetGlobalLinks().mLinks[linkId]);
                                    }

                                    ImGui.TreePop();
                                }
                            }

                            ImGui.Unindent();
                            ImGui.TreePop();
                        }

                        var glSourceHashes = course.GetGlobalLinks().GetSrcHashesFromDest(mSelectedActor.mHash);
                        var glSourceIDs = course.GetGlobalLinks().GetIndicesOfLinksWithDest_ForDelete(mSelectedActor.mHash);

                        var glDestTree = ImGui.TreeNodeEx("Global Destination Links", ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.SetItemTooltip("Global Links this actor is the destination of");
                        ImGui.SameLine();
                        if (ImGui.BeginCombo("##Add Global Link Dst", "Add Global Link", ImGuiComboFlags.WidthFitPreview))
                        {
                            for (int i = 0; i < mLinkTypes.Length; i++)
                            {
                                var linkType = mLinkTypes[i];

                                if (ImGui.Selectable(linkType))
                                {
                                    reverseGlobalLink = true;
                                    pickingComplete = false;
                                    globalLinkType = linkType;
                                    globalSource = mSelectedActor;
                                    startingArea = selectedArea.GetName();
                                    if (globalSource != null)
                                    {
                                        startedPicker = true;
                                        RunGlobalPicker();
                                    }
                                }
                            }

                            ImGui.EndCombo();
                        }
                        if (glDestTree)
                        {
                            ImGui.Indent();


                            var sourceIDs = glSourceIDs.ToList();

                            foreach ((string linkName, List<ulong> hashArray) in glSourceHashes)
                            {
                                if (ImGui.TreeNodeEx(linkName, ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    for (int idx = 0; idx < hashArray.Count; idx++)
                                    {
                                        int linkId = sourceIDs[idx];
                                        ulong srcHash = hashArray[idx];


                                        CourseActor? srcActor = course.ResolveActorByHash(srcHash);
                                        actorName = srcActor?.mName ?? "(Missing Actor)";

                                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);

                                        if (ImGui.Button($"Link {linkId}: {actorName}",
                                            new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 1.6f, 0)))
                                        {
                                            showGlobalLinkWindow = true;
                                            gLink = course.GetGlobalLinks().mLinks[linkId];
                                            linkNumb = linkId;
                                        }

                                        // TRASH BUTTON
                                        ImGui.SameLine();

                                        var cursorSP = ImGui.GetCursorScreenPos();
                                        var padding = ImGui.GetStyle().FramePadding;

                                        float deleteButtonWidth = ImGui.GetFrameHeight() * 1.6f;

                                        bool clicked = ImGui.InvisibleButton(
                                            $"##DeleteLink_{linkId}",
                                            new Vector2(deleteButtonWidth, ImGui.GetFrameHeight())
                                        );

                                        string deleteIcon = IconUtil.ICON_TRASH_ALT;

                                        uint WithAlphaFactor(uint color, float factor)
                                            => color & 0xFFFFFF | ((uint)((color >> 24) * factor) << 24);

                                        ImGui.GetWindowDrawList().AddText(
                                            cursorSP + new Vector2(
                                                (deleteButtonWidth - ImGui.CalcTextSize(deleteIcon).X) / 2,
                                                padding.Y),
                                            WithAlphaFactor(
                                                ImGui.GetColorU32(ImGuiCol.Text),
                                                ImGui.IsItemHovered() ? 1 : 0.5f),
                                            deleteIcon
                                        );

                                        ImGui.SetItemTooltip("Delete Link");

                                        if (clicked)
                                            course.RemoveGlobalLink(course.GetGlobalLinks().mLinks[linkId]);
                                    }

                                    ImGui.TreePop();
                                }
                            }

                            ImGui.Unindent();
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        ImGui.Text("This is a one-area course, so there are no global links to display.");
                    }
                }
            }
            // Actor to rail links
            if (ImGui.CollapsingHeader("Actor to Rail Links"))
            {

                var ctx = areaScenes[selectedArea].EditContext;
                var rails = selectedArea.mRailHolder.mRails;
                var railLinks = selectedArea.mRailLinksHolder.TryGetLinksWithSrcActor(mSelectedActor.mHash);

                if (ImGui.BeginTable("actorRails", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("Rail");
                    ImGui.TableSetupColumn("Point");
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < railLinks.Count; i++)
                    {
                        CourseActorToRailLink link = railLinks[i];

                        ImGui.PushID(i);
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);

                        int railIndex = rails.FindIndex(x => x.mHash == link.mDestRail);
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                        if (ImGui.BeginCombo("##rail", railIndex >= 0 ? ("rail " + railIndex) : "None"))
                        {
                            for (int iRail = 0; iRail < rails.Count; iRail++)
                            {
                                if (ImGui.Selectable("Rail " + iRail, railIndex == iRail))
                                    link.mDestRail = rails[iRail].mHash;
                            }
                            ImGui.EndCombo();
                        }

                        if (railIndex == -1)
                        {
                            ImGui.SameLine();
                            ImGui.TextDisabled("Invalid");
                        }

                        ImGui.TableNextColumn();

                        if (railIndex >= 0 && rails[railIndex].mPoints.Count > 0)
                        {
                            int pointIndex = rails[railIndex].mPoints.FindIndex(x => x.mHash == link.mDestPoint);

                            if (pointIndex == -1)
                                pointIndex = 0;

                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.InputInt("##railpoint", ref pointIndex))
                                pointIndex = Math.Clamp(pointIndex, 0, rails[railIndex].mPoints.Count - 1);

                            link.mDestPoint = rails[railIndex].mPoints[pointIndex].mHash;
                        }

                        ImGui.TableNextColumn();
                        if (ImGui.Button("Delete", new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ScrollbarSize, 0)))
                        {
                            ctx.DeleteRailLink(link);
                            i--;
                        }

                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                }

                float width = ImGui.GetItemRectMax().X - ImGui.GetCursorScreenPos().X;

                ImGui.Dummy(new Vector2(0, ImGui.GetFrameHeight() * 0.5f));

                if (ImGui.Button("Add", new Vector2(width, ImGui.GetFrameHeight() * 1.5f)))
                {
                    var newLink = new CourseActorToRailLink("Reference");
                    newLink.mSourceActor = mSelectedActor.mHash;
                    ctx.AddRailLink(newLink);
                }
            }

            if (!Course.IsWorldMap)
            {
                // Simultaneous Groups
                if (ImGui.CollapsingHeader("Actor to Simultaneous Group Links"))
                {
                    var groups = selectedArea.mGroupsHolder.mGroups;
                    ImGui.Indent();
                    for (int i = 0; i < groups.Count; i++)
                    {
                        if (groups[i].ContainsActor(mSelectedActor.mHash))
                        {
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);

                            if (ImGui.Button($"Simultaneous Group {i}", new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 1.6f, 0)))
                            {
                                var glEditContext = areaScenes[selectedArea].EditContext;
                                glEditContext.DeselectAll();
                                glEditContext.Select(groups[i]);
                            }
                        }
                    }
                    ImGui.Unindent();
                    ImGui.TreePop();

                }
                #endregion

                bool needsRecapture = false;

                if (!ImGui.IsAnyItemActive())
                {
                    if (propertyCapture.capture.TryGetRevertable(out var revertable,
                        names => $"{IconUtil.ICON_WRENCH} Change {string.Join(", ", names)}"))
                    {
                        mEditContext.CommitAction(revertable);
                        needsRecapture = true;
                    }
                }
                if (needsRecapture || propertyCapture.courseObj != mSelectedActor)
                {
                    propertyCapture = (
                        mSelectedActor,
                        new FullPropertyCapture(mSelectedActor)
                    );
                }

                propertyCapture.capture.MakeCheckpoint();
            }
        }
        private void SelectedCursor()
        {
            var cursor = activeViewport.cursor;

            if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                activeViewport.cursor = null;

            if (cursor == null)
                return;

            ImGui.Text("Select both an actor along with the cursor.");
            ImGui.Text("You can also press 'R' to pivot.");

            ImGui.Dummy(new Vector2(0, 10));

            ImGui.Text("Actor Ring Generator");
            ImGui.Separator();

            if (mEditContext.GetObjectCountOfType<CourseActor>() == 1)
                pickActor = mEditContext.GetFirstObjectOfType<CourseActor>();

            var pickActorText = "Select an actor";

            ImGui.Text("Select Actor:");
            if (ImGui.Button(IconUtil.ICON_EYE_DROPPER))
            {
                Task.Run(async () =>
                {
                    pickActor = await ActorPicker();
                });
            }

            ImGui.SameLine();

            if (pickActor != null)
                pickActorText = pickActor.mPackName;

            ImGui.InputText("##Ring Actor", ref pickActorText, 256, ImGuiInputTextFlags.ReadOnly);

            //Credits to https://github.com/aurelionshole/aurelionshole.github.io for Actor Ring code

            ImGui.Text("Points:");
            if (ImGui.InputInt($"##Count", ref count, 1))
                count = Math.Clamp(count, 1, 360);

            ImGui.Text("Offset:");
            ImGui.InputFloat($"##Offset", ref offset, 1);

            ImGui.Text("Radius:");
            ImGui.InputFloat($"##Radius", ref radius, 1);

            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Checkbox("Rotate Actors", ref doRotate);

            if (count != 0 && pickActor != null)
            {
                float ringAngle = 360 / count;
                float angle = offset % ringAngle;
                if (ImGui.Button("Create Actors"))
                {
                    var batchAction = mEditContext.BeginBatchAction();
                    CourseActor newActor = null;
                    for (int i = 0; i < count; i++)
                    {
                        newActor = new CourseActor(pickActor.mPackName, selectedArea.mRootHash, pickActor.mLayer);
                        newActor.mTranslation.X = (float)(cursor.mTranslation.X - radius * Math.Sin(angle * (Math.PI / 180)));
                        newActor.mTranslation.Y = (float)(cursor.mTranslation.Y + radius * Math.Cos(angle * (Math.PI / 180)));

                        if (doRotate)
                            newActor.mRotation.Z = angle * (MathF.PI / 180f);

                        var j = 0;
                        do
                        {
                            j++;
                        } while (selectedArea.GetActors().Any(x => x.mName == $"{newActor.mPackName}{j}"));

                        newActor.mName = $"{newActor.mPackName}{j}";

                        mEditContext.AddActor(newActor);
                        angle = (angle + ringAngle) % 360f;
                    }
                    batchAction.Commit($"{IconUtil.ICON_PLUS_CIRCLE} Placed Actor Ring");
                }
            }

            if (mEditContext.GetSelectedObjects<CourseActor>().ToList().Count >= 1)
            {
                ImGui.Dummy(new Vector2(0, 50));
                bool run = false;

                string pivotText = cursor.delta.ToString();

                ImGui.Text("Pivot Actors");
                ImGui.Separator();

                ImGui.Text("Pivot Angle: Enter angle in degrees");
                if (ImGui.InputText("##Pivot", ref pivotText, 32))
                {
                    if (pivotText == "")
                        cursor.delta = 0;
                    else if (float.TryParse(pivotText, out float value))
                        cursor.delta = value;

                    run = true;
                }

                if (previousDelta != cursor.delta && run)
                {
                    float deltaAngle = cursor.delta - previousDelta;
                    cursor.applyRotation = true;
                    cursor.pivotedActors = mEditContext.GetSelectedObjects<CourseActor>().ToArray();
                    foreach (CourseActor actor in cursor.pivotedActors)
                    {
                        actor.mRotation = actor.mStartingRot;
                        actor.mTranslation = actor.mStartingTrans;

                        Vector2 delta = ImGui.GetIO().MouseDelta;
                        System.Numerics.Vector3 cursorTrans = cursor.mTranslation;
                        actor.mRotation.Z += cursor.delta * (MathF.PI / 180f);
                        actor.mTranslation = cursorTrans + System.Numerics.Vector3.Transform(actor.mTranslation -
                                             cursorTrans, Matrix4x4.CreateRotationZ(cursor.delta * (MathF.PI / 180f)));
                        float angle = cursor.delta * (MathF.PI / 180f);
                    }
                    previousDelta = cursor.delta;
                    return;
                }
            }
        }
        private static void NoneSelected()
        {
            ImGui.AlignTextToFramePadding();

            string text = "No item selected";

            var windowWidth = ImGui.GetWindowSize().X;
            var textWidth = ImGui.CalcTextSize(text).X;

            var windowHight = ImGui.GetWindowSize().Y;
            var textHeight = ImGui.CalcTextSize(text).Y;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
            ImGui.SetCursorPosY((windowHight - textHeight) * 0.5f);
            ImGui.BeginDisabled();
            ImGui.Text(text);
            ImGui.EndDisabled();
        }
        #endregion

        #region Layers

        private async Task AddSelectedLayer()
        {
            var ctx = areaScenes[selectedArea].EditContext;

            if (mSelectedLayer == "PlayArea" || mSelectedLayer == "DecoArea")
            {
                int startIdx = mSelectedLayer == "DecoArea" ? 0 : 1;
                for (int i = startIdx; /*no condition*/; i++)
                {
                    if (!mLayersVisibility.ContainsKey($"{mSelectedLayer}{i}"))
                    {
                        mSelectedLayer += i;
                        break;
                    }
                }
            }
            ctx.CommitAction(new PropertyFieldsSetUndo(
                    this,
                    [("mLayersVisibility", new Dictionary<string, bool>(mLayersVisibility))],
                    $"{IconUtil.ICON_LAYER_GROUP} Added Layer: {mSelectedLayer}"
                )
            );
            mLayersVisibility[mSelectedLayer] = true;

            mSelectedLayer = null;
        }

        private void AddLayerFromFile()
        {
            string[] Layers = mLayersVisibility.Keys.ToArray();


            if (Layers.Contains(mSelectedLayer))
                return;

            var ctx = areaScenes[selectedArea].EditContext;
            ctx.CommitAction(new PropertyFieldsSetUndo(
                  this,
                  [("mLayersVisibility", new Dictionary<string, bool>(mLayersVisibility))],
                  $"{IconUtil.ICON_LAYER_GROUP} Added Layer: {mSelectedLayer}"
              )
          );
            mLayersVisibility[mSelectedLayer] = true;

            mSelectedLayer = null;
        }
        private void BGUnitPanel()
        {
            ImGui.Begin("Terrain Units");

            CourseUnitView(selectedArea.mUnitHolder);

            ImGui.End();
        }

        private void FillLayers(CourseActorHolder actorArray)
        {
            mLayersVisibility.Clear();
            foreach (CourseActor actor in actorArray.mActors)
            {
                string actorLayer = actor.mLayer;
                mLayersVisibility[actorLayer] = true;
            }

            mHasFilledLayers = true;
        }
        private void CourseActorsLayerView(CourseActorHolder actorArray)
        {
            var editContext = areaScenes[selectedArea].EditContext;

            float em = ImGui.GetFrameHeight();

            if (!mHasFilledLayers)
            {
                FillLayers(actorArray);
            }

            float margin = 1.5f * em;

            float headerHeight = 1.4f * em;
            Vector2 cp = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(
                cp,
                cp + new Vector2(ImGui.GetContentRegionAvail().X, headerHeight),
                ImGui.GetColorU32(ImGuiCol.FrameBg));
            ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), em * 0.9f,
                cp + new Vector2(em, (headerHeight - em) / 2 + 0.05f), 0xFF_FF_FF_FF,
                "Layers");

            var wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            var wcMax = wcMin + ImGui.GetContentRegionAvail();

            ImGui.SetCursorScreenPos(new Vector2(wcMax.X - margin, cp.Y + (headerHeight - em) / 2));
            if (ToggleButton($"VisibleCheckbox All", IconUtil.ICON_EYE, IconUtil.ICON_EYE_SLASH,
                ref mAllLayersVisible, new Vector2(em)))
                UpdateAllLayerVisiblity();

            ImGui.SetCursorScreenPos(cp + new Vector2(0, headerHeight));

            ImGui.BeginChild("Layers");

            wcMin = ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetScrollY());
            wcMax = wcMin + ImGui.GetContentRegionAvail();

            ImGui.PushClipRect(wcMin, wcMax - new Vector2(margin, 0), true);

            bool isSearch = !string.IsNullOrWhiteSpace(mActorSearchText);
            var sortedLayers = mLayersVisibility.Keys.ToList();
            sortedLayers.Sort(layerSort);

            ImGui.Spacing();
            foreach (string layer in sortedLayers) //Use sortedLayers if you think the sorting code is good
            {
                ImGui.PushID(layer);
                cp = ImGui.GetCursorScreenPos();
                bool expanded = false;
                bool isVisible = true;

                if (!isSearch)
                {
                    expanded = ImGui.TreeNodeEx("TreeNode", ImGuiTreeNodeFlags.FramePadding, layer);

                    ImGui.PushClipRect(wcMin, wcMax, false);
                    ImGui.SetCursorScreenPos(new Vector2(wcMax.X - (margin + em * 4) / 2, cp.Y));
                    isVisible = mLayersVisibility[layer];
                    if (ToggleButton($"VisibleCheckbox", IconUtil.ICON_EYE, IconUtil.ICON_EYE_SLASH,
                        ref isVisible, new Vector2(em)))
                        mLayersVisibility[layer] = isVisible;
                    ImGui.PopClipRect();
                }
                else
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(layer);
                }
                var dummy = false;
                ImGui.PushClipRect(wcMin, wcMax, false);
                ImGui.SetCursorScreenPos(new Vector2(wcMax.X - (margin + em) / 2, cp.Y));
                if (ToggleButton($"Delete Layer", IconUtil.ICON_TRASH, IconUtil.ICON_TRASH,
                    ref dummy, new Vector2(em)))
                    _ = DeleteLayerWithWarningPrompt(layer, actorArray, editContext);
                ImGui.PopClipRect();

                ImGui.BeginDisabled(!isVisible);

                if (expanded || isSearch)
                {
                    foreach (CourseActor actor in actorArray.mActors)
                    {

                        string actorName = actor.mPackName;
                        string jpName = actorName;
                        string name = actor.mName;

                        if (UserSettings.GetEnableTranslation())
                        {
                            actorName = Translate.FetchTranslatedName(actorName);
                        }

                        ulong actorHash = actor.mHash;
                        string actorLayer = actor.mLayer;

                        //Check if the node is within the necessary search filter requirements if search is used
                        bool HasText = actorName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       actorHash.ToString().Equals(mActorSearchText);

                        if(UserSettings.GetEnableTranslation())
                        {
                            HasText = actorName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     actorHash.ToString().Equals(mActorSearchText) || jpName.IndexOf(mActorSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        if (isSearch && !HasText)
                            continue;

                        if (actorLayer != layer)
                        {
                            continue;
                        }

                        bool isSelected = editContext.IsSelected(actor);

                        ImGui.PushID(actorHash.ToString());
                        if (ImGui.BeginTable("##Links", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableSetColumnIndex(0);

                            if (ImGui.Selectable(actorName, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                            {
                                activeViewport.SelectedActor(actor);
                            }
                            else if (ImGui.IsItemFocused())
                            {
                                activeViewport.SelectedActor(actor);
                            }

                            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                            {
                                activeViewport.FrameSelectedActor(actor);
                            }


                            ImGui.TableNextColumn();
                            ImGui.BeginDisabled();
                            ImGui.Text(name);
                            ImGui.EndDisabled();
                            ImGui.EndTable();
                        }

                        ImGui.PopID();
                    }

                    if (!isSearch)
                        ImGui.TreePop();
                }

                ImGui.EndDisabled();

                ImGui.PopID();
            }

            ImGui.PopClipRect();

            ImGui.EndChild();
        }
        private void UpdateAllLayerVisiblity()
        {
            foreach (string layer in mLayersVisibility.Keys)
            {
                mLayersVisibility[layer] = mAllLayersVisible;
            }
        }

        #endregion 

        #region Picking
        private async Task<(CourseActor?, KeyboardModifier modifiers)> PickLinkDestInViewportFor(CourseActor source)
        {
            using var tokenSource = new CancellationTokenSource();
            var (picked, modifier) = await activeViewport.PickObject(
                            "Select the destination actor you wish to link to. -- Hold SHIFT to link multiple",
                            x => x is CourseActor && x != source, tokenSource);
            return (picked as CourseActor, modifier);
        }

        private async Task<(CourseActor?, KeyboardModifier modifiers)> PickActor()
        {
            using var tokenSource = new CancellationTokenSource();
            var (picked, modifier) = await activeViewport.PickObject(
                            "Select actor to use ring", x => x is CourseActor, tokenSource);
            return (picked as CourseActor, modifier);
        }

        private async void RunGlobalPicker()
        {
            startedPicker = false;
            isPickingSecondArea = true;
            secondPickArea = selectedArea.GetName();
            selectedArea.hasStartedPick = true;
            oldViewport = activeViewport;
            var (picked, modifier) = await PickLinkDestInViewportFor(pendingSource);
            if (picked != null)
            {

                var link = new CourseLink(globalLinkType)
                {
                    mSource = globalSource.mHash,
                    mDest = picked.mHash
                };

                if (reverseGlobalLink)
                {
                    link.mSource = picked.mHash;
                    link.mDest = globalSource.mHash;
                }
                areaScenes[selectedArea].EditContext.AddGlobalLink(link, course);

                if ((modifier & KeyboardModifier.Shift) != 0)
                {
                    return;
                }
                pickingComplete = true;
                selectedArea.hasPickCompleted = true;
            }
        }

        public async Task<CourseActor?> ActorPicker()
        {
            ImGui.SetWindowFocus(selectedArea.GetName());
            bypassSelection = true;

            var (pickedActor, _) = await PickActor();

            return pickedActor;
        }
        #endregion

        #region Deletion
        private async Task DeleteObjectsWithWarningPrompt(IReadOnlyList<object> objectsToDelete,
            CourseAreaEditContext ctx, string actionName)
        {
            var actors = objectsToDelete.OfType<CourseActor>();
            if (actors.Count() == 1)
                actionName = "Delete " + actors.ElementAt(0).mPackName;

            if (!UserSettings.HideDeletingLinkedActorsPopup())
            {
                List<string> dstMsgStrs = [];
                List<string> srcMsgStrs = [];

                foreach (var actor in actors)
                {
                    if (selectedArea.mLinkHolder.HasLinksWithDest(actor.mHash))
                    {
                        var links = selectedArea.mLinkHolder.GetSrcHashesFromDest(actor.mHash);

                        foreach (KeyValuePair<string, List<ulong>> kvp in links)
                        {
                            var hashes = kvp.Value;

                            foreach (var hash in hashes)
                            {
                                /* only delete actors that the hash exists for...this may be caused by a user already deleting the source actor */
                                if (selectedArea.mActorHolder.TryGetActor(hash, out _))
                                {
                                    dstMsgStrs.Add($"{selectedArea.mActorHolder[hash].mPackName} [{selectedArea.mActorHolder[hash].mName}]\n");
                                }
                            }
                        }

                        var destHashes = selectedArea.mLinkHolder.GetDestHashesFromSrc(actor.mHash);

                        foreach (KeyValuePair<string, List<ulong>> kvp in destHashes)
                        {
                            var hashes = kvp.Value;

                            foreach (var hash in hashes)
                            {
                                if (selectedArea.mActorHolder.TryGetActor(hash, out _))
                                {
                                    srcMsgStrs.Add($"{selectedArea.mActorHolder[hash].mPackName} [{selectedArea.mActorHolder[hash].mName}]\n");
                                }
                            }
                        }
                    }
                }

                if (dstMsgStrs.Count > 0 || srcMsgStrs.Count > 0)
                {
                    var result = await OperationWarningDialog.ShowDialog(mPopupModalHost,
                    "Deletion warning",
                    "The object(s) you are about to delete " +
                    "are being used in other places",
                    ("As link source for", srcMsgStrs),
                    ("As link destination for", dstMsgStrs));

                    if (result == OperationWarningDialog.DialogResult.Cancel)
                        return;
                }
            }

            var batchAction = ctx.BeginBatchAction();
            foreach (var actor in actors)
            {
                ctx.DeleteActor(actor);
            }

            batchAction.Commit($"{IconUtil.ICON_TRASH} {actionName}");
        }

        //TODO making this undoable
        private async Task DeleteLayerWithWarningPrompt(string layer,
            CourseActorHolder actorArray, CourseAreaEditContext ctx)
        {
            var actors = actorArray.mActors.FindAll(x => x.mLayer == layer);
            bool noWarnings = !(actors.Count > 0);

            if (!noWarnings)
            {
                List<string> warningActors = [];
                foreach (var actor in actors)
                {
                    if (selectedArea.mActorHolder.TryGetActor(actor.mHash, out _))
                    {
                        warningActors.Add($"{selectedArea.mActorHolder[actor.mHash].mPackName} [{selectedArea.mActorHolder[actor.mHash].mName}]\n");
                    }
                }

                var result = await OperationWarningDialog.ShowDialog(mPopupModalHost,
                "Deletion warning",
                "Deleting " + layer +
                " will delete the following actors",
                ("Actors", warningActors));

                if (result == OperationWarningDialog.DialogResult.Cancel)
                    return;
            }
            else
            {
                var result = await OperationWarningDialog.ShowDialog(mPopupModalHost,
                "Deletion warning",
                "Are you sure you want to delete " +
                layer + "?");

                if (result == OperationWarningDialog.DialogResult.Cancel)
                    return;
            }

            var batchAction = ctx.BeginBatchAction();

            foreach (var actor in actors)
            {
                ctx.DeleteActor(actor);
            }
            ctx.CommitAction(new PropertyFieldsSetUndo(
                    this,
                    [("mLayersVisibility", new Dictionary<string, bool>(mLayersVisibility))],
                    $"{IconUtil.ICON_TRASH} Delete {layer}"
                )
            );
            mLayersVisibility.Remove(layer);

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete Layer: {layer}");
        }

        #endregion

        #region Save Logic
        public void Save(bool backup)
        {
            //RomFS.courseNamesMSBT.Messages = RomFS.courseNames;
            //RomFS.courseNamesMSBT.Save(Path.Combine(UserSettings.GetModRomFSPath(), "Mals", "GameMsg", "Name_CourseRemoveLineFeed.msbt"));

            saveStatus = true;
            string savePath = UserSettings.GetModRomFSPath();
            DateTime now = DateTime.Now;
            if (backup)
            {
                savePath = Path.Combine(UserSettings.SettingsDir, "backups");
                savePath = Path.Combine(savePath, $"{now.Year}-{now.Month}-{now.Day}_{now.Hour}-{now.Minute}-{now.Second}");
            }

            var rstbPath = Path.Combine(UserSettings.GetRomFSPath(), "System", "Resource");

            string[] sizeTables = Directory.GetFiles(rstbPath, "*.zs");

            foreach (string path in sizeTables)
            {

                RSTB resource_table = new RSTB();
                resource_table.Load(Path.GetFileName(path));

                List<string> pathsToWriteTo;

                //Save the Course file
                Console.WriteLine($"{(backup ? "Backing up" : "Saving")} course {course.GetName()}...");
                course.SaveCourse(resource_table, Path.Combine(savePath, "BancMapUnit"));


                //Save the CourseInfo file
                if (!Course.IsWorldMap)
                {
                    Console.WriteLine($"{(backup ? "Backing up" : "Saving")} course info for {course.GetName()}...");
                    course.mCourseInfo.Save(resource_table, Path.Combine(savePath, "Stage", "CourseInfo"), course.GetName());


                    //Save the MapAnalysisInfo file
                    Console.WriteLine($"{(backup ? "Backing up" : "Saving")} map analysis info for {course.GetName()}...");
                    course.mMapAnalysisInfo.Save(resource_table, Path.Combine(savePath, "Stage", "MapAnalysisInfo"), course.GetName());

                }

                //Save the StageLoadInfo file
                Console.WriteLine($"{(backup ? "Backing up" : "Saving")} stage load info for {course.GetName()}...");
                course.mStageLoadInfo.Save(resource_table, Path.Combine(savePath, "Stage", "StageLoadInfo"), course.GetName());

                foreach (var area in course.GetAreas())
                {
                    Console.WriteLine($"{(backup ? "Backing up" : "Saving")} area {area.GetName()}...");
                    Console.WriteLine($"{(backup ? "Backing up" : "Saving")} area parameters for {area.GetName()}...");

                    var name = area.mAreaParams.EnvPaletteSetting.InitPaletteBaseName;

                    if (hasOpened)
                        envPaletteWindow.SavePalette(resource_table, Path.Combine(savePath, "Gyml", "Gfx", "EnvPaletteParam"));

                    area.Save(resource_table, Path.Combine(savePath, "BancMapUnit"), false);
                    area.mAreaParams.Save(resource_table, Path.Combine(savePath, "Stage", "AreaParam"), area.mAreaName, false);

                    if (!Course.IsWorldMap)
                        area.SaveStageParam(resource_table, Path.Combine(savePath));
                }
                resource_table.Save(savePath);
            }
        }

        bool EnsureFileIsWritable(string path)
        {
            if (!File.Exists(path))
                return true;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    return fs.CanWrite;
                }
            }
            catch (IOException e)
            {
                return false;
            }
        }

        public bool HasUnsavedChanges()
        {
            if (!saveStatus)
                return true;
            else
                return false;
        }
        double backupTime = 0;
        public static bool blankLevel = false;
        private string mLayerSearch = "";
        private string prevLayerSearch;
        private int prevCallFrom = 0;
        private string noPrefabsText = "You have no saved prefabs. \nYou can save a prefab by selecting multiple actors,\nright clicking and selecting 'Save as Prefab'. ";
        private bool showActorVisibility;

        public bool checkForEmptyRails()
        {
            foreach (var area in course.GetAreas())
            {
                var rails = area.mRailHolder.mRails;
                foreach (CourseRail rail in rails)
                {
                    if (rail.mPoints.Count == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        class SaveFailureAlert : OkDialog<SaveFailureAlert>
        {
            protected override string Title => "Saving failed";
            protected override void DrawBody(Promise<Void> promise)
            {
                ImGui.Text("The course files may be open in an external app, or Super Mario Bros. Wonder may currently be running in an emulator. \n" +
                    "Close the emulator or external app and try again.");

                if (ImGui.Button("OK"))
                    promise.SetResult(new Void());
            }
        }
    }
}

#endregion