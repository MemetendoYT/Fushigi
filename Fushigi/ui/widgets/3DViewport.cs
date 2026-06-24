using EditorToolkit.Core;
using EditorToolkit.ImGui;
using Fasterflect;
using Fushigi.actor_pack.components;
using Fushigi.course;
using Fushigi.env;
using Fushigi.gl;
using Fushigi.param;
using Fushigi.ui.modal;
using Fushigi.ui.SceneObjects;
using Fushigi.util;
using FuzzySharp.Edits;
using ImGuiNET;
using OpenAbility.ImGui.Nodes;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;
using static DiscordRPC.User;

namespace Fushigi.ui.widgets
{
    // Full Credits to Jupahe64 for the 3D Viewport code that originated from https://github.com/jupahe64/MarioToyStudio
    public class Viewport3D {

        private bool _isDraggingFromOrientationCube;
        public static bool CameraSnapped = true;

        internal Vector3? HitPointOnPlane(Vector3 planePoint, Vector3 planeNormal, LevelViewport VP)
        => HitPointOnPlane(planePoint, planeNormal, ImGui.GetMousePos(), VP);

        internal Vector3? HitPointOnPlane(Vector3 planePoint, Vector3 planeNormal, Vector2 mousePos, LevelViewport VP)
        {
            (Vector3 rayOrigin, Vector3 rayDirection) = GetMouseRay(mousePos, VP);
            var res = MathUtil.IntersectPlaneRay(rayDirection, rayOrigin, planeNormal, planePoint);

            var depth = Vector3.Dot(res - GetCameraPosition(VP), GetCameraForwardDirection(VP));

            return (depth > 10_000 || depth < 0) ? null : res;
        }

        internal void HandleCameraControls(double deltaSeconds, LevelViewport VP)
        {
            var io = ImGui.GetIO();
            bool isPanGesture = ImGui.IsMouseDragging(ImGuiMouseButton.Middle);
            string ActiveTool = null;
            const float baseCameraSpeed = 0.25f * 60;
            const float scalingRate = 10.0f;
            var dt = (float)deltaSeconds;
            var zoomSpeedFactor = Math.Max(VP.Camera.Distance / scalingRate, 1);
            var zoomedCameraSpeed = MathF.Floor(zoomSpeedFactor) * baseCameraSpeed;

            float zoomFactor = Math.Max(VP.Camera.Distance / scalingRate, 1);
            const float baseSpeed = 0.25f * 60;

            //if ((ImGui.IsMouseDragging(ImGuiMouseButton.Left) &&
            //    VP.modifiers == KeyboardModifier.Shift && ActiveTool is null))
            //    isPanGesture = true;

            if (VP.IsViewportActive && isPanGesture)
            {
                var planeOrigin = VP.Camera.Target;
                var planeNormal = -GetCameraForwardDirection(VP);
                var prevMousePos = ImGui.GetMousePos() - ImGui.GetIO().MouseDelta;
                var preCameraTarget = VP.Camera.Target;
                VP.Camera.Target +=
                    HitPointOnPlane(planeOrigin, planeNormal, prevMousePos, VP) -
                    HitPointOnPlane(planeOrigin, planeNormal, VP) ?? Vector3.One;
            }

            if (VP.IsViewportHovered)
            {
                VP.Camera.Distance *= MathF.Pow(2, -ImGui.GetIO().MouseWheel / 10);
            }


            if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                CameraSnapped = false;
                var mouseDelta = ImGui.GetIO().MouseDelta;
                VP.Camera.Rotation =
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, mouseDelta.X * -0.01f) * VP.Camera.Rotation;

                VP.Camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, mouseDelta.Y * -0.01f);

                VP.Camera.UpdateMatrices();
            }


            Vector3 forward = Vector3.Transform(-Vector3.UnitZ, VP.Camera.Rotation);
            Vector3 rightDir = Vector3.Transform(Vector3.UnitX, VP.Camera.Rotation);
            Vector3 up = Vector3.Transform(Vector3.UnitY, VP.Camera.Rotation);

            Vector3 movement = Vector3.Zero;
            float speed = MathF.Floor(zoomFactor) * baseSpeed;


            //if (!VP.Camera.IsOrthographic)
            //{
            //    if (ImGui.IsKeyDown(ImGuiKey.W)) movement += forward;
            //    if (ImGui.IsKeyDown(ImGuiKey.S)) movement -= forward;
            //}
            //else
            //{
            //    if (ImGui.IsKeyDown(ImGuiKey.W)) movement.Z += up.Z;
            //    if (ImGui.IsKeyDown(ImGuiKey.S)) movement.Z -= up.Z;
            //}
            //if (ImGui.IsKeyDown(ImGuiKey.A)) movement -= rightDir;
            //if (ImGui.IsKeyDown(ImGuiKey.D)) movement += rightDir;

            VP.Camera.Target += movement * speed * dt;

            //var keyMoveUp = ImGuiKey.Q;
            //var keyMoveDown = ImGuiKey.E;
            //if (VP.Camera.IsOrthographic)
            //{
            //    keyMoveUp = ImGuiKey.W;
            //    keyMoveDown = ImGuiKey.S;
            //}

            //if (!VP.Camera.IsOrthographic)
            //{
            //    if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.Q) && !io.KeyCtrl)
            //    {
            //        VP.Camera.Target.Y += zoomedCameraSpeed * dt;
            //    }

            //    if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.E) && !io.KeyCtrl)
            //    {
            //        VP.Camera.Target.Y -= zoomedCameraSpeed * dt;
            //    }
            //}
            //else
            //{
            //    if (ImGui.IsKeyDown(ImGuiKey.UpArrow) || ImGui.IsKeyDown(ImGuiKey.S) && !io.KeyCtrl)
            //    {
            //        VP.Camera.Target.Z += zoomedCameraSpeed * dt;
            //    }

            //    if (ImGui.IsKeyDown(ImGuiKey.DownArrow) || ImGui.IsKeyDown(ImGuiKey.W) && !io.KeyCtrl)
            //    {
            //        VP.Camera.Target.Z -= zoomedCameraSpeed * dt;
            //    }
            //}
        }


        internal void TestDrawActor(Box Box, LevelViewport viewport, CourseAreaEditContext mEditContext)
        {
            var hitPoint = HitPointOnPlane(Box.Center, GetCameraForwardDirection(viewport), viewport);
            //if (!IsVisible || !_visibilityParent.IsVisible)
            //    return;
            uint color = CourseActor.CourseActorColors[CourseActorType.None];
            //CourseActor.CourseActorColors.TryGetValue(actor.mType, out color);

            if (mEditContext.IsSelected(Box))
                color = Color.White.ToAbgr();

            bool hovered = false;
            Quaternion quat = MathUtil.QuatFromEulerXYZ(Box.mRotation);

            var mtx =
                Matrix4x4.CreateScale(Box.mExtents * 2) *
                Matrix4x4.CreateFromQuaternion(quat) *
                Matrix4x4.CreateTranslation(Box.Center);


            List<Vector2[]> allFaces = new();
            List<Vector3> allNormals = new();
            void BuildFaces(Vector3 normal, Vector3 rightVec, ref Vector3? hitPoint)

            {
                Vector3 upVec = Vector3.Cross(rightVec, normal);

                Span<Vector3> points =
                [
                        Vector3.Transform(normal*.5f+rightVec*-.5f+upVec*+.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*+.5f+upVec*+.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*-.5f+upVec*-.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*+.5f+upVec*-.5f, mtx),
                ];

                Span<Vector2> points2D =
                [
                   viewport.WorldToScreen(points[1]),
                   viewport.WorldToScreen(points[0]),
                   viewport.WorldToScreen(points[2]),
                   viewport.WorldToScreen(points[3]),
            ];

                var camForward = -GetCameraForwardDirection(viewport); // note the minus
                var worldNormal = Vector3.TransformNormal(normal, mtx);

                // cull when the face is pointing away from the camera
                if (Vector3.Dot(worldNormal, camForward) <= 0)
                    return;

                hovered |= MathUtil.HitTestConvexPolygonPoint(points2D, ImGui.GetMousePos());

                if (Math.Asin(Vector3.Dot(Vector3.Transform(normal, quat), -camForward)) > Math.PI / 4)
                {
                    if (
                    Vector2.DistanceSquared(points2D[0], ImGui.GetMousePos()) < 4 * 4 ||
                    Vector2.DistanceSquared(points2D[1], ImGui.GetMousePos()) < 4 * 4 ||
                    Vector2.DistanceSquared(points2D[2], ImGui.GetMousePos()) < 4 * 4 ||
                    Vector2.DistanceSquared(points2D[3], ImGui.GetMousePos()) < 4 * 4)
                    {
                        hovered = true;
                    }
                }
                allFaces.Add(points2D.ToArray());
                allNormals.Add(normal);
            }

            void ProcessFace(Vector2[] points2D, Vector3 normal, uint color)
            {
                viewport.mDrawList.AddPolyline(ref points2D[0], points2D.Length,
                color, ImDrawFlags.Closed, 1.5f);

                var camForward = GetCameraForwardDirection(viewport);
                if (Math.Asin(Vector3.Dot(Vector3.Transform(normal, quat), -camForward)) > Math.PI / 4)
                {
                    viewport.mDrawList.AddCircleFilled(points2D[0], 4, color);
                    viewport.mDrawList.AddCircleFilled(points2D[1], 4, color);
                    viewport.mDrawList.AddCircleFilled(points2D[2], 4, color);
                    viewport.mDrawList.AddCircleFilled(points2D[3], 4, color);
                }
            }

            BuildFaces(Vector3.UnitX, Vector3.UnitZ, ref hitPoint);
            BuildFaces(-Vector3.UnitX, -Vector3.UnitZ, ref hitPoint);
            BuildFaces(Vector3.UnitY, Vector3.UnitX, ref hitPoint);
            BuildFaces(-Vector3.UnitY, -Vector3.UnitX, ref hitPoint);
            BuildFaces(Vector3.UnitZ, Vector3.UnitX, ref hitPoint);
            BuildFaces(-Vector3.UnitZ, -Vector3.UnitX, ref hitPoint);

            if (hovered && viewport.mHoveredObject == null)
            {
                color = Color.DarkGray.ToAbgr();
                hitPoint = HitPointOnPlane(Box.Center, GetCameraForwardDirection(viewport), viewport);
                viewport.mHoveredObject = Box;

                //var label = actor.mPackName ?? actor.mName;
                //if (!string.IsNullOrEmpty(label))
                //    ImGui.SetTooltip(label);
            }

            //if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            //{
            //    ctx.DeselectAll();
            //    ctx.Select(VP.mHoveredObject);
            //}


            int i = 0;
            foreach (var face in allFaces)
            {
                ProcessFace(face, allNormals[i], color);
                i++;
            }
        }

        internal void TestDrawActor2(CourseActor actor, LevelViewport viewport, CourseAreaEditContext mEditContext)
        {
            var hitPoint = HitPointOnPlane(actor.mTranslation, GetCameraForwardDirection(viewport), viewport);
            //if (!IsVisible || !_visibilityParent.IsVisible)
            //    return;
            uint color = CourseActor.CourseActorColors[CourseActorType.None];
            CourseActor.CourseActorColors.TryGetValue(actor.mType, out color);

            if (mEditContext.IsSelected(actor))
                color = Color.White.ToAbgr();

            bool hovered = false;
            Quaternion quat = MathUtil.QuatFromEulerXYZ(actor.mRotation);

            var mtx =
                Matrix4x4.CreateScale(actor.mScale) *
                Matrix4x4.CreateFromQuaternion(quat) *
                Matrix4x4.CreateTranslation(actor.mTranslation);


            List<Vector2[]> allFaces = new();
            List<Vector3> allNormals = new();
            void BuildFaces(Vector3 normal, Vector3 rightVec, ref Vector3? hitPoint)

            {
                Vector3 upVec = Vector3.Cross(rightVec, normal);

                Span<Vector3> points =
                [
                        Vector3.Transform(normal*.5f+rightVec*-.5f+upVec*+.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*+.5f+upVec*+.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*-.5f+upVec*-.5f, mtx),
                    Vector3.Transform(normal*.5f+rightVec*+.5f+upVec*-.5f, mtx),
                ];

                Span<Vector2> points2D =
                [
                   viewport.WorldToScreen(points[1]),
                   viewport.WorldToScreen(points[0]),
                   viewport.WorldToScreen(points[2]),
                   viewport.WorldToScreen(points[3]),
            ];

                var camForward = -GetCameraForwardDirection(viewport); // note the minus
                var worldNormal = Vector3.TransformNormal(normal, mtx);

                // cull when the face is pointing away from the camera
                if (Vector3.Dot(worldNormal, camForward) <= 0)
                    return;

                if (viewport.IsViewportHovered)
                {
                    if (!actor.mPackName.Contains("Area") && !actor.mPackName.Contains("Frustum"))
                        hovered |= MathUtil.HitTestConvexPolygonPoint(points2D, ImGui.GetMousePos());
                    else
                        hovered |= MathUtil.HitTestLineLoopPoint(points2D, 4f, ImGui.GetMousePos());
                }
                if (Math.Asin(Vector3.Dot(Vector3.Transform(normal, quat), -camForward)) > Math.PI / 4)
                {
                    if (
                    Vector2.DistanceSquared(points2D[0], ImGui.GetMousePos()) < 4 * 4 ||
                    Vector2.DistanceSquared(points2D[1], ImGui.GetMousePos()) < 4 * 4 ||
                    Vector2.DistanceSquared(points2D[2], ImGui.GetMousePos()) < 4 * 4 ||
                    Vector2.DistanceSquared(points2D[3], ImGui.GetMousePos()) < 4 * 4)
                    {
                        hovered = true;
                    }
                }
                allFaces.Add(points2D.ToArray());
                allNormals.Add(normal);
            }

            void ProcessFace(Vector2[] points2D, Vector3 normal, uint color)
            {
                viewport.mDrawList.AddPolyline(ref points2D[0], points2D.Length,
                color, ImDrawFlags.Closed, 1.5f);

                var camForward = GetCameraForwardDirection(viewport);
                if (Math.Asin(Vector3.Dot(Vector3.Transform(normal, quat), -camForward)) > Math.PI / 4)
                {
                    viewport.mDrawList.AddCircleFilled(points2D[0], 4, color);
                    viewport.mDrawList.AddCircleFilled(points2D[1], 4, color);
                    viewport.mDrawList.AddCircleFilled(points2D[2], 4, color);
                    viewport.mDrawList.AddCircleFilled(points2D[3], 4, color);
                }
            }

            BuildFaces(Vector3.UnitX, Vector3.UnitZ, ref hitPoint);
            BuildFaces(-Vector3.UnitX, -Vector3.UnitZ, ref hitPoint);
            BuildFaces(Vector3.UnitY, Vector3.UnitX, ref hitPoint);
            BuildFaces(-Vector3.UnitY, -Vector3.UnitX, ref hitPoint);
            BuildFaces(Vector3.UnitZ, Vector3.UnitX, ref hitPoint);
            BuildFaces(-Vector3.UnitZ, -Vector3.UnitX, ref hitPoint);

            if (hovered && viewport.mHoveredObject == null)
            {
                color = Color.LightPink.ToAbgr();
                hitPoint = HitPointOnPlane(actor.mTranslation, GetCameraForwardDirection(viewport), viewport);
                viewport.mHoveredObject = actor;

                var label = actor.mPackName ?? actor.mName;
                if (!string.IsNullOrEmpty(label))
                    ImGui.SetTooltip(label);
            }

            //if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            //{
            //    ctx.DeselectAll();
            //    ctx.Select(VP.mHoveredObject);
            //}


            int i = 0;
            foreach (var face in allFaces)
            {
                ProcessFace(face, allNormals[i], color);
                i++;
            }
        }

        internal (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(Vector2 mousePos, LevelViewport VP)
        {
            var mouseRayBegin = VP.ScreenToWorld(mousePos, -1);
            var mouseRayEnd = VP.ScreenToWorld(mousePos, 1);

            return (mouseRayBegin, Vector3.Normalize(mouseRayEnd - mouseRayBegin));
        }
        internal (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(LevelViewport VP)
              => GetMouseRay(ImGui.GetMousePos(), VP);

        internal Vector3 GetCameraPosition(LevelViewport vp) => vp.Camera.Target - GetCameraForwardDirection(vp) * vp.Camera.Distance;

        internal Vector3 GetCameraForwardDirection(LevelViewport VP) => Vector3.Transform(-Vector3.UnitZ, VP.Camera.Rotation);

        internal void Gizmos(bool viewportHovered, bool viewportClicked, out bool isAnyGizmoHovered, LevelViewport VP)
        {
            var camForward = Vector3.Transform(-Vector3.UnitZ, VP.Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, VP.Camera.Rotation);

            GizmoDrawer.BeginGizmoDrawing("ViewportGizmos", VP.mDrawList, new SceneViewState(
                new CameraState(GetCameraPosition(VP), camForward, camUp, VP.Camera.Rotation),
                VP.Camera.ViewProjectionMatrix, new Rect(VP.mTopLeft, VP.mTopLeft + VP.mSize), ImGui.GetMousePos(), GetMouseRay(VP)
                ));

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                _isDraggingFromOrientationCube = false;

            if (GizmoDrawer.OrientationCube(VP.mTopLeft + VP.mSize with { Y = 0 } + new Vector2(-40, 40), 40, out Vector3 facingDirection))
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    _isDraggingFromOrientationCube = true;

                facingDirection = Vector3.Normalize(facingDirection);
                if (viewportClicked)
                {
                    //if (MathF.Acos(Vector3.Dot(camForward, -facingDirection)) < 0.1f)
                    //    VP.Camera.IsOrthographic = !VP.Camera.IsOrthographic;

                    if (MathF.Abs(facingDirection.Y) == 1)
                    {
                        var upVec = Vector3.Cross(Vector3.UnitX, -facingDirection);
                        VP.Camera.Rotation =
                        Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateWorld(Vector3.Zero, -facingDirection, upVec));
                    }
                    else
                    {
                        VP.Camera.Rotation =
                        Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateWorld(Vector3.Zero, -facingDirection, Vector3.UnitY));
                    }

                    CameraSnapped = true;
                    VP.Camera.UpdateMatrices();
                }
            }

            if (_isDraggingFromOrientationCube)
            {
                var mouseDelta = ImGui.GetIO().MouseDelta;
                VP.Camera.Rotation =
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, mouseDelta.X * -0.01f) * VP.Camera.Rotation;

                VP.Camera.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, mouseDelta.Y * -0.01f);

                VP.Camera.UpdateMatrices();
                CameraSnapped = false;
            }

            GizmoDrawer.EndGizmoDrawing(out isAnyGizmoHovered);
        }

    }
}