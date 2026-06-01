using EditorToolkit.Core;
using EditorToolkit.ImGui;
using Fasterflect;
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
using Silk.NET.OpenGL;
using Silk.NET.SDL;
using System.Numerics;
using static DiscordRPC.User;

namespace Fushigi.ui.widgets
{
    public class CollisionEditor
    {
        public List<string> Nodes = new List<string>();
        public static bool init = false;
        LevelViewport viewport = null;
        private CourseActor CollisionActor = new CourseActor("EnemyKuribo", 0, "PlayArea");
        private string mActorSearchAll = "";

        private bool _isDraggingFromOrientationCube;
        private bool hasSetCamera;
        private string prevSearch;
        private List<string> filteredActors = new List<string>();
        private bool isSearch;

        public Vector3? HitPointOnPlane(Vector3 planePoint, Vector3 planeNormal)
        => HitPointOnPlane(planePoint, planeNormal, ImGui.GetMousePos());

        public Vector3? HitPointOnPlane(Vector3 planePoint, Vector3 planeNormal, Vector2 mousePos)
        {
            (Vector3 rayOrigin, Vector3 rayDirection) = GetMouseRay(mousePos);
            var res = MathUtil.IntersectPlaneRay(rayDirection, rayOrigin, planeNormal, planePoint);

            var depth = Vector3.Dot(res - GetCameraPosition(), GetCameraForwardDirection());

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
                var planeNormal = -GetCameraForwardDirection();
                var prevMousePos = ImGui.GetMousePos() - ImGui.GetIO().MouseDelta;
                var preCameraTarget = VP.Camera.Target;
                VP.Camera.Target +=
                    HitPointOnPlane(planeOrigin, planeNormal, prevMousePos) -
                    HitPointOnPlane(planeOrigin, planeNormal) ?? Vector3.One;

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
        internal void Draw(GLTaskScheduler scheduler, double delta)
        {
            ActorsPanel();
            ImGui.Begin("Collision Viewport");
            var size = ImGui.GetContentRegionAvail();
            var drawPos = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(drawPos);
            
            if (!init)
            {
                _ = fuck(scheduler);
                init = true;
            }

        
            if(viewport != null)
            {
                if (!hasSetCamera)
                {
                    viewport.Camera.Distance = 10;
                    viewport.Camera.IsOrthographic = false;
                    hasSetCamera = true;
                }

                viewport.DrawSimple(size, delta, CollisionActor, this);
          
            }
            ImGui.End();
        }

        internal async Task fuck (GLTaskScheduler scheduler) {
            string romFSPath = UserSettings.GetRomFSPath();
            await scheduler.Schedule(gl => RomFS.SetRoot(romFSPath, gl));
            var area = new CourseArea("dummy", false);
            viewport = await scheduler.Schedule(gl => new LevelViewport(area, gl, new CourseAreaScene(area, new CourseAreaSceneRoot(area))));
            }

        public void ActorsPanel()
        {
            ImGui.Begin("Actors");
            ImGui.InputText("##ActorSearch", ref mActorSearchAll, 0x100);
            bool isSearch = !string.IsNullOrWhiteSpace(mActorSearchAll);
            if (prevSearch != mActorSearchAll)
            {
                filteredActors.Clear();
                prevSearch = mActorSearchAll;

                foreach (var actor in ParamDB.GetActors())
                {

                    bool HasText = actor.IndexOf(mActorSearchAll, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isSearch && !HasText)
                        continue;

                    filteredActors.Add(actor);
                }

            }

            ImGui.BeginChild("ActorScroll", ImGui.GetContentRegionAvail());


            if (ImGui.BeginTable("##ActorsAndLayers", 1))
            {
                    int i = 0;
                    foreach (string actor in filteredActors)
                    {

                        ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Selectable(actor);
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        CollisionActor = new CourseActor(actor, 0, "PlayArea");

                }
                

                ImGui.EndTable();
            }
            ImGui.EndChild();
            ImGui.End();
        }

        public (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay(Vector2 mousePos)
        {
            var mouseRayBegin = viewport.ScreenToWorld(mousePos, -1);
            var mouseRayEnd = viewport.ScreenToWorld(mousePos, 1);

            return (mouseRayBegin, Vector3.Normalize(mouseRayEnd - mouseRayBegin));
        }
        public (Vector3 rayOrigin, Vector3 rayDirection) GetMouseRay()
              => GetMouseRay(ImGui.GetMousePos());

        public Vector3 GetCameraPosition() => viewport.Camera.Target - GetCameraForwardDirection() * viewport.Camera.Distance;

        public Vector3 GetCameraForwardDirection() => Vector3.Transform(-Vector3.UnitZ, viewport.Camera.Rotation);

        internal void Gizmos(bool viewportHovered, bool viewportClicked, out bool isAnyGizmoHovered, LevelViewport VP)
        {
            var camForward = Vector3.Transform(-Vector3.UnitZ, VP.Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, VP.Camera.Rotation);
            GizmoDrawer.BeginGizmoDrawing("ViewportGizmos", VP.mDrawList, new SceneViewState(
                new CameraState(GetCameraPosition(), camForward, camUp, VP.Camera.Rotation),
                VP.Camera.ViewProjectionMatrix, new Rect(VP.mTopLeft, VP.mTopLeft + VP.mSize), ImGui.GetMousePos(), GetMouseRay()
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