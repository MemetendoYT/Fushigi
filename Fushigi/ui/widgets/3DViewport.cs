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
using Silk.NET.SDL;
using System.Numerics;
using static DiscordRPC.User;

namespace Fushigi.ui.widgets
{
    public class Viewport3D {

        //LevelViewport viewport = null;
        private bool _isDraggingFromOrientationCube;
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

            if ((ImGui.IsMouseDragging(ImGuiMouseButton.Left) &&
                VP.modifiers == KeyboardModifier.Shift && ActiveTool is null))
                isPanGesture = true;

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
                    if (MathF.Acos(Vector3.Dot(camForward, -facingDirection)) < 0.1f)
                        VP.Camera.IsOrthographic = !VP.Camera.IsOrthographic;

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
            }

            GizmoDrawer.EndGizmoDrawing(out isAnyGizmoHovered);
        }

    }
}