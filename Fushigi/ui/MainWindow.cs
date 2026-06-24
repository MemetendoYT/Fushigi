﻿using Fushigi.course;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.ui.widgets;
using Fushigi.util;
using Fushigi.windowing;
using ImGuiNET;
using Silk.NET.Core;
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui
{
    public partial class MainWindow : IPopupModalHost
    {
        private readonly GLTaskScheduler mGLTaskScheduler = new();
        public static readonly PopupModalHost mModalHost = new();

        private ImFontPtr mDefaultFont;
        private readonly ImFontPtr mIconFont;

        private static readonly Dictionary<int, RawImage> Icons = [];
        public static bool reloadIni = false;
        public static bool reloadLevel = false;
        public static bool addNewArea = false;
        public static bool removeCurrentArea = false;
        public static float dpiScale = 0;
        public static float backupdpiScale = 0;
        private GL _gl;
        public static GLTexture FushigiIcon;

        public MainWindow()
        {
        
            Logger.Logger.LogMessage("MainWindow", "Loading icons");

            unsafe
            {

                for (int i = 1; i < 10; i++)
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(Path.Combine("res", $"icon{i}.png"));
                    var memoryGroup = image.GetPixelMemoryGroup();
                    Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof(Rgba32)];
                    var block = MemoryMarshal.Cast<byte, Rgba32>(array.Span);
                    foreach (var memory in memoryGroup)
                    {
                        memory.Span.CopyTo(block);
                        block = block[memory.Length..];
                    }

                    Icons.Add(i, new RawImage(image.Width, image.Height, array));
                }
            }

            WindowManager.CreateWindow(out mWindow,
                onConfigureIO: () =>
                {
                    Logger.Logger.LogMessage("MainWindow", "Initializing Window");
                    unsafe
                    {
                        SetWindowIcon(1);

                        var io = ImGui.GetIO();
                        io.ConfigFlags = ImGuiConfigFlags.NavEnableKeyboard;

                        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var iconConfig = ImGuiNative.ImFontConfig_ImFontConfig();
                        var nativeConfigJP = ImGuiNative.ImFontConfig_ImFontConfig();

                        //Add a higher horizontal/vertical sample rate for global scaling.
                        nativeConfig->OversampleH = 8;
                        nativeConfig->OversampleV = 8;
                        nativeConfig->RasterizerMultiply = 1f;
                        nativeConfig->GlyphOffset = new Vector2(0);

                        nativeConfigJP->MergeMode = 1;
                        nativeConfigJP->PixelSnapH = 1;

                        iconConfig->MergeMode = 1;
                        iconConfig->OversampleH = 2;
                        iconConfig->OversampleV = 2;
                        iconConfig->RasterizerMultiply = 1f;
                        iconConfig->GlyphOffset = new Vector2(0);


                        [DllImport("User32.dll")]
                        static extern uint GetDpiForWindow(IntPtr hWnd);

                        var native = mWindow.Native;
                        _gl = GL.GetApi(mWindow);
                        FushigiIcon = GLTexture2D.Load(_gl, "res/icon_menu.png");

                        IntPtr hwnd = native.Win32!.Value.Hwnd;

                        uint dpi = GetDpiForWindow(hwnd);
                        backupdpiScale = dpi;

                        if(UserSettings.GetDPIOverride()) {
                            dpi = (uint)(96 * UserSettings.GetDPIVal());
                        }

                        if (dpi == 0)
                            dpi = 96;

                        dpiScale = dpi / 96f;
                        io.ConfigWindowsMoveFromTitleBarOnly = true;

                        ImGui.GetStyle().ScaleAllSizes(dpiScale);

                        float size = 16f * dpiScale;
                        backupSize = size;
                        CourseSelect.thumbnailSize *= dpiScale;
                        mDefaultFont = io.Fonts.AddFontFromFileTTF(
                            Path.Combine("res", "Font.ttf"),
                            size, nativeConfig, io.Fonts.GetGlyphRangesDefault());

                        io.Fonts.AddFontFromFileTTF(
                            Path.Combine("res", "NotoSansCJKjp-Medium.otf"),
                            size, nativeConfigJP, io.Fonts.GetGlyphRangesJapanese());

                        io.Fonts.Build();

                        //other fonts go here and follow the same schema
                        GCHandle rangeHandle = GCHandle.Alloc(new ushort[] { IconUtil.MIN_GLYPH_RANGE, IconUtil.MAX_GLYPH_RANGE, 0 }, GCHandleType.Pinned);
                        try
                        {
                            io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "la-regular-400.ttf"),
                                size, iconConfig, rangeHandle.AddrOfPinnedObject());

                            io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "la-solid-900.ttf"),
                                size, iconConfig, rangeHandle.AddrOfPinnedObject());

                            io.Fonts.AddFontFromFileTTF(
                                Path.Combine("res", "la-brands-400.ttf"),
                                size, iconConfig, rangeHandle.AddrOfPinnedObject());

                            io.Fonts.Build();
                        }
                        finally
                        {
                            if (rangeHandle.IsAllocated)
                                rangeHandle.Free();
                        }
                    }
                });
            mWindow.Load += () => WindowManager.RegisterRenderDelegate(mWindow, Render);
            mWindow.Closing += Close;
        }
        public void ReloadRomfs()
        {
            if (UserSettings.GetRomfsReload() && UserSettings.GetAllowRomfsReload())
            {
                string romFSPath = UserSettings.GetRomFSPath();
                UserSettings.SetRomfsReload(false);
                    Task.Run(async () =>
                    {

                        if (mCurrentCourseName is null)
                            return;

                        if (await TryCloseCourse())
                        {
                           await ProgressBarDialog.ShowDialogForAsyncAction(
                           this,
                           "Preloading Thumbnails",
                           async (p) =>
                           {
                               await mModalHost.WaitTick();
                               await mGLTaskScheduler.Schedule(gl => RomFS.SetRoot(romFSPath, gl));
                           });
                            await LoadParamDBWithProgressBar(this);
                            Logger.Logger.LogMessage("MainWindow", $"Reload course {mCurrentCourseName}!");
                            await LoadCourseWithProgressBar(mCurrentCourseName);
                            BfresCache.Clear();
                            UserSettings.AppendRecentCourse(mCurrentCourseName);
                            CourseScene.saveStatus = true;
                        }
                    }).ConfigureAwait(false);
                }
              else
                UserSettings.SetRomfsReload(false);
              }
        public void SetWindowIcon(int id)
        {
            var icon = Icons[id];
            mWindow.SetWindowIcon(ref icon);
        }

        public async Task<bool> TryCloseCourse()
        {
            if (mSelectedCourseScene is not null &&
                mSelectedCourseScene.HasUnsavedChanges())
            {
                var result = await CloseConfirmationDialog.ShowDialog(this);

                if (result == CloseConfirmationDialog.DialogResult.Yes)
                {
                    mSelectedCourseScene = null;
                    return true;
                }
                else
                    return false;
            }

            return true;
        }

        public async Task<bool> TrySaveCourse()
        {
            if (mSelectedCourseScene is not null)
            {
                var result = await RailConfirmationDialog.ShowDialog(this);

                if (result == RailConfirmationDialog.DialogResult.Yes)
                {
                    mSelectedCourseScene.deleteEmptyRails();
                    return true;
                }
                else if (result == RailConfirmationDialog.DialogResult.AutoDelete)
                { 
                    mSelectedCourseScene.deleteEmptyRails();
                    return true;
                }
                else
                    return false;
            }

            return true;
        }

        public async Task<bool> ResetCourse()
        {
            if (mSelectedCourseScene is not null)
            {
                var result = await ResetConfirmationDialog.ShowDialog(this);

                if (result == ResetConfirmationDialog.DialogResult.Yes)
                {
                    return true;
                }
                else
                    return false;
            }

            return true;
        }

        public async Task<bool> RemoveArea()
        {
            if (mSelectedCourseScene is not null)
            {
                var result = await RemoveAreaConfirmationDialog.ShowDialog(this, "Remove Area", "Do you want to remove this area?\nThis action cannot be undone.");

                if (result == RemoveAreaConfirmationDialog.DialogResult.Yes)
                {
                    return true;
                }
                else
                    return false;
            }

            return true;
        }


        bool mSkipCloseTest = false;
        public void Close()
        {
            //prevent infinite loop
            if (mSkipCloseTest)
            {
                UserSettings.Save();
                return;
            }

            mWindow.IsClosing = false;

            Task.Run(async () =>
            {
                if(await TryCloseCourse())
                {
                    mSkipCloseTest = true;
                    mWindow.Close();
                }
            }).ConfigureAwait(false); //fire and forget
        }

        public static bool isRegenerate(bool Regenerate) {
            return Regenerate;
        }
        
        //TODO put this somewhere else
        public static Task LoadParamDBWithProgressBar(IPopupModalHost modalHost)
        {
            isRegenerate(true);
            return ProgressBarDialog.ShowDialogForAsyncAction(modalHost,
                    "Loading ParamDB",
                    async (p) =>
                    {
                        p.Report(("Creating task", 0));
                        await modalHost.WaitTick();
                        var task = ParamDB.sIsInit ? 
                        Task.Run(() => ParamDB.Reload(p)) : 
                        Task.Run(() => ParamDB.Load(p));
                        await task;
                    });
        }

        
        public async Task StartupRoutine()
        {
            await WaitTick();

            bool shouldShowPreferenceWindow = true;
            bool shouldShowWelcomeDialog = true;
            string romFSPath = UserSettings.GetRomFSPath();
            if (RomFS.IsValidRoot(romFSPath))
            {
                await ProgressBarDialog.ShowDialogForAsyncAction(this,
                    "Preloading Thumbnails",
                    async (p) =>
                    {
                        await mModalHost.WaitTick();
                        await mGLTaskScheduler.Schedule(gl => RomFS.SetRoot(romFSPath, gl));
                    });
                ChildActorParam.Load();

                if (!ParamDB.sIsInit)
                {
                    Console.WriteLine("Parameter database needs to be initialized...");

                    await LoadParamDBWithProgressBar(this);
                    await Task.Delay(500); 
                }

                string? latestCourse = UserSettings.GetLatestCourse();
                if (latestCourse != null && ParamDB.sIsInit)
                {
                    //wait for other pending dialogs to close
                    await mModalHost.WaitTick();
                    
                    await LoadCourseWithProgressBar(latestCourse);
                    shouldShowPreferenceWindow = false;
                    shouldShowWelcomeDialog = false;
                }
            }

            ActorIconLoader.Init();

            if (!string.IsNullOrEmpty(RomFS.GetRoot()) &&
                !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
            {
                shouldShowPreferenceWindow = false;
                shouldShowWelcomeDialog = false;
            }

            if(shouldShowPreferenceWindow)
                mIsShowPreferenceWindow = true;

             if(shouldShowWelcomeDialog)
                await WelcomeMessage.ShowDialog(this);
        }
        public async Task reloadAfterAreaReset()
        {
            if (mCurrentCourseName is null)
                return;

            if (!await ResetCourse())
                return;

            await ProgressBarDialog.ShowDialogForAsyncAction(this,
                $"Loading {mCurrentCourseName}",
                async (p) =>
                {
                    mSelectedCourseScene.overwriteLevel(CourseScene.currentArea, mGLTaskScheduler);

                    Logger.Logger.LogMessage("MainWindow", $"Reload course {mCurrentCourseName}!");

                    var currentCourse = mSelectedCourseScene.course;

                    mSelectedCourseScene = await CourseScene.Create(
                        currentCourse,
                        mGLTaskScheduler,
                        mModalHost,
                        p);

                    CourseScene.saveStatus = true;
                });
        }

        public async Task removeArea()
        {
            await Task.Yield();
            if (mCurrentCourseName is null)
                return;

            if (!await RemoveArea())
                return;

            CourseScene.saveStatus = false;
            var selectedArea = mSelectedCourseScene.selectedArea;
            string areaName = selectedArea.GetName();
            mSelectedCourseScene.DeleteAreaFiles(areaName);
            mSelectedCourseScene.course.GetAreas().Remove(selectedArea);
            //mSelectedCourseScene.course.renameArea();
            //Course.updateStageParam = true;
        }

        public Task LoadCourseWithProgressBar(string name)
        {
            return ProgressBarDialog.ShowDialogForAsyncAction(this,
                    $"Loading {name}",
                    async (p) =>
                    {
                        p.Report(("Loading course files", null));
                        await mModalHost.WaitTick();
                        bool isWorldMap = false;
                        Course.SetIsWorldMap(false);
                        if (name.Contains("World00"))
                        {
                            isWorldMap = true;
                            Course.SetIsWorldMap(true);
                        }
                        var course = new Course(name);
                        p.Report(("Loading other resources (this temporarily freezes the app)", null));
                        await mModalHost.WaitTick();

                        mSelectedCourseScene?.PreventFurtherRendering();
                        mSelectedCourseScene = await CourseScene.Create(course, mGLTaskScheduler, mModalHost, p);
                        mCurrentCourseName = name;
                        //Console.WriteLine(name.Split("_")[0]);
                        //mCurrentLevelName = RomFS.courseNames[name.Split("_")[0]];
                        //Console.WriteLine(mCurrentLevelName);
                    });
        }
        public static async Task UpdateEnglishNamesFromGitHub()
        {
            string hashPath = "res/translation_hash.txt";
            string jsonPath = "res/EnglishNames.json";

            string commitApiUrl =
                "https://api.github.com/repos/MemetendoYT/Fushigi-TranslationJSON/commits/main";


            string JsonUrl =
                "https://raw.githubusercontent.com/MemetendoYT/Fushigi-TranslationJSON/main/EnglishNames.json";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FushigiTool/1.0");

            string remoteJson = await client.GetStringAsync(commitApiUrl);
            using var doc = JsonDocument.Parse(remoteJson);
            string remoteHash = doc.RootElement.GetProperty("sha").GetString();


            string localHash = null;
            if (File.Exists(hashPath))
                localHash = File.ReadAllText(hashPath).Trim();

            if (localHash != remoteHash)
            {
                string json = await client.GetStringAsync(JsonUrl);

                Directory.CreateDirectory("res");

                File.WriteAllText(jsonPath, json);


                File.WriteAllText(hashPath, remoteHash);

                Console.WriteLine("Translation JSON updated successfully.");
                CourseScene.refreshTranslation = true;
                return;
            }

            Console.WriteLine("No translation updates available.");
        }

        void DrawMainMenu()
        {

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu(" "))
                {
                    if (ImGui.MenuItem("Change Mode"))
                        EditorMode.Draw(this);

                    if (ImGui.MenuItem("Settings"))
                        mIsShowPreferenceWindow = true;

                    ImGui.EndMenu();
                }

                float buttonSize = ImGui.GetFrameHeight();
                float iconSize = buttonSize * 0.75f;

                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();

                Vector2 iconMin = new Vector2(
                    min.X + (buttonSize - iconSize) * 0.5f,
                    min.Y + (buttonSize - iconSize) * 0.5f
                );

                Vector2 iconMax = iconMin + new Vector2(iconSize, iconSize);


                ImGui.GetWindowDrawList().AddImage(
                    (IntPtr)FushigiIcon.ID,
                    iconMin,
                    iconMax
                );


                if (ImGui.BeginMenu("File"))
                {

                    if (EditorMode.editMode == "Level Editor")
                    {
                        if (!string.IsNullOrEmpty(RomFS.GetRoot()) &&
                            !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                        {
                            if (ImGui.MenuItem("Open Course"))
                            {
                                Task.Run(async () =>
                                {
                                    string? selectedCourse = await CourseSelect.ShowDialog(this, mCurrentCourseName);

                                    if (selectedCourse is null || mCurrentCourseName == selectedCourse)
                                        return;

                                    if (await TryCloseCourse())
                                    {
                                        mCurrentCourseName = selectedCourse;

                                        Logger.Logger.LogMessage("MainWindow", $"Selected course {mCurrentCourseName}!");
                                        await LoadCourseWithProgressBar(mCurrentCourseName);
                                        UserSettings.AppendRecentCourse(mCurrentCourseName);
                                        CourseScene.saveStatus = true;
                                    }
                                }).ConfigureAwait(false); //fire and forget
                            }

                            // Reload Course
                            if (ImGui.MenuItem("Reload Course"))
                            {
                                Task.Run(async () =>
                                {
                                    if (mCurrentCourseName is null)
                                        return;

                                    if (await TryCloseCourse())
                                    {
                                        Logger.Logger.LogMessage("MainWindow", $"Reload course {mCurrentCourseName}!");
                                        await LoadCourseWithProgressBar(mCurrentCourseName);
                                        UserSettings.AppendRecentCourse(mCurrentCourseName);
                                        CourseScene.saveStatus = true;
                                    }
                                }).ConfigureAwait(false); //fire and forget
                            }
                        }

                        if (ImGui.MenuItem("Rename Course"))
                            RenameLevel();


                        ImGui.Separator();

                        /* Saves the currently loaded course */
                        var text_color = mSelectedCourseScene == null ?
                                 ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled] :
                                 ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

                        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(text_color));

                        if (ImGui.MenuItem("Save") && mSelectedCourseScene != null)
                        {
                            //Ensure the romfs path is set for saving
                            if (!string.IsNullOrEmpty(UserSettings.GetModRomFSPath()))
                            {
                                if (mSelectedCourseScene.attemptSave() && !UserSettings.GetDeleteEmptyRails())
                                {
                                    Task.Run(async () =>
                                    {
                                        if (await TrySaveCourse())
                                            mSelectedCourseScene.Save(false);
                                        else
                                            return;
                                    }).ConfigureAwait(false);
                                }
                                else if (UserSettings.GetDeleteEmptyRails())
                                {
                                    mSelectedCourseScene.deleteEmptyRails();
                                }
                                mSelectedCourseScene.Save(false);
                            }
                            else //Else configure the mod path
                            {
                                FolderDialog dlg = new FolderDialog();
                                if (dlg.ShowDialog("Select the romfs directory to save to."))
                                {
                                    Logger.Logger.LogMessage("MainWindow", $"Setting RomFS path to {dlg.SelectedPath}");
                                    UserSettings.SetModRomFSPath(dlg.SelectedPath);
                                    mSelectedCourseScene.Save(false);
                                }
                            }
                        }
                        if (ImGui.MenuItem("Save As") && mSelectedCourseScene != null)
                        {
                            FolderDialog dlg = new FolderDialog();
                            if (dlg.ShowDialog("Select the romfs directory to save to."))
                            {
                                UserSettings.SetModRomFSPath(dlg.SelectedPath);
                                mSelectedCourseScene.Save(false);
                            }
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Blank out baked collisions") && mSelectedCourseScene != null)
                        {
                            string directory = Path.Combine(UserSettings.GetModRomFSPath(), "Phive", "StaticCompoundBody");

                            if (!Directory.Exists(directory))
                                Directory.CreateDirectory(directory);

                            foreach (var area in mSelectedCourseScene.GetCourse().GetAreas())
                            {
                                var filePath = Path.Combine(directory, $"{area.GetName()}.Nin_NX_NVN.bphsc.zs");
                                File.Copy(Path.Combine(AppContext.BaseDirectory, "res", "BlankStaticCompoundBody.bphsc.zs"),
                                    filePath, overwrite: true);
                            }
                        }

                        ImGui.Separator();

                        if (ImGui.MenuItem("Reset Area"))
                        {
                            CourseScene.blankLevel = true;
                        }

                        if (ImGui.MenuItem("Use this area as template"))
                        {
                            var area = mSelectedCourseScene.selectedArea;
                            area.mAreaParams.Save(null, "", "", true);
                            area.Save(null, "", true);


                        }
                        ImGui.PopStyleColor();

                    }

                    if (EditorMode.editMode == "Collision")
                    {

                        if (ImGui.MenuItem("Export Collision"))
                        {
                            FileDialog dlg = new FileDialog();
                            if (dlg.ShowSaveDialog("Export Collision"))
                            {
                                string path = dlg.SelectedPath;

                                if (!path.EndsWith(".bgyml"))
                                    path += ".bgyml";

                                CollisionEditor.Save(path);
                            }
                        }

                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Update Translation JSON"))
                    {
                        UpdateEnglishNamesFromGitHub();
                    }

                    ImGui.Separator();

                    /* a ImGUI menu item that just closes the application */
                    if (ImGui.MenuItem("Close"))
                        mWindow.Close();

                    /* end File menu */
                    ImGui.EndMenu();
                }
            
                if (ImGui.BeginMenu("Edit"))
                {

                    if (ImGui.MenuItem("Undo"))
                        mSelectedCourseScene?.Undo();

                    if (ImGui.MenuItem("Redo"))
                        mSelectedCourseScene?.Redo();

                    ImGui.Separator();

                    if (ImGui.MenuItem("Reset User Interface"))
                        reloadIni = true;

                    if (ImGui.MenuItem("Regenerate Parameter Database", ParamDB.sIsInit))
                    {
                        _ = LoadParamDBWithProgressBar(this);
                    }

                    /* end Edit menu */
                    ImGui.EndMenu();
                }

                /* end entire menu bar */
                ImGui.EndMenuBar();
            }
        }

        
        public async Task RenameLevel()
        {
            var result = await SavePrefabDialog.ShowDialog(MainWindow.mModalHost, "Rename Course", "Enter new name for course");

            if (result.Result == SavePrefabDialog.DialogResult.Yes)
            {
                mSelectedCourseScene.course.SetName(result.PrefabName + "_Course");
                mSelectedCourseScene.course.renameAreaFromMenu();
            }

        }

     
        public async Task AddArea()
        {
            addNewArea = false;
            mSelectedCourseScene.course.AddArea();
            await mSelectedCourseScene.RebuildAreaData(mGLTaskScheduler, mSelectedCourseScene.course.GetAreas().Last());

        }
        public void Render(GL gl, double delta, ImGuiController controller)
        {
            mGLTaskScheduler.ExecutePending(gl);

            /* keep OpenGLs viewport size in sync with the window's size */
            gl.Viewport(mWindow.FramebufferSize);
            ReloadRomfs();
            gl.ClearColor(.45f, .55f, .60f, 1f);
            gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            ImGui.DockSpaceOverViewport();

            //only works after the first frame
            if (EditorMode.editMode == "")
                EditorMode.Draw(this);

            if (ImGui.GetFrameCount() == 2)
            {
                ImGui.LoadIniSettingsFromDisk("imgui.ini");
                //_ = EditorRoutine();
                //_ = StartupRoutine();
            }


            if (reloadLevel)
            {
                reloadLevel = false;
                reloadAfterAreaReset();
            }
            DrawMainMenu();

            if(addNewArea)
            {
                AddArea();

            }

            if (removeCurrentArea)
            {
                removeArea();
                removeCurrentArea = false;
            }

            if (!string.IsNullOrEmpty(RomFS.GetRoot()) &&
                !string.IsNullOrEmpty(UserSettings.GetModRomFSPath()) && EditorMode.editMode == "Level Editor")
            {
                mSelectedCourseScene?.DrawUI(gl, delta);
            }

            if(EditorMode.editMode != "")
            {

                if (EditorMode.editMode == "Collision")
                {
                    collisionEditor.Draw(mGLTaskScheduler, delta);
                }

                if (EditorMode.editMode == "MSBT")
                {
                    msbtEditor.Draw();
                }

                if (EditorMode.editMode == "SaveData")
                {
                    saveEditor.Draw();
                }
            }

            if (mIsShowPreferenceWindow)
                Preferences.Draw(ref mIsShowPreferenceWindow, mGLTaskScheduler, this);

            mModalHost.DrawHostedModals();

            //Update viewport from any framebuffers being used
            if (reloadIni)
            {
                ImGui.LoadIniSettingsFromDisk("res/imgui-default.ini");
                reloadIni = false;
            }


            gl.Viewport(mWindow.FramebufferSize);

            /* render our ImGUI controller */
            controller.Render();
        }

        public Task<(bool wasClosed, TResult result)> ShowPopUp<TResult>(IPopupModal<TResult> modal,
            string title,
            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None,
            Vector2? minWindowSize = null)
        {
            return mModalHost.ShowPopUp(modal, title, windowFlags, minWindowSize);
        }

        public Task WaitTick() => ((IPopupModalHost)mModalHost).WaitTick();

        readonly IWindow mWindow;
        public static string? mCurrentCourseName;
        public static string mCurrentLevelName = "";
        CourseScene? mSelectedCourseScene;
        CollisionEditor? collisionEditor = new CollisionEditor();
        MsbtEditor? msbtEditor = new MsbtEditor();
        SaveEditor? saveEditor = new SaveEditor();
        bool mIsShowPreferenceWindow = false;
        public static float backupSize;
    }
}