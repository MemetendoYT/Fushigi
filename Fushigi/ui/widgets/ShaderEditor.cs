using CommunityToolkit.HighPerformance;
using Fushigi.actor_pack.components;
using Fushigi.Byml;
using Fushigi.course;
using Fushigi.gl;
using Fushigi.gl.Bfres;
using Fushigi.param;
using Fushigi.ui.SceneObjects;
using Fushigi.util;
using ImGuiNET;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;
using static Fushigi.gl.Bfres.TileBfresRender;
using Vector3 = System.Numerics.Vector3;

namespace Fushigi.ui.widgets
{
    public class ShaderEditor
    {
        public static bool init = false;
        LevelViewport viewport = null;
        private string mActorSearchAll = "";

        private bool hasSetCamera;
        private string prevSearch;
        public Viewport3D vp3D = new Viewport3D();
        CourseAreaEditContext mEditContext = null;
        public static BfresRender.BfresModel bfresModel;
        private List<string> filteredActors = new List<string>();
        public static CourseActor CollisionActor = new CourseActor("EnemyKuribo", 0, "PlayArea");
        private float prevFront;
        private uint capsuleColor = ImGui.ColorConvertFloat4ToU32(new(0.125f, 0.988f, 0.561f, 0.4f));
        private int meshNum;
        private string materialName = "";
        private BfresMaterialRender currentMaterial;
        private List<BfresMaterialRender> materialsList;

        internal void Draw(GLTaskScheduler scheduler, double delta, GL gl)
        {
            ActorsPanel();
            ImGui.Begin("Shader Viewport");
            var size = ImGui.GetContentRegionAvail();
            var drawPos = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(drawPos);

            if (!init)
            {
                _ = InitLevel(scheduler);
                init = true;
            }

            if (viewport != null)
            {
                DrawCollisionVP(size, delta, CollisionActor, viewport, gl);

            }
            ImGui.End();
        }
        internal void DrawCollisionVP(Vector2 size, double deltaSeconds, CourseActor actor, LevelViewport viewport, GL gl)
        {
            var modelInfo = CollisionActor.mActorPack.ModelInfoRef;
            var resourceName = modelInfo.mFilePath;
            var render = BfresCache.Load(gl, resourceName);
            var modelName = modelInfo.mModelName;
            if (render == null || !render.Models.TryGetValue(modelName, out BfresRender.BfresModel? value))
                return;

            bfresModel = value;
            

            RightClickMenu(actor);
            SelectionPanel(mEditContext);
            var io = ImGui.GetIO();
            float fps = 1.0f / io.DeltaTime;


            Vector2 mouse = ImGui.GetMousePos();
            Vector3 world = viewport.ScreenToWorld(mouse);

            viewport.mTopLeft = ImGui.GetCursorScreenPos();

            ImGui.InvisibleButton("canvas2", size,
                ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);

            bool isViewportLeftClicked = ImGui.IsItemDeactivated() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
              ImGui.GetMouseDragDelta().Length() < 5;
            viewport.IsViewportHovered = viewport.IsViewportHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
            viewport.IsViewportActive = ImGui.IsItemActive();

            viewport.ProcessModifiers();

            viewport.mSize = size;
            viewport.mDrawList = ImGui.GetWindowDrawList();
            ImGui.PushClipRect(viewport.mTopLeft, viewport.mTopLeft + size, true);

            vp3D.HandleCameraControls(deltaSeconds, viewport);

            if (viewport.Camera.Width != viewport.mSize.X || viewport.Camera.Height != viewport.mSize.Y)
            {
                viewport.Camera.Width = viewport.mSize.X;
                viewport.Camera.Height = viewport.mSize.Y;
            }

            if (!viewport.Camera.UpdateMatrices())
                return;

            viewport.DrawScene3D(size, null);

            if (viewport.ShowGrid)
                DrawGrid(viewport);


            vp3D.Gizmos(viewport.IsViewportHovered, isViewportLeftClicked, out bool isAnyGizmoHovered, viewport);

            viewport.Selection();

        }
      

        internal void RightClickMenu(CourseActor actor)
        {
          
        }


        internal void SelectionPanel(CourseAreaEditContext editContext)
        {
            if (!ImGui.Begin("Shader Selection"))
                return;

            //if (ImGui.BeginCombo("##Dropdown", "Select File"))
            //{
            //    foreach (var collisionFile in CollisionActor.mActorPack.shapes)
            //    {
            //        if (ImGui.Selectable(collisionFile.FilePath))
            //        {
            //            CollisionActor.mActorPack.updateCollisionFile(collisionFile.FilePath);
            //        }
            //    }
            //    ImGui.EndCombo();
            //}

            ImGui.InputText("material name", ref materialName, 64);

            //if (materialsList == null) 
            //    materialsList = bfresModel.GetMaterialsList();

            //materialsList = bfresModel.GetMaterialsList();

            if (materialsList.Count > 0)
            {
                currentMaterial ??= materialsList[0];

                if (ImGui.BeginCombo("Material", currentMaterial.Name))
                {
                    foreach (var material in materialsList)
                    {
                        bool selected = currentMaterial == material;

                        if (ImGui.Selectable(material.Name, selected))
                            currentMaterial = material;

                        if (selected)
                            ImGui.SetItemDefaultFocus();
                    }

                    ImGui.EndCombo();
                }
            }


            if (currentMaterial != null)
            {
                var floatVal = (float)currentMaterial.GetParam("material_fresnel_offset");
                if (ImGui.InputFloat("material fresnel", ref floatVal))
                {
                    currentMaterial.SetParam("material_fresnel_offset", floatVal);
                }
            }

            ImGui.End();
        }

        internal void DrawGrid(LevelViewport viewport)
        {
            var camForward = Vector3.Transform(-Vector3.UnitZ, viewport.Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, viewport.Camera.Rotation);

            if ((camForward.X < -0.7 && camForward.X > -0.8) || (camForward.X > 0.7 && camForward.X < 0.8) ||
                (camForward.Y < -0.7 && camForward.Y > -0.8) || (camForward.Y > 0.7 && camForward.Y < 0.8))
                Viewport3D.CameraSnapped = false;

            if (Viewport3D.CameraSnapped)
            {
                viewport.DrawGridLines(false, 20f, 10);
                viewport.DrawGridLines(true, 20f, 10);
            }
        }
        internal async Task InitLevel(GLTaskScheduler scheduler)
        {
            string romFSPath = UserSettings.GetRomFSPath();
            await scheduler.Schedule(gl => RomFS.SetRoot(romFSPath, gl));
            var area = new CourseArea("dummy", false);
            var areaScene = new CourseAreaScene(area, new CourseAreaSceneRoot(area));
            viewport = await scheduler.Schedule(gl => new LevelViewport(area, gl, areaScene));
            mEditContext = areaScene.EditContext;
        }

        internal void DrawActorCollisionPoop(CourseActor actor, CourseAreaEditContext mEditContext, LevelViewport VP)
        {
            VP.mHoveredObject = null;
            var shapeParams = actor.mActorPack?.ShapeParams;

            var shapesList = new List<Object>();


            if (shapeParams != null)
            {
                if (shapeParams.mBox?.Count > 0)
                {
                    foreach (var box in shapeParams.mBox)
                        shapesList.Add(box);
                }
                //shapesList.Add("box");

                if (shapeParams.mSphere?.Count > 0)
                {
                    foreach (var sphere in shapeParams.mSphere)
                        shapesList.Add(sphere);
                }

                if (shapeParams.mPoly?.Count > 0)
                {
                    foreach (var mPoly in shapeParams.mPoly)
                        shapesList.Add(mPoly);
                }

                if (shapeParams.mCapsule?.Count > 0)
                {
                    foreach (var capsule in shapeParams.mCapsule)
                        shapesList.Add(capsule);
                }
            }

            //if (shapeParams.mCapsule?.Count > 0)
            //    shapesList.Add("capsule");

            //if (shapeParams.mPoly?.Count > 0)
            //    shapesList.Add("polytope");


            const float pointSize = 8.0f;

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
                //else if ((shapes.mPoly?.Count ?? 0) > 0)
                //{
                //    calc = shapes.mPoly[0].mCalc;
                //}

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

            string[] halfOffsetCDP = {
                        "NoModel_1x1x1_Bottom",
                        "SameArea"
                    };

            // Changed this cause it still wasn't correct
            if (actor.mActorPack?.ShapeParams == null && halfOffsetCDP.Contains(actor.mCalcDistanceParam))
                off = new(0, .5f, 0);

            uint color = CourseActor.CourseActorColors[CourseActorType.None];
            CourseActor.CourseActorColors.TryGetValue(actor.mType, out color);

            bool isHovered = VP.mHoveredObject == actor;
            int index = 0;
            foreach (var shape in shapesList)
            {
                switch (shape)
                {
                    case Box Box:
                        vp3D.TestDrawActor(Box, viewport, mEditContext);
                        break;
                    case Sphere Sphere:
                        HandleSphereCollision(Sphere, actor, center, transform, color);
                        break;
                    case Polytope polytope:
                        HandlePolytopeCollision(polytope, color);
                        break;
                    case Capsule Capsule:
                        HandleCapsuleCollision(Capsule, actor, center, transform, color);
                        break;
                }
            }
        }
        public static Vector3 RotatePoint(Vector3 point, float angleDeg)
        {
            float radians = MathF.PI * angleDeg / 180f;

            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            float xRot = point.X * cos - point.Y * sin;
            float yRot = point.X * sin + point.Y * cos;

            return new Vector3(xRot, yRot, point.Z);
        }


        public static Vector3 translatePoint(Vector3 point, Capsule capsule)
        {
            point = RotatePoint(point, capsule.OffsetRotation.Z);

            point += capsule.OffsetTranslation;
            return point;
        }



        internal Vector2 DrawCapsulePoint(Capsule Capsule, CourseActor actor, Vector3 center, Matrix4x4 transform, uint color, bool point)
        {
            var capsulePoint = Capsule.mCenterA;

            if (point)
                capsulePoint = Capsule.mCenterB;

            capsulePoint.Parent = Capsule;
            var centerPoint = translatePoint(capsulePoint.Center, Capsule);
            Vector3 worldCenter = Vector3.Transform(centerPoint, transform);
            float worldRadius = actor.mScale.X * Capsule.Radius;
            var camForward = Vector3.Transform(-Vector3.UnitZ, viewport.Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, viewport.Camera.Rotation);
            var camRight = Vector3.Normalize(Vector3.Cross(camUp, camForward));

            Vector2 screenCenter = viewport.WorldToScreen(worldCenter);
            Vector3 worldOffset = worldCenter + camRight * worldRadius;

            Vector2 screenOffset = viewport.WorldToScreen(worldOffset);
            float screenRadius = Vector2.Distance(screenCenter, screenOffset);
            viewport.mDrawList.AddCircle(screenCenter, screenRadius, color);
            Capsule.screenRadius = screenRadius;
            bool hovered = Vector2.Distance(ImGui.GetMousePos(), screenCenter) <= screenRadius;

            if (hovered)
                viewport.mHoveredObject = capsulePoint;

            return screenCenter;
        }
        internal void HandleCapsuleCollision(Capsule Capsule, CourseActor actor, Vector3 center, Matrix4x4 transform, uint color)
        {
            if (mEditContext.IsSelected(Capsule))
                color = Color.BlueViolet.ToAbgr();

            var pointA = DrawCapsulePoint(Capsule, actor, center, transform, color, false);
            var pointB = DrawCapsulePoint(Capsule, actor, center, transform, color, true);
            viewport.mDrawList.AddLine(pointA, pointB, capsuleColor, Capsule.screenRadius * 2f);
        }

        public static Vector3 InverseTranslatePoint(Vector3 worldPoint, Capsule capsule)
        {
            worldPoint -= capsule.OffsetTranslation;
            worldPoint = RotatePoint(worldPoint, -capsule.OffsetRotation.Z);
            return worldPoint;
        }



        public static double cross(PolytopeVertex O, PolytopeVertex A, PolytopeVertex B)
        {
            return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
        }

        public static List<PolytopeVertex> GetConvexHull(List<PolytopeVertex> points)
        {
            if (points == null)
                return null;

            if (points.Count() <= 1)
                return points;

            int n = points.Count(), k = 0;
            List<PolytopeVertex> H = new List<PolytopeVertex>(new PolytopeVertex[2 * n]);

            points.Sort((a, b) =>
                 a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

            // Build lower hull
            for (int i = 0; i < n; ++i)
            {
                while (k >= 2 && cross(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && cross(H[k - 2], H[k - 1], points[i]) <= 0)
                    k--;
                H[k++] = points[i];
            }

            return H.Take(k - 1).ToList();
        }

        public List<Vector2> calcPos(Polytope Polytope, List<PolytopeVertex> vectors, bool zOverride)
        {
            var posList = new List<Vector2>();

            var zValue = Polytope.zBack;
            foreach (var vertex in vectors)
            {
                if (!zOverride)
                    zValue = vertex.Center.Z;

                posList.Add(viewport.WorldToScreen(new(vertex.Center.X, vertex.Center.Y, zValue)));
            }

            return posList;
        }

        public void DrawPolytope(Polytope Polytope, List<Vector2> points, uint color)
        {
            int i = 0;
            foreach (var point in points)
            {
                color = Color.PaleGreen.ToAbgr();
                if (mEditContext.IsSelected(Polytope.mergeList[i]))
                {
                    color = Color.BlueViolet.ToAbgr();
                    Polytope.mergeList[i].Parent = Polytope;
                }

                Vector2 pnt = new(point.X, point.Y);

                viewport.mDrawList.AddCircleFilled(pnt, 7, color);

                if ((ImGui.GetMousePos() - pnt).Length() < 10.0f)
                    viewport.mHoveredObject = Polytope.mergeList[i];

                i++;
            }
        }

        public void DrawZ(List<Vector2> front, List<Vector2> back, uint color)
        {
            for (int i = 0; i < front.Count; i++)
            {
                var frontPnt = front[i];
                var backPnt = back[i];
                viewport.mDrawList.AddLine(frontPnt, backPnt, color);

            }
        }
        public void DrawLine(List<Vector2> vectors, uint color)
        {
            for (int i = 0; i < vectors.Count; i++)
            {
                if (i != (vectors.Count - 1))
                {
                    var pnt1 = vectors[i];
                    var pnt2 = vectors[i + 1];
                    viewport.mDrawList.AddLine(pnt1, pnt2, color);
                }
                else
                {
                    var pnt1 = vectors[0];
                    var pnt2 = vectors[i];
                    viewport.mDrawList.AddLine(pnt1, pnt2, color);
                }
            }
        }

        public static void Save(string savePath)
        {
            var actor = CollisionActor;
            BymlHashTable root = new();

            var shapeParams = actor.mActorPack?.ShapeParams;

            var mCalc = shapeParams.mCalc;
            var autoCalc = DefaultShape.AutoCalc(mCalc);
            root.AddNode(BymlNodeId.Hash, autoCalc, "AutoCalc");

            if (shapeParams == null)
                return;

            if (shapeParams.mBox?.Count > 0)
            {
                root.AddNode(BymlNodeId.Array, Box.SerializeToArray(shapeParams), "Box");
            }

            if (shapeParams.mSphere?.Count > 0)
            {
                root.AddNode(BymlNodeId.Array, Sphere.SerializeToArray(shapeParams), "Sphere");
            }

            if (shapeParams.mPoly?.Count > 0)
            {
                root.AddNode(BymlNodeId.Array, Polytope.SerializeToArray(shapeParams), "Polytope");
            }



            var byml = new Byml.Byml(root);
            var mem = new MemoryStream();
            byml.Save(mem);
            File.WriteAllBytes(savePath, FileUtil.CompressData(mem.ToArray()));
            Console.WriteLine("saved");
        }


        public List<PolytopeVertex> MergeVerticies(Polytope Polytope)
        {
            List<PolytopeVertex> merged = new();
            float eps = 0.0001f;
            var max = -20f;

            foreach (var v in Polytope.Vertices)
            {
                bool exists = false;

                if (v.Z > max)
                    max = v.Z;

                foreach (var m in merged)
                {
                    if (MathF.Abs(m.X - v.X) < eps &&
                        MathF.Abs(m.Y - v.Y) < eps)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    merged.Add(v);
            }

            foreach (var v in merged)
                v.Z = max;

            Polytope.zFront = max;
            Polytope.Merged = true;
            return merged;
        }
        internal void HandlePolytopeCollision(Polytope Polytope, uint color)
        {

            if (!Polytope.Merged)
                Polytope.mergeList = MergeVerticies(Polytope);

            var organizedList = GetConvexHull(Polytope.mergeList);
            var verticies = calcPos(Polytope, Polytope.mergeList, false);
            var organized = calcPos(Polytope, organizedList, false);
            DrawPolytope(Polytope, verticies, color);
            DrawLine(organized, color);

            verticies = calcPos(Polytope, Polytope.mergeList, true);
            var organizedBack = calcPos(Polytope, organizedList, true);
            DrawPolytope(Polytope, verticies, color);
            DrawLine(organizedBack, color);
            DrawZ(organized, organizedBack, color);

        }
        internal void HandleSphereCollision(Sphere Sphere, CourseActor actor, Vector3 center, Matrix4x4 transform, uint color)
        {
            if (mEditContext.IsSelected(Sphere))
                color = Color.BlueViolet.ToAbgr();

            Vector3 worldCenter = Vector3.Transform(Sphere.Center, transform);
            float worldRadius = actor.mScale.X * Sphere.Radius;
            var camForward = Vector3.Transform(-Vector3.UnitZ, viewport.Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, viewport.Camera.Rotation);
            var camRight = Vector3.Normalize(Vector3.Cross(camUp, camForward));

            Vector2 screenCenter = viewport.WorldToScreen(worldCenter);
            Vector3 worldOffset = worldCenter + camRight * worldRadius;

            Vector2 screenOffset = viewport.WorldToScreen(worldOffset);
            float screenRadius = Vector2.Distance(screenCenter, screenOffset);
            viewport.mDrawList.AddCircle(screenCenter, screenRadius, color);

            bool hovered = Vector2.Distance(ImGui.GetMousePos(), screenCenter) <= screenRadius;
            if (hovered)
            {
                viewport.mHoveredObject = Sphere;
            }
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
                    {
                        CollisionActor = new CourseActor(actor, 0, "PlayArea");
                        mEditContext.DeselectAll();
                    }
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();
            ImGui.End();
        }

    }
}