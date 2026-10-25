using Fushigi.course;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Numerics;
using NumVec = System.Numerics.Vector3;
public class Prefab
{
    private static Dictionary<ulong, ulong> hashMapActors = new();
    private static string mPrefabSearch = "";
    private static string prevPrefabSearch;
    public static bool regeneratePrefabList;
    private static bool prevPrefabTab;
    private static List<string> filteredPrefabs = new();
    private static CourseActorHolder prefabActors;

    public static string AppDataPath = Path.Combine(UserSettings.SettingsDir, "prefabs");
    private static string noPrefabsText = "You have no saved prefabs. " +
                                   "\nYou can save a prefab by selecting multiple actors," +
                                   "\nright clicking and selecting 'Save as Prefab'. ";
    private static CourseScene courseScene;

    internal static void SavePrefab(CourseAreaEditContext mEditContext, string prefabName, CourseArea mArea)
    {
        var median = System.Numerics.Vector3.Zero;
        
        if (mEditContext.GetSelectedObjects<CourseActor>().Count() > 0 || mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().Count() > 0)
        {
            List<CourseActor> actors = mEditContext.GetSelectedObjects<CourseActor>().ToList();
            List<CourseActor> copiedActors = new List<CourseActor>();

            List<CourseRail.CourseRailPoint> courseRailPoints = mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().ToList();
            List<CourseRail> courseRails = new List<CourseRail>();
            List<CourseRail> courseRailsClone = new List<CourseRail>();

            foreach (var point in courseRailPoints)
            {
                if (!courseRails.Contains(point.mParent))
                    courseRails.Add(point.mParent);
            }

            foreach (var actor in actors)
                copiedActors.Add(actor.ClonePrefab(mArea));

            foreach (var rail in courseRails)
            {
                courseRailsClone.Add(rail.CloneRail(mArea));
            }

            foreach (CourseActor actor in copiedActors)
                median += actor.mTranslation;

            median /= actors.Count;

            foreach (var actor in copiedActors)
            {
                actor.mTranslation.X -= median.X;
                actor.mTranslation.Y -= median.Y;
            }

            foreach (var rail in courseRailsClone)
            {
                foreach (var point in rail.mPoints)
                {
                    point.mTranslation.X -= median.X;
                    point.mTranslation.Y -= median.Y;

                    if (point.mIsCurve)
                    {
                        point.mControl.mTranslation.X -= median.X;
                        point.mControl.mTranslation.Y -= median.Y;
                    }
                }
            }
            regeneratePrefabList = true;
            mArea.SaveActorsToPrefab(copiedActors, actors, prefabName, courseRailsClone, courseRails);
        }
    }
    public static void ImportPrefab()
    {
        FileDialog dlg = new FileDialog();
        if (dlg.ShowDialog())
        {
            string path = dlg.SelectedPath;

            if (!path.EndsWith(".bcett.byml.zs", StringComparison.OrdinalIgnoreCase))
                return;

            string prefabPath = Path.Combine(AppDataPath, Path.GetFileName(path));
            File.Copy(path, prefabPath, overwrite: true);
            regeneratePrefabList = true;
        }
    }

    public static void ExportPrefab(string prefab, string directory)
    {
        FileDialog dlg = new FileDialog();
        if (dlg.ShowSaveDialog("Export Prefab"))
        {
            string path = dlg.SelectedPath;
            if (!path.EndsWith(".bcett.byml.zs"))
                path += ".bcett.byml.zs";

            string prefabPath = Path.Combine(directory, $"{prefab}.bcett.byml.zs");
            File.Copy(prefabPath, path, overwrite: true);
        }
    }

    internal static async Task PrefabPopup(CourseAreaEditContext mEditContext, CourseArea area)
    {
        var result = await SavePrefabDialog.ShowDialog(MainWindow.mModalHost, "Save Prefab", "Enter name for this prefab");

        if (result.Result == SavePrefabDialog.DialogResult.Yes)
            SavePrefab(mEditContext, result.PrefabName, area);

    }

    internal static void PrefabsView(CourseScene cs)
    {
        courseScene = cs;
        string resPath = "res/prefabs";
        string[] resPrefab = Directory.GetFiles(resPath, "*.bcett*");

        if (ImGui.BeginTabBar("PrefabTabs"))
        {
            if (ImGui.BeginTabItem("User Prefabs"))
            {
                if (ImGui.IsItemClicked())
                    regeneratePrefabList = true;

                if (Directory.Exists(Prefab.AppDataPath))
                {
                    string[] files = Directory.GetFiles(Prefab.AppDataPath, "*.bcett*");
                    prefabList(files, true, Prefab.AppDataPath);
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

    public static void prefabList(string[] files, bool canDelete, string directory)
    {
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.SetWindowFocus(null);

        ImGui.InputText("##PrefabSearch", ref mPrefabSearch, 0x100);
        ImGui.SameLine();

        bool delay = false;
        if (ImGui.Button("Import Prefab"))
        {
            Prefab.ImportPrefab();
            delay = true;
        }

        ImGui.BeginChild("PrefabScroll", ImGui.GetContentRegionAvail());
        float rowHeight = ImGui.GetFrameHeight();
        float deleteButtonWidth = rowHeight * 1.6f;
        bool isSearch = !string.IsNullOrWhiteSpace(mPrefabSearch);

        if (prevPrefabTab != canDelete)
            regeneratePrefabList = true;

        if (files.Length == 0)
            ImGui.Text(noPrefabsText);

        if (prevPrefabSearch != mPrefabSearch || (regeneratePrefabList && !delay))
        {
            regeneratePrefabList = false;
            filteredPrefabs.Clear();
            prevPrefabSearch = mPrefabSearch;

            foreach (string file in files)
            {
                string prefab = Path.GetFileName(file).Split(".bcett")[0];

                var actorEnglish = Translate.FetchTranslatedName(prefab);
                bool HasText = file.IndexOf(mPrefabSearch, StringComparison.OrdinalIgnoreCase) >= 0;

                if (isSearch && !HasText)
                    continue;

                filteredPrefabs.Add(prefab);
            }
            prevPrefabTab = canDelete;
        }

        foreach (var prefab in filteredPrefabs)
        {
            ImGui.PushID(prefab);

            float fullWidth = ImGui.GetContentRegionAvail().X;
            float nameWidth = fullWidth - deleteButtonWidth - 8;

            //ImGui.BeginGroup();
            //ImGui.PushItemWidth(nameWidth);

            if (ImGui.Selectable(prefab, false, ImGuiSelectableFlags.None, new Vector2(nameWidth, rowHeight)))
                LoadPrefab(prefab, directory);

            //ImGui.PopItemWidth();
            //ImGui.EndGroup();

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup($"PrefabExport");

            if (ImGui.BeginPopup($"PrefabExport"))
            {
                if (ImGui.MenuItem("Export Prefab"))
                    Prefab.ExportPrefab(prefab, directory);

                ImGui.EndPopup();
            }

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
                    DeletePrefabPopup(Path.Combine(directory, prefab + ".bcett.byml.zs"));

                ImGui.PopID();
            }
        }
        ImGui.EndChild();
    }

    public static async Task DeletePrefabPopup(string file)
    {
        Console.WriteLine(file);
        var result = await RemoveAreaConfirmationDialog.ShowDialog(MainWindow.mModalHost, "Remove Prefab", "Do you want to remove this prefab?\nThis action cannot be undone!");

        if (result == RemoveAreaConfirmationDialog.DialogResult.Yes)
        {
            File.Delete(file);
            regeneratePrefabList = true;
        }

    }

    public static List<CourseActor> CreatePrefab(NumVec placement, string prefab, byte[] levelBytes, uint areaHash)
    {
        var actorsArray = courseScene.selectedArea.LoadPrefab(prefab, levelBytes, "Actors");
        var linksArray = courseScene.selectedArea.LoadPrefab(prefab, levelBytes, "Links");

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

        var links = courseScene.selectedArea.mLinkHolder.mLinks;
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

    public static List<CourseRail> CreatePrefabRail(NumVec placement, string prefab, byte[] levelBytes, uint areaHash)
    {
        var railsArray = courseScene.selectedArea.LoadPrefab(prefab, levelBytes, "Rails");
        var prefabRails = new CourseRailHolder(railsArray);
        var railLinksArray = courseScene.selectedArea.LoadPrefab(prefab, levelBytes, "ActorToRailLinks");
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

        var links = courseScene.selectedArea.mRailLinksHolder.mLinks;
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


    public static async void LoadPrefab(string prefab, string directory)
    {
        var viewport = courseScene.activeViewport;
        var area = courseScene.selectedArea;
        var ctx = courseScene.areaScenes[courseScene.selectedArea].EditContext;

        NumVec? pos;

        var areaHash = courseScene.selectedArea.mRootHash;
        var areaLinks = courseScene.selectedArea.mLinkHolder;

        KeyboardModifier modifier;
        var mSelectedLayer = "PlayArea1";

        if (!CourseScene.mLayersVisibility.ContainsKey(mSelectedLayer))
        {
            mSelectedLayer = "PlayArea";
            courseScene.AddSelectedLayer(mSelectedLayer);
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

            var actorsArray = courseScene.selectedArea.LoadPrefab(prefab, levelBytes, "Actor");
            var linksArray = courseScene.selectedArea.LoadPrefab(prefab, levelBytes, "Links");

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
                courseScene.AddLayerFromFile(actor.mLayer);

                if (actor.mPackName == "DVBasePosLocator")
                    courseScene.activeViewport.DistantViewScrollManager.Reload(actor, true, courseScene.mEditContext);

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

}
