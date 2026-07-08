using Fushigi.ui;
using Fushigi.ui.widgets;
using ImGuiNET;
using System.Drawing;
using System.Numerics;

namespace Fushigi.course
{

    public class FushigiCursor : Transformable
    {
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
    }
}
