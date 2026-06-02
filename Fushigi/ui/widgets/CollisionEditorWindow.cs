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
    public class CollisionEditor
    {
        public static bool init = false;
        LevelViewport viewport = null;
        private string mActorSearchAll = "";

        private bool hasSetCamera;
        private string prevSearch;
        public Viewport3D vp3D = new Viewport3D();

        private List<string> filteredActors = new List<string>();
        private CourseActor CollisionActor = new CourseActor("EnemyKuribo", 0, "PlayArea");

        internal void Draw(GLTaskScheduler scheduler, double delta)
        {
            ActorsPanel();
            ImGui.Begin("Collision Viewport");
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
                if (!hasSetCamera)
                {
                    viewport.Camera.Distance = 10;
                    viewport.Camera.IsOrthographic = false;
                    hasSetCamera = true;
                }

                viewport.DrawCollisionVP(size, delta, CollisionActor, this);

            }
            ImGui.End();
        }

        internal void SelectionPanel(CourseAreaEditContext editContext)
        {
            ImGui.Begin("Collision Selection");
            if (editContext.IsSingleObjectSelected(out Sphere? sphere))
            {
                if (ImGui.CollapsingHeader("Sphere:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Indent();
                    if (ImGui.BeginTable("Trans", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
                    {
                        // --- Center ---
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Center");

                        ImGui.TableSetColumnIndex(1);
                        var center = sphere.Center;
                        ImGui.DragFloat3("##Center", ref center);
                        sphere.Center = center;


                        // --- Radius ---
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Radius");

                        ImGui.TableSetColumnIndex(1);
                        var radius = sphere.Radius;
                        ImGui.DragFloat("##Radius", ref radius, 0.25f, 0, float.MaxValue);
                        sphere.Radius = radius;


                        // --- Material header ---
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        ImGui.Text("Material");

                        // First material entry goes in column 1
                        ImGui.TableSetColumnIndex(1);

                        // --- Material list ---
                        for (int i = 0; i < sphere.mPresets.Count; i++)
                        {
                            var mat = sphere.mPresets[i];

                            ImGui.InputText($"##Mat{i}", ref mat, 255);
                            sphere.mPresets[i] = mat;

                            // If more materials exist, start a new row
                            if (i < sphere.mPresets.Count - 1)
                            {
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(1);
                            }
                        }

                        // Optional width adjustment
                        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);



                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                }

            }
                ImGui.End();
        }

        internal void RightClickMenu(CourseActor actor)
        {
            if ((ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGui.IsKeyPressed(ImGuiKey.Menu)) && viewport.IsViewportHovered)
            {
                ImGui.OpenPopup("ViewportContextMenu");
            }

            if (ImGui.BeginPopup("ViewportContextMenu"))
            {
                if (ImGui.BeginMenu("Add"))
                {
                    if (ImGui.MenuItem("Sphere"))
                        actor.mActorPack.ShapeParams.mSphere.Add(new Sphere
                        {
                            Radius = 1.0f,
                            mPresets = new List<string> {"Dummy"},
                        });
                      
                    ImGui.EndMenu();

                }
                ImGui.EndPopup();
            }
        }
        internal async Task InitLevel(GLTaskScheduler scheduler)
        {
            string romFSPath = UserSettings.GetRomFSPath();
            await scheduler.Schedule(gl => RomFS.SetRoot(romFSPath, gl));
            var area = new CourseArea("dummy", false);
            viewport = await scheduler.Schedule(gl => new LevelViewport(area, gl, new CourseAreaScene(area, new CourseAreaSceneRoot(area))));
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
                    {

                    }
                }
                //shapesList.Add("box");

                if (shapeParams.mSphere?.Count > 0)
                {
                    foreach (var sphere in shapeParams.mSphere)
                        shapesList.Add(sphere);
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



            //s_actorRectPolygon[0] = WorldToScreen(Vector3.Transform(new Vector3(min.X, max.Y, 0) + off, transform));
            ////topRight
            //s_actorRectPolygon[1] = WorldToScreen(Vector3.Transform(new Vector3(max.X, max.Y, 0) + off, transform));
            ////bottomRight
            //s_actorRectPolygon[2] = WorldToScreen(Vector3.Transform(new Vector3(max.X, min.Y, 0) + off, transform));
            ////bottomLeft
            //s_actorRectPolygon[3] = WorldToScreen(Vector3.Transform(new Vector3(min.X, min.Y, 0) + off, transform));


            uint color = CourseActor.CourseActorColors[CourseActorType.None];
            CourseActor.CourseActorColors.TryGetValue(actor.mType, out color);

            bool isHovered = VP.mHoveredObject == actor;
            int index = 0;
            foreach (var shape in shapesList)
            {
                switch (shape)
                {
                    case "box":
                        for (int i = 0; i < 4; i++)
                        {
                            //mDrawList.AddLine(
                            //s_actorRectPolygon[i],
                            //s_actorRectPolygon[(i + 1) % 4],
                            //color, isHovered ? 2.5f : 1.5f);
                        }
                        break;
                    case Sphere Sphere:
                        HandleSphereCollision(Sphere, actor, center, transform, color);
                        break;
                    case "polytope":
                        {
                            int j = 0;
                            foreach (var poly in shapeParams.mPoly)
                            {

                                float bestDist = float.MaxValue;
                                VP.mHoveredObject = null;

                                foreach (var vertex in poly.Vertices)
                                {
                                    Vector3 worldVertex = Vector3.Transform(new Vector3(vertex.X, vertex.Y, vertex.Z), transform);

                                    float depth3;
                                    Vector2 screenPos = VP.WorldToScreen(worldVertex, out depth3);

                                    VP.mDrawList.AddCircleFilled(screenPos, pointSize,
                                        ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)));

                                    float dist = (ImGui.GetMousePos() - screenPos).Length();
                                    if (dist < 10.0f && dist < bestDist)
                                    {
                                        Console.WriteLine(j);
                                        bestDist = dist;
                                        VP.mHoveredObject = vertex;
                                    }
                                }
                                j++;


                                foreach (var vertex in poly.Vertices)
                                {
                                    var match = poly.Vertices.FirstOrDefault(v =>
                                        Math.Abs(v.X - vertex.X) < 0.001f &&
                                        Math.Abs(v.Y - vertex.Y) < 0.001f &&
                                        v != vertex);

                                    if (match != null)
                                    {
                                        Vector3 A = Vector3.Transform(new Vector3(vertex.X, vertex.Y, vertex.Z), transform);
                                        Vector3 B = Vector3.Transform(new Vector3(match.X, match.Y, match.Z), transform);

                                        float d1, d2;
                                        Vector2 p1 = VP.WorldToScreen(A, out d1);
                                        Vector2 p2 = VP.WorldToScreen(B, out d2);

                                        VP.mDrawList.AddLine(p1, p2, color);
                                    }
                                }


                                var groupsByZ = poly.Vertices
                                    .GroupBy(v => v.Z)
                                    .ToDictionary(g => g.Key, g => g.ToList());

                                foreach (var kv in groupsByZ)
                                {
                                    float z = kv.Key;
                                    var verts = kv.Value;

                                    Vector2 centroid = Vector2.Zero;
                                    foreach (var v in verts)
                                        centroid += new Vector2(v.X, v.Y);
                                    centroid /= verts.Count;

                                    var ordered = verts
                                        .Where(v => Vector2.Distance(new(v.X, v.Y), centroid) > 0.01f)
                                        .OrderBy(v => MathF.Atan2(v.Y - centroid.Y, v.X - centroid.X))
                                        .ToList();

                                    for (int i = 0; i < ordered.Count; i++)
                                    {
                                        var a = ordered[i];
                                        var b = ordered[(i + 1) % ordered.Count];

                                        Vector3 A = Vector3.Transform(new Vector3(a.X, a.Y, z), transform);
                                        Vector3 B = Vector3.Transform(new Vector3(b.X, b.Y, z), transform);

                                        float d1, d2;
                                        Vector2 p1 = VP.WorldToScreen(A, out d1);
                                        Vector2 p2 = VP.WorldToScreen(B, out d2);

                                        VP.mDrawList.AddLine(p1, p2, color);
                                    }
                                }
                            }

                            break;
                        }


                }

                //if (ImGui.IsMouseClicked(0) && mHoveredObject != null)
                //{
                //    mEditContext.Select(mHoveredObject);
                //}


                //if (mEditContext.IsSelected(actor))
                //{
                //    for (int i = 0; i < 4; i++)
                //    {
                //        VP.mDrawList.AddCircleFilled(s_actorRectPolygon[i],
                //            pointSize, color);
                //        if (drawing == "sphere")
                //        {
                //            mDrawList.AddLine(
                //            s_actorRectPolygon[i],
                //            s_actorRectPolygon[(i + 1) % 4],
                //            color, isHovered ? 2.5f : 1.5f);
                //        }
                //    }
                //    mDrawList.AddEllipse(WorldToScreen(transform.Translation), pointSize * 3, pointSize * 3, color, -actor.mRotation.Z, 4, 2);
                //}

            }

        }
        internal void HandleSphereCollision(Sphere Sphere, CourseActor actor, Vector3 center, Matrix4x4 transform, uint color)
        {
            Vector3 worldCenter = Vector3.Transform(Sphere.Center, transform);
            float worldRadius = actor.mScale.X * Sphere.Radius;
            var camForward = Vector3.Transform(-Vector3.UnitZ, viewport.Camera.Rotation);
            var camUp = Vector3.Transform(Vector3.UnitY, viewport.Camera.Rotation);
            var camRight = Vector3.Normalize(Vector3.Cross(camUp, camForward));

            float depth;
            Vector2 screenCenter = viewport.WorldToScreen(worldCenter, out depth);
            Vector3 worldOffset = worldCenter + camRight * worldRadius;

            float depth2;
            Vector2 screenOffset = viewport.WorldToScreen(worldOffset, out depth2);
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
                        CollisionActor = new CourseActor(actor, 0, "PlayArea");
                }

                ImGui.EndTable();
            }
            ImGui.EndChild();
            ImGui.End();
        }

    }
}