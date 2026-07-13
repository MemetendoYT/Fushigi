using Fasterflect;
using Fushigi.actor_pack.components;
using Fushigi.Bfres;
using Fushigi.Byml.Serializer;
using Fushigi.course;
using Fushigi.course.distance_view;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.gl.Bfres.AreaData;
using Fushigi.param;
using Fushigi.ui.SceneObjects.bgunit;
using Fushigi.ui.undo;
using Fushigi.util;
using ImGuiNET;
using Silk.NET.OpenGL;
using System.Data;
using System.Drawing;
using System.IO;
using System.Numerics;
using static Fushigi.course.CourseUnit;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Fushigi.ui.widgets
{

    [Flags]
    enum KeyboardModifier
    {
        None = 0,
        Shift = 1,
        CtrlCmd = 2,
        Alt = 4
    }

    internal class LevelViewport(CourseArea area, GL gl, CourseAreaScene areaScene)
    {
        #region Variables and Properties
        public event Action<IReadOnlyList<object>>? ObjectDeletionRequested;

        public readonly CourseArea mArea = area;
        private readonly CourseAreaEditContext mEditContext = areaScene.EditContext;
        private LevelViewportWM mWorldMapVP = new LevelViewportWM();
        private Sprites sprite = new Sprites(gl);
        public Vector2 mSize = Vector2.Zero;
        public Vector2 mTopLeft = Vector2.Zero;
        public Vector2 vpMin;
        public Vector2 vpMax;
        public bool IsViewportHovered;
        public bool IsViewportActive;
        public bool isPanGesture;
        public ImDrawListPtr mDrawList;
        public static List<string> HiddenModels = new();
        public bool PlayAnimations = false;
        public bool ShowGrid = true;
        public bool ShowBackground = true;
        public bool ShowActors = true;
        bool pasteContext = false;
        bool copyContext = false;
        bool deleteContext = false;
        public static CourseScene _courseScene;
        public void PreventFurtherRendering() => mIsNoMoreRendering = true;

        public bool dragRelease;
        List<object> newSelection = new();
        public List<CourseActor> BgUnits = new();
        public static object?[] CopiedObjects = [];
        public static Vector3 CopiedMedianPosition;
        public bool ScreenshotMode;
        public object? mHoveredObject;
        private object? lastHoveredObject;

        public Camera Camera = new Camera();
        public GLFramebuffer Framebuffer; //Draws opengl data into the viewport
        public HDRScreenBuffer HDRScreenBuffer = new HDRScreenBuffer();
        public TileBfresRender TileBfresRenderFieldA;
        public TileBfresRender TileBfresRenderFieldB;
        public AreaResourceManager EnvironmentData = new AreaResourceManager(gl, area.mInitEnvPalette);
        private bool mIsNoMoreRendering = false;
        public static bool updateSkinA = false;
        public static bool updateSkinB = false;
        public IDictionary<string, bool>? mLayersVisibility;
        DistantViewManager DistantViewScrollManager = new DistantViewManager(area);

        List<CourseActor> backupSelection;
        public Vector2 storedMousePos;
        Vector2? mMultiSelectStartPos;
        Vector2? mMultiSelectCurrentPos;
        public bool mMultiSelecting = false;
        bool mMultiSelectEnded = true;

        public static uint GridColor = 0x77_FF_FF_FF;
        public static float GridLineThickness = 1.5f;
        public static uint MultiSelectBoxColor = 0x90_00_00_FF;
        public static float MultiSelectBoxThickness = 5f;

        public static bool setGlobalSrc;
        public static bool setGlobalDst;
        public static ulong globalHash;
        public bool panOverride = false;

        public FushigiCursor cursor;
        private List<(BGUnitRail Rail, BGUnitRail.RailPoint Point)> deleteList2;
        private List<CourseUnit> rebuildList;
        private bool CommitObjectTranslation;
        private Vector3 startPosWorld;
        private Vector3 currentPosWorld;
        #endregion

        #region Picking Logic
        public (string message, Predicate<object?> predicate,
            TaskCompletionSource<(object? picked, KeyboardModifier modifiers)> promise)?
            mObjectPickingRequest = null;
        public (string message, string layer, TaskCompletionSource<(Vector3? picked, KeyboardModifier modifiers)> promise)?
            mPositionPickingRequest = null;
        public bool tileRebuild;
        private bool hasInitialized;
        public KeyboardModifier modifiers;


        public Task<(object? picked, KeyboardModifier modifiers)> PickObject(string tooltipMessage,
            Predicate<object?> predicate, CancellationTokenSource tokenSource)
        {
            CancelOngoingPickingRequests(tokenSource);
            var promise = new TaskCompletionSource<(object? picked, KeyboardModifier modifiers)>();
            mObjectPickingRequest = (tooltipMessage, predicate, promise);
            return promise.Task;
        }

        public Task<(Vector3? picked, KeyboardModifier modifiers)> PickPosition(string tooltipMessage, string layer, CancellationTokenSource tokenSource)
        {
            CancelOngoingPickingRequests(tokenSource);
            var promise = new TaskCompletionSource<(Vector3? picked, KeyboardModifier modifiers)>();
            mPositionPickingRequest = (tooltipMessage, layer, promise);
            return promise.Task;
        }

        private void CancelOngoingPickingRequests(CancellationTokenSource tokenSource)
        {
            if (mObjectPickingRequest.TryGetValue(out var objectPickingRequest))
            {
                tokenSource.Cancel();
                mObjectPickingRequest = null;
            }
            if (mPositionPickingRequest.TryGetValue(out var positionPickingRequest))
            {
                tokenSource.Cancel();
                mPositionPickingRequest = null;
            }
        }
        #endregion

        #region Camera Related Methods
        public Matrix4x4 GetCameraMatrix() => Camera.ViewProjectionMatrix;

        public Vector2 GetCameraSizeIn2DWorldSpace()
        {
            var cameraBoundsSize = ScreenToWorld(mSize) - ScreenToWorld(new Vector2(0));
            return new Vector2(cameraBoundsSize.X, Math.Abs(cameraBoundsSize.Y));
        }

        public Vector2 WorldToScreen(Vector3 pos) => WorldToScreen(pos, out _);
        public Vector2 WorldToScreen(Vector3 pos, out float ndcDepth)
        {
            var ndc = Vector4.Transform(pos, Camera.ViewProjectionMatrix);
            ndc /= ndc.W;

            ndcDepth = ndc.Z;

            return mTopLeft + new Vector2(
                (ndc.X * .5f + .5f) * mSize.X,
                (1 - (ndc.Y * .5f + .5f)) * mSize.Y
            );
        }
        public Vector3 ScreenToWorld(Vector2 pos, float ndcDepth = 0)
        {
            pos -= mTopLeft;

            var ndc = new Vector3(
                (pos.X / mSize.X) * 2 - 1,
                (1 - (pos.Y / mSize.Y)) * 2 - 1,
                ndcDepth
            );

            var world = Vector4.Transform(ndc, Camera.ViewProjectionMatrixInverse);
            world /= world.W;

            return new(world.X, world.Y, world.Z);
        }
        public void FrameSelectedActor(CourseActor actor)
        {
            this.Camera.Target = new Vector3(actor.mTranslation.X, actor.mTranslation.Y, 0);
        }
        public void FrameSelectedComment(CourseComment comment)
        {
            this.Camera.Target = new Vector3(comment.mTranslation.X, comment.mTranslation.Y, 0);
        }
        public void SelectedActor(CourseActor actor)
        {
            if (ImGui.GetIO().KeyShift || ImGui.GetIO().KeyCtrl)
            {
                mEditContext.Select(actor);
            }
            else
            {
                mEditContext.DeselectAll();
                mEditContext.Select(actor);
            }
        }
        public void isInMultiSelectBox(Vector2 pos, Transformable obj)
        {
            bool inBox = pos.X > startPosWorld.X &&
                   pos.X < currentPosWorld.X &&
                   pos.Y > startPosWorld.Y &&
                   pos.Y < currentPosWorld.Y;

            if (inBox && !newSelection.Contains(obj))
                newSelection.Add(obj);
            else if (!inBox && newSelection.Contains(obj))
                newSelection.Remove(obj);
        }

        public void HandleCameraControls(double deltaSeconds)
        { 
            isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle) && !ImGui.GetIO().KeyCtrl ||
                (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyShift &&
                mHoveredObject == null && !mEditContext.IsSelected(mHoveredObject) && !dragRelease);

            if (IsViewportActive && isPanGesture)
            {
                Camera.Target += ScreenToWorld(ImGui.GetMousePos() - ImGui.GetIO().MouseDelta) -
                    ScreenToWorld(ImGui.GetMousePos());
            }
            var io = ImGui.GetIO();

            if ((IsViewportHovered || panOverride) && !ImGui.IsPopupOpen("ViewportContextMenu"))
            {

                if (!ImGui.GetIO().KeyCtrl)
                {
                    if(!CourseComment.draggingComment && !panOverride)
                    Camera.Distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);
                }
                else if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
                {
                    Vector2 delta = ImGui.GetIO().MouseDelta;
                    Camera.Distance *= MathF.Pow(2, delta.Y / 200f);
                }

                 // Default camera distance is 10, so speed is constant until 0.5 at 20
                const float baseCameraSpeed = 0.25f * 60;
                const float scalingRate = 10.0f;
                var zoomSpeedFactor = Math.Max(Camera.Distance / scalingRate, 1);
                var zoomedCameraSpeed = MathF.Floor(zoomSpeedFactor) * baseCameraSpeed;
                var dt = (float)deltaSeconds;

                if (!io.WantTextInput)
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftArrow) || ImGui.IsKeyDown(ImGuiKey.A) && !ImGui.GetIO().KeyCtrl)
                        Camera.Target.X -= zoomedCameraSpeed * dt;

                    if (ImGui.IsKeyDown(ImGuiKey.RightArrow) || ImGui.IsKeyDown(ImGuiKey.D) && !ImGui.GetIO().KeyCtrl)
                        Camera.Target.X += zoomedCameraSpeed * dt;

                    if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.W) && !ImGui.GetIO().KeyCtrl)
                        Camera.Target.Y += zoomedCameraSpeed * dt;

                    if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.S) && !ImGui.GetIO().KeyCtrl)

                        Camera.Target.Y -= zoomedCameraSpeed * dt;
                }
            }

            if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))
                panOverride = false;
        }
        #endregion
        #region Translation Handling

        // Handle Translation for all objects 
        public bool IsTransformableSelected()
        {
            return mEditContext.IsAnySelected<Transformable>() ||
                   mEditContext.IsAnySelected<DefaultShape>();
        }
        public void HandleTranslation(object obj, Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            switch (obj)
            {
                case Transformable:
                    foreach (Transformable transformable in mEditContext.GetSelectedObjects<Transformable>())
                    {
                        TransformObjects(transformable, StartingTrans, CurrentTrans);
                        if(transformable is CourseRail.CourseRailPoint point && point.mIsCurve)
                            TransformObjects(point.mControl, StartingTrans, CurrentTrans);
                        if (transformable is BGUnitRail.RailPoint unitRail)
                        {
                            var unit = unitRail.mRail.mCourseUnit;

                            if (!rebuildList.Contains(unit))
                                rebuildList.Add(unit);
                        }
                    }
                    break;
                case DefaultShape:
                    CollisionEditor.HandleShapeTranslation(StartingTrans, CurrentTrans, mEditContext);
                    break;
            }
        }
        public void TransformObjects(Transformable transformable, Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            Vector3 relativePos = transformable.mStartingTrans - StartingTrans;
            if (transformable is BGUnitRail.RailPoint)
            {
                float newX = CurrentTrans.X + relativePos.X;
                float newY = CurrentTrans.Y + relativePos.Y;

                //if (UserSettings.GetEnableHalfTile())
                //{
                //    newX = MathF.Round(newX * 2, MidpointRounding.AwayFromZero) / 2;
                //    newY = MathF.Round(newY * 2, MidpointRounding.AwayFromZero) / 2;
                //}
                //else
                //{
                newX = MathF.Round(newX, MidpointRounding.AwayFromZero);
                newY = MathF.Round(newY, MidpointRounding.AwayFromZero);
                //}

                transformable.mTranslation.X = newX;
                transformable.mTranslation.Y = newY;
            }
            else
            {
                transformable.mTranslation.X = CurrentTrans.X + relativePos.X;
                transformable.mTranslation.Y = CurrentTrans.Y + relativePos.Y;
                if (Course.IsWorldMap)
                    transformable.mTranslation.Z = CurrentTrans.Z + relativePos.Z;
            }
        }
        public string GetTransformableType(Transformable transformable)
        {
            string label = "";
            switch (transformable)
            {
                case CourseActor actor:
                    label = actor.GetFieldValue("mPackName").ToString();
                    break;
                case CourseRail.CourseRailPoint point:
                    label = "Rail Point";
                    break;
                case CourseRail.CourseRailPointControl point:
                    label = "Rail Point Control";
                    break;
                case BGUnitRail.RailPoint point:
                    label = "Terrain point";
                    break;
            }

            return label;
        }
        public void CommitTranslation(Transformable transformable)
        {
            if (transformable != null)
            {
                mEditContext.CommitAction(new PropertyFieldsSetUndo(
                     transformable,
                     [("mTranslation", transformable.GetFieldValue("mStartingTrans"))],
                     "Move Transformable"));
            }
        }
        public void CommitRotation(CourseActor actor)
        {
            mEditContext.CommitAction(new PropertyFieldsSetUndo(
                actor,
                [("mRotation", actor.GetFieldValue("mStartingRot"))],
                $"{IconUtil.ICON_ARROWS_ALT} Pivot {string.Join(", ", actor.mPackName)}"));
        }

        public Vector3 CalcPosVec(Vector3 startingTrans)
        {
            Vector3 posVec = ScreenToWorld(ImGui.GetMousePos());
            posVec -= ScreenToWorld(ImGui.GetIO().MouseClickedPos[0]) - startingTrans;

            if (!ImGui.GetIO().KeyShift)
            {
                posVec.X = MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2;
                posVec.Y = MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2;
                if (Course.IsWorldMap || EditorMode.editMode == "Collision")
                    posVec.Z = MathF.Round(posVec.Z * 2, MidpointRounding.AwayFromZero) / 2;
                if (!ImGui.GetIO().KeyAlt)
                {
                    posVec.X += startingTrans.X - MathF.Round(startingTrans.X * 2, MidpointRounding.AwayFromZero) / 2;
                    posVec.Y += startingTrans.Y - MathF.Round(startingTrans.Y * 2, MidpointRounding.AwayFromZero) / 2;
                    if (Course.IsWorldMap || EditorMode.editMode == "Collision")
                        posVec.Z += startingTrans.Z - MathF.Round(startingTrans.Z * 2, MidpointRounding.AwayFromZero) / 2;
                }
            }

            return posVec;
        }
        #endregion

        #region Rendering Logic
        public static void DefaultSelect(CourseAreaEditContext ctx, object selectable)
        {
            if (ImGui.GetIO().KeyShift || ImGui.GetIO().KeyCtrl)
                ctx.Select(selectable);

            else if (!ctx.IsSelected(selectable))
            {
                ctx.WithSuspendUpdateDo(() =>
                {
                    ctx.DeselectAll();
                    ctx.Select(selectable);
                });
            }

            foreach (Transformable transformable in ctx.GetSelectedObjects<Transformable>())
            {
                transformable.mStartingTrans = transformable.mTranslation;
                switch (transformable)
                {
                    case CourseActor actor:
                        actor.mStartingRot = actor.mRotation;
                        break;
                    case CourseRail.CourseRailPoint point:
                        if (point.mIsCurve)
                            point.mControl.mStartingTrans = point.mControl.mTranslation;
                        break;
                }
            }

            foreach (DefaultShape shape in ctx.GetSelectedObjects<DefaultShape>())
            {
                if (shape is PolytopeVertex vertex)
                    vertex.mStartingTrans = vertex.Center;
                else if (shape is CapsulePoint point)
                {
                    var capsule = point.Parent;
                    point.mStartingTrans = CollisionEditor.translatePoint(point.Center, capsule);
                }
                else
                    shape.mStartingTrans = shape.Center;

            }

        }
        public void DrawWM(Vector2 size, double deltaSeconds, IDictionary<string, bool> layersVisibility)
        {
            mLayersVisibility = layersVisibility;
            mWorldMapVP.Draw(size, deltaSeconds, this, mEditContext, areaScene);
        }
        public void Draw(Vector2 size, double deltaSeconds, IDictionary<string, bool> layersVisibility)
        {
            var io = ImGui.GetIO();

            RightClickMenu();

            if (size.X * size.Y == 0)
                return;

            mLayersVisibility = layersVisibility;
            mTopLeft = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton("canvas", size,
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            IsViewportHovered = IsViewportHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            IsViewportActive = ImGui.IsItemActive();

            ProcessModifiers();

            mSize = size;
            mDrawList = ImGui.GetWindowDrawList();
            ImGui.PushClipRect(mTopLeft, mTopLeft + size, true);

            HandleCameraControls(deltaSeconds);

            if (Camera.Width != mSize.X || Camera.Height != mSize.Y)
            {
                Camera.Width = mSize.X;
                Camera.Height = mSize.Y;
            }

            if (!Camera.UpdateMatrices())
                return;

            this.DrawScene3D(size, mLayersVisibility);

            if (ShowGrid)
                DrawGrid();

            DrawAreaContent();
            Selection();

            ImGui.PopClipRect();
        }
        public void Selection()
        {
            if (!IsViewportHovered)
                mHoveredObject = null;

            CourseActor? hoveredActor = mHoveredObject as CourseActor;
            CourseRail.CourseRailPoint? hoveredRailPoint = mHoveredObject as CourseRail.CourseRailPoint;
            string actorName;

            if (hoveredActor != null && mObjectPickingRequest == null && mPositionPickingRequest == null)
            {
                actorName = hoveredActor.mPackName;
                if (UserSettings.GetEnableTranslation())
                    actorName = Translate.FetchTranslatedName(actorName);

                ImGui.SetTooltip($"{actorName}\n{hoveredActor.mName}");
            }

            if (hoveredRailPoint != null && mObjectPickingRequest == null && mPositionPickingRequest == null)
            {
                CourseRailHolder railArray = mArea.mRailHolder;
                var railIndex = CourseRail.findRailNum(railArray, hoveredRailPoint);
                var childIndex = CourseRail.findPointNum(railArray, hoveredRailPoint);
                ImGui.SetTooltip($"Rail Point {childIndex} from Rail {railIndex}");
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Z) && modifiers == KeyboardModifier.CtrlCmd)
            {
                mEditContext.Undo();
            }

            if ((ImGui.IsKeyPressed(ImGuiKey.Y) && modifiers == KeyboardModifier.CtrlCmd) ||
                (ImGui.IsKeyPressed(ImGuiKey.Z) && modifiers == (KeyboardModifier.Shift | KeyboardModifier.CtrlCmd)))
            {
                mEditContext.Redo();
            }

            CourseActor[] selectedActors = areaScene.EditContext.GetSelectedObjects<CourseActor>().ToArray();

            if (selectedActors.Length != 0 &&
                ((ImGui.IsKeyPressed(ImGuiKey.C) && modifiers == KeyboardModifier.CtrlCmd) || copyContext))
            {
                CopiedMedianPosition = Vector3.Zero;
                foreach (CourseActor actor in selectedActors)
                {
                    CopiedMedianPosition += actor.mTranslation;
                }
                CopiedMedianPosition /= selectedActors.Length;

                CopiedObjects = new CourseActor[selectedActors.Length];
                for (int i = 0; i < CopiedObjects.Length; i++)
                {
                    CopiedObjects[i] = selectedActors[i].Clone(mArea);
                }
                copyContext = false;
            }

            bool ctrlOrCtrlShift = (modifiers == KeyboardModifier.CtrlCmd || modifiers == (KeyboardModifier.CtrlCmd | KeyboardModifier.Shift));
            bool ctrlAndShift = modifiers == (KeyboardModifier.CtrlCmd | KeyboardModifier.Shift);

            if (CopiedObjects.Length != 0 && IsViewportHovered &&
                ((ImGui.IsKeyPressed(ImGuiKey.V) && ctrlOrCtrlShift) || pasteContext))
            {
                DoPaste(freshCopy: ctrlAndShift);
                pasteContext = false;
            }

            if (CopiedObjects.Length == 0)
            {
                pasteContext = false;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                mMultiSelecting = false;
                mMultiSelectStartPos = null;
                mMultiSelectCurrentPos = null;
                mMultiSelectEnded = true;

                if (newSelection.Count > 0)
                {
                    mEditContext.Select(newSelection);
                    newSelection.Clear();
                }
            }

            if (IsViewportHovered || IsViewportActive)
                InteractionWithFocus(modifiers);
        }
        public void DrawScene3D(Vector2 size, IDictionary<string, bool> layersVisibility)
        {
            if (mIsNoMoreRendering)
                goto SKIP_RENDERING; //sue me // ok

            if(layersVisibility != null)
                mLayersVisibility = layersVisibility;

            if (Framebuffer == null)
                Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, (uint)size.X, (uint)size.Y);

            //Resize if needed
            if (Framebuffer.Width != (uint)size.X || Framebuffer.Height != (uint)size.Y)
                Framebuffer.Resize((uint)size.X, (uint)size.Y);

            RenderStats.Reset();

            //Wonder shader system params
            if (PlayAnimations)
                WonderGameShader.UpdateSystem();

            //Background calculations
            EnvironmentData.UpdateBackground(gl, this.Camera);

            //Render viewport settings for game shaders
            GsysShaderRender.GsysResources.UpdateViewport(this.Camera);
            //Setup light map resources for the currently loaded area
            GsysShaderRender.GsysResources.Lightmaps = EnvironmentData.Lightmaps;
            //Distance view scrol calculations
            DistantViewScrollManager.Calc(this.Camera.Target);
            //Set active area for getting env settings by the materials
            AreaResourceManager.ActiveArea = this.EnvironmentData;

            Framebuffer.Bind();

            gl.ClearColor(0, 0, 0, 0);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            gl.Viewport(0, 0, Framebuffer.Width, Framebuffer.Height);

            gl.Enable(EnableCap.DepthTest);

            //Start drawing the scene. Bfres draw upside down so flip the viewport clip
            gl.ClipControl(ClipControlOrigin.UpperLeft, ClipControlDepth.ZeroToOne);

            if (!UserSettings.GetUseSprites() && EditorMode.editMode != "Collision")
            {
                if (!CourseScene.HideWalls)
            {
                //TODO put this somewhere else and maybe cache this
                TileBfresRender CreateTileRendererForSkin(SkinDivision division, string skinName)
                {
                    var bootupPack = RomFS.GetOrLoadBootUpPack();

                    var bytes = bootupPack.OpenFile(
                        "System/CombinationDataTableData/DefaultBgUnitSkinConfigTable.pp__CombinationDataTableData.bgyml");
                    var table = BymlSerialize.Deserialize<DefaultBgUnitSkinConfigTable>(bytes);


                    var render = new TileBfresRender(gl,
                        new TileBfresRender.UnitPackNames(
                            FullHit: table.GetPackName(skinName, "FullHit"),
                            HalfHit: table.GetPackName(skinName, "HalfHit"),
                            NoHit: table.GetPackName(skinName, "NoHit"),
                            Bridge: table.GetPackName(skinName, "Bridge")
                        ), division);
                    render.Load(this.mArea.mUnitHolder);

                    return render;
                }
                string? fieldASkin = mArea.mAreaParams.SkinParam?.FieldA;
                string? fieldBSkin = mArea.mAreaParams.SkinParam?.FieldB;

                if(updateSkinA)
                {
                    TileBfresRenderFieldA = null;
                    //BfresCache.Clear();
                    updateSkinA = false;
                }
                
                if(updateSkinB)
                {
                    TileBfresRenderFieldB = null;
                    //BfresCache.Clear();
                    updateSkinB = false;
                }

                if (TileBfresRenderFieldA == null && !string.IsNullOrEmpty(fieldASkin))
                    TileBfresRenderFieldA = CreateTileRendererForSkin(SkinDivision.FieldA, fieldASkin);

                if (TileBfresRenderFieldB == null && !string.IsNullOrEmpty(fieldBSkin))
                    TileBfresRenderFieldB = CreateTileRendererForSkin(SkinDivision.FieldB, fieldBSkin);


                if (!hasInitialized)
                {
                    tileRebuild = true;
                    hasInitialized = true;
                }

                if (BGUnitRailSceneObj.rebuildTiles)
                {
                    if (TileBfresRenderFieldA is not null)            
                        TileBfresRenderFieldA.DoLoad(this.mArea.mUnitHolder, this.BgUnits);

                    if (TileBfresRenderFieldB is not null)
                        TileBfresRenderFieldB.DoLoad(this.mArea.mUnitHolder, this.BgUnits);

                        BGUnitRailSceneObj.rebuildTiles = false;
                    //CourseUnit.UpdateTiles = false;
                }


                TileBfresRenderFieldA?.Render(gl, this.Camera);
                TileBfresRenderFieldB?.Render(gl, this.Camera);
            }

            //Display skybox
            EnvironmentData.RenderSky(gl, this.Camera);

            // Actors are listed in the order they were pulled from the yaml.
            // So they are ordered by depth for rendering.
            if (layersVisibility != null)
            {
                    foreach (var actor in this.mArea.GetSortedActors())
                    {
                        if (actor.mActorPack == null || (mLayersVisibility.ContainsKey(actor.mLayer) && !mLayersVisibility[actor.mLayer]) ||
                        !actor.wonderVisible)
                            continue;


                        if (!HiddenModels.Contains(actor.mType.ToString()))
                        {
                            RenderActor(actor, actor.mActorPack.ModelInfoRef);
                            RenderActor(actor, actor.mActorPack.DrawArrayModelInfoRef);
                        }

                    }
                }
            }
            else if(EditorMode.editMode == "Collision")
            {
                RenderActor(CollisionEditor.CollisionActor, CollisionEditor.CollisionActor.mActorPack.ModelInfoRef);
                RenderActor(CollisionEditor.CollisionActor, CollisionEditor.CollisionActor.mActorPack.DrawArrayModelInfoRef);
            }
            //Reset back to defaults
                gl.ClipControl(ClipControlOrigin.LowerLeft, ClipControlDepth.ZeroToOne);

            Framebuffer.Unbind();

            //Draw final output in post buffer
            HDRScreenBuffer.Render(gl, (int)size.X, (int)size.Y, (GLTexture2D)Framebuffer.Attachments[0]);

            Framebuffer.Unbind();

            SKIP_RENDERING:
            //Draw framebuffer
            ImGui.SetCursorScreenPos(mTopLeft);
            ImGui.Image((IntPtr)HDRScreenBuffer.GetOutput().ID, new Vector2(size.X, size.Y));

            ImGui.SetNextItemAllowOverlap();
        }
        public void ProcessModifiers()
        {
            modifiers = KeyboardModifier.None;

            if (ImGui.GetIO().KeyShift)
                modifiers |= KeyboardModifier.Shift;
            if (ImGui.GetIO().KeyAlt)
                modifiers |= KeyboardModifier.Alt;
            if (OperatingSystem.IsMacOS() ? ImGui.GetIO().KeySuper : ImGui.GetIO().KeyCtrl)
                modifiers |= KeyboardModifier.CtrlCmd;

        }
        private void RenderActor(CourseActor actor, ModelInfo modelInfo)
        {
            if (modelInfo == null || modelInfo.mFilePath == null)
                return;

            //if (modelInfo.ModelVariationAnims != null)
            //{
//                if(modelInfo.ModelVariationAnims.Count > 0)
//                {
//                    byte[] data = File.ReadAllBytes(
//                    Path.Combine(UserSettings.GetRomFSPath(), modelInfo.ModelVariationAnims[0].Fmab)
//                    );
//                    var par = BymlSerialize.Deserialize<MaterialAnimation>(data);
//                    Console.WriteLine(par.FrameCount);
//                }
//            }
            var resourceName = modelInfo.mFilePath;
            var modelName = modelInfo.mModelName;

            var render = BfresCache.Load(gl, resourceName);

            if (render == null || !render.Models.TryGetValue(modelName, out BfresRender.BfresModel? value))
                return;

            var transMat = Matrix4x4.CreateTranslation(actor.mTranslation);
            var scaleMat = Matrix4x4.CreateScale(actor.mScale);
            var rotMat = Matrix4x4.CreateRotationX(actor.mRotation.X) *
                    Matrix4x4.CreateRotationY(actor.mRotation.Y) *
                    Matrix4x4.CreateRotationZ(actor.mRotation.Z);

            var debugSMat = Matrix4x4.CreateScale(modelInfo.mModelScale != default ? modelInfo.mModelScale : Vector3.One);

            var mat = debugSMat * scaleMat * rotMat * transMat;

            //if(actor.mPackName.StartsWith("DV") || actor.mPackName.StartsWith("Cloud"))
                DistantViewScrollManager.UpdateMatrix(actor.mLayer, ref mat);

            var model = render.Models[modelName];

            if (actor.mActorPack.ModelExpandParamRef != null)
            {
                ActorModelExpand(actor, model);
                ActorModelExpand(actor, model, "Main"); //yeah idk either

                //TODO SubModels
            }
            //switch for drawing models with different methods easier
            if (actor.mActorPack.DrainPipeRef != null && actor.mActorPack.DrainPipeRef.ModelKeyTop != null &&
            actor.mActorPack.DrainPipeRef.ModelKeyMiddle != null)
            {
                var drainRef = actor.mActorPack.DrainPipeRef;
                var calc = actor.mActorPack.ShapeParams.mCalc;
                var KeyMats = new Dictionary<string, Matrix4x4>{
                    {drainRef.ModelKeyTop ?? "Top", debugSMat *
                        Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.X, actor.mScale.Z) *
                        Matrix4x4.CreateTranslation(0, (actor.mScale.Y-actor.mScale.X)*(calc.mMax.Y-calc.mMin.Y), 0) *
                        rotMat *
                        transMat},

                    {drainRef.ModelKeyMiddle ?? "Middle", debugSMat *
                        Matrix4x4.CreateScale(actor.mScale.X, (actor.mScale.Y-1)*2, actor.mScale.Z) *
                        rotMat *
                        transMat}};

                model.Render(gl, render, KeyMats[modelInfo.SearchModelKey], this.Camera);
                if ((modelInfo.SubModels?.Count ?? 0) != 0)
                    render.Models[modelInfo.SubModels[0].FmdbName].Render(gl, render, KeyMats[modelInfo.SubModels[0].SearchModelKey], this.Camera);
            }
            else
            {
                if (modelInfo.IsUseTilingMode)
                {
                    for (int y = 0; y < actor.mScale.Y; y++)
                    {
                        for (int x = 0; x < actor.mScale.X; x++)
                        {
                            model.Render(gl, render,
                                Matrix4x4.CreateTranslation(
                                    -actor.mScale.X / 2 + x + 0.5f,
                                    -actor.mScale.Y / 2 + y + 0.5f,
                                    0)
                                * rotMat * transMat,
                            this.Camera);
                        }
                    }
                }
                else
                    model.Render(gl, render, mat, this.Camera, actor.mPackName);

            }
        }
        public Vector2 ExpandCalcTypes(string type, Vector2 actScale)
        {
            var result = type switch
            {
                "ActorScale" => actScale,
                "ActorScaleMinus1" => actScale - Vector2.One,
                "ActorScaleMinus2" => actScale - new Vector2(2),
                "ActorScaleDiv2" => actScale / 2,
                "ActorScaleDiv4" => actScale / 4,
                "ZeroWhenActorScaleOne" => new Vector2(actScale.X == 1 ? 0 : 1, actScale.Y == 1 ? 0 : 1),
                "None" => Vector2.One,
                _ => actScale
            };
            return result;
        }
        public Vector2 ExpandScaleTypes(string type, Vector2 scale)
        {
            var result = type switch
            {
                "XAxisOnly" => new(scale.X, 1),
                "YAxisOnly" => new(1, scale.Y),
                "XYAxis" => scale,
                _ => scale
            };
            return result;
        }
        private void ActorModelExpand(CourseActor actor, BfresRender.BfresModel model, string modelKeyName = "")
        {
            //Model Expand Param

            //TODO is that actually how the game does it?
            var param = actor.mActorPack.ModelExpandParamRef;
            ModelExpandParamSettings? setting = null;
            do
            {
                if (param.Settings != null)
                    setting = param.Settings.FindLast(x => x.mModelKeyName == modelKeyName);

                param = param.Parent;
            } while (setting == null && param != null);

            if (setting == null)
                return;

            var clampedActorScale = new Vector2(
                Math.Max(actor.mScale.X, setting.mMinScale.X),
                Math.Max(actor.mScale.Y, setting.mMinScale.Y)
            );

            foreach (var matParam in setting.mMatSetting?.MatInfoList ?? [])
            {
                var material = model.Meshes.Select(x => x.MaterialRender)
                    .FirstOrDefault(x => x.Name.EndsWith(matParam.mMatNameSuffix));

                if (material == null)
                    return;

                Vector2 matScale;
                if (matParam.mIsCustomCalc)
                {
                    float a = matParam.mCustomCalc.A;
                    float b = matParam.mCustomCalc.B == 0 ? 1 : matParam.mCustomCalc.B;
                    matScale = (clampedActorScale - new Vector2(a)) / b;
                }
                else
                {
                    matScale = ExpandCalcTypes(matParam.mCalcType, clampedActorScale);
                }

                matScale = ExpandScaleTypes(matParam.mScalingType, matScale);

                matScale.X = Math.Max(matScale.X, 0);
                matScale.Y = Math.Max(matScale.Y, 0);

                // for now
                material.SetParam("tex_srt0", new ShaderParam.TexSrt
                {
                    Mode = ShaderParam.TexSrt.TexSrtMode.ModeMaya,
                    Scaling = matScale
                });
            }

            Dictionary<string, Vector3> boneScaleLookup = [];

            foreach (var boneParam in setting.mBoneSetting?.BoneInfoList ?? [])
            {
                Vector2 boneScale;
                if (boneParam.mIsCustomCalc)
                {
                    float a = boneParam.mCustomCalc.A;
                    float b = boneParam.mCustomCalc.B == 0 ? 1 : boneParam.mCustomCalc.B;
                    boneScale = (clampedActorScale - new Vector2(a)) / b;
                }
                else
                {
                    boneScale = ExpandCalcTypes(boneParam.mCalcType, clampedActorScale);
                }

                boneScale = ExpandScaleTypes(boneParam.mScalingType, boneScale);

                boneScale.X = Math.Max(boneScale.X, 0);
                boneScale.Y = Math.Max(boneScale.Y, 0);

                boneScaleLookup[boneParam.mBoneName] = new Vector3(boneScale, 1);
            }

            var rootMatrix = Matrix4x4.CreateScale(
                1 / actor.mScale.X,
                1 / actor.mScale.Y,
                1
                );

            model.Skeleton.Bones[0].WorldMatrix = rootMatrix;

            var nonScaledMatrices = new Matrix4x4[model.Skeleton.Bones.Count];
            nonScaledMatrices[0] = rootMatrix;


            for (int i = 1; i < model.Skeleton.Bones.Count; i++)
            {
                var bone = model.Skeleton.Bones[i];
                bone.WorldMatrix = bone.CalculateLocalMatrix();

                var parent = model.Skeleton.Bones[bone.ParentIndex];

                Vector3 scale;
                if (boneScaleLookup.TryGetValue(parent.Name ?? "", out scale))
                {
                    bone.WorldMatrix.Translation *= scale;
                }

                bone.WorldMatrix *= nonScaledMatrices[bone.ParentIndex];

                nonScaledMatrices[i] = bone.WorldMatrix;
                if (boneScaleLookup.TryGetValue(bone.Name, out scale))
                {
                    bone.WorldMatrix = Matrix4x4.CreateScale(scale) * bone.WorldMatrix;
                }
            }
        }
        public void DrawUnits()
        {
            DrawBGUnits();
        }
        public void DrawBGUnits()
        {
            foreach (var unit in mArea.mUnitHolder.mUnits)
            {
                if (!unit.Visible)
                    continue;

                foreach (var wall in unit.Walls)
                {
                    DrawRail(wall.ExternalRail, false);
                    foreach (var internalRail in wall.InternalRails)
                        DrawRail(internalRail, false);
                }
                foreach (var belt in unit.mBeltRails)
                {
                    if (belt.mCourseUnit.mModelType != ModelType.Solid)
                        DrawRail(belt, true);
                }
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Delete) && mEditContext.IsAnySelected<BGUnitRail.RailPoint>())
            {
                var railPoints = mEditContext.GetSelectedObjects<BGUnitRail.RailPoint>().ToArray();
                BGUnitRailSceneObj.pointsToDelete.AddRange(railPoints);
            }

            if (BGUnitRailSceneObj.pointsToDelete.Count > 0)
            {
                var batchAction = mEditContext.BeginBatchAction();
                foreach (var point in BGUnitRailSceneObj.pointsToDelete)
                {
                    var revertible = point.mRail.Points.RevertableRemove(point);
                    mEditContext.CommitAction(revertible);
                    BGUnitRailSceneObj.rebuildUnit(point.mRail.mCourseUnit);
                    mEditContext.CommitAction(new TileRebuildRevertable(point.mRail.mCourseUnit));
                }
                batchAction.Commit($"{IconUtil.ICON_TRASH} Delete Rail Points");
                BGUnitRailSceneObj.pointsToDelete.Clear();
            }
        }
        private void DrawRail(BGUnitRail rail, bool isBelt)
        {
            if(rail.mCourseUnit.UpdateTiles)
            {
                BGUnitRailSceneObj.rebuildUnit(rail.mCourseUnit);
            }

            float thickness = mHoveredObject == rail ? 4f : 3.5f;
            var segmentCount = rail.Points.Count;
            BGUnitRail.RailPoint selectedPoint = null;
            BGUnitRailSceneObj.Draw2D(mEditContext, this, mDrawList, rail, isBelt);
            foreach (var point in rail.Points)
            {
                BGUnitRailSceneObj.Draw2D(mEditContext, this, mDrawList, point);
            }
        }
        public void DrawOverlay()
        {
            var drawList = ImGui.GetWindowDrawList();

            Vector2 padding = new Vector2(16, 20);
            Vector2 pos = vpMin + padding;

            var io = ImGui.GetIO();
            float fps = 1.0f / io.DeltaTime;

            Vector2 mouse = ImGui.GetMousePos();
            Vector3 world = ScreenToWorld(mouse);

            string text =
                $"X: {Math.Round(world.X, 3)}\n" +
                $"Y: {Math.Round(world.Y, 3)}\n" +
                $"FPS: {Math.Round(fps)}";

            uint textCol = ImGui.GetColorU32(ImGuiCol.Text);

            drawList.AddText(pos, textCol, text);
        }
        void DrawGrid()
        {
            DrawGridLines(false, 20f, 10);
            DrawGridLines(true, 20f, 10);
        }
        public void DrawGridLines(bool is_vertical, float min_minor_tick_size, int major_tick_interval)
        {
            var camForward = Vector3.Transform(-Vector3.UnitZ, Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, Camera.Rotation);
            // grid lines are drawn in intervals from a to b
            // the 0 and 1 coordinates represent the line ends at a and b
            Vector2 a0, a1, b0, b1;
            float min_value, max_value, a, b;

            Vector2 min = mTopLeft;
            Vector2 max = mTopLeft + mSize;

            Vector3 minWorld = ScreenToWorld(min);
            Vector3 maxWorld = ScreenToWorld(max);


            if (is_vertical)
            {
                if (camUp.Z > -0.9 && camUp.Z < 0.9) {
                    if (minWorld.Y > maxWorld.Y)
                        (minWorld.Y, maxWorld.Y) = (maxWorld.Y, minWorld.Y);

                    min_value = minWorld.Y;
                    max_value = maxWorld.Y;
                }
                else
                {
                    if (minWorld.Z > maxWorld.Z)
                       (minWorld.Z, maxWorld.Z) = (maxWorld.Z, minWorld.Z);

                       min_value = minWorld.Z;
                       max_value = maxWorld.Z;
                }

                a = max.Y;
                b = min.Y;

                a0 = new Vector2(min.X, a);
                a1 = new Vector2(max.X, a);
                b0 = new Vector2(min.X, b);
                b1 = new Vector2(max.X, b);
            }
            else
            {
                if (camForward.X > -0.9 && camForward.X < 0.9) 
                {
                    if (minWorld.X > maxWorld.X)
                        (minWorld.X, maxWorld.X) = (maxWorld.X, minWorld.X);

                    min_value = minWorld.X;
                    max_value = maxWorld.X;
                }
                else
                {
                    if (minWorld.Z > maxWorld.Z)
                        (minWorld.Z, maxWorld.Z) = (maxWorld.Z, minWorld.Z);

                    min_value = minWorld.Z;
                    max_value = maxWorld.Z;
                }
                    a = min.X;
                    b = max.X;

                    a0 = new Vector2(a, min.Y);
                    a1 = new Vector2(a, max.Y);
                    b0 = new Vector2(b, min.Y);
                    b1 = new Vector2(b, max.Y);
                
            }


            float ideal_tick_interval =
                min_minor_tick_size * (max_value - min_value) / MathF.Abs(b - a);
            float tick_interval_log = MathF.Log(ideal_tick_interval) / MathF.Log(major_tick_interval);
            float tick_interval = MathF.Pow(major_tick_interval, MathF.Ceiling(tick_interval_log));
            float blend = 1 - (tick_interval_log - MathF.Floor(tick_interval_log));

            float min_tick_value = MathF.Ceiling(min_value / tick_interval) * tick_interval;
            int tick_offset = (int)MathF.Ceiling(min_value / tick_interval);
            int tick_count = (int)MathF.Floor(max_value / tick_interval) -
                             (int)MathF.Floor(min_value / tick_interval) + 1;

            for (int i = 0; i < tick_count; i++)
            {
                bool is_major_tick = (i + tick_offset) % major_tick_interval == 0;

                float t = ((min_tick_value + i * tick_interval) - min_value) / (max_value - min_value);

                Vector4 colorVec = ImGui.ColorConvertU32ToFloat4(GridColor);
                colorVec.W *= is_major_tick ? 1f : blend;
                mDrawList.AddLine(a0 * (1 - t) + b0 * t, a1 * (1 - t) + b1 * t,
                              ImGui.ColorConvertFloat4ToU32(colorVec), GridLineThickness);
            }
        }
        public void DrawAreaContent()
        {
            mHoveredObject = null;
            CourseRail.deleteList = new List<(CourseRail rail, CourseRail.CourseRailPoint point)>();
            deleteList2 = new List<(BGUnitRail rail, BGUnitRail.RailPoint point)>();
            DrawUnits();
            CourseRail.DrawRails(this, mEditContext, mArea);

            cursor?.DrawCursor(this, mEditContext);

            if (!mMultiSelecting && mEditContext.IsSingleObjectSelected(out CourseComment? comment))
                comment.DragComment(this, mEditContext);
            
            if (!Course.IsWorldMap)
            {
                CourseActor.DrawActorCollision(this, mEditContext, mArea);
                DrawMultiSelectBox();
            }
        }
        #endregion

        #region Edit Context Actions
        private void DoImmediatePaste(bool freshCopy)
        {
            if (mHoveredObject is not CourseActor actor) return;

            CourseActor newActor;
            AddLayer(actor.mLayer);
            if (freshCopy)
                newActor = new CourseActor(actor.mPackName, actor.mAreaHash, actor.mLayer);
            else
                newActor = actor.Clone(mArea);

            newActor.mStartingTrans = newActor.mTranslation;
            newActor.mStartingRot = newActor.mRotation;

            mEditContext.AddActor(newActor);

            mEditContext.DeselectAll();
            mEditContext.Select(newActor);
        }
        private async void DoPaste(bool freshCopy)
        {
            if (CopiedObjects.Length == 0) return;

            Vector3? _pos;
            KeyboardModifier modifier;
            if (CopiedObjects is not CourseActor[] actors) return;
            string msg;
            string actorName;
            if (actors.Length == 1)
            {
                actorName = actors[0].mPackName;
                msg = $"Placing actor {actorName}";
            }
            else
                msg = $"Placing {actors.Length} actors";
            msg += " -- Hold SHIFT to place multiple";
            // Cancellation token source for cancellation. Make sure to dispose after use (which is done here through the using expression).
            using var tokenSource = new CancellationTokenSource();
            do
            {
                (_pos, modifier) = await PickPosition(msg, actors[0].mLayer, tokenSource);
                if (_pos == null) return;

                var batchAction = mEditContext.BeginBatchAction();
                var offset = (Vector3)_pos - CopiedMedianPosition;

                offset.X = MathF.Round(offset.X * 2) / 2;
                offset.Y = MathF.Round(offset.Y * 2) / 2;

                for (var i = 0; i < actors.Length; i++)
                {
                    var actor = actors[i];
                    CourseActor newActor;

                    AddLayer(actor.mLayer);

                    if (freshCopy)
                        newActor = new CourseActor(actor.mPackName, actor.mAreaHash, actor.mLayer);
                    else
                        newActor = actor.Clone(mArea);

                    newActor.mTranslation = actor.mTranslation + offset;

                    newActor.mTranslation.Z = actor.mTranslation.Z;

                    var n = 0;
                    do { n++; }
                    while (area.GetActors().Any(x => x.mName == $"{actor.mPackName}{n}"));

                    newActor.mName = $"{actor.mPackName}" + (n == 0 ? "" : n);

                    mEditContext.AddActor(newActor);
                }

                batchAction.Commit($"{IconUtil.ICON_PLUS_CIRCLE} Paste {actors.Length} Actor{(actors.Length > 1 ? "s" : "")}");

                mEditContext.Select(actors);
            } while (modifier == KeyboardModifier.Shift);

        }
        private void AddLayer(string layer)
        {
            string[] Layers = mLayersVisibility.Keys.ToArray();

            if (Layers.Contains(layer))
                return;

            var oldDict = new Dictionary<string, bool>(mLayersVisibility);


            mLayersVisibility[layer] = true;
         
    
            mEditContext.CommitAction(new PropertyFieldsSetUndo(
                _courseScene,
                [("mLayersVisibility", oldDict)],
                $"{IconUtil.ICON_LAYER_GROUP} Added Layer: {layer}"
            ));
        }
        public void CancelPick()
        {
                if(mObjectPickingRequest.TryGetValue(out var objectPickingRequest)) {
                    mArea.hasStartedPick = false;
                    mObjectPickingRequest = null;
                    objectPickingRequest.promise.SetResult((null, KeyboardModifier.None));
                }
        }
        public void InteractionWithFocus(KeyboardModifier modifiers)
        {
            if (IsViewportHovered &&
                mObjectPickingRequest.TryGetValue(out var objectPickingRequest))
            {
                bool isValid = objectPickingRequest.predicate(mHoveredObject);

                string currentlyHoveredObjText = "";
                if (isValid && mHoveredObject is CourseActor hoveredActor)
                    currentlyHoveredObjText = $"\n\nCurrently Hovered: {hoveredActor.mPackName}";

                ImGui.SetTooltip(objectPickingRequest.message + "\nPress Escape to cancel" +
                    currentlyHoveredObjText);

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    CourseScene.pickingComplete = true;
                    mObjectPickingRequest = null;
                    objectPickingRequest.promise.SetResult((null, modifiers));
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) &&
                    isValid)
                {
                    mObjectPickingRequest = null;
                    objectPickingRequest.promise.SetResult((mHoveredObject, modifiers));
                }

                return;
            }

            if (IsViewportHovered &&
                mPositionPickingRequest.TryGetValue(out var positionPickingRequest))
            {
                ImGui.SetTooltip(positionPickingRequest.message + "\nPress Escape to cancel");
                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                {
                    mPositionPickingRequest = null;
                    positionPickingRequest.promise.SetResult((null, modifiers));
                }
                else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    //TODO use positionPickingRequest.layer
                    mPositionPickingRequest = null;
                    positionPickingRequest.promise.SetResult((ScreenToWorld(ImGui.GetMousePos()), modifiers));
                }

                return;
            }

            if (mHoveredObject is CourseActor hovered &&
                ImGui.IsMouseClicked(ImGuiMouseButton.Left) &&
                modifiers == KeyboardModifier.CtrlCmd && UserSettings.GetClickDuplicate())
            {
                CopiedMedianPosition = hovered.mTranslation;

                var clone = hovered.Clone(mArea);
                CopiedObjects = new CourseActor[] { clone };
                DoImmediatePaste(freshCopy: false);

                clone.mStartingTrans = clone.mTranslation;
                clone.mStartingRot = clone.mRotation;

                return;
            }

            if (mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().ToArray().Length > 0)
            {
                    var selRailPoint = mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().ToArray();
                    var railHolder = mArea.mRailHolder;
                    foreach (var selPoint in selRailPoint)
                    {
                        int index = CourseRail.findRailNum(railHolder, selPoint);
                        if (index != -1)
                        {
                            if (ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A))
                            {

                                CourseRail rail = railHolder.mRails.ToArray()[index];

                                foreach (CourseRail.CourseRailPoint points in rail.mPoints)
                                    mEditContext.Select(points);

                            }
                        } else
                        mEditContext.Deselect(selPoint);
                    }
            }

            Multiselection();

            cursor?.PivotActors(mEditContext, this);

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (mHoveredObject == null)
                {
                    if (!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                        mEditContext.DeselectAll();
                }
                else
                    DefaultSelect(mEditContext, mHoveredObject);
            }

            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    mMultiSelectStartPos = ImGui.GetMousePos();

                if (!isPanGesture)
                    dragRelease = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
            }


            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                mMultiSelecting = false;
                mMultiSelectEnded = true;
                if (mHoveredObject is CourseActor && !dragRelease)
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || CourseScene.bypassSelection)
                    {
                        CourseScene.bypassSelection = false;
                        //mEditContext.Deselect(mHoveredObject!);
                    }
                    else if (!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        mEditContext.DeselectAll();
                        DefaultSelect(mEditContext, mHoveredObject);
                    }
                }
            }


            if (ImGui.IsKeyPressed(ImGuiKey.Delete) || (ImGui.GetIO().KeyShift && ImGui.IsKeyPressed(ImGuiKey.Backspace)) || deleteContext)
            {
                List<CourseActor> selected;

                if (deleteContext)
                    selected = backupSelection;  
                else
                    selected = mEditContext.GetSelectedObjects<CourseActor>().ToList();

                if (selected.Count > 0)
                {
                    ObjectDeletionRequested?.Invoke(selected);

                }

                deleteContext = false;

            }

            if (mEditContext.IsSingleObjectSelected(out CourseRail.CourseRailPoint? point) &&
            mHoveredObject == point &&
            ImGui.IsMouseDoubleClicked(0))
            {
                bool oldValue = point.mIsCurve;
                point.mIsCurve = !point.mIsCurve;
                undoPointToggleMethod(point, oldValue);
                point.mControl.mStartingTrans = point.mControl.mTranslation;
            }

                if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                mEditContext.DeselectAll();

        }
        public void undoPointToggleMethod(CourseRail.CourseRailPoint point, bool oldValue)
        {
            mEditContext.CommitAction(
               new PropertyFieldsSetUndo(
                   point,
                   [("mIsCurve", oldValue)],
                   $"{IconUtil.ICON_ARROWS_ALT} Toggled Curve Control"
               )
           );
        }
        public void RightClickMenu()
        {
            if ((ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGui.IsKeyPressed(ImGuiKey.Menu)) && IsViewportHovered)
            {
                backupSelection = mEditContext.GetSelectedObjects<CourseActor>().ToList();
                storedMousePos = ImGui.GetMousePos();
                ImGui.OpenPopup("ViewportContextMenu");
            }

            if (ImGui.BeginPopup("ViewportContextMenu"))
            {
                bool showCopyDelete = mEditContext.GetSelectedObjects<CourseActor>().ToArray().Length >= 1;
                if (showCopyDelete)
                {
                    if (ImGui.MenuItem("Copy"))
                        copyContext = true;
                }
                ImGui.SetItemDefaultFocus();


                if (ImGui.MenuItem("Paste"))
                    pasteContext = true;


                if (showCopyDelete)
                {
                    ImGui.Separator();
                    if (ImGui.MenuItem("Delete"))
                        deleteContext = true;
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Add Comment"))
                {
                    CourseComment.AddComment(this, mEditContext);
                }


                BGUnitRail[] mUnitRails = null;

                if (mEditContext.GetObjectCountOfType<BGUnitRail>() >= 1)
                    mUnitRails = mEditContext.GetSelectedObjects<BGUnitRail>().ToArray();

                if (mUnitRails != null && mUnitRails.Count() > 0)
                {
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Tilesets"))
                    {

                        if (ImGui.MenuItem("Reverse Rails"))
                        {
                            foreach (var mUnitRail in mUnitRails)
                                mUnitRail.ReverseRailPoints();
                        }

                        if (ImGui.MenuItem("Delete Unit"))
                            DeleteUnit();

                        ImGui.EndMenu();
                    }
                }

                if (mEditContext.GetObjectCountOfType<CourseActor>() > 0 && !mEditContext.IsAnySelected<BGUnitRail>())
                {
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Links"))
                    {
                        if (mEditContext.IsSingleObjectSelected(out CourseActor? mSelectedActor))
                        {
                            if (CourseScene.showGlobalLinkWindow)
                            {

                                if (ImGui.MenuItem("Make Global Src"))
                                {
                                    setGlobalSrc = true;
                                    globalHash = mSelectedActor.mHash;
                                }

                                if (ImGui.MenuItem("Make Global Dst"))
                                {
                                    setGlobalDst = true;
                                    globalHash = mSelectedActor.mHash;
                                }
                                ImGui.Separator();
                            }


                            if (ImGui.MenuItem("Copy Src"))
                                CourseScene.mCopiedLinks = mArea.mLinkHolder.GetDestHashesFromSrc(mSelectedActor.mHash);

                            if (ImGui.MenuItem("Paste Src"))
                            {
                                var total = 0;
                                var batch = mEditContext.BeginBatchAction();
                                foreach ((string linkName, List<ulong> hashArray) in CourseScene.mCopiedLinks)
                                {
                                    for (int i = 0; i < hashArray.Count; i++)
                                    {
                                        var link = new CourseLink(linkName)
                                        {
                                            mSource = mSelectedActor.mHash,
                                            mDest = hashArray[i]
                                        };
                                        if (!mArea.mLinkHolder.mLinks.Contains(link))
                                        {
                                            mEditContext.AddLink(link);
                                            total++;
                                        }
                                    }
                                }
                                batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");
                            }

                            ImGui.Separator();
                            if (ImGui.MenuItem("Copy Dst"))
                                CourseScene.mCopiedLinks = mArea.mLinkHolder.GetSrcHashesFromDest(mSelectedActor.mHash);


                            if (ImGui.MenuItem("Paste Dst"))
                            {
                                var total = 0;
                                var batch = mEditContext.BeginBatchAction();
                                foreach ((string linkName, List<ulong> hashArray) in CourseScene.mCopiedLinks)
                                {
                                    for (int i = 0; i < hashArray.Count; i++)
                                    {
                                        var link = new CourseLink(linkName)
                                        {
                                            mSource = hashArray[i],
                                            mDest = mSelectedActor.mHash
                                        };
                                        if (!mArea.mLinkHolder.mLinks.Contains(link))
                                        {
                                            mEditContext.AddLink(link);
                                            total++;
                                        }
                                    }
                                }
                                batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");

                            }
                        }

                        if (mEditContext.GetSelectedObjects<CourseActor>().ToArray().Length > 1)
                        {
                            ImGui.Separator();
                            var actors = mEditContext.GetSelectedObjects<CourseActor>().ToArray();
                            if (ImGui.MenuItem("Paste Src"))
                            {
                                var total = 0;
                                var batch = mEditContext.BeginBatchAction();
                                foreach ((string linkName, List<ulong> hashArray) in CourseScene.mCopiedLinks)
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
                                                if (!mArea.mLinkHolder.mLinks.Contains(link))
                                                {
                                                    mEditContext.AddLink(link);
                                                    total++;
                                                }
                                            }
                                        }
                                        if (!mArea.mLinkHolder.mLinks.Contains(link))
                                        {
                                            mEditContext.AddLink(link);
                                            total++;
                                        }
                                    }
                                }
                                batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");
                            }

                            if (ImGui.MenuItem("Paste Dst"))
                            {
                                var total = 0;
                                var batch = mEditContext.BeginBatchAction();
                                foreach ((string linkName, List<ulong> hashArray) in CourseScene.mCopiedLinks)
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
                                                    mDest = actor.mHash,
                                                };
                                                if (!mArea.mLinkHolder.mLinks.Contains(link))
                                                {
                                                    mEditContext.AddLink(link);
                                                    total++;
                                                }
                                            }
                                        }
                                        if (!mArea.mLinkHolder.mLinks.Contains(link))
                                        {
                                            mEditContext.AddLink(link);
                                            total++;
                                        }
                                    }
                                }
                                batch.Commit($"{IconUtil.ICON_PASTE} Paste {total} Link{(total == 1 ? "" : "s")}");
                            }

                        }
                        ImGui.EndMenu();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Save as Prefab"))
                    {
                        Prefab.PrefabPopup(mEditContext, mArea);
                    }
                }

                ImGui.Separator();
                if (cursor == null)
                {
                    if (ImGui.MenuItem("Place Cursor"))
                    {
                        cursor = new FushigiCursor();
                        cursor.CursorPlacement(this, storedMousePos);
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Move Cursor"))
                        cursor.CursorPlacement(this, storedMousePos);

                    if (ImGui.MenuItem("Remove Cursor"))
                        cursor = null;
                }

                bool popupHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);


                if ((ImGui.IsMouseClicked(ImGuiMouseButton.Left) && IsViewportHovered && !popupHovered) || ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                    ImGui.CloseCurrentPopup();


                ImGui.EndPopup();
            }
        }
        public void DeleteUnit()
        {
            bool deleteWall = false;
            var unitToSel = new CourseUnit();
            CourseUnitHolder unitHolder = mArea.mUnitHolder;
            foreach (var unit in unitHolder.mUnits)
            {

                mEditContext.WithSuspendUpdateDo(() =>
                {

                    for (int i = unit.Walls.Count - 1; i >= 0; i--)
                    {
                        if (mEditContext.IsSelected(unit.Walls[i].ExternalRail))
                        {
                            mEditContext.DeleteWall(unit, unit.Walls[i]);
                            deleteWall = true;
                            unitToSel = unit;
                        }

                    }

                    for (int i = unit.mBeltRails.Count - 1; i >= 0; i--)
                    {
                        if (mEditContext.IsSelected(unit.mBeltRails[i]))
                        {
                            mEditContext.DeleteBeltRail(unit, unit.mBeltRails[i]);
                            deleteWall = true;
                            unitToSel = unit;
                        }
                    }
                });
            }
            if (deleteWall)
            {
                mEditContext.DeselectAll();
                mEditContext.Select(unitToSel);
            }
        }
        #endregion

        #region Multi-Selection and Drag Logic
        public void Multiselection()
        {
            if ((ImGui.IsMouseDragging(ImGuiMouseButton.Left) && !isPanGesture))
            {
                if (IsTransformableSelected() && mMultiSelectEnded)
                    mMultiSelecting = false;
                else if (mMultiSelectStartPos != null && !ImGui.IsWindowHovered())
                {
                    mMultiSelectCurrentPos = ImGui.GetMousePos();
                    mMultiSelecting = true;
                    mMultiSelectEnded = false;

                    Vector3 startPosWorldStart = ScreenToWorld(mMultiSelectStartPos.Value);
                    Vector3 currentPosWorldStart = ScreenToWorld(mMultiSelectCurrentPos.Value);

                    startPosWorld = startPosWorldStart;
                    currentPosWorld = currentPosWorldStart;

                    if (currentPosWorldStart.X < startPosWorldStart.X)
                    {
                        currentPosWorld.X = startPosWorldStart.X;
                        startPosWorld.X = currentPosWorldStart.X;
                    }
                    if (currentPosWorldStart.Y < startPosWorldStart.Y)
                    {
                        currentPosWorld.Y = startPosWorldStart.Y;
                        startPosWorld.Y = currentPosWorldStart.Y;
                    }
                }

                // Perform Object Translation
                if (!mMultiSelecting && IsTransformableSelected())
                {
                    if (!IsViewportActive)
                        return;

                    Vector3 StartingTrans = new Vector3();
                    Vector3 CurrentTrans = new Vector3();

                    if (mHoveredObject != null)
                        lastHoveredObject = mHoveredObject;

                    switch (lastHoveredObject)
                    {
                        case Transformable transformable:
                            StartingTrans = transformable.mStartingTrans;
                            CurrentTrans = transformable.mTranslation;
                            break;
                        case DefaultShape shape:
                            StartingTrans = shape.Center;
                            CurrentTrans = shape.mStartingTrans;

                            if (shape is PolytopeVertex vertex)
                            {
                                StartingTrans = vertex.Center;
                                CurrentTrans = shape.mStartingTrans;
                            }

                            if (shape is CapsulePoint point)
                            {
                                var capsule = point.Parent;
                                StartingTrans = CollisionEditor.translatePoint(point.mStartingTrans, capsule);
                                CurrentTrans = CollisionEditor.translatePoint(point.Center, capsule);
                            }

                            break;
                    }

                    if (Camera.IsOrthographic)
                    {
                        var posVec = CalcPosVec(StartingTrans);
                        CurrentTrans.X = posVec.X;
                        CurrentTrans.Y = posVec.Y;

                        if (Course.IsWorldMap || EditorMode.editMode == "Collision")
                            CurrentTrans.Z = posVec.Z;

                        rebuildList = new();

                        foreach (Transformable transformable in mEditContext.GetSelectedObjects<Transformable>())
                            HandleTranslation(transformable, StartingTrans, CurrentTrans);

                        foreach (var unit in rebuildList)
                            BGUnitRailSceneObj.rebuildUnit(unit);
                            
                        if (StartingTrans != CurrentTrans)
                            CommitObjectTranslation = true;
                    }
                }
                else
                    CommitObjectTranslation = false;
            }

            // Save Object Translation to history
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && CommitObjectTranslation)
            {
                var objCount = mEditContext.GetSelectedObjects<Transformable>().Count();
                var batch = mEditContext.BeginBatchAction();

                foreach (Transformable transformable in mEditContext.GetSelectedObjects<Transformable>())
                {
                    CommitTranslation(transformable);

                    if (transformable is CourseRail.CourseRailPoint p && p.mIsCurve)
                        CommitTranslation(p.mControl);
                }
                rebuildList.Clear();

                if(objCount > 1) 
                    batch.Commit($"{IconUtil.ICON_ARROWS_ALT} Move {objCount} Objects");
                else
                    batch.Commit($"{IconUtil.ICON_ARROWS_ALT} Move {GetTransformableType(mEditContext.GetFirstObjectOfType<Transformable>())}");

                CommitObjectTranslation = false;
            }
        }
        public void DrawMultiSelectBox()
        {
            if (mMultiSelecting && mMultiSelectStartPos != null && mMultiSelectCurrentPos != null)
            {
                Vector2 pMin = mMultiSelectStartPos.Value;
                Vector2 pMax = mMultiSelectCurrentPos.Value;
                if (mMultiSelectCurrentPos.Value.X < mMultiSelectStartPos.Value.X)
                {
                    pMax.X = mMultiSelectStartPos.Value.X;
                    pMin.X = mMultiSelectCurrentPos.Value.X;
                }
                if (mMultiSelectCurrentPos.Value.Y < mMultiSelectStartPos.Value.Y)
                {
                    pMax.Y = mMultiSelectStartPos.Value.Y;
                    pMin.Y = mMultiSelectCurrentPos.Value.Y;
                }
                mDrawList.AddRect(pMin, pMax, MultiSelectBoxColor, 2f, ImDrawFlags.RoundCornersAll, MultiSelectBoxThickness);
            }
        }
        #endregion

    }

    static class ColorExtensions
    {
        public static uint ToAbgr(this Color c) => (uint)(
            c.A << 24 |
            c.B << 16 |
            c.G << 8 |
            c.R);
    }
}