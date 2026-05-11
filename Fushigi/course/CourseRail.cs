using Fasterflect;
using Fushigi.Byml;
using Fushigi.param;
using Fushigi.util;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi.course
{
    public class CourseRail
    {
        public CourseRail(uint areaHash, string type = "Default")
        {
            mType = type;
            mHash = RandomUtil.GetRandom();
            mAreaHash = areaHash;
            mRailParam = "Work/Gyml/Rail/RailParam/"+type+".game__rail__RailParam.gyml";
            mIsClosed = false;
            var railParams = ParamDB.GetRailComponent(mType);
            var railParent = ParamDB.GetRailComponentParent(railParams);
            var comp = ParamDB.GetRailComponentParams(railParams);
            if (railParent != "null")
            {
                var parentComp = ParamDB.GetRailComponentParams(railParent);
                foreach (var component in parentComp)
                {
                    comp.TryAdd(component.Key, component.Value);
                }
            }

            foreach (string component in comp.Keys)
            {
                var c = comp[component];
                mParameters.Add(component, c.InitValue);
            }
        }

        public CourseRail(BymlHashTable node)
        {
            mAreaHash = BymlUtil.GetNodeData<uint>(node["AreaHash"]);
            mRailParam = BymlUtil.GetNodeData<string>(node["Gyaml"]);
            mHash = BymlUtil.GetNodeData<ulong>(node["Hash"]);
            mIsClosed = BymlUtil.GetNodeData<bool>(node["IsClosed"]);

            mType = Path.GetFileNameWithoutExtension(BymlUtil.GetNodeData<string>(node["Gyaml"])).Split(".game")[0];
            var railParams = ParamDB.GetRailComponent(mType);
            var railParent = ParamDB.GetRailComponentParent(railParams);
            var comp = ParamDB.GetRailComponentParams(railParams);
            if (railParent != "null")
            {
                var parentComp = ParamDB.GetRailComponentParams(railParent);
                foreach (var component in parentComp)
                {
                    comp.TryAdd(component.Key, component.Value);
                }
            }

            if (!node.ContainsKey("Dynamic"))
            {
                foreach (string component in comp.Keys)
                {
                    var c = comp[component];
                    mParameters.Add(component, c.InitValue);
                }
            }
            else
            {
                var dynamicNode = node["Dynamic"] as BymlHashTable;

                foreach (string component in comp.Keys)
                {
                    if (dynamicNode.ContainsKey(component))
                    {
                        mParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], comp[component]));
                    }
                    else
                    {
                        var c = comp[component];
                        mParameters.Add(component, c.InitValue);
                    }
                }
            }

            var railArray = node["Points"] as BymlArrayNode;

            foreach(BymlHashTable rail in railArray.Array)
            {
                mPoints.Add(new CourseRailPoint(rail, mType, this));
            }
        }

        public CourseRail CloneRail(CourseArea areaTo)
        {
            CourseRail cloned = new(mAreaHash, mType)
            {
                mType = mType,
                mAreaHash = mAreaHash,
                mRailParam = mRailParam,
                mIsClosed = mIsClosed,
                mParameters = mParameters
            };
            cloned.mHash = mHash;
            cloned.mAreaHash = areaTo.mRootHash;
            foreach (var point in mPoints)
            {
                cloned.mPoints.Add(point.ClonePoint(point, this));
            }
           
            return cloned;
        }

        public static CourseRail fetchIndex(CourseRailHolder railHolder, CourseRailPoint mSelectedRailPoint)
        {
            var parentRail = new CourseRail(0);
            foreach (CourseRail rail in railHolder.mRails)
            {
                if (rail.mPoints.Contains(mSelectedRailPoint))
                {
                    parentRail = rail;
                    parentRail.mPoints.IndexOf(mSelectedRailPoint);
                    break;
                }
            }
            //return railHolder.mRails.IndexOf(parentRail)
                  return parentRail;
        }

        public static int findRailNum(CourseRailHolder railHolder, CourseRailPoint mSelectedRailPoint)
        {
            var parentRail = fetchIndex(railHolder, mSelectedRailPoint);
            return railHolder.mRails.IndexOf(parentRail);
        }

        public static int findPointNum(CourseRailHolder railHolder, CourseRailPoint mSelectedRailPoint)
        {
            var parentRail = fetchIndex(railHolder, mSelectedRailPoint);
            return parentRail.mPoints.IndexOf(mSelectedRailPoint);
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable node = new();

            node.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mAreaHash), "AreaHash");
            node.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mRailParam), "Gyaml");
            node.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");
            node.AddNode(BymlNodeId.Bool, BymlUtil.CreateNode<bool>(mIsClosed), "IsClosed");

            BymlHashTable dynamicNode = new();

            foreach (KeyValuePair<string, object> dynParam in mParameters)
            {
                object param = mParameters[dynParam.Key];
                var valueNode = BymlUtil.CreateNode(param);
                dynamicNode.AddNode(valueNode.Id, valueNode, dynParam.Key);
            }

            node.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");

            BymlArrayNode pointsArr = new((uint)mPoints.Count);

            foreach (CourseRailPoint pnt in mPoints)
            {
                pointsArr.AddNodeToArray(pnt.BuildNode());
            }

            node.AddNode(BymlNodeId.Array, pointsArr, "Points");

            return node;
        }

        public bool TryGetPoint(ulong hash, [NotNullWhen(true)] out CourseRailPoint? point)
        {
            point = mPoints.Find(x => x.mHash == hash);
            return point is not null;
        }

        public void ReverseRailPoints()
        {
            List<CourseRailPoint> newPoints = new List<CourseRailPoint>();

            foreach (var p in mPoints)
            {
                newPoints = newPoints.Prepend(p).ToList();

                if (p.mIsCurve)
                {
                    p.mControl.mTranslation = 2 * p.mTranslation - p.mControl.mTranslation;
                }
            }

            mPoints = newPoints;
        }

        public CourseRailPoint this[ulong hash]
        {
            get
            {
                bool exists = TryGetPoint(hash, out CourseRailPoint? point);
                Debug.Assert(exists);
                return point!;
            }
        }

        public uint mAreaHash;
        public string mRailParam;
        public ulong mHash;
        public string mType;
        public bool mIsClosed;
        public List<CourseRailPoint> mPoints = new();
        public Dictionary<string, object> mParameters = new();

        public class CourseRailPoint
        {
            public CourseRailPoint(string type, CourseRail rail)
            {
                this.mHash = RandomUtil.GetRandom();
                this.mTranslation = new System.Numerics.Vector3();
                this.mControl = new(this, mTranslation);
                this.mParent = rail;

                IDictionary<string, ParamDB.ComponentParam> comp;

                if (ParamDB.TryGetRailPointComponent(type, out var componentName))
                    comp = ParamDB.GetRailComponentParams(componentName);
                else
                    comp = ImmutableDictionary.Create<string, ParamDB.ComponentParam>();

                foreach (string component in comp.Keys)
                {
                    var c = comp[component];
                    mParameters.Add(component, c.InitValue);
                }
            }


            public CourseRailPoint(CourseRailPoint point, CourseRail rail)
            {
                this.mHash = RandomUtil.GetRandom();
                this.mTranslation = point.mTranslation;
                this.mControl = new(this, point.mControl.mTranslation);
                this.mParent = rail;
                foreach (var param in point.mParameters)
                    this.mParameters.Add(param.Key, param.Value);
            }

            public CourseRailPoint(BymlHashTable node, string pointParam, CourseRail rail)
            {
                mHash = BymlUtil.GetNodeData<ulong>(node["Hash"]);
                mTranslation = BymlUtil.GetVector3FromArray(node["Translate"] as BymlArrayNode);
                mControl = new(this, mTranslation);
                mParent = rail;
                IDictionary<string, ParamDB.ComponentParam> comp;
                if (ParamDB.TryGetRailPointComponent(pointParam, out var componentName))
                    comp = ParamDB.GetRailComponentParams(componentName);
                else
                    comp = ImmutableDictionary.Create<string, ParamDB.ComponentParam>();

                if (!node.ContainsKey("Dynamic"))
                {
                    foreach (string component in comp.Keys)
                    {
                        var c = comp[component];
                        mParameters.Add(component, c.InitValue);
                    }

                    /* we're done with this rail, so we exit as we have no more data to read */
                    return;
                }

                if (node.ContainsKey("Control1"))
                {
                    mControl.mTranslation = BymlUtil.GetVector3FromArray(node["Control1"] as BymlArrayNode);
                    mIsCurve = true;
                }

                var dynamicNode = node["Dynamic"] as BymlHashTable;

                foreach (string component in comp.Keys)
                {
                    if (dynamicNode.ContainsKey(component))
                    {
                        mParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], comp[component]));
                    }
                    else
                    {
                        var c = comp[component];
                        mParameters.Add(component, c.InitValue);
                    }
                }
            }

            public CourseRailPoint ClonePoint(CourseRailPoint point, CourseRail rail)
            {
                CourseRailPoint cloned = new(point, rail);
                cloned.mIsCurve = point.mIsCurve;
                cloned.mParent = rail;
                cloned.mHash = mHash;

                return cloned;
            }
            public BymlHashTable BuildNode()
            {
                BymlHashTable tbl = new();
                tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");

                BymlHashTable dynamicNode = new();

                foreach (KeyValuePair<string, object> dynParam in mParameters)
                {
                    object param = mParameters[dynParam.Key];
                    var valueNode = BymlUtil.CreateNode(param);
                    dynamicNode.AddNode(valueNode.Id, valueNode, dynParam.Key);
                }

                tbl.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");

                if (mIsCurve)
                {
                    BymlArrayNode controlNode = new(3);
                    controlNode.AddNodeToArray(BymlUtil.CreateNode(mControl.mTranslation.X));
                    controlNode.AddNodeToArray(BymlUtil.CreateNode(mControl.mTranslation.Y));
                    controlNode.AddNodeToArray(BymlUtil.CreateNode(mControl.mTranslation.Z));

                    tbl.AddNode(BymlNodeId.Array, controlNode, "Control1");
                }

                BymlArrayNode translateNode = new(3);
                translateNode.AddNodeToArray(BymlUtil.CreateNode(mTranslation.X));
                translateNode.AddNodeToArray(BymlUtil.CreateNode(mTranslation.Y));
                translateNode.AddNodeToArray(BymlUtil.CreateNode(mTranslation.Z));

                tbl.AddNode(BymlNodeId.Array, translateNode, "Translate");

                return tbl;
            }

            public ulong mHash;
            public Dictionary<string, object> mParameters = new();
            public System.Numerics.Vector3 mStartingTrans;
            public System.Numerics.Vector3 mTranslation;
            public CourseRailPointControl mControl;
            public CourseRail mParent;
            public bool mIsCurve;
        }
        public class CourseRailPointControl
        {
            public CourseRailPointControl(CourseRail.CourseRailPoint point, System.Numerics.Vector3 pos)
            {
                this.point = point;
                this.mTranslation = pos;
            }
            public CourseRail.CourseRailPoint point;
            public System.Numerics.Vector3 mTranslation;
            public System.Numerics.Vector3 mStartingTrans;
        }
    }
    public class CourseRailHolder
    {
        public CourseRailHolder()
        {

        }

        public CourseRailHolder(BymlArrayNode railArray)
        {
            foreach(BymlHashTable rail in railArray.Array)
            {
                mRails.Add(new CourseRail(rail));
            }
        }

        public bool TryGetRail(ulong hash, [NotNullWhen(true)] out CourseRail? rail)
        {
            rail = mRails.Find(x => x.mHash == hash);
            return rail is not null;
        }

        public CourseRail this[ulong hash]
        {
            get
            {
                bool exists = TryGetRail(hash, out CourseRail? rail);
                Debug.Assert(exists);
                return rail!;
            }
        }
        public BymlArrayNode SerializePrefab(List<CourseRail> prefabRail)
        {
            BymlArrayNode node = new((uint)prefabRail.Count);

            foreach (CourseRail rail in prefabRail)
            {
                node.AddNodeToArray(rail.BuildNode());
            }

            return node;
        }
    

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mRails.Count);

            foreach (CourseRail rail in mRails)
            {
                node.AddNodeToArray(rail.BuildNode());
            }

            return node;
        }

        public List<CourseRail> mRails = new();
    }

    public class CourseActorToRailLink
    {
        public CourseActorToRailLink(string linkName)
        {
            mSourceActor = 0;
            mDestRail = 0;
            mDestPoint = 0;
            mLinkName = linkName;
        }

        public CourseActorToRailLink(BymlHashTable table)
        {
            mSourceActor = BymlUtil.GetNodeData<ulong>(table["Src"]);
            mDestRail = BymlUtil.GetNodeData<ulong>(table["Dst"]);
            mDestPoint = BymlUtil.GetNodeData<ulong>(table["Point"]);
            mLinkName = BymlUtil.GetNodeData<string>(table["Name"]);
        }

        public BymlHashTable BuildNode()
        {
            BymlHashTable tbl = new();
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mDestRail), "Dst");
            tbl.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mLinkName), "Name");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mDestPoint), "Point");
            tbl.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mSourceActor), "Src");
            return tbl;
        }

        public ulong mSourceActor;
        public ulong mDestRail;
        public ulong mDestPoint;
        public string mLinkName;
    }

    public class CourseActorToRailLinksHolder
    {
        public CourseActorToRailLinksHolder()
        {
        }

        public CourseActorToRailLinksHolder(BymlArrayNode array, CourseActorHolder actorHolder, CourseRailHolder railHolder)
        {
            foreach (BymlHashTable railLink in array.Array)
            {
                mLinks.Add(new CourseActorToRailLink(railLink));
            }
        }

        public bool TryGetLinkWithSrcActor(ulong hash, 
            [NotNullWhen(true)] out CourseActorToRailLink? link)
        {
            link = mLinks.Find(x => x.mSourceActor == hash);

            return link is not null;
        }

        public List<CourseActorToRailLink?> TryGetLinksWithSrcActor(ulong hash)
        {
            List<CourseActorToRailLink?> list = new();

            if (mLinks.Count > 0)
            {
                foreach (CourseActorToRailLink link in mLinks)
                {
                    if (link.mSourceActor == hash)
                        list.Add(link);
                }
            }

            return list;
        }

        public bool TryGetLinkWithDestRail(ulong hash,
            [NotNullWhen(true)] out CourseActorToRailLink? link)
        {
            link = mLinks.Find(x => x.mDestRail == hash);

            return link is not null;
        }

        public bool TryGetLinkWithDestRailAndPoint(ulong railHash, ulong pointHash,
            [NotNullWhen(true)] out CourseActorToRailLink? link)
        {
            link = mLinks.Find(x => x.mDestRail == railHash && x.mDestPoint == pointHash);

            return link is not null;
        }

        public BymlArrayNode SerializePrefab(List<CourseActor> selectedActors, List<CourseRail> selectedRails)
        {
            BymlArrayNode node = new();
            HashSet<CourseActorToRailLink> added = new();

            HashSet<ulong> selectedActorHashes = selectedActors
                .Select(a => a.mHash)
                .ToHashSet();

            HashSet<ulong> selectedRailHashes = selectedRails
              .Select(r => r.mHash)
              .ToHashSet();


            foreach (var link in mLinks)
            {
                bool sourceSelected = selectedActorHashes.Contains(link.mSourceActor);
                bool destSelected = selectedRailHashes.Contains(link.mDestRail);

                if (sourceSelected && destSelected)
                {
                    if (added.Add(link))
                        node.AddNodeToArray(link.BuildNode());
                }
            }

            return node;
        }

   

        public BymlArrayNode SerializeToArray()
        {
            BymlArrayNode node = new((uint)mLinks.Count);

            foreach (var link in mLinks)
            {
                node.AddNodeToArray(link.BuildNode());
            }

            return node;
        }

        public List<CourseActorToRailLink> mLinks = new();
    }
}
