using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using Microsoft.VisualBasic;
using System.Drawing;
using System.Numerics;

namespace Fushigi.course
{
    public class FushigiCursor : Transformable
    {
        public bool applyRotation;
        public CourseActor[] pivotedActors;
        public float delta;

        private static int count = 0;
        private static float offset = 0;
        private static float radius = 0;
        private static bool doRotate = true;
        private static float previousDelta;
        public static CourseActor pickActor;
        public FushigiCursor()
        {
            mTranslation = new Vector3(0.0f);
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

        internal static void SelectedCursor(CourseScene cs)
        {
            var activeViewport = cs.activeViewport;
            var mEditContext = cs.mEditContext;
            var selectedArea = cs.selectedArea;

            var cursor = activeViewport.cursor;

            if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                activeViewport.cursor = null;

            if (cursor == null)
                return;

            ImGui.Text("Select both an actor along with the cursor.");
            ImGui.Text("You can also press 'R' to pivot.");

            ImGui.Dummy(new Vector2(0, 10));

            ImGui.Text("Actor Ring Generator");
            ImGui.Separator();

            if (mEditContext.GetObjectCountOfType<CourseActor>() == 1)
                pickActor = mEditContext.GetFirstObjectOfType<CourseActor>();

            var pickActorText = "Select an actor";

            ImGui.Text("Select Actor:");
            if (ImGui.Button(IconUtil.ICON_EYE_DROPPER))
            {
                Task.Run(async () =>
                {
                    pickActor = await cs.ActorPicker();
                });
            }

            ImGui.SameLine();

            if (pickActor != null)
                pickActorText = pickActor.mPackName;

            ImGui.InputText("##Ring Actor", ref pickActorText, 256, ImGuiInputTextFlags.ReadOnly);

            //Credits to https://github.com/aurelionshole/aurelionshole.github.io for Actor Ring code

            ImGui.Text("Points:");
            if (ImGui.InputInt($"##Count", ref count, 1))
                count = Math.Clamp(count, 1, 360);

            ImGui.Text("Offset:");
            ImGui.InputFloat($"##Offset", ref offset, 1);

            ImGui.Text("Radius:");
            ImGui.InputFloat($"##Radius", ref radius, 1);

            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Checkbox("Rotate Actors", ref doRotate);

            if (count != 0 && pickActor != null)
            {
                float ringAngle = 360 / count;
                float angle = offset % ringAngle;
                if (ImGui.Button("Create Actors"))
                {
                    var batchAction = mEditContext.BeginBatchAction();
                    CourseActor newActor = null;
                    for (int i = 0; i < count; i++)
                    {
                        newActor = new CourseActor(pickActor.mPackName, selectedArea.mRootHash, pickActor.mLayer);
                        newActor.mTranslation.X = (float)(cursor.mTranslation.X - radius * Math.Sin(angle * (Math.PI / 180)));
                        newActor.mTranslation.Y = (float)(cursor.mTranslation.Y + radius * Math.Cos(angle * (Math.PI / 180)));

                        if (doRotate)
                            newActor.mRotation.Z = angle * (MathF.PI / 180f);

                        var j = 0;
                        do
                        {
                            j++;
                        } while (selectedArea.GetActors().Any(x => x.mName == $"{newActor.mPackName}{j}"));

                        newActor.mName = $"{newActor.mPackName}{j}";

                        mEditContext.AddActor(newActor);
                        angle = (angle + ringAngle) % 360f;
                    }
                    batchAction.Commit($"{IconUtil.ICON_PLUS_CIRCLE} Placed Actor Ring");
                }
            }

            if (mEditContext.GetSelectedObjects<CourseActor>().ToList().Count >= 1)
            {
                ImGui.Dummy(new Vector2(0, 50));
                bool run = false;

                string pivotText = cursor.delta.ToString();

                ImGui.Text("Pivot Actors");
                ImGui.Separator();

                ImGui.Text("Pivot Angle: Enter angle in degrees");
                if (ImGui.InputText("##Pivot", ref pivotText, 32))
                {
                    if (pivotText == "")
                        cursor.delta = 0;
                    else if (float.TryParse(pivotText, out float value))
                        cursor.delta = value;

                    run = true;
                }

                if (previousDelta != cursor.delta && run)
                {
                    float deltaAngle = cursor.delta - previousDelta;
                    cursor.applyRotation = true;
                    cursor.pivotedActors = mEditContext.GetSelectedObjects<CourseActor>().ToArray();
                    foreach (CourseActor actor in cursor.pivotedActors)
                    {
                        actor.mRotation = actor.mStartingRot;
                        actor.mTranslation = actor.mStartingTrans;

                        Vector2 delta = ImGui.GetIO().MouseDelta;
                        System.Numerics.Vector3 cursorTrans = cursor.mTranslation;
                        actor.mRotation.Z += cursor.delta * (MathF.PI / 180f);
                        actor.mTranslation = cursorTrans + System.Numerics.Vector3.Transform(actor.mTranslation -
                                             cursorTrans, Matrix4x4.CreateRotationZ(cursor.delta * (MathF.PI / 180f)));
                        float angle = cursor.delta * (MathF.PI / 180f);
                    }
                    previousDelta = cursor.delta;
                    return;
                }
            }
        }
    }
}
