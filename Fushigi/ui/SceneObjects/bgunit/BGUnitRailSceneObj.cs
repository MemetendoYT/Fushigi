using DiscordRPC;
using Fushigi.course;
using Fushigi.ui.undo;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using System.Numerics;

namespace Fushigi.ui.SceneObjects.bgunit
{
    internal class BGUnitRailSceneObj(CourseUnit unit, BGUnitRail rail, bool isBelt) : ISceneObject
    {

        public List<BGUnitRail.RailPoint> GetSelected(CourseAreaEditContext ctx) => rail.Points.Where(ctx.IsSelected).ToList();

        public bool mouseDown = false;
        public bool transformStart = false;

        public bool Visible = true;

        public static uint Color_Default = 0xFFFFFFFF;
        public static uint Color_SelectionEdit = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));
        public static uint Color_SlopeError = 0xFF0000FF;

   
        private (Vector3 pos, int index)? addPointPos;

        public CourseUnit CourseUnit = unit;

        public void Update(ISceneUpdateContext ctx, bool isSelected)
        {
           
        }

        private bool IsSelected(CourseAreaEditContext ctx) => ctx.IsSelected(rail);

        private void DeselectAll(CourseAreaEditContext ctx)
        {
            ctx.WithSuspendUpdateDo(() =>
            {
                foreach (var point in rail.Points)
                    ctx.Deselect(point);
            });

        }

        public void SelectAll(CourseAreaEditContext ctx)
        {
            ctx.WithSuspendUpdateDo(() =>
            {
                foreach (var point in rail.Points)
                    ctx.Select(point);
            });
        }

        public static void InsertPoint(CourseAreaEditContext ctx, BGUnitRail.RailPoint point, int index, BGUnitRail rail)
        {
            var batch = ctx.BeginBatchAction();
            var revertible = rail.Points.RevertableInsert(point, index,
                $"{IconUtil.ICON_PLUS_CIRCLE} Rail Point Add");

            ctx.CommitAction(new TileRebuildRevertable(rail.mCourseUnit));
            ctx.CommitAction(revertible);
            batch.Commit($"{IconUtil.ICON_PLUS_CIRCLE} Rail Point Add");
            ctx.Select(point);

        }

        public void AddPoint(CourseAreaEditContext ctx, BGUnitRail.RailPoint point)
        {
            var revertible = rail.Points.RevertableAdd(point,
                $"{IconUtil.ICON_PLUS_CIRCLE} Rail Point Add");

            ctx.CommitAction(revertible);
            ctx.Select(point);
        }

        public void RemoveSelected(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            var selected = GetSelected(ctx);
            if (selected.Count == 0)
                return;

            var batchAction = ctx.BeginBatchAction();

            foreach (var point in selected)
            {
                var revertible = rail.Points.RevertableRemove(point);
                ctx.CommitAction(revertible);
            }

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete Rail Points");
        }

        public void OnKeyDown(CourseAreaEditContext ctx, LevelViewport viewport)
        {
            //TODO move the delete logic over to CourseAreaEditContext and remove this

            if ((ImGui.IsKeyPressed(ImGuiKey.Delete) && !ImGui.GetIO().KeyShift) || (ImGui.GetIO().KeyShift && ImGui.IsKeyPressed(ImGuiKey.Backspace)))
            {
                RemoveSelected(ctx, viewport);
            }
            if (IsSelected(ctx) && ImGui.GetIO().KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A))
                SelectAll(ctx);
        }

        private bool HitTest(LevelViewport viewport)
        {
            return MathUtil.HitTestLineLoopPoint(GetPoints(viewport), 10f,
                    ImGui.GetMousePos());
        }

        public static (Vector3 pos, int index)? EvaluateAddPointPos(CourseAreaEditContext ctx, LevelViewport viewport, BGUnitRail rail)
        {

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
                pointB = rail.Points.GetWrapped(i + 1).mTranslation ;

                var seg = pointB - pointA;
                float segLenSq = seg.LengthSquared();
                if (segLenSq < 0.0001f)
                    continue;

                float t = Vector3.Dot(pos - pointA, seg) / segLenSq;
                float tClamped = Math.Clamp(t, 0f, 1f);

                var closest = pointA + seg * tClamped;
                float distance = Vector3.Distance(pos, closest);

                var delta = distance;
                if (delta <= min.distance)
                    min = (delta, i + 1);
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

       
        private Vector2[] GetPoints(LevelViewport viewport)
        {
            Vector2[] points = new Vector2[rail.Points.Count];
            for (int i = 0; i < rail.Points.Count; i++)
            {
                Vector3 p = rail.Points[i].mTranslation;
                points[i] = viewport.WorldToScreen(new(p.X, p.Y, p.Z));
            }
            return points;
        }


       
      

        public static void rebuildUnit(CourseUnit unit1)
        {
            unit1.GenerateTileSubUnits();
            unit1.GenerateCorrectTiles();
        }

        void Draw2D(CourseAreaEditContext ctx, LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj)
        {
            if (!Visible)
                return;

            //addPointPos = EvaluateAddPointPos(ctx, viewport);

            if ((addPointPos.HasValue && ctx.IsSelected(rail)) || (!isBelt && HitTest(viewport)))
                isNewHoveredObj = true;

            bool isSelected = IsSelected(ctx);

           
           

            //TODO does it still need a condition like this?
            //if (viewport.mEditorState == LevelViewport.EditorState.Selecting)
            //if (CourseScene.leftClickStartedInsideViewport)
            //    OnSelecting(ctx, viewport);

            OnKeyDown(ctx, viewport);

            var lineThickness = viewport.IsHovered(this) ? 3.5f : 2.5f;

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

        public static bool IsValidAngle(Vector2 point1, Vector2 point2)
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

        /// <summary>
        /// Check if the proposed position is valid within the rail
        /// </summary>
        /// <param name="pos">new proposed position</param>
        /// <param name="index">index of the point to set to the new position</param>
        /// <returns>true if the point is valid there, false otherwise</returns>
      
      

            private readonly BGUnitRail.RailPoint point;

            public Transform Transform = new Transform();

            //For transforming
            public Vector3 PreviousPosition { get; private set; }

          

        

            //void IViewportDrawable.Draw2D(CourseAreaEditContext ctx, LevelViewport viewport, ImDrawListPtr dl, ref bool isNewHoveredObj)
            //{
            //    var pos2D = viewport.WorldToScreen(point.mTranslation);

            //    //Display point color
            //    uint color = 0xFFFFFFFF;
            //    if (ctx.IsSelected(point))
            //        color = ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1));

            //    dl.AddCircleFilled(pos2D, 10.0f, color);

            //    if (viewport.IsHovered(this))
            //        dl.AddCircle(pos2D, 15.0f, color, 10, 1.5f);

            //    if (HitTest(viewport))
            //        isNewHoveredObj = true;
            //}
        
    }
}