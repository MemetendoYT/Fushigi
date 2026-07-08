using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.Byml.Writer;
using Fushigi.param;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Fushigi.course
{

    public class CourseComment
    {
        private static CourseComment commentToDelete;
        public static bool draggingComment;
        private static bool canEditStart;
        public static bool draggingCommentIcon;
        private static int commentVal;
        public CourseComment(BymlHashTable commentNode)
        {
            mTranslation = BymlUtil.GetVector3FromArray(commentNode["Translate"] as BymlArrayNode);
            mText = BymlUtil.GetNodeData<string>(commentNode["Comment"]);
        }

        public CourseComment()
        {
            mText = "";
            mTranslation = new System.Numerics.Vector3(0.0f);
            mOpened = false;
        }



        public BymlHashTable BuildNode()
        {
            BymlHashTable tbl = new();
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<string>(mText), "Comment");

            BymlArrayNode translateNode = new(3);
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.X));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.Y));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.Z));

            tbl.AddNode(BymlNodeId.Array, translateNode, "Translate");

            return tbl;
        }
        internal void DragComment(LevelViewport viewport, CourseAreaEditContext mEditContext)
        {
            if (draggingComment || draggingCommentIcon)
            {
                if (!viewport.mMultiSelecting && mEditContext.IsSingleObjectSelected(out CourseComment? comment))
                {
                    if (canEditStart)
                    {
                        comment.mStartingTrans = comment.mTranslation;
                        canEditStart = false;
                    }

                    var posVec = viewport.CalcPosVec(comment.mStartingTrans);
                    comment.mTranslation.X = posVec.X;
                    comment.mTranslation.Y = posVec.Y;
                }
            }
            else
            {
                canEditStart = true;
            }
        }

        internal static void AddComment(LevelViewport viewport, CourseAreaEditContext mEditContext)
        {
            var comment = new CourseComment();
            var pos = viewport.ScreenToWorld(viewport.storedMousePos);
            comment.mTranslation.X = MathF.Round(pos.X * 2, MidpointRounding.AwayFromZero) / 2;
            comment.mTranslation.Y = MathF.Round(pos.Y * 2, MidpointRounding.AwayFromZero) / 2;
            comment.mTranslation.Z = 0.0f;
            mEditContext.AddComment(comment);
        }


        public string mText;
        public string mLayer;
        public bool mOpened;
        public System.Numerics.Vector3 mStartingTrans;
        public System.Numerics.Vector3 mTranslation;

        internal static void DrawComments(LevelViewport viewport, CourseAreaEditContext mEditContext, CourseArea mArea)
        {
            int i = 0;
            foreach (CourseComment comment in mArea.GetComments())
            {
                i++;
                Vector2 pos = viewport.WorldToScreen(comment.mTranslation);
                Vector2 iconPos = new Vector2(pos.X - (30 * MainWindow.dpiScale), pos.Y - (20 * MainWindow.dpiScale));
                ImGui.SetCursorScreenPos(iconPos);


                ImGui.BeginChild(
                    $"CommentIcon{i}",
                    new Vector2(40 * MainWindow.dpiScale, 40 * MainWindow.dpiScale),
                    ImGuiChildFlags.None,
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse |
                    ImGuiWindowFlags.NoBackground
                );


                ImGui.SetCursorPos(Vector2.Zero);


                if (ImGui.Button(IconUtil.ICON_MAIL_BULK, new Vector2(40 * MainWindow.dpiScale, 40 * MainWindow.dpiScale)))
                {
                    if (!draggingCommentIcon)
                        comment.mOpened = !comment.mOpened;

                }

                if (!comment.mOpened &&
                  ImGui.IsItemActive() &&
                  ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    draggingCommentIcon = true;
                    if (mEditContext.GetSelectedObjects<object>().Count() > 1)
                    {
                        mEditContext.DeselectAll();
                    }
                    mEditContext.Select(comment);



                    if (!comment.mOpened && ImGui.IsItemActive() && ImGui.IsKeyPressed(ImGuiKey.Delete))
                    {
                        commentToDelete = comment;
                        commentVal = i;
                    }
                }


                bool iconHovered = ImGui.IsItemHovered();

                if (iconHovered)
                    viewport.panOverride = true;

                if (iconHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    if (mEditContext.GetSelectedObjects<object>().Count() > 1)
                        mEditContext.DeselectAll();

                    mEditContext.Select(comment);
                }


                ImGui.EndChild();

                if (!comment.mOpened)
                    continue;

                ImGui.SetCursorScreenPos(pos);

                ref string text = ref comment.mText;
                ImGui.BeginChild(
                  $"CommentWindow{i}",
                  new Vector2(200 * MainWindow.dpiScale, 100 * MainWindow.dpiScale),
                  ImGuiChildFlags.None,
                  ImGuiWindowFlags.NoDecoration |
                  ImGuiWindowFlags.NoScrollbar |
                  ImGuiWindowFlags.NoScrollWithMouse
                 );

                ImGui.InputTextMultiline(
                    $"##Comment{i}",
                    ref text,
                    1024,
                    new Vector2(200 * MainWindow.dpiScale, 100 * MainWindow.dpiScale)
                );


                bool textActive = ImGui.IsItemActive();
                bool textFocused = ImGui.IsItemFocused();
                bool textHovered = ImGui.IsItemHovered();

                if (textHovered)
                {
                    viewport.panOverride = true;
                }


                if (textActive || textFocused)
                {
                    draggingComment = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                    if (mEditContext.GetSelectedObjects<object>().Count() > 1)
                    {
                        mEditContext.DeselectAll();
                    }
                    mEditContext.Select(comment);
                    if (ImGui.IsKeyPressed(ImGuiKey.Delete))
                    {
                        commentToDelete = comment;
                        commentVal = i;
                    }
                }
                ImGui.EndChild();
            }

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                draggingCommentIcon = false;

            if (!CourseScene.insideViewport)
                viewport.panOverride = false;

            if (commentToDelete != null)
            {
                mEditContext.RemoveComment(commentToDelete, commentVal);
                commentToDelete = null;
            }
        }
        public class CourseCommentHolder
    {
        public CourseCommentHolder()
        {

        }

        public CourseCommentHolder(BymlArrayNode commentArray)
        {
            foreach (BymlHashTable comment in commentArray.Array)
                mComments.Add(new CourseComment(comment));
        }
   
            public BymlArrayNode SerializeToArray()
            {
                BymlArrayNode node = new((uint)mComments.Count);

                foreach (var comment in mComments)
                {
                    node.AddNodeToArray(comment.BuildNode());
                }

                return node;
            }


            public List<CourseComment> mComments = [];
    }


    }
}
