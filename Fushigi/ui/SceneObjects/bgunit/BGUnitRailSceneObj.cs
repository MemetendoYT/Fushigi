using DiscordRPC;
using Fushigi.actor_pack.components;
using Fushigi.course;
using Fushigi.gl;
using Fushigi.ui.undo;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System;
using System.Drawing;
using System.Numerics;

namespace Fushigi.ui.SceneObjects.bgunit
{
    internal class BGUnitRailSceneObj(CourseUnit unit, BGUnitRail rail, bool isBelt)
    {
        public static bool rebuildTiles = false;
        public static uint Color_Default = 0xFFFFFFFF;
        public static uint Color_SelectionEdit = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
        public static uint Color_SlopeError = 0xFF0000FF;
        private static (Vector3 pos, int index)? addPointPos;
        public static List<BGUnitRail.RailPoint> pointsToDelete = new();
 
        public static void InsertPoint(CourseAreaEditContext ctx, BGUnitRail.RailPoint point, int index, BGUnitRail rail)
        {
            ctx.DeselectAll();
            ctx.CommitAction(rail.Points.RevertableInsert(point, index,
                $"{IconUtil.ICON_PLUS_CIRCLE} Terrain Point Insert"));
            rebuildUnit(rail.mCourseUnit);
            ctx.Select(point);
        }

        private static bool HitTest(LevelViewport viewport, BGUnitRail rail)
        {
            return MathUtil.HitTestLineLoopPoint(GetPoints(viewport, rail), 10f,
                    ImGui.GetMousePos());
        }

        private static (Vector3 pos, int index)? EvaluateAddPointPos(CourseAreaEditContext ctx, LevelViewport viewport, BGUnitRail rail)
        {
            if (!ImGui.GetIO().KeyAlt || !ctx.IsSelected(rail))
                return null;

            Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
            Vector3 pos;
            if (UserSettings.GetEnableHalfTile())
            {
                pos = new(
                MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2,
                MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2,
                rail.mCourseUnit.mModelType switch
                {
                    CourseUnit.ModelType.Solid => 0,
                    CourseUnit.ModelType.SemiSolid => -2,
                    CourseUnit.ModelType.NoCollision => -4,
                    CourseUnit.ModelType.Bridge => -2,
                    _ => 0
                });
            }
            else
            {
                pos = new(
                      MathF.Round(posVec.X, MidpointRounding.AwayFromZero),
                      MathF.Round(posVec.Y, MidpointRounding.AwayFromZero),
                      rail.mCourseUnit.mModelType switch
                      {
                          CourseUnit.ModelType.Solid => 0,
                          CourseUnit.ModelType.SemiSolid => -2,
                          CourseUnit.ModelType.NoCollision => -4,
                          CourseUnit.ModelType.Bridge => -2,
                          _ => 0
                      });
            }
            if (rail.Points.Count == 0)
                return (pos, 0);

            if (rail.Points.Count == 1)
                return (pos, 1);


            //find best index to insert at (minimizing distance)

            var min = (distance: float.PositiveInfinity, index: 0);

            int segmentCount = rail.Points.Count;
            if (!rail.IsClosed)
                segmentCount--;

            Vector3 pointA, pointB;

            for (int i = 0; i < segmentCount; i++)
            {
                pointA = rail.Points[i].mTranslation;
                pointB = rail.Points.GetWrapped(i + 1).mTranslation;
                var seg = pointB - pointA;
                float segLenSq = seg.LengthSquared();
                if (segLenSq < 0.0001f)
                    continue;
                float t = Vector3.Dot(pos - pointA, seg) / segLenSq;
                float tClamped = Math.Clamp(t, 0f, 1f);
                var closest = pointA + seg * tClamped;
                float distance = Vector3.Distance(pos, closest);
                if (distance <= min.distance)
                    min = (distance, i + 1);
            }

            if (rail.IsClosed)
            {
                if (min.distance == float.PositiveInfinity)
                    return null;

                return (pos, min.index);
            }

            //!rail is not closed here

            //prefer appending/prepending
            //only allow inserting in the middle if the point is close enough to or on the edge
            if (min.distance < 1)
                return (pos, min.index);


            pointA = rail.Points[0].mTranslation;
            pointB = rail.Points[^1].mTranslation;
            if (Vector3.Distance(pointA, pos) < Vector3.Distance(pointB, pos))
                return (pos, 0);
            else
                return (pos, rail.Points.Count);
        }

        public static void OnMouseDown(CourseAreaEditContext ctx, LevelViewport viewport, BGUnitRail rail)
        {

            if (!ctx.IsSelected(rail))
                return;

            var mouseDownPos = viewport.ScreenToWorld(ImGui.GetMousePos());

            if (addPointPos.TryGetValue(out var addPos))
            {
                //DeselectAll(ctx, rail);
                InsertPoint(ctx, new BGUnitRail.RailPoint(rail, addPos.pos), addPos.index, rail);
            }
            else
            {
                /*if (!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                    DeselectAll(ctx);*/
            }
            var mouseDown = true;
        }

        private static Vector2[] GetPoints(LevelViewport viewport, BGUnitRail rail)
        {
            Vector2[] points = new Vector2[rail.Points.Count];
            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 p = rail.Points[i].mTranslation;
                points[i] = viewport.WorldToScreen(new(p.X, p.Y, p.Z));
            }
            return points;
        }


        public static void DrawBGUnitLines(CourseAreaEditContext ctx, LevelViewport viewport, ImDrawListPtr dl, BGUnitRail rail, bool isBelt)
        {
            //if (!Visible)
            //    return;

            BGUnitRail.RailPoint point = null;
            if (ctx.IsAnySelected<BGUnitRail.RailPoint>())
            {
                point = ctx.GetFirstObjectOfType<BGUnitRail.RailPoint>();
                ctx.Select(point.mRail);
            }
     
            bool isNewHoveredObj = false;
            addPointPos = EvaluateAddPointPos(ctx, viewport, rail);

            if ((addPointPos.HasValue && ctx.IsSelected(rail)) || (!isBelt && HitTest(viewport, rail)))
                isNewHoveredObj = true;

            bool isSelected = ctx.IsSelected(rail);

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                OnMouseDown(ctx, viewport, rail);

            var lineThickness = 3.5f;

            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 pos = rail.Points[i].mTranslation;

                Vector3 nextPos;

                if (i < rail.Points.Count - 1) //is not last point
                {
                    nextPos = rail.Points[i + 1].mTranslation;
                }
                else if (rail.IsClosed) //last point to first if closed
                {
                    nextPos = rail.Points[0].mTranslation;
                }
                else //last point but not closed, draw no line
                    continue;

                var pos2D = viewport.WorldToScreen(pos);
                var nextPos2D = viewport.WorldToScreen(nextPos);

                uint line_color = IsValidAngle(new Vector2(pos.X, pos.Y), new Vector2(nextPos.X, nextPos.Y)) ? Color_Default : Color_SlopeError;
                if (isSelected && line_color != Color_SlopeError)
                    line_color = Color_SelectionEdit;

                if (isBelt)
                {
                    var bottomPos2D = viewport.WorldToScreen(pos - Vector3.UnitY * 0.5f);
                    var bottomNextPos2D = viewport.WorldToScreen(nextPos - Vector3.UnitY * 0.5f);

                    dl.AddQuadFilled(pos2D, nextPos2D, bottomNextPos2D, bottomPos2D, line_color & 0x00FFFFFF | 0x55000000);
                    dl.AddQuad(pos2D, nextPos2D, bottomNextPos2D, bottomPos2D, line_color, lineThickness - 1);

                    if (MathUtil.HitTestConvexQuad(pos2D, nextPos2D, bottomNextPos2D, bottomPos2D,
                    ImGui.GetMousePos()))
                    {
                        isNewHoveredObj = true;
                    }
                }
                else
                {
                    dl.AddLine(pos2D, nextPos2D, line_color, lineThickness);
                }

                if (isSelected)
                {
                    //Arrow display
                    Vector3 next = i < rail.Points.Count - 1 ? rail.Points[i + 1].mTranslation : rail.Points[0].mTranslation;
                    Vector3 dist = next - rail.Points[i].mTranslation;
                    var angleInRadian = MathF.Atan2(dist.Y, dist.X); //angle in radian
                    var rotation = Matrix4x4.CreateRotationZ(angleInRadian);

                    float width = 1f;

                    var line = Vector3.TransformNormal(new Vector3(0, width, 0), rotation);

                    Vector2[] arrow =
                    [
                        viewport.WorldToScreen(rail.Points[i].mTranslation + dist / 2f),
                        viewport.WorldToScreen(rail.Points[i].mTranslation + dist / 2f + line),
                    ];
                    float alpha = 0.5f;

                    dl.AddLine(arrow[0], arrow[1], ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha)), lineThickness);
                }
            }

            //draw visual hint for added point
            if (addPointPos.TryGetValue(out var addPos))
            {
                var pos2D = viewport.WorldToScreen(addPos.pos);

                if (rail.Points.Count > 0)
                {
                    int index = addPos.index;
                    var pointA = viewport.WorldToScreen(rail.Points.GetWrapped(index - 1).mTranslation);
                    var pointB = viewport.WorldToScreen(rail.Points.GetWrapped(index).mTranslation);
                    var pointC = pos2D;

                    var pointAVec = rail.Points.GetWrapped(index - 1).mTranslation;
                    var pointBVec = rail.Points.GetWrapped(index).mTranslation;
                    ImGui.SetTooltip("X: " + (addPos.pos.X - pointAVec.X) + ", Y: " + (addPos.pos.Y - pointAVec.Y));
                    if (!isBelt)
                        dl.AddTriangleFilled(pointA, pointB, pointC, 0x99FFFFFF);

                    if (rail.IsClosed || index > 0)
                        dl.AddLine(pointA, pointC, 0xFFFFFFFF, 2.5f);
                    if (rail.IsClosed || index < rail.Points.Count)
                        dl.AddLine(pointB, pointC, 0xFFFFFFFF, 2.5f);

                    if (!rail.IsClosed)
                    {
                        if (index == 0)
                            ImGui.SetTooltip("Prepend point");
                        else if (index == rail.Points.Count)
                            ImGui.SetTooltip("Append point");
                        else
                            ImGui.SetTooltip("Insert point");
                    }

                }

                dl.AddCircleFilled(pos2D, 8.5f, 0xFFFFFFFF);
            }
        }

        private static bool IsValidAngle(Vector2 point1, Vector2 point2)
        {
            var dist = point2 - point1;
            var angleInRadian = MathF.Atan2(dist.Y, dist.X); //angle in radian
            var angle = angleInRadian * (180.0f / (float)Math.PI); //to degrees

            bool isCorrectDist = (MathF.Abs(MathF.Round(dist.X) - dist.X) <= 0.01) && (MathF.Abs(MathF.Round(dist.Y) - dist.Y) <= 0.01);

            //TODO improve check and simplify

            //The game supports 30 and 45 degree angle variants
            //Then ground (0) and wall (90)
            float[] validAngles = new float[]
            {
                0, -0,
                27, -27,
                45, -45,
                90, -90,
                135,-135,
                153,-153,
                180,-180,
            };

            return validAngles.Contains(MathF.Round(angle)) && isCorrectDist;
        }

        public static void rebuildUnit(CourseUnit unit)
        {
            unit.GenerateTileSubUnits();
            unit.GenerateCorrectTiles();
            rebuildTiles = true;
        }


        public static void DrawBGUnitPoints(CourseAreaEditContext ctx, LevelViewport viewport, ImDrawListPtr dl, BGUnitRail.RailPoint point)
        {
            var pos2D = viewport.WorldToScreen(point.mTranslation);

            //Display point color
            uint color = 0xFFFFFFFF;
            if (ctx.IsSelected(point))
                color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));

            dl.AddCircleFilled(pos2D, 10.0f, color);

            bool isHovered = (ImGui.GetMousePos() - pos2D).Length() < 10.0f;

            if (isHovered)
            {
                dl.AddCircle(pos2D, 15.0f, color, 10, 1.5f);
                viewport.mHoveredObject = point;
            }

            if (viewport.mMultiSelecting)
            {
                float pntX = point.mTranslation.X;
                float pntY = point.mTranslation.Y;

                viewport.isInMultiSelectBox(new Vector2(pntX, pntY), point);
            }

            if (ImGui.IsKeyPressed(ImGuiKey.A) && ImGui.GetIO().KeyCtrl && ctx.IsSelected(point))
            {
                foreach (var newPoint in point.mRail.Points)
                {
                    ctx.Select(newPoint);
                }
            }

            //if (HitTest(viewport, point))
            //    viewport.mHoveredObject = point;
        }

    }
}