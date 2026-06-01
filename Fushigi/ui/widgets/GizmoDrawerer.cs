using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EditorToolkit.Core;

namespace EditorToolkit.ImGui
{
   

    public record struct Rect(Vector2 TopLeft, Vector2 BottomRight)
    {
        public readonly Vector2 Size => BottomRight - TopLeft;
        public readonly bool Contains(Vector2 pos) =>
            TopLeft.X <= pos.X && pos.X <= BottomRight.X &&
            TopLeft.Y <= pos.Y && pos.Y <= BottomRight.Y;
    }

     
    /// <summary>
    /// Draws and handles Gizmos in Imgui
    /// </summary>
    public static class GizmoDrawer
    {
       
        private static readonly Dictionary<uint, int> s__lastFrameHoveredParts__ = new();
        private static int s__currentFrameHoveredPart__;
        private static int s__currentHoverIndex__;


        private static uint s_itemID;
        public static bool HoverablePart(bool isHovered)
        {
            bool wasHovered = s__lastFrameHoveredParts__[s_itemID] ==
                              s__currentHoverIndex__;

            if (isHovered)
                s__currentFrameHoveredPart__ = s__currentHoverIndex__;

            s__currentHoverIndex__++;

            return wasHovered;
        }

        const uint HOVER_COLOR = 0xFF_33_FF_FF;

        const int ELLIPSE_NUM_SEGMENTS = 32;

        private static SceneViewState s_view;


        private static uint[] s_axisColors = new uint[]
        {
            0xFF_44_44_FF,
            0xFF_FF_88_44,
            0xFF_44_FF_44
        };
        private static nint s_orientationCubeTexture;

        public static uint AlphaBlend(uint colA, uint colB)
        {
            float blend = (colB >> 24 & 0xFF) / 255f;

            uint r = (uint)((colA >> 0 & 0xFF) * (1 - blend) + (colB >> 0 & 0xFF) * blend);
            uint g = (uint)((colA >> 8 & 0xFF) * (1 - blend) + (colB >> 8 & 0xFF) * blend);
            uint b = (uint)((colA >> 16 & 0xFF) * (1 - blend) + (colB >> 16 & 0xFF) * blend);

            uint a = Math.Min((colA >> 24 & 0xFF) + (colB >> 24 & 0xFF), 255);

            return
                r |
                g << 8 |
                b << 16 |
                a << 24;
        }

        public unsafe static void EndGizmoDrawing(out bool isAnythingHovered)
        {
            isAnythingHovered = false;

            ImGuiNET.ImGui.SetCursorScreenPos(s_view.ViewportRect.TopLeft);
            ImGuiNET.ImGui.PushID(unchecked((int)s_itemID));
            _ = ImGuiNET.ImGui.InvisibleButton("", s_view.ViewportRect.Size);
            ImGuiNET.ImGui.PopID();

            //a bit hacky but should work
            bool isHovered = s_view.ViewportRect.Contains(ImGuiNET.ImGui.GetMousePos()) && ImGuiNET.ImGui.IsAnyItemHovered();

            s__lastFrameHoveredParts__[s_itemID] = -1;

            if (isHovered)
            {
                isAnythingHovered = s__currentFrameHoveredPart__ > -1;
                s__lastFrameHoveredParts__[s_itemID] = s__currentFrameHoveredPart__;
            }
        }



        public static void BeginGizmoDrawing(string id, ImDrawListPtr drawlist, in SceneViewState view)
        {
            s__currentHoverIndex__ = 0;
            s__currentFrameHoveredPart__ = -1;
            s_view = view;
            Drawlist = drawlist;

            s_itemID = ImGuiNET.ImGui.GetID(id);

            if (!s__lastFrameHoveredParts__.ContainsKey(s_itemID))
                s__lastFrameHoveredParts__[s_itemID] = -1;
        }

        public static ImDrawListPtr Drawlist { get; private set; }

        public static bool OrientationCube(Vector2 position, float radius, out Vector3 facingDirection)
        {
            var rotMtx = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(s_view.CamRotation));

            var cubeToScreenSpace = rotMtx * Matrix4x4.CreateScale(radius / 2, -radius / 2, radius / 2) *
                            Matrix4x4.CreateTranslation(new Vector3(position.X, position.Y, 0));

            const float MAX_EDGE_WIDTH = 20;

            float edgeWidthPercent = MathF.Min(MAX_EDGE_WIDTH, radius / 3) / radius;

            Vector3 edgeHit = Vector3.Zero;
            Vector3 faceHit = Vector3.Zero;

            void CubeSide(Vector3 up, Vector3 forward, uint col, Vector2 uvOffset, in Matrix4x4 rotMtx)
            {
                Matrix4x4 mtx =
                                Matrix4x4.CreateTranslation(new Vector3(0, 0, 1)) *
                                Matrix4x4.CreateWorld(
                                    Vector3.Zero,
                                    forward,
                                    up
                                    ) *
                                cubeToScreenSpace;

                Vector2 Transform(Vector2 position)
                {
                    return Vector2.Transform(position, mtx);
                }

                if (Vector3.Transform(forward, rotMtx).Z < -0.001)
                {
                    var mpos = ImGuiNET.ImGui.GetMousePos();

                    var center = Transform(Vector2.Zero);

                    var u_vec = Transform(Vector2.UnitX) - center;
                    var v_vec = Transform(Vector2.UnitY) - center;

                    var m_vec = mpos - center;

                    var u_norm = Vector2.Normalize(u_vec);
                    var v_norm = Vector2.Normalize(v_vec);


                    //v up ortho coord system
                    var y_dir = v_norm;
                    var x_dir = new Vector2(y_dir.Y, -y_dir.X);

                    var slope = Vector2.Dot(u_norm, y_dir) / Vector2.Dot(u_norm, x_dir);

                    var mx = Vector2.Dot(m_vec, x_dir);
                    var my = Vector2.Dot(m_vec, y_dir);

                    var w = Vector2.Dot(u_vec, x_dir);
                    var h = Vector2.Dot(v_vec, y_dir);

                    var mu = mx / w;
                    var mv = (my - slope * mx) / h;

                    var mu_abs = Math.Abs(mu);
                    var mv_abs = Math.Abs(mv);

                    if (HoverablePart(mu_abs <= 1 && mv_abs <= 1))
                    {
                        faceHit = -forward + mv * up + mu * Vector3.Cross(forward, up);

                        if (mu_abs <= 1 - edgeWidthPercent && mv_abs <= 1 - edgeWidthPercent)
                            col = AlphaBlend(col, 0x88_CC_FF_FF);
                        else
                            edgeHit = faceHit;
                    }

                    if (s_orientationCubeTexture != nint.Zero)
                    {
                        Drawlist.AddImageQuad(
                        s_orientationCubeTexture,
                        Transform(new Vector2(-1, 1)),
                        Transform(new Vector2(1, 1)),
                        Transform(new Vector2(1, -1)),
                        Transform(new Vector2(-1, -1)),
                        uvOffset + new Vector2(0, 0),
                        uvOffset + new Vector2(0.25f, 0),
                        uvOffset + new Vector2(0.25f, 0.5f),
                        uvOffset + new Vector2(0, 0.5f),
                        col
                        );
                    }
                    else
                    {
                        Drawlist.AddQuadFilled(
                        Transform(new Vector2(-1, -1)),
                        Transform(new Vector2(1, -1)),
                        Transform(new Vector2(1, 1)),
                        Transform(new Vector2(-1, 1)), col);
                    }

                    Drawlist.AddQuad(
                        Transform(new Vector2(-1, -1)),
                        Transform(new Vector2(1, -1)),
                        Transform(new Vector2(1, 1)),
                        Transform(new Vector2(-1, 1)), col, 1.5f);
                }
            }


            CubeSide(Vector3.UnitY, Vector3.UnitX, s_axisColors[0], new Vector2(0.5f, 0), rotMtx);
            CubeSide(Vector3.UnitY, -Vector3.UnitX, s_axisColors[0], new Vector2(0.75f, 0), rotMtx);
            CubeSide(Vector3.UnitY, -Vector3.UnitZ, s_axisColors[2], new Vector2(0, 0), rotMtx);
            CubeSide(Vector3.UnitY, Vector3.UnitZ, s_axisColors[2], new Vector2(0.25f, 0), rotMtx);

            CubeSide(-Vector3.UnitZ, -Vector3.UnitY, s_axisColors[1], new Vector2(0, 0.5f), rotMtx);
            CubeSide(Vector3.UnitZ, Vector3.UnitY, s_axisColors[1], new Vector2(0.25f, 0.5f), rotMtx);

            float Round(float num) => (float)Math.Round(num);


            Vector2 Transform(Vector3 position)
            {
                var vec = Vector3.Transform(position, cubeToScreenSpace);

                return new Vector2(vec.X, vec.Y);
            }

            Vector3 snappedHitPos = new(
                Math.Abs(faceHit.X) < 1 - edgeWidthPercent ? 0 : Round(faceHit.X),
                Math.Abs(faceHit.Y) < 1 - edgeWidthPercent ? 0 : Round(faceHit.Y),
                Math.Abs(faceHit.Z) < 1 - edgeWidthPercent ? 0 : Round(faceHit.Z)
                );

            uint highlight_col = 0xFF_88_CC_FF;

            if (edgeHit != Vector3.Zero)
            {
                if (snappedHitPos.X == 0)
                    Drawlist.AddLine(Transform(new Vector3(-1, snappedHitPos.Y, snappedHitPos.Z)),
                               Transform(new Vector3(1, snappedHitPos.Y, snappedHitPos.Z))
                        , highlight_col, 2.5f);

                else if (snappedHitPos.Y == 0)
                    Drawlist.AddLine(Transform(new Vector3(snappedHitPos.X, -1, snappedHitPos.Z)),
                               Transform(new Vector3(snappedHitPos.X, 1, snappedHitPos.Z))
                        , highlight_col, 2.5f);

                else if (snappedHitPos.Z == 0)
                    Drawlist.AddLine(Transform(new Vector3(snappedHitPos.X, snappedHitPos.Y, -1)),
                               Transform(new Vector3(snappedHitPos.X, snappedHitPos.Y, 1))
                        , highlight_col, 2.5f);
                else
                    Drawlist.AddCircleFilled(Transform(snappedHitPos), 2f, highlight_col);
            }
            else if (faceHit != Vector3.Zero)
            {
                Drawlist.AddCircleFilled(Transform(snappedHitPos), 2f, 0xFF_FF_FF_FF);
            }

            facingDirection = snappedHitPos;

            return snappedHitPos != Vector3.Zero;
        }

    }
}
