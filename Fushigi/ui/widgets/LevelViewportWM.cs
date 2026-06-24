
using EditorToolkit.Core;
using EditorToolkit.ImGui;
using Fushigi.course;
using Fushigi.gl;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.ConjugateGradient;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using System.ComponentModel.Design;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ZstdSharp.Unsafe;

internal class LevelViewportWM()
{
    public event Action? ActiveToolChanged;


    //public (IViewportDrawable obj, Vector3 hitPoint)? _draggedObject = null;
    public CourseAreaEditContext ctx;
    private LevelViewport VP;
    bool hasSetCamera = false;
    private ImDrawListPtr dl;
    private bool _isDraggingFromOrientationCube;
    private bool _canStartNewTransformAction;
    internal Vector3 mDragClickWorld;
    public Viewport3D vp3D = new Viewport3D();


    public Vector3 GetCameraForwardDirection() => Vector3.Transform(-Vector3.UnitZ, VP.Camera.Rotation);



    public void Draw(Vector2 size, double deltaSeconds, LevelViewport viewport, CourseAreaEditContext mEditContext, CourseAreaScene areaScene)
    {
        ctx = mEditContext;
        VP = viewport;
        if (!ImGui.BeginChild("LevelViewport", size))
        {
            ImGui.EndChild();
            return;
        }

        //if (!hasSetCamera)
        //{
        //    VP.Camera.Distance = 10;
        //    VP.Camera.IsOrthographic = false;
        //    hasSetCamera = true;
        //}

        DrawViewport(size, deltaSeconds, areaScene);
    }

    public void DrawViewport(Vector2 size, double deltaSeconds, CourseAreaScene areaScene)
    {
        object? newHoveredObject = null;
        ImGui.InvisibleButton("Viewport", ImGui.GetContentRegionAvail(),
            ImGuiButtonFlags.MouseButtonLeft |
            ImGuiButtonFlags.MouseButtonRight |
            ImGuiButtonFlags.MouseButtonMiddle);

        ImGui.PushClipRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), false);

        VP.IsViewportHovered = ImGui.IsItemHovered();
        VP.IsViewportActive = ImGui.IsItemActive();

        VP.ProcessModifiers();

        VP.mTopLeft = ImGui.GetItemRectMin();
        VP.mSize = ImGui.GetItemRectSize();

        bool isViewportLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                ImGui.GetMouseDragDelta().Length() < 5;

        if (VP.Camera.Width != VP.mSize.X || VP.Camera.Height != VP.mSize.Y)
        {
            VP.Camera.Width = VP.mSize.X;
            VP.Camera.Height = VP.mSize.Y;
        }

        vp3D.HandleCameraControls(deltaSeconds, VP);

        if (!VP.Camera.UpdateMatrices())
            return;


        VP.mDrawList = ImGui.GetWindowDrawList();

        VP.mHoveredObject = null;
        VP.DrawScene3D(size, VP.mLayersVisibility);

        vp3D.Gizmos(VP.IsViewportHovered, isViewportLeftClicked, out bool isAnyGizmoHovered, VP);

        VP.DrawAreaContent();
        foreach (CourseActor actor in VP.mArea.GetActors())
        {
            vp3D.TestDrawActor2(actor, VP, ctx);
        }

        VP.Selection();

        ImGui.PopClipRect();
    }

 

    //private void HandleCameraControls(double deltaSeconds)
    //{
    //    var io = ImGui.GetIO();
    //    bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle);
    //    string ActiveTool = null;
    //    const float baseCameraSpeed = 0.25f * 60;
    //    const float scalingRate = 10.0f;
    //    var dt = (float)deltaSeconds;
    //    var zoomSpeedFactor = Math.Max(VP.Camera.Distance / scalingRate, 1);
    //    var zoomedCameraSpeed = MathF.Floor(zoomSpeedFactor) * baseCameraSpeed;

    //    float zoomFactor = Math.Max(VP.Camera.Distance / scalingRate, 1);
    //    const float baseSpeed = 0.25f * 60;

    //    if ((ImGui.IsMouseDragging(ImGuiMouseButton.Left) &&
    //        VP.modifiers == KeyboardModifier.Shift && ActiveTool is null))
    //        isPanGesture = true;

    //    if (VP.IsViewportActive && isPanGesture)
    //    {
    //        var planeOrigin = VP.Camera.Target;
    //        var planeNormal = -GetCameraForwardDirection();
    //        var prevMousePos = ImGui.GetMousePos() - ImGui.GetIO().MouseDelta;
    //        var preCameraTarget = VP.Camera.Target;
    //        VP.Camera.Target +=
    //            vp3D.HitPointOnPlane(planeOrigin, planeNormal, prevMousePos, VP) -
    //            vp3D.HitPointOnPlane(planeOrigin, planeNormal, VP) ?? Vector3.One;

    //    }

    //    if (VP.IsViewportHovered)
    //    {
    //        VP.Camera.Distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);
    //    }


    //    if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
    //    {
    //        var mouseDelta = ImGui.GetIO().MouseDelta;
    //        VP.Camera.Rotation =
    //            Quaternion.CreateFromAxisAngle(Vector3.UnitY, mouseDelta.X * -0.01f) * VP.Camera.Rotation;

    //        VP.Camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, mouseDelta.Y * -0.01f);

    //        VP.Camera.UpdateMatrices();
    //    }


    //    Vector3 forward = Vector3.Transform(-Vector3.UnitZ, VP.Camera.Rotation);
    //    Vector3 rightDir = Vector3.Transform(Vector3.UnitX, VP.Camera.Rotation);
    //    Vector3 up = Vector3.Transform(Vector3.UnitY, VP.Camera.Rotation);

    //    Vector3 movement = Vector3.Zero;
    //    float speed = MathF.Floor(zoomFactor) * baseSpeed;


    //    if (!VP.Camera.IsOrthographic)
    //    {
    //        if (ImGui.IsKeyDown(ImGuiKey.W)) movement += forward;
    //        if (ImGui.IsKeyDown(ImGuiKey.S)) movement -= forward;
    //    }
    //    else
    //    {
    //        if (ImGui.IsKeyDown(ImGuiKey.W)) movement.Z += up.Z;
    //        if (ImGui.IsKeyDown(ImGuiKey.S)) movement.Z -= up.Z;
    //    }
    //    if (ImGui.IsKeyDown(ImGuiKey.A)) movement -= rightDir;
    //    if (ImGui.IsKeyDown(ImGuiKey.D)) movement += rightDir;

    //    VP.Camera.Target += movement * speed * dt;

    //    var keyMoveUp = ImGuiKey.Q;
    //    var keyMoveDown = ImGuiKey.E;
    //    if (VP.Camera.IsOrthographic)
    //    {
    //        keyMoveUp = ImGuiKey.W;
    //        keyMoveDown = ImGuiKey.S;
    //    }

    //    //if (!VP.Camera.IsOrthographic)
    //    //{
    //    //    if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.Q) && !io.KeyCtrl)
    //    //    {
    //    //        VP.Camera.Target.Y += zoomedCameraSpeed * dt;
    //    //    }

    //    //    if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.E) && !io.KeyCtrl)
    //    //    {
    //    //        VP.Camera.Target.Y -= zoomedCameraSpeed * dt;
    //    //    }
    //    //}
    //    //else
    //    //{
    //    //    if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.S) && !io.KeyCtrl)
    //    //    {
    //    //        VP.Camera.Target.Z += zoomedCameraSpeed * dt;
    //    //    }

    //    //    if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.W) && !io.KeyCtrl)
    //    //    {
    //    //        VP.Camera.Target.Z -= zoomedCameraSpeed * dt;
    //    //    }
    //    //}
    //}


}