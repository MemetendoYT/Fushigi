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
using Fushigi.ui.undo;
using Fushigi.util;
using ImGuiNET;
using Microsoft.Msagl.GraphmapsWithMesh;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Data;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Fushigi.course.CourseUnit;
using Vector3 = System.Numerics.Vector3;


namespace Fushigi.ui.widgets
{
    interface IViewportDrawable
    {
        void Draw2D(CourseAreaEditContext editContext, LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj);
    }

    #region Selection Logic
    interface IViewportSelectable
    {
        void OnSelect(CourseAreaEditContext editContext);


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

            foreach (CourseActor act in ctx.GetSelectedObjects<CourseActor>())
            {
                act.mStartingTrans = act.mTranslation;
                act.mStartingRot = act.mRotation;
            }

            foreach (CourseRail.CourseRailPoint point in ctx.GetSelectedObjects<CourseRail.CourseRailPoint>())
            {
                point.mStartingTrans = point.mTranslation;
                if (point.mIsCurve)
                    point.mControl.mStartingTrans = point.mControl.mTranslation;
            }

            foreach (CourseRail.CourseRailPointControl point in ctx.GetSelectedObjects<CourseRail.CourseRailPointControl>())
                point.mStartingTrans = point.mTranslation;

            foreach (FushigiCursor cursor in ctx.GetSelectedObjects<FushigiCursor>())
                cursor.mStartingTrans = cursor.mTranslate;

            foreach (PolytopeVertex vertex in ctx.GetSelectedObjects<PolytopeVertex>())
                vertex.mStartingTrans = vertex.mTranslation;

            foreach (Sphere sphere in ctx.GetSelectedObjects<Sphere>())
                sphere.mStartingTrans = sphere.Center;
        }
    }
    #endregion
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
        private LevelViewportWM mWM = new LevelViewportWM();
        private Sprites sprite = new Sprites(gl);
        public Vector2 mSize = Vector2.Zero;
        public Vector2 mTopLeft = Vector2.Zero;
        public Vector2 vpMin;
        public Vector2 vpMax;
        public bool IsViewportHovered;
        public bool IsViewportActive;
        public bool isPanGesture;
        public ImDrawListPtr mDrawList;
        public static List<string> HiddenActors = new();
        public static List<string> HiddenModels = new();
        public bool PlayAnimations = false;
        public bool ShowGrid = true;
        public bool ShowBackground = true;
        public bool ShowActors = true;
        public static bool ShowRails = true;
        bool pasteContext = false;
        bool copyContext = false;
        bool deleteContext = false;
        public static CourseScene _courseScene;
        public static bool draggingCommentIcon;
        public void PreventFurtherRendering() => mIsNoMoreRendering = true;

        public ulong prevSelectVersion { get; private set; } = 0;
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
        private readonly HashSet<CourseUnit> mRegisteredUnits = [];
        private readonly HashSet<CourseActor> mRegisteredBgUnits = [];
        DistantViewManager DistantViewScrollManager = new DistantViewManager(area);

        List<CourseActor> backupSelection;
        private Vector2 storedMousePos;
        Vector2? mMultiSelectStartPos;
        Vector2? mMultiSelectCurrentPos;
        bool mMultiSelecting = false;
        bool mMultiSelectEnded = true;

        public static uint GridColor = 0x77_FF_FF_FF;
        public static float GridLineThickness = 1.5f;
        public static uint MultiSelectBoxColor = 0x90_00_00_FF;
        public static float MultiSelectBoxThickness = 5f;

        private static Vector2[] s_actorRectPolygon = new Vector2[4];
        public static bool setGlobalSrc;
        public static bool setGlobalDst;
        public static ulong globalHash;

        public CourseActor[] pivotedActors;
        private CourseComment commentToDelete;
        private bool draggingComment;
        private int commentVal;
        private bool canEditStart;
        public bool panOverride = false;

        public FushigiCursor cursor;
        private CourseRail.CourseRailPoint closestSelected;
        private List<(CourseRail rail, CourseRail.CourseRailPoint point)> deleteList;
        private bool multiRailDelete;
        public bool applyRotation;
        private bool DoTranslateObjects;
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

        public bool IsHovered(ISceneObject obj) => mHoveredObject == obj;
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

        public void isInMultiSelectBox(Vector2 pos, object obj)
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
                    if(!draggingComment && !panOverride)
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
            return mEditContext.IsAnySelected<CourseActor>() || mEditContext.IsAnySelected<CourseRail.CourseRailPoint>() ||
                   mEditContext.IsAnySelected<FushigiCursor>() || mEditContext.IsAnySelected<CourseRail.CourseRailPointControl>() || mEditContext.IsAnySelected<PolytopeVertex>() || mEditContext.IsAnySelected<Sphere>();
        }


        public bool IsNonTransformableSelected()
        {
            return mEditContext.IsAnySelected<BGUnitRail.RailPoint>();
        }
        public void HandleTranslation(object obj, Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            switch (obj)
            {
                case CourseActor:
                    HandleActorTranslation(StartingTrans, CurrentTrans);
                    break;
                case CourseRail.CourseRailPoint:
                    HandleCourseRailPointTranslation(StartingTrans, CurrentTrans);
                        break;
                case CourseRail.CourseRailPointControl:
                    HandleCourseRailPointControlTranslation(StartingTrans, CurrentTrans);
                    break;
                case FushigiCursor:
                    HandleCursorTranslation(StartingTrans, CurrentTrans);
                    break;
                case PolytopeVertex:
                    HandleVertexTranslation(StartingTrans, CurrentTrans);
                    break;
                case Sphere:
                    HandleSphereTranslation(StartingTrans, CurrentTrans);
                    break;
            }
        }
        public void ApplyTranslation(object obj)
        {
            switch (obj)
            {
                case CourseActor actor:
                    CommitTranslation(actor);
                    actor.mStartingTrans = actor.mTranslation;
                    break;
                case CourseRail.CourseRailPoint point:
                    if(point.mIsCurve)
                    //{
                    //    var batch = mEditContext.BeginBatchAction();
                    //}

                    CommitTranslation(point.mControl);
                    CommitTranslation(point);
                    break;
                case CourseRail.CourseRailPointControl point:
                    CommitTranslation(point);
                    break;
                    //case FushigiCursor:
                    //    ApplyCursorTranslation(StartingTrans, CurrentTrans);
                    //    break;
            }
        }

        public void HandleVertexTranslation(Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            foreach (PolytopeVertex vertex in mEditContext.GetSelectedObjects<PolytopeVertex>())
            {
                Vector3 relativePos = vertex.mStartingTrans - StartingTrans;
                vertex.X = CurrentTrans.X - relativePos.X;
                vertex.Y = CurrentTrans.Y - relativePos.Y;
                vertex.Z = CurrentTrans.Z - relativePos.Z;
            }
        }

        public void HandleSphereTranslation(Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            foreach (var sphere in mEditContext.GetSelectedObjects<Sphere>())
            {
                Vector3 relativePos = sphere.mStartingTrans - StartingTrans;
                var sphereCenter = sphere.Center;
                sphereCenter.X = CurrentTrans.X + relativePos.X;
                sphereCenter.Y = CurrentTrans.Y + relativePos.Y;
                sphereCenter.Z = CurrentTrans.Z + relativePos.Z;

                sphere.Center = sphereCenter;
            }
        }

        public void HandleActorTranslation(Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            foreach (CourseActor actor in mEditContext.GetSelectedObjects<CourseActor>())
            {
                Vector3 relativePos = actor.mStartingTrans - StartingTrans;
                actor.mTranslation.X = CurrentTrans.X + relativePos.X;
                actor.mTranslation.Y = CurrentTrans.Y + relativePos.Y;
                
                if(Course.IsWorldMap)
                    actor.mTranslation.Z = CurrentTrans.Z + relativePos.Z;
            }
        }

        public void HandleCourseRailPointTranslation(Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            foreach (CourseRail.CourseRailPoint p in mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>())
            {
                Vector3 relativePos = p.mStartingTrans - StartingTrans;
                p.mTranslation.X = CurrentTrans.X + relativePos.X;
                p.mTranslation.Y = CurrentTrans.Y + relativePos.Y;

                if (Course.IsWorldMap)
                    p.mTranslation.Z = CurrentTrans.Z + relativePos.Z;

                if (p.mIsCurve)
                {
                    Vector3 relativePosCtrl = p.mControl.mStartingTrans - p.mStartingTrans;
                    p.mControl.mTranslation.X = p.mTranslation.X + relativePosCtrl.X;
                    p.mControl.mTranslation.Y = p.mTranslation.Y + relativePosCtrl.Y;

                    if (Course.IsWorldMap)
                        p.mControl.mTranslation.Y = p.mTranslation.Y + relativePosCtrl.Y;
                }
            }
        }

        public void HandleCourseRailPointControlTranslation(Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            foreach (CourseRail.CourseRailPointControl p in mEditContext.GetSelectedObjects<CourseRail.CourseRailPointControl>())
            {
                Vector3 relativePos = p.mStartingTrans - StartingTrans;
                p.mTranslation.X = CurrentTrans.X + relativePos.X;
                p.mTranslation.Y = CurrentTrans.Y + relativePos.Y;

                if (Course.IsWorldMap)
                    p.mTranslation.Z = CurrentTrans.Z + relativePos.Z;
            }
        }

        public void HandleCursorTranslation(Vector3 StartingTrans, Vector3 CurrentTrans)
        {
            if (mEditContext.IsAnySelected<FushigiCursor>())
            {
                Vector3 relativePos = cursor.mStartingTrans - StartingTrans;
                cursor.mTranslate.X = CurrentTrans.X + relativePos.X;
                cursor.mTranslate.Y = CurrentTrans.Y + relativePos.Y;
            }
        }

        public void CommitTranslation(object obj)
        {
            string label = "";
            switch (obj)
            {
                case CourseActor actor:
                    label = obj.GetFieldValue("mPackName").ToString();
                    break;
                case CourseRail.CourseRailPoint point:
                    label = "Rail Point";
                    break;
                case CourseRail.CourseRailPointControl point:
                    label = "Rail Point Control";
                    break;
            }

            mEditContext.CommitAction(new PropertyFieldsSetUndo(
                 obj,
                 [("mTranslation", obj.GetFieldValue("mStartingTrans"))],
                 $"{IconUtil.ICON_ARROWS_ALT} Move {label}"));
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
                if (Course.IsWorldMap)
                    posVec.Z = MathF.Round(posVec.Z * 2, MidpointRounding.AwayFromZero) / 2;
                if (!ImGui.GetIO().KeyAlt)
                {
                    posVec.X += startingTrans.X - MathF.Round(startingTrans.X * 2, MidpointRounding.AwayFromZero) / 2;
                    posVec.Y += startingTrans.Y - MathF.Round(startingTrans.Y * 2, MidpointRounding.AwayFromZero) / 2;
                    if (Course.IsWorldMap)
                        posVec.Z += startingTrans.Z - MathF.Round(startingTrans.Z * 2, MidpointRounding.AwayFromZero) / 2;
                }
            }

            return posVec;
        }
        #endregion

        #region Rendering Logic

        public void DrawWM(Vector2 size, double deltaSeconds, IDictionary<string, bool> layersVisibility)
        {
            mLayersVisibility = layersVisibility;
            mWM.Draw(size, deltaSeconds, this, mEditContext, areaScene);
        }

        public void DrawCollisionVP(Vector2 size, double deltaSeconds, CourseActor actor, CollisionEditor colEditor)
        {
            colEditor.RightClickMenu(actor);
            colEditor.SelectionPanel(mEditContext);
            var io = ImGui.GetIO();
            float fps = 1.0f / io.DeltaTime;

            Vector2 mouse = ImGui.GetMousePos();
            Vector3 world = ScreenToWorld(mouse);

            mTopLeft = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton("canvas2", size,
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            bool isViewportLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
              ImGui.GetMouseDragDelta().Length() < 5;
            IsViewportHovered = IsViewportHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            IsViewportActive = ImGui.IsItemActive();

            ProcessModifiers();

            mSize = size;
            mDrawList = ImGui.GetWindowDrawList();
            ImGui.PushClipRect(mTopLeft, mTopLeft + size, true);

            colEditor.vp3D.HandleCameraControls(deltaSeconds, this);

            if (Camera.Width != mSize.X || Camera.Height != mSize.Y)
            {
                Camera.Width = mSize.X;
                Camera.Height = mSize.Y;
            }

            if (!Camera.UpdateMatrices())
                return;

            this.DrawScene3D(size, actor);

            if (ShowGrid)
                DrawGrid();

   
            colEditor.vp3D.Gizmos(IsViewportHovered, isViewportLeftClicked, out bool isAnyGizmoHovered, this);

            Selection();

            colEditor.DrawActorCollisionPoop(actor, mEditContext, this);

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

            if (!UserSettings.GetUseSprites())
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

                //continuously register all course units that haven't been registered yet
                //foreach (var actor in BgUnits)
                //{
                //    if (!tileRebuild)
                //    {
                //        if (mRegisteredBgUnits.Contains(actor))
                //            continue;
                //    }

                //    TileBfresRenderFieldB.LoadBGUnit(this.BgUnits);

                //    if (!tileRebuild)
                //    {
                //        mRegisteredBgUnits.Add(actor);
                //    }

                //    tileRebuild = false;

                //}

                if (!hasInitialized)
                {
                    tileRebuild = true;
                    hasInitialized = true;
                }

                if (tileRebuild || CourseUnit.UpdateTiles)
                {
                    if (TileBfresRenderFieldA is not null)            
                        TileBfresRenderFieldA.DoLoad(this.mArea.mUnitHolder, this.BgUnits);

                    if (TileBfresRenderFieldB is not null)
                        TileBfresRenderFieldB.DoLoad(this.mArea.mUnitHolder, this.BgUnits);

                    tileRebuild = false;
                    CourseUnit.UpdateTiles = false;
                }

                //foreach (var courseUnit in mArea.mUnitHolder.mUnits)
                //{
                //    if (mRegisteredUnits.Contains(courseUnit))
                //        continue;

                //    if (courseUnit.mSkinDivision == SkinDivision.FieldA && TileBfresRenderFieldA is not null)
                //    {
                //        courseUnit.TilesUpdated += () => TileBfresRenderFieldA.Load(this.mArea.mUnitHolder);
                //    }
                //    else if (courseUnit.mSkinDivision == SkinDivision.FieldB && TileBfresRenderFieldB is not null)
                //    {
                //        courseUnit.TilesUpdated += () => TileBfresRenderFieldB.Load(this.mArea.mUnitHolder);
                //    }

                //    mRegisteredUnits.Add(courseUnit);
                //}


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

                        //if (actor.mActorPack.BgUnitControl != null && actor.mActorPack.BgUnitControl.UnitType == "FullHitB")
                        //{
                        //    BgUnits.Add(actor);

                        //    //Console.WriteLine("does this run though");
                        //}
                        //actor.wonderVisible = WonderViewMode == actor.mWonderView ||
                        //                        WonderViewMode == WonderViewType.Normal ||
                        //                        actor.mWonderView == WonderViewType.Normal;

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

        public void DrawScene3D(Vector2 size, CourseActor actor)
        {
            if (mIsNoMoreRendering)
                goto SKIP_RENDERING; //sue me // ok


            if (Framebuffer == null)
                Framebuffer = new GLFramebuffer(gl, FramebufferTarget.Framebuffer, (uint)size.X, (uint)size.Y);

            //Resize if needed
            if (Framebuffer.Width != (uint)size.X || Framebuffer.Height != (uint)size.Y)
                Framebuffer.Resize((uint)size.X, (uint)size.Y);

            RenderStats.Reset();

            //Wonder shader system params
          

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

         
            //Display skybox
            EnvironmentData.RenderSky(gl, this.Camera);

            RenderActor(actor, actor.mActorPack.ModelInfoRef);
            RenderActor(actor, actor.mActorPack.DrawArrayModelInfoRef);

                   
                   
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
            if (!ScreenshotMode)
            {
                areaScene.ForEach<IViewportDrawable>(obj =>
                {
                    bool isNewHoveredObj = false;
                    obj.Draw2D(mEditContext, this, mDrawList, ref isNewHoveredObj);
                    if (isNewHoveredObj)
                        mHoveredObject = obj;
                });
            }
        }

        public void DrawActorCollision()
        {
            const float pointSize = 8.0f;
            foreach (CourseActor actor in mArea.GetActors())
            {
                if(HiddenActors.Contains(actor.mType.ToString()))
                    continue;

                Vector3 min = new(-.5f);
                Vector3 max = new(.5f);
                Vector3 off = new(0f);
                Vector3 center = new(0f);
                var drawing = "box";

                if (actor.mActorPack?.ShapeParams != null)
                {
                    var shapes = actor.mActorPack.ShapeParams;
                    var calc = shapes.mCalc;

                    if (((shapes.mSphere?.Count ?? 0) > 0) ||
                        ((shapes.mCapsule?.Count ?? 0) > 0))
                    {
                        drawing = "sphere";
                    }
                    else if ((shapes.mPoly?.Count ?? 0) > 0)
                    {
                        calc = shapes.mPoly[0].mCalc;
                    }

                    if (calc != null)
                    {
                        center = calc.mCenter;
                        min = calc.mMin;
                        max = calc.mMax;
                    }

                    // Fix this so that always min < max to avoid negative length sides
                    if (min.X == max.X)
                    {
                        if (min.X == 0)
                        {
                            min.X = -0.5f;
                            max.X = 0.5f;
                        }
                        else
                        {
                            min.X = -Math.Abs(min.X);
                            max.X = Math.Abs(max.X);
                        }
                    }
                    if (min.Y == max.Y)
                    {
                        if (min.Y == 0)
                        {
                            min.Y = -0.5f;
                            max.Y = 0.5f;
                        }
                        else
                        {
                            min.Y = -Math.Abs(min.Y);
                            max.Y = Math.Abs(max.Y);
                        }
                    }
                    if (min.Z == max.Z)
                    {
                        if (min.Z == 0)
                        {
                            min.Z = -0.5f;
                            max.Z = 0.5f;
                        }
                        else
                        {
                            min.Z = -Math.Abs(min.Z);
                            max.Z = Math.Abs(max.Z);
                        }
                    }
                }

                string layer = actor.mLayer;

                if (mLayersVisibility!.TryGetValue(layer, out bool isVisible) && isVisible && actor.wonderVisible && !ScreenshotMode)
                {
                    Matrix4x4 transform =
                        Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.Y, actor.mScale.Z
                        ) *
                        Matrix4x4.CreateRotationZ(
                            actor.mRotation.Z
                        ) *
                        Matrix4x4.CreateTranslation(
                            actor.mTranslation.X,
                            actor.mTranslation.Y,
                            actor.mTranslation.Z
                        ); ;

                    // Because fuck consistency I guess.
                    // (It's mostly cause usually AreaObjs use a distance calculation "NoModel_1x1x1", meaning a centered block.
                    //  While some use "SameActor" and "NoModel_1x1x1_Bottom" which place the origin at the bottom. I can't be bothered to
                    //  get this info and since it's only a handful of actors afaik, I'll just hardcode them here. If you got an issue with this,
                    //  feel free to change it.)
                    string[] halfOffsetCDP = {
                        "NoModel_1x1x1_Bottom",
                        "SameArea"
                    };

                    // Changed this cause it still wasn't correct
                    if (actor.mActorPack?.ShapeParams == null && halfOffsetCDP.Contains(actor.mCalcDistanceParam))
                        off = new(0, .5f, 0);

                    //topLeft
                    s_actorRectPolygon[0] = WorldToScreen(Vector3.Transform(new Vector3(min.X, max.Y, 0) + off, transform));
                    //topRight
                    s_actorRectPolygon[1] = WorldToScreen(Vector3.Transform(new Vector3(max.X, max.Y, 0) + off, transform));
                    //bottomRight
                    s_actorRectPolygon[2] = WorldToScreen(Vector3.Transform(new Vector3(max.X, min.Y, 0) + off, transform));
                    //bottomLeft
                    s_actorRectPolygon[3] = WorldToScreen(Vector3.Transform(new Vector3(min.X, min.Y, 0) + off, transform));

                    Vector2 topLeft = s_actorRectPolygon[0];
                    Vector2 bottomRight = s_actorRectPolygon[2];

                    Vector2 tl = s_actorRectPolygon[0];
                    Vector2 tr = s_actorRectPolygon[1];
                    Vector2 br = s_actorRectPolygon[2];
                    Vector2 bl = s_actorRectPolygon[3];

                    if (Sprites.OverrideSize.TryGetValue(actor.mPackName, out var size))
                    {
                        // Scale relative to center
                        Vector2 newCenter = (tl + br) * 0.5f;

                        tl = newCenter + (tl - newCenter) * size;
                        tr = newCenter + (tr - newCenter) * size;
                        br = newCenter + (br - newCenter) * size;
                        bl = newCenter + (bl - newCenter) * size;
                    }

                    if (UserSettings.GetUseSprites())
                    {
                        var key = actor.mPackName;
                        if (Sprites.SpriteAliases.TryGetValue(actor.mPackName, out var alias))
                            key = alias;

                        if (Sprites.ActorSprites.TryGetValue(key, out var tex))
                        {
                            mDrawList.AddImageQuad(
                                (IntPtr)tex.ID,
                                tl, 
                                tr, 
                                br,
                                bl, 
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                                new Vector2(0, 1),
                                0xFFFFFFFF
                            );
                        }
                    }

                    uint color = CourseActor.CourseActorColors[CourseActorType.None];
                    CourseActor.CourseActorColors.TryGetValue(actor.mType, out color);

                    bool isHovered = mHoveredObject == actor;

                    if (!ScreenshotMode)
                    {
                        switch (drawing)
                        {
                            default:
                                for (int i = 0; i < 4; i++)
                                {
                                    mDrawList.AddLine(
                                    s_actorRectPolygon[i],
                                    s_actorRectPolygon[(i + 1) % 4],
                                    color, isHovered ? 2.5f : 1.5f);
                                }
                                break;
                            case "sphere":
                                var pos = WorldToScreen(Vector3.Transform(center, transform));
                                var scale = Matrix4x4.CreateScale(actor.mScale);
                                Vector2 rad = (WorldToScreen(Vector3.Transform(max, scale)) - WorldToScreen(Vector3.Transform(min, scale))) / 2;
                                mDrawList.AddEllipse(pos, Math.Abs(rad.X), Math.Abs(rad.Y), color, -actor.mRotation.Z, 0, isHovered ? 2.5f : 1.5f);

                                break;
                        }
                        if (mEditContext.IsSelected(actor))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                mDrawList.AddCircleFilled(s_actorRectPolygon[i],
                                    pointSize, color);
                                if (drawing == "sphere")
                                {
                                    mDrawList.AddLine(
                                    s_actorRectPolygon[i],
                                    s_actorRectPolygon[(i + 1) % 4],
                                    color, isHovered ? 2.5f : 1.5f);
                                }
                            }
                            mDrawList.AddEllipse(WorldToScreen(transform.Translation), pointSize * 3, pointSize * 3, color, -actor.mRotation.Z, 4, 2);
                        }
                    }

                    string name = actor.mPackName;
                    isHovered = MathUtil.HitTestConvexPolygonPoint(s_actorRectPolygon, ImGui.GetMousePos());

                    if (name.Contains("Area"))
                    {
                        isHovered = MathUtil.HitTestLineLoopPoint(s_actorRectPolygon, 4f,
                            ImGui.GetMousePos());
                    }

                    if (isHovered)
                    {
                        mHoveredObject = actor;
                    }

                    if (mMultiSelecting)
                    {
                        float actorX = actor.mTranslation.X;
                        float actorY = actor.mTranslation.Y;

                        isInMultiSelectBox(new Vector2(actorX, actorY), actor);
                    }

                }
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

        void DrawGridLines(bool is_vertical, float min_minor_tick_size, int major_tick_interval)
        {
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
                min_value = maxWorld.Y;
                max_value = minWorld.Y;

                a = max.Y;
                b = min.Y;

                a0 = new Vector2(min.X, a);
                a1 = new Vector2(max.X, a);
                b0 = new Vector2(min.X, b);
                b1 = new Vector2(max.X, b);
            }
            else
            {
                min_value = minWorld.X;
                max_value = maxWorld.X;

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

        public void DrawRails()
        {
            if (mArea.mRailHolder.mRails.Count > 0 && !ScreenshotMode && ShowRails)
            {
                const float pointSize = 8.0f;
                uint color = Color.HotPink.ToAbgr();

                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {
                    bool rail_selected = mEditContext.IsSelected(rail);

                    Vector2[] GetPoints()
                    {
                        Vector2[] points = new Vector2[rail.mPoints.Count];
                        for (int i = 0; i < rail.mPoints.Count; i++)
                        {
                            Vector3 p = rail.mPoints[i].mTranslation;
                            points[i] = WorldToScreen(new(p.X, p.Y, p.Z));
                        }
                        return points;
                    }

                    bool hovered = MathUtil.HitTestLineLoopPoint(GetPoints(), 10f, ImGui.GetMousePos());
                    CourseRail.CourseRailPoint selectedPoint = null;

                    foreach (var point in rail.mPoints)
                    {
                        var pos2D = this.WorldToScreen(new(point.mTranslation.X, point.mTranslation.Y, point.mTranslation.Z));
                        var contPos2D = this.WorldToScreen(point.mControl.mTranslation);
                        Vector2 pnt = new(pos2D.X, pos2D.Y);
                        bool isHovered = (ImGui.GetMousePos() - pnt).Length() < 10.0f;

                        if (isHovered)
                            mHoveredObject = point;

                        bool selected = false;

                        if (closestSelected != null)
                        {
                            if (point == closestSelected)
                                selected = true;
                        }
                        else
                            selected = mEditContext.IsSelected(point) || mEditContext.IsSelected(point.mControl);

                        if (selected)
                        {
                            selectedPoint = point;
                            if ((ImGui.GetMousePos() - contPos2D).Length() < 10.0f)
                                mHoveredObject = point.mControl;
                        }
                    }

                    if (selectedPoint != null && (ImGui.IsKeyPressed(ImGuiKey.Delete) || (ImGui.GetIO().KeyShift && ImGui.IsKeyPressed(ImGuiKey.Backspace))))
                    {
                        if (mEditContext.GetObjectCountOfType<CourseRail.CourseRailPoint>() > 1)
                        {
                            var railPoints = mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().ToArray();
                            multiRailDelete = true;
                            foreach (var point in railPoints)
                            {
                                if (rail.mPoints.Contains(point))
                                    deleteList.Add((rail, point));
                            }
                        }
                        else
                            mEditContext.DeleteRailPoint(rail, selectedPoint);
                    }

                    bool add_point = ImGui.IsMouseClicked(0) && ImGui.IsMouseDown(0) && ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyShift && !mEditContext.IsAnySelected<CourseActor>();

                    //Insert point to existing rail
                    if (selectedPoint != null && add_point)
                    {
                        var index = rail.mPoints.IndexOf(selectedPoint);
                        var newPoint = new CourseRail.CourseRailPoint(selectedPoint, rail);
                        addRailPoint(index, newPoint, rail);

                    }
                    //Add first point to rail
                    else if (rail_selected && add_point)
                    {
                        var newPoint = new CourseRail.CourseRailPoint(rail.mType, rail);
                        addRailPoint(-1, newPoint, rail);
                    }
                }

                if (multiRailDelete)
                {
                    Console.WriteLine("Batch deleting " + deleteList.Count + " rail points");
                    var batch = mEditContext.BeginBatchAction();

                    foreach (var (rail, point) in deleteList)
                    {
                        var revertible = rail.mPoints.RevertableRemove(point);
                        mEditContext.CommitAction(revertible);
                    }

                    batch.Commit($"{IconUtil.ICON_TRASH} Delete Rail Points");
                    multiRailDelete = false;
                    deleteList.Clear();
                }

                // Draw Rails to the Viewport
                mDrawList.Flags &= ~ImDrawListFlags.AntiAliasedLines;
                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {

                    bool selected = mEditContext.IsSelected(rail);

                    if (selected && rail.mPoints.Count == 0 && ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyShift)
                    {
                        Vector3 pos = ScreenToWorld(ImGui.GetMousePos());

                        pos.X = MathF.Round(pos.X * 2) / 2;
                        pos.Y = MathF.Round(pos.Y * 2) / 2;

                        Vector2 pos2D = WorldToScreen(pos);

                        mDrawList.AddCircleFilled(pos2D, pointSize, ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)));

                        continue;
                    }

                    if (rail.mPoints.Count == 0)
                        continue;

                    var rail_color = selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;

                    List<Vector2> pointsList = [];

                    var segmentCount = rail.mPoints.Count;
                    if (!rail.mIsClosed)
                        segmentCount--;

                    mDrawList.PathLineTo(WorldToScreen(rail.mPoints[0].mTranslation));
                    for (int i = 0; i < segmentCount; i++)
                    {
                        var pointA = rail.mPoints[i];
                        var pointB = rail.mPoints[(i + 1) % rail.mPoints.Count];

                        var posA2D = WorldToScreen(pointA.mTranslation);
                        var posB2D = WorldToScreen(pointB.mTranslation);

                        Vector2 cpOutA2D = posA2D;
                        Vector2 cpInB2D = posB2D;

                        if (pointA.mIsCurve)
                            cpOutA2D = WorldToScreen(pointA.mControl.mTranslation);

                        if (pointB.mIsCurve)
                            cpInB2D = WorldToScreen(pointB.mTranslation - (pointB.mControl.mTranslation - pointB.mTranslation));

                        if (cpOutA2D == posA2D && cpInB2D == posB2D)
                        {
                            mDrawList.PathLineTo(posB2D);
                            continue;
                        }

                        mDrawList.PathBezierCubicCurveTo(cpOutA2D, cpInB2D, posB2D);
                    }

                    float thickness = mHoveredObject == rail ? 4f : 3.5f;

                    mDrawList.PathStroke(rail_color, ImDrawFlags.None, thickness);
                    float closestDist = float.MaxValue;

                    Vector2 mouse = ImGui.GetMousePos();

                    closestSelected = null;

                    foreach (var pnt in rail.mPoints)
                    {
                        bool point_selected = mEditContext.IsSelected(pnt) || mEditContext.IsSelected(pnt.mControl);
                        if (!point_selected)
                            continue;

                        Vector2 pos2D = WorldToScreen(pnt.mTranslation);
                        float dist = Vector2.Distance(pos2D, mouse);

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestSelected = pnt;
                        }
                    }

                    foreach (var pnt in rail.mPoints)
                    {
                        bool point_selected = mEditContext.IsSelected(pnt) || mEditContext.IsSelected(pnt.mControl);
                        var rail_point_color = point_selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;
                        var size = 10.0f;

                        Vector2 pos2D = WorldToScreen(pnt.mTranslation);
                        mDrawList.AddCircleFilled(pos2D, size, rail_point_color);

                        if (mHoveredObject == pnt)
                            mDrawList.AddCircle(pos2D, 15.0f, rail_point_color, 10, 1.5f);

                        pointsList.Add(pos2D);

                        if (pnt == closestSelected && ImGui.GetIO().KeyAlt && !ImGui.IsMouseDragging(ImGuiMouseButton.Left) && !ImGui.GetIO().KeyShift && !mEditContext.IsAnySelected<CourseActor>())
                        {
                            Vector3 previewPos = ScreenToWorld(mouse);

                            previewPos.X = MathF.Round(previewPos.X * 2) / 2;
                            previewPos.Y = MathF.Round(previewPos.Y * 2) / 2;
                            previewPos.Z = pnt.mTranslation.Z;

                            Vector2 preview2D = WorldToScreen(previewPos);

                            mDrawList.AddLine(pos2D, preview2D, rail_point_color, 2.5f);
                            mDrawList.AddCircleFilled(preview2D, size, rail_point_color);
                        }

                        if (point_selected && pnt.mIsCurve)
                        {
                            var contPos2D = WorldToScreen(pnt.mControl.mTranslation);
                            mDrawList.AddLine(pos2D, contPos2D, rail_point_color, thickness);
                            mDrawList.AddCircleFilled(contPos2D, size, rail_point_color);

                            if (mHoveredObject == pnt.mControl)
                                mDrawList.AddCircle(contPos2D, 15.0f, rail_point_color, 10, 1.5f);
                        }

                        if (mMultiSelecting)
                        {
                            float pntX = pnt.mTranslation.X;
                            float pntY = pnt.mTranslation.Y;

                            isInMultiSelectBox(new Vector2(pntX, pntY), pnt);
                        }
                    }

                }
                mDrawList.Flags |= ImDrawListFlags.AntiAliasedLines;
            }
        }

        public void addRailPoint(int index, CourseRail.CourseRailPoint newPoint, CourseRail rail)
        {
            Vector3 posVec = this.ScreenToWorld(ImGui.GetMousePos());
            float zCord = 0;
            if (index != -1)
            {
                zCord = newPoint.mTranslation.Z;
            }
            newPoint.mTranslation = new(
                MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2,
                MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2,
                zCord);

            newPoint.mControl.mTranslation = newPoint.mTranslation + new Vector3(0, 1, 0);


            if (rail.mPoints.Count - 1 == index || index == -1)
                mEditContext.AddRailPoint(rail, newPoint);
            else
            {
                (float distance, int index) min = (float.PositiveInfinity, -1);

                if (index != 0)
                {
                    for (int i = 0; i < rail.mPoints.Count - 1; i++)
                    {
                        var pointA = rail.mPoints[i].mTranslation;
                        var pointB = rail.mPoints[i + 1].mTranslation;

                        var ab = pointB - pointA;
                        var length = ab.Length();
                        if (length < 0.0001f)
                            continue;

                        var dir = ab / length;

                        var t = Vector3.Dot(posVec - pointA, dir) / length;
                        if (t < 0 || t > 1)
                            continue;

                        var normal = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitZ));
                        float distance = MathF.Abs(Vector3.Dot(posVec - pointA, normal));

                        if (distance <= min.distance)
                            min = (distance, i + 1);

                    }
                }
                else
                    min.index = 0;

                mEditContext.InsertRailPoint(rail, newPoint, min.index);
            }
            


            this.mEditContext.DeselectAll();
            this.mEditContext.Select(newPoint);
            mHoveredObject = newPoint;
        }

        public void DrawAreaContent()
        {
            mHoveredObject = null;
            deleteList = new List<(CourseRail rail, CourseRail.CourseRailPoint point)>();

            DrawUnits();
            DrawRails();
            DrawCursor();
            DragComment();
            if (!Course.IsWorldMap)
            {
                DrawActorCollision();
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
                for (var i = 0; i < actors.Length; i++)
                {
                    var actor = actors[i];
                    CourseActor newActor;
                    AddLayer(actor.mLayer);
                    if (freshCopy)
                        newActor = new CourseActor(actor.mPackName, actor.mAreaHash, actor.mLayer);
                    else
                        newActor = actor.Clone(mArea);

                    newActor.mTranslation = (Vector3)_pos + (actor.mTranslation - CopiedMedianPosition);
                    newActor.mTranslation.X = MathF.Round(newActor.mTranslation.X * 2) / 2;
                    newActor.mTranslation.Y = MathF.Round(newActor.mTranslation.Y * 2) / 2;
                    newActor.mTranslation.Z = actor.mTranslation.Z;
                    var n = 0;
                    do
                    {
                        n++;
                    } while (area.GetActors().Any(x => x.mName == $"{actor.mPackName}{n}"));
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

            PivotActors();

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (mHoveredObject == null)
                {
                    if (!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                        mEditContext.DeselectAll();
                }
                else if (mHoveredObject is IViewportSelectable selectable)
                {
                    prevSelectVersion = mEditContext.SelectionVersion;
                    selectable.OnSelect(mEditContext);
                }
                else
                {
                    prevSelectVersion = mEditContext.SelectionVersion;
                    IViewportSelectable.DefaultSelect(mEditContext, mHoveredObject);

                }
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
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) &&
                        prevSelectVersion == mEditContext.SelectionVersion || CourseScene.bypassSelection)
                    {
                        CourseScene.bypassSelection = false;
                        mEditContext.Deselect(mHoveredObject!);
                    }
                    else if (!ImGui.IsKeyDown(ImGuiKey.LeftShift))
                    {
                        mEditContext.DeselectAll();
                        IViewportSelectable.DefaultSelect(mEditContext, mHoveredObject);
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
                    AddComment();


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
                        PrefabPopup();
                    }
                }

                ImGui.Separator();
                if (cursor == null)
                {
                    if (ImGui.MenuItem("Place Cursor"))
                    {
                        cursor = new FushigiCursor();
                        CursorPlacement();
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Move Cursor"))
                        CursorPlacement();

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
                else if (mMultiSelectStartPos != null && !ImGui.IsWindowHovered() && !IsNonTransformableSelected())
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
                            case CourseActor actor:
                                StartingTrans = actor.mStartingTrans;
                                CurrentTrans = actor.mTranslation;
                                tileRebuild = true;
                                break;

                            case CourseRail.CourseRailPoint mPoint:
                                StartingTrans = mPoint.mStartingTrans;
                                CurrentTrans = mPoint.mTranslation;
                                break;
                            case PolytopeVertex mVertex:
                                StartingTrans = mVertex.mStartingTrans;
                                CurrentTrans = mVertex.mTranslation;
                                break;
                            case Sphere sphere:
                                StartingTrans = sphere.Center;
                                CurrentTrans = sphere.mStartingTrans;
                                break;
                        }

                    if (Camera.IsOrthographic)
                    {
                        var posVec = CalcPosVec(StartingTrans);
                        CurrentTrans.X = posVec.X;
                        CurrentTrans.Y = posVec.Y;
                        if (Course.IsWorldMap || EditorMode.editMode == "Collision")
                            CurrentTrans.Z = posVec.Z;

                        foreach (object obj in mEditContext.GetSelectedObjects<object>())
                            HandleTranslation(obj, StartingTrans, CurrentTrans);

                        if (StartingTrans != CurrentTrans)
                            DoTranslateObjects = true;

                    }
                    else 
                        mWM.HandleDrag3D();
                }
            }

            // Save Object Translation to history
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && DoTranslateObjects)
            {
                var objCount = mEditContext.GetObjectCount();

                if (objCount > 1)
                {
                    var batch = mEditContext.BeginBatchAction();

                    foreach (object obj in mEditContext.GetSelectedObjects<object>())
                    {
                        ApplyTranslation(obj);
                    }

                    batch.Commit($"{IconUtil.ICON_ARROWS_ALT} Move {objCount} Objects");
                }
                else
                {
                    if (mEditContext.IsSingleObjectSelected(out CourseRail.CourseRailPoint point) && point.mIsCurve) {
                        var batch = mEditContext.BeginBatchAction();
                        ApplyTranslation(point);
                        ApplyTranslation(point.mControl);
                        batch.Commit($"{IconUtil.ICON_ARROWS_ALT} Move Rail Point");
                    }
                    else       
                        ApplyTranslation(mEditContext.GetFirstObject());
                }

                DoTranslateObjects = false;
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

        #region 2D Cursor

        public void PivotActors()
        {
            if (ImGui.IsKeyPressed(ImGuiKey.R))
            {
                if (mEditContext.GetSelectedObjects<CourseActor>().ToArray().Length >= 1 && mEditContext.GetSelectedObjects<FushigiCursor>().ToArray().Length == 1)
                {
                    pivotedActors = mEditContext.GetSelectedObjects<CourseActor>().ToArray();
                    foreach (CourseActor actor in pivotedActors)
                    {
                        Vector2 delta = ImGui.GetIO().MouseDelta;
                        float rotationSpeed = 0.05f;
                        float deltaAngle = ImGui.GetIO().MouseDelta.X * rotationSpeed;
                        Vector3 cursorTrans = cursor.mTranslate;
                        actor.mRotation.Z += deltaAngle;
                        actor.mTranslation = cursorTrans + Vector3.Transform(actor.mTranslation - cursorTrans, Matrix4x4.CreateRotationZ(deltaAngle));

                    }

                    if (pivotedActors[0].mRotation != pivotedActors[0].mStartingRot)
                        applyRotation = true;
                }
            }

            if (applyRotation && !ImGui.IsKeyDown(ImGuiKey.R))
            {
                applyRotation = false;

                if (pivotedActors.Length == 1)
                {
                    CommitRotation(pivotedActors[0]);
                    CommitTranslation(pivotedActors[0]);
                }
                else
                {
                    var batch = mEditContext.BeginBatchAction();

                    foreach (var actor in pivotedActors)
                    {
                        CommitRotation(actor);
                        CommitTranslation(actor);
                    }
                    batch.Commit($"{IconUtil.ICON_ARROWS_ALT} Pivoted {pivotedActors.Count()} Actors");
                }

            }
        }

        public void DrawCursor()
        {
            if (cursor != null)
            {
                var cursorPos2D = this.WorldToScreen(new(cursor.mTranslate.X, cursor.mTranslate.Y, cursor.mTranslate.Z));
                Vector2 pnt = new(cursorPos2D.X, cursorPos2D.Y);
                bool isHovered = (ImGui.GetMousePos() - pnt).Length() < 10.0f;

                if (isHovered)
                    mHoveredObject = cursor;

                uint color = Color.BlueViolet.ToAbgr();
                bool point_selected = mEditContext.IsSelected(cursor);
                var rail_point_color = point_selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;
                var size = 10.0f;

                var pos2D = WorldToScreen(cursor.mTranslate);
                mDrawList.AddCircleFilled(pos2D, size, rail_point_color);

                if (mHoveredObject == cursor)
                    mDrawList.AddCircle(pos2D, 15.0f, rail_point_color, 10, 1.5f);

            }
        }

        public void CursorPlacement()
        {
            var pos = ScreenToWorld(storedMousePos);
            cursor.mTranslate.X = MathF.Round(pos.X * 2, MidpointRounding.AwayFromZero) / 2;
            cursor.mTranslate.Y = MathF.Round(pos.Y * 2, MidpointRounding.AwayFromZero) / 2;
            cursor.mTranslate.Z = 0.0f;
        }

        #endregion

        #region Comments
        public void DragComment()
        {
            if (draggingComment || draggingCommentIcon)
            {
                if (!mMultiSelecting && mEditContext.IsSingleObjectSelected(out CourseComment? comment))
                {
                    if (canEditStart)
                    {
                        comment.mStartingTrans = comment.mTranslation;
                        canEditStart = false;
                    }

                    var posVec = CalcPosVec(comment.mStartingTrans);
                    comment.mTranslation.X = posVec.X;
                    comment.mTranslation.Y = posVec.Y;
                }
            }
            else
            {
                canEditStart = true;
            }
        }

        private void AddComment()
        {
            var comment = new CourseComment();
            var pos = ScreenToWorld(storedMousePos);
            comment.mTranslation.X = MathF.Round(pos.X * 2, MidpointRounding.AwayFromZero) / 2;
            comment.mTranslation.Y = MathF.Round(pos.Y * 2, MidpointRounding.AwayFromZero) / 2;
            comment.mTranslation.Z = 0.0f;
            mEditContext.AddComment(comment);
        }

        public void DrawComments()
        {
            int i = 0;
            foreach (CourseComment comment in mArea.GetComments())
            {
                i++;
                Vector2 pos = WorldToScreen(comment.mTranslation);
                Vector2 iconPos = new Vector2(pos.X - (30 * MainWindow.dpiScale), pos.Y - (20 * MainWindow.dpiScale));
                ImGui.SetCursorScreenPos(iconPos);


                ImGui.BeginChild(
                    $"CommentIcon{i}",
                    new Vector2(40 * MainWindow.dpiScale, 40 * MainWindow.dpiScale),
                    ImGuiChildFlags.None,
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoBackground 
                );


                ImGui.SetCursorPos(Vector2.Zero);
             

                if (ImGui.Button(IconUtil.ICON_MAIL_BULK, new Vector2(40 * MainWindow.dpiScale, 40 * MainWindow.dpiScale)))
                    {
                        if (!draggingCommentIcon)
                            comment.mOpened = !comment.mOpened;
            
                    }

                if (!comment.mOpened &&
                  ImGui.IsItemActive() &&
                  ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    draggingCommentIcon = true;
                    if (mEditContext.GetSelectedObjects<object>().Count() > 1)
                    {
                        mEditContext.DeselectAll();
                    }
                    mEditContext.Select(comment);

    

                    if (!comment.mOpened && ImGui.IsItemActive() && ImGui.IsKeyPressed(ImGuiKey.Delete))     
                    {            
                            commentToDelete = comment;
                            commentVal = i;
                    }
                }


                bool iconHovered = ImGui.IsItemHovered();

                if (iconHovered)
                    panOverride = true;

                if (iconHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (mEditContext.GetSelectedObjects<object>().Count() > 1)
                        mEditContext.DeselectAll();

                    mEditContext.Select(comment);
                }


                ImGui.EndChild();

                if (!comment.mOpened)
                    continue;

                    ImGui.SetCursorScreenPos(pos);

                ref string text = ref comment.mText;
                ImGui.BeginChild(
                  $"CommentWindow{i}",
                  new Vector2(200 * MainWindow.dpiScale, 100 * MainWindow.dpiScale),
                  ImGuiChildFlags.None,
                  ImGuiWindowFlags.NoDecoration |
                  ImGuiWindowFlags.NoScrollbar |
                  ImGuiWindowFlags.NoScrollWithMouse
                 );

                ImGui.InputTextMultiline(
                    $"##Comment{i}",
                    ref text,
                    1024,
                    new Vector2(200 * MainWindow.dpiScale, 100 * MainWindow.dpiScale)
                );


                    bool textActive = ImGui.IsItemActive();
                    bool textFocused = ImGui.IsItemFocused();
                    bool textHovered = ImGui.IsItemHovered();

                if (textHovered)
                {
                    panOverride = true;
                }


                    if (textActive || textFocused)
                    {
                        draggingComment = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                            if (mEditContext.GetSelectedObjects<object>().Count() > 1)
                        {
                            mEditContext.DeselectAll();
                        }
                        mEditContext.Select(comment);
                        if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                        {
                            commentToDelete = comment;
                            commentVal = i;
                        }
                }
                    ImGui.EndChild();

    
            }

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                draggingCommentIcon = false;

            if(!CourseScene.insideViewport)
                panOverride = false;

            if(commentToDelete != null)
            {
              mEditContext.RemoveComment(commentToDelete, commentVal);
              commentToDelete = null;
            }
        }
        #endregion

        #region Prefabs 
        public void SavePrefab(string prefabName)
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

                mArea.SaveActorsToPrefab(copiedActors, actors, prefabName, courseRailsClone, courseRails);
            }
        }

        public async Task PrefabPopup()
        {
            var result = await SavePrefabDialog.ShowDialog(MainWindow.mModalHost, "Save Prefab", "Enter name for this prefab");

            if (result.Result == SavePrefabDialog.DialogResult.Yes)
            {
                SavePrefab(result.PrefabName);
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