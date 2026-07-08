using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.Byml.Writer;
using Fushigi.param;
using Fushigi.util;
using ImGuiNET;
using System.Diagnostics.CodeAnalysis;

namespace Fushigi.course
{

    public class CourseComment
    {
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

        public string mText;
        public string mLayer;
        public bool mOpened;
        public System.Numerics.Vector3 mStartingTrans;
        public System.Numerics.Vector3 mTranslation;
     

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
