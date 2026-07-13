using Fasterflect;
using Fushigi.Byml;
using Fushigi.param;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

using System.Numerics;

namespace Fushigi.course
{
    public class CourseRail
    {
        public static CourseRailPoint closestSelected;
        private static bool multiRailDelete;
        public static List<(CourseRail rail, CourseRail.CourseRailPoint point)> deleteList;
        public static bool ShowRails = true;
        public CourseRail(uint areaHash, string type = "Default")
        {
            mType = type;
            mHash = RandomUtil.GetRandom();
            mAreaHash = areaHash;
            mRailParam = "Work/Gyml/Rail/RailParam/"+type+".game__rail__RailParam.gyml";
            mIsClosed = false;
            RegenerateParameters();
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

        internal static void DrawRails(LevelViewport viewport, CourseAreaEditContext mEditContext, CourseArea mArea)
        {
            if (mArea.mRailHolder.mRails.Count > 0 && !viewport.ScreenshotMode && ShowRails)
            {
                const float pointSize = 8.0f;
                uint color = Color.HotPink.ToAbgr();

                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {
                    bool rail_selected = mEditContext.IsSelected(rail);

                    Vector2[] GetPoints()
                    {
                        Vector2[] points = new Vector2[rail.mPoints.Count];
                        for (int i = 0; i < rail.mPoints.Count; i++)
                        {
                            System.Numerics.Vector3 p = rail.mPoints[i].mTranslation;
                            points[i] = viewport.WorldToScreen(new(p.X, p.Y, p.Z));
                        }
                        return points;
                    }


                    CourseRail.CourseRailPoint selectedPoint = null;

                    foreach (var point in rail.mPoints)
                    {
                        var pos2D = viewport.WorldToScreen(new(point.mTranslation.X, point.mTranslation.Y, point.mTranslation.Z));
                        var contPos2D = viewport.WorldToScreen(point.mControl.mTranslation);

                        bool isHovered = (ImGui.GetMousePos() - pos2D).Length() < 10.0f;

                        if (isHovered)
                            viewport.mHoveredObject = point;

                        bool selected = false;

                        if (closestSelected != null)
                        {
                            if (point == closestSelected)
                                selected = true;
                        }
                        else
                            selected = mEditContext.IsSelected(point) || mEditContext.IsSelected(point.mControl);

                        if (selected)
                        {
                            selectedPoint = point;
                            if ((ImGui.GetMousePos() - contPos2D).Length() < 10.0f)
                                viewport.mHoveredObject = point.mControl;
                        }
                    }

                    if (selectedPoint != null && (ImGui.IsKeyPressed(ImGuiKey.Delete) || (ImGui.GetIO().KeyShift && ImGui.IsKeyPressed(ImGuiKey.Backspace))))
                    {
                        if (mEditContext.GetObjectCountOfType<CourseRail.CourseRailPoint>() > 1)
                        {
                            var railPoints = mEditContext.GetSelectedObjects<CourseRail.CourseRailPoint>().ToArray();
                            multiRailDelete = true;
                            foreach (var point in railPoints)
                            {
                                if (rail.mPoints.Contains(point))
                                    deleteList.Add((rail, point));
                            }
                        }
                        else
                            mEditContext.DeleteRailPoint(rail, selectedPoint);
                    }

                    bool add_point = ImGui.IsMouseClicked(0) && ImGui.IsMouseDown(0) && ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyShift && !mEditContext.IsAnySelected<CourseActor>();

                    //Insert point to existing rail
                    if (selectedPoint != null && add_point)
                    {
                        var index = rail.mPoints.IndexOf(selectedPoint);
                        var newPoint = new CourseRail.CourseRailPoint(selectedPoint, rail);
                        rail.addRailPoint(index, newPoint, mEditContext, viewport);

                    }
                    //Add first point to rail
                    else if (rail_selected && add_point)
                    {
                        var newPoint = new CourseRail.CourseRailPoint(rail.mType, rail);
                        rail.addRailPoint(-1, newPoint, mEditContext, viewport);
                    }
                }

                if (multiRailDelete)
                {
                    Console.WriteLine("Batch deleting " + deleteList.Count + " rail points");
                    var batch = mEditContext.BeginBatchAction();

                    foreach (var (rail, point) in deleteList)
                    {
                        //var revertible = rail.mPoints.RevertableRemove(point);
                        //mEditContext.CommitAction(revertible);
                    }

                    batch.Commit($"{IconUtil.ICON_TRASH} Delete Rail Points");
                    multiRailDelete = false;
                    deleteList.Clear();
                }

                // Draw Rails to the Viewport
                viewport.mDrawList.Flags &= ~ImDrawListFlags.AntiAliasedLines;
                foreach (CourseRail rail in mArea.mRailHolder.mRails)
                {

                    bool selected = mEditContext.IsSelected(rail);

                    if (selected && rail.mPoints.Count == 0 && ImGui.GetIO().KeyAlt && !ImGui.GetIO().KeyShift)
                    {
                        System.Numerics.Vector3 pos = viewport.ScreenToWorld(ImGui.GetMousePos());

                        pos.X = MathF.Round(pos.X * 2) / 2;
                        pos.Y = MathF.Round(pos.Y * 2) / 2;

                        Vector2 pos2D = viewport.WorldToScreen(pos);

                        viewport.mDrawList.AddCircleFilled(pos2D, pointSize, ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)));

                        continue;
                    }

                    if (rail.mPoints.Count == 0)
                        continue;

                    var rail_color = selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;

                    List<Vector2> pointsList = [];

                    var segmentCount = rail.mPoints.Count;
                    if (!rail.mIsClosed)
                        segmentCount--;

                    viewport.mDrawList.PathLineTo(viewport.WorldToScreen(rail.mPoints[0].mTranslation));
                    for (int i = 0; i < segmentCount; i++)
                    {
                        var pointA = rail.mPoints[i];
                        var pointB = rail.mPoints[(i + 1) % rail.mPoints.Count];

                        var posA2D = viewport.WorldToScreen(pointA.mTranslation);
                        var posB2D = viewport.WorldToScreen(pointB.mTranslation);

                        Vector2 cpOutA2D = posA2D;
                        Vector2 cpInB2D = posB2D;

                        if (pointA.mIsCurve)
                            cpOutA2D = viewport.WorldToScreen(pointA.mControl.mTranslation);

                        if (pointB.mIsCurve)
                            cpInB2D = viewport.WorldToScreen(pointB.mTranslation - (pointB.mControl.mTranslation - pointB.mTranslation));

                        if (cpOutA2D == posA2D && cpInB2D == posB2D)
                        {
                            viewport.mDrawList.PathLineTo(posB2D);
                            continue;
                        }

                        viewport.mDrawList.PathBezierCubicCurveTo(cpOutA2D, cpInB2D, posB2D);
                    }

                    float thickness = viewport.mHoveredObject == rail ? 4f : 3.5f;

                    viewport.mDrawList.PathStroke(rail_color, ImDrawFlags.None, thickness);
                    float closestDist = float.MaxValue;

                    Vector2 mouse = ImGui.GetMousePos();

                    closestSelected = null;

                    foreach (var pnt in rail.mPoints)
                    {
                        bool point_selected = mEditContext.IsSelected(pnt) || mEditContext.IsSelected(pnt.mControl);
                        if (!point_selected)
                            continue;

                        Vector2 pos2D = viewport.WorldToScreen(pnt.mTranslation);
                        float dist = Vector2.Distance(pos2D, mouse);

                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestSelected = pnt;
                        }
                    }

                    foreach (var pnt in rail.mPoints)
                    {
                        bool point_selected = mEditContext.IsSelected(pnt) || mEditContext.IsSelected(pnt.mControl);
                        var rail_point_color = point_selected ? ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) : color;
                        var size = 10.0f;

                        Vector2 pos2D = viewport.WorldToScreen(pnt.mTranslation);
                        viewport.mDrawList.AddCircleFilled(pos2D, size, rail_point_color);

                        if (viewport.mHoveredObject == pnt)
                            viewport.mDrawList.AddCircle(pos2D, 15.0f, rail_point_color, 10, 1.5f);

                        pointsList.Add(pos2D);

                        if (pnt == closestSelected && ImGui.GetIO().KeyAlt && !ImGui.IsMouseDragging(ImGuiMouseButton.Left) && !ImGui.GetIO().KeyShift && !mEditContext.IsAnySelected<CourseActor>())
                        {
                            System.Numerics.Vector3 previewPos = viewport.ScreenToWorld(mouse);

                            previewPos.X = MathF.Round(previewPos.X * 2) / 2;
                            previewPos.Y = MathF.Round(previewPos.Y * 2) / 2;
                            previewPos.Z = pnt.mTranslation.Z;

                            Vector2 preview2D = viewport.WorldToScreen(previewPos);

                            viewport.mDrawList.AddLine(pos2D, preview2D, rail_point_color, 2.5f);
                            viewport.mDrawList.AddCircleFilled(preview2D, size, rail_point_color);
                        }

                        if (point_selected && pnt.mIsCurve)
                        {
                            var contPos2D = viewport.WorldToScreen(pnt.mControl.mTranslation);
                            viewport.mDrawList.AddLine(pos2D, contPos2D, rail_point_color, thickness);
                            viewport.mDrawList.AddCircleFilled(contPos2D, size, rail_point_color);

                            if (viewport.mHoveredObject == pnt.mControl)
                                viewport.mDrawList.AddCircle(contPos2D, 15.0f, rail_point_color, 10, 1.5f);
                        }

                        if (viewport.mMultiSelecting)
                        {
                            float pntX = pnt.mTranslation.X;
                            float pntY = pnt.mTranslation.Y;

                            viewport.isInMultiSelectBox(new Vector2(pntX, pntY), pnt);
                        }
                    }

                }
                viewport.mDrawList.Flags |= ImDrawListFlags.AntiAliasedLines;
            }
        }

        internal void addRailPoint(int index, CourseRailPoint newPoint, CourseAreaEditContext mEditContext, LevelViewport viewport)
        {
            System.Numerics.Vector3 posVec = viewport.ScreenToWorld(ImGui.GetMousePos());
            float zCord = 0;
            if (index != -1)
            {
                zCord = newPoint.mTranslation.Z;
            }
            newPoint.mTranslation = new(
                MathF.Round(posVec.X * 2, MidpointRounding.AwayFromZero) / 2,
                MathF.Round(posVec.Y * 2, MidpointRounding.AwayFromZero) / 2,
                zCord);

            newPoint.mControl.mTranslation = newPoint.mTranslation + new System.Numerics.Vector3(0, 1, 0);


            if (mPoints.Count - 1 == index || index == -1)
                mEditContext.AddRailPoint(this, newPoint);
            else
            {
                (float distance, int index) min = (float.PositiveInfinity, -1);

                if (index != 0)
                {
                    for (int i = 0; i < mPoints.Count - 1; i++)
                    {
                        var pointA = mPoints[i].mTranslation;
                        var pointB = mPoints[i + 1].mTranslation;

                        var ab = pointB - pointA;
                        var length = ab.Length();
                        if (length < 0.0001f)
                            continue;

                        var dir = ab / length;

                        var t = System.Numerics.Vector3.Dot(posVec - pointA, dir) / length;
                        if (t < 0 || t > 1)
                            continue;

                        var normal = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(dir, System.Numerics.Vector3.UnitZ));
                        float distance = MathF.Abs(System.Numerics.Vector3.Dot(posVec - pointA, normal));

                        if (distance <= min.distance)
                            min = (distance, i + 1);

                    }
                }
                else
                    min.index = 0;

                mEditContext.InsertRailPoint(this, newPoint, min.index);
            }
            mEditContext.DeselectAll();
            mEditContext.Select(newPoint);
            viewport.mHoveredObject = newPoint;
        }

        public void RegenerateParameters()
        {
            this.mParameters = new Dictionary<string, object>();
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

        public class CourseRailPoint : Transformable
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
            public CourseRailPointControl mControl;
            public CourseRail mParent;
            public bool mIsCurve;
        }
        public class CourseRailPointControl : Transformable
        {
            public CourseRailPointControl(CourseRail.CourseRailPoint point, System.Numerics.Vector3 pos)
            {
                this.point = point;
                this.mTranslation = pos;
            }
            public CourseRail.CourseRailPoint point;
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
