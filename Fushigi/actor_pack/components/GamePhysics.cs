using Fushigi.Bfres;
using Fushigi.Byml;
using Fushigi.Byml.Serializer;
using Fushigi.course;
using Fushigi.util;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Routing;
using Silk.NET.SDL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vector3 = System.Numerics.Vector3;

namespace Fushigi.actor_pack.components
{
    [Serializable]
    public class GamePhysics
    {
        [BymlProperty("$parent")]
        public string parent { get; set; }
        
        [BymlProperty("ControllerSetPath", DefaultValue = "")]
        public string mPath { get; set; }
    }

    [Serializable]
    public class ControllerSetParam
    {
        [BymlProperty("$parent")]
        public string parent { get; set; }
        
        public List<PathAry> ShapeNamePathAry { get; set; }

        [BymlProperty("MatterRigidBodyNamePathAry", DefaultValue = "")]
        public List<PathAry> mRigids { get; set; }

        [BymlProperty("RigidBodyEntityNamePathAry", DefaultValue = "")]
        public List<PathAry> mEntity { get; set; }

        [BymlProperty("RigidBodySensorNamePathAry", DefaultValue = "")]
        public List<PathAry> mSensor { get; set; }
    }

    [Serializable]
    public class PathAry
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public class ShapeParamList
    {
        [BymlProperty("AutoCalc")]
        public AutoCalc mCalc { get; set; }

        [BymlProperty("Box", DefaultValue = "")]
        public List<Box> mBox { get; set; }

        [BymlProperty("Sphere", DefaultValue = "")]
        public List<Sphere> mSphere { get; set; } 

        [BymlProperty("Capsule", DefaultValue = "")]
        public List<Capsule> mCapsule { get; set; }

        [BymlProperty("Polytope", DefaultValue = "")]
        public List<Polytope> mPoly { get; set; }

    }

    [Serializable]
    public class RigidParam
    {
        [BymlProperty("$parent")]
        public string parent { get; set; }
        public string ShapeName { get; set; }

        public List<object> ShapeNames { get; set; }
    }

    [Serializable]
    public class AutoCalc
    {
        [BymlProperty("Axis")]
        public Vector3 mAxis { get; set; }

        [BymlProperty("Center")]
        public Vector3 mCenter { get; set; }

        [BymlProperty("Min")]
        public Vector3 mMin { get; set; }

        [BymlProperty("Max")]
        public Vector3 mMax { get; set; }

        [BymlProperty("Tensor")]
        public Vector3 mTensor { get; set; }


        [BymlProperty("Volume")]
        public float mVolume { get; set; }

    }

    [Serializable]
    public class ShapeParam
    {
        [BymlProperty("AutoCalc", DefaultValue = "")]
        public AutoCalc mCalc { get; set; }

        public float Radius { get; set; }

    }

    [Serializable]
    public class Capsule : DefaultShape
    {
        internal float screenRadius;

        public float Radius { get; set; }

        [BymlProperty("CenterA")]
        public CapsulePoint mCenterA { get; set; }

        [BymlProperty("CenterB")]
        public CapsulePoint mCenterB { get; set; }
    }

    public class CapsulePoint : DefaultShape
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 Center => new Vector3(X, Y, Z);
        public Capsule Parent;
    }


    public class PolytopeVertex : DefaultShape
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 Center => new Vector3(X, Y, Z);
        public Polytope Parent; 
    }
    public class Box : DefaultShape
    {
        public Vector3 mRotation { get; set; }

        [BymlProperty("HalfExtents")]
        public Vector3 mExtents { get; set; }

        public static BymlArrayNode SerializeToArray(ShapeParamList list)
        {
            BymlArrayNode node = new((uint)list.mBox.Count);

            foreach (var box in list.mBox)
                node.AddNodeToArray(BuildNode(box));

            return node;
        }

        public static BymlHashTable BuildNode(Box box)
        {
            BymlHashTable table = new();

            AddMat(box, table);

            table.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(box.mExtents), "HalfExtents");
            return table;
        }

    }
    public class Polytope : DefaultShape
    {
        public List<Polytope> mPolytopes = [];

        [BymlProperty("Vertices")]
        public List<PolytopeVertex> Vertices { get; set; }

        public float zBack = -5;
        public float zFront;

        public bool Merged = false;
        public List<PolytopeVertex> mergeList;

        [BymlProperty("AutoCalc")]
        public AutoCalc mCalc { get; set; }

        public float MassDistributionFactor;

        [BymlProperty("Name")]
        public string name { get; set; }


        public static BymlArrayNode SerializeToArray(ShapeParamList list)
        {
            BymlArrayNode node = new((uint)list.mPoly.Count);

            foreach (var polytope in list.mPoly)
                node.AddNodeToArray(BuildNode(polytope, list.mCalc));

            return node;
        }

        public static BymlHashTable BuildNode(Polytope polytope, AutoCalc backupCalc)
        {
            BymlHashTable table = new();

            var mCalc = polytope.mCalc;
            if (mCalc == null)
                mCalc = backupCalc;

            var autoCalc = AutoCalc(mCalc);
            table.AddNode(BymlNodeId.Hash, autoCalc, "AutoCalc");

            AddMat(polytope, table);

            BymlArrayNode vertsArray = new((uint)polytope.Vertices.Count);

            if(polytope.name != null)      
                table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(polytope.name), "Name");

                table.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(polytope.OffsetTranslation), "OffsetTranslation");
                table.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(polytope.OffsetRotation), "OffsetRotation");

            foreach (var v in polytope.mergeList)
            {
                vertsArray.AddNodeToArray(DefaultShape.MakeVec3(new Vector3(v.X, v.Y, polytope.zFront)));
                vertsArray.AddNodeToArray(DefaultShape.MakeVec3(new Vector3(v.X, v.Y, polytope.zBack)));
            }

            table.AddNode(BymlNodeId.Array, vertsArray, "Vertices");


            return table;
        }

    }

        public class Sphere : DefaultShape
    {
        public float Radius { get; set; }

        public static BymlArrayNode SerializeToArray(ShapeParamList list)
        {
            BymlArrayNode node = new((uint)list.mSphere.Count);

            foreach (var sphere in list.mSphere)
                node.AddNodeToArray(BuildNode(sphere));

            return node;
        }

        public static BymlHashTable BuildNode(Sphere sphere)
        {
            BymlHashTable table = new();

            AddMat(sphere, table);

            //table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(sphere.name), "Name");
            //table.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(sphere.OffsetTranslation), "OffsetTranslation");
            //table.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(sphere.OffsetRotation), "OffsetRotation");
            table.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>(sphere.Radius), "Radius");
            return table;
        }

    }
    public class DefaultShape
    {
        [BymlProperty("MaterialPresets")]
        public List<string> mPresets { get; set; }

        public Vector3 mStartingTrans = new Vector3(0, 0, 0);

        public Vector3 Center { get; set; }

        public Vector3 OffsetRotation { get; set; }

        public Vector3 OffsetTranslation { get; set; }

        public static BymlHashTable MakeVec3(Vector3 v)
        {
            BymlHashTable vertHash = new();
            vertHash.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>((Single)v.X), "X");
            vertHash.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>((Single)v.Y), "Y");
            vertHash.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>((Single)v.Z), "Z");
            return vertHash;
        }

        public static BymlHashTable AutoCalc(AutoCalc mCalc)
        {
            BymlHashTable autoCalc = new();
            autoCalc.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(mCalc.mAxis), "Axis");
            autoCalc.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(mCalc.mCenter), "Center");
            autoCalc.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(mCalc.mMax), "Max");
            autoCalc.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(mCalc.mMin), "Min");
            autoCalc.AddNode(BymlNodeId.Hash, DefaultShape.MakeVec3(mCalc.mTensor), "Tensor");
            autoCalc.AddNode(BymlNodeId.Float, BymlUtil.CreateNode<float>(mCalc.mVolume), "Volume");
            return autoCalc;
        }
        public static void AddMat(DefaultShape shape, BymlHashTable table)
        {
            BymlArrayNode matArray = new((uint)shape.mPresets.Count);

            foreach (var mat in shape.mPresets)
                matArray.AddNodeToArray(BymlUtil.CreateNode<string>(mat));

            table.AddNode(BymlNodeId.Array, matArray, "MaterialPresets");
        }
    


    }
}