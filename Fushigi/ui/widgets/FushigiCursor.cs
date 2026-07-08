using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Drawing;
using System.Numerics;

namespace Fushigi.course
{
    public class FushigiCursor : Transformable
    {
        public bool applyRotation;
        public CourseActor[] pivotedActors;
        public float delta;
        public FushigiCursor()
        {
            mTranslation = new System.Numerics.Vector3(0.0f);
            delta = 0;
        }

        internal void CursorPlacement(LevelViewport viewport, Vector2 storedMousePos)
        {
            var pos = viewport.ScreenToWorld(storedMousePos);
            mTranslation.X = MathF.Round(pos.X * 2, MidpointRounding.AwayFromZero) / 2;
            mTranslation.Y = MathF.Round(pos.Y * 2, MidpointRounding.AwayFromZero) / 2;
            mTranslation.Z = 0.0f;
        }

        internal void DrawCursor(LevelViewport viewport, CourseAreaEditContext mEditContext)
        { 
            var cursorPos2D = viewport.WorldToScreen(new(mTranslation.X, mTranslation.Y, mTranslation.Z));
            Vector2 pnt = new(cursorPos2D.X, cursorPos2D.Y);
            bool isHovered = (ImGui.GetMousePos() - pnt).Length() < 10.0f;

            if (isHovered)
                viewport.mHoveredObject = this;

            uint color = Color.BlueViolet.ToAbgr();
            bool point_selected = mEditContext.IsSelected(this);
            var rail_point_color = point_selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;
            var size = 10.0f;

            var pos2D = viewport.WorldToScreen(mTranslation);
            viewport.mDrawList.AddCircleFilled(pos2D, size, rail_point_color);

            if (viewport.mHoveredObject == this)
                viewport.mDrawList.AddCircle(pos2D, 15.0f, rail_point_color, 10, 1.5f);

        }

        internal void PivotActors(CourseAreaEditContext mEditContext, LevelViewport viewport)
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
                        Vector3 cursorTrans = mTranslation;
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
                    viewport.CommitRotation(pivotedActors[0]);
                    viewport.CommitTranslation(pivotedActors[0]);
                }
                else
                {
                    var batch = mEditContext.BeginBatchAction();

                    foreach (var actor in pivotedActors)
                    {
                        viewport.CommitRotation(actor);
                        viewport.CommitTranslation(actor);
                    }
                    batch.Commit($"{IconUtil.ICON_ARROWS_ALT} Pivoted {pivotedActors.Count()} Actors");
                }
            }
        }
    }
}
