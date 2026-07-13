using Fushigi.Byml;
using Fushigi.Byml.Writer;
using Fushigi.param;
using Fushigi.ui;
using Fushigi.ui.widgets;
using Fushigi.util;
using ImGuiNET;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Threading.Channels;

namespace Fushigi.course
{
    public enum WonderViewType
    {
        Normal,
        WonderOff,
        WonderOnly
    }
    public enum CourseActorType
    {
        None,
        Tag,
        Area,
        Block,
        BgUnit,
        DV,
        Enemy,
        Event,
        Item,
        MapObj,
        Object,
        Sound,
        Unit,
        WMap,
        WObj,
        WorldMap,
        Camera,
        Cloud
    }

    public class CourseActor : Transformable
    {
        private static Vector2[] s_actorRectPolygon = new Vector2[4];
        public static List<string> HiddenActors = new();
        public CourseActor(BymlHashTable actorNode)
        {
            mPackName = BymlUtil.GetNodeData<string>(actorNode["Gyaml"]);
            mType = GetActorTypeFromGyaml(mPackName);
            mLayer = BymlUtil.GetNodeData<string>(actorNode["Layer"]);
            mTranslation = BymlUtil.GetVector3FromArray(actorNode["Translate"] as BymlArrayNode);
            mRotation = BymlUtil.GetVector3FromArray(actorNode["Rotate"] as BymlArrayNode);
            mScale = BymlUtil.GetVector3FromArray(actorNode["Scale"] as BymlArrayNode);
            mAreaHash = BymlUtil.GetNodeData<uint>(actorNode["AreaHash"]);
            mHash = BymlUtil.GetNodeData<ulong>(actorNode["Hash"]);
            mName = BymlUtil.GetNodeData<string>(actorNode["Name"]);
            mActorPack = ActorPackCache.Load(mPackName);
            if (mActorPack != null)
            {
                mActorChildRef = mActorPack.ChildActorParamName;

                mCalcDistanceParam = mActorPack.CalcDistanceParam;
            }
            if (actorNode.ContainsKey("Dynamic"))
            {
                if (ParamDB.HasActorComponents(mPackName))
                {
                    var actorParameters = new Dictionary<string, object>();

                    List<string> paramList = ParamDB.GetActorComponents(mPackName);

                    foreach (string p in paramList)
                    {
                        var components = ParamDB.GetComponentParams(p);
                        var dynamicNode = actorNode["Dynamic"] as BymlHashTable;

                        foreach (string component in components.Keys)
                        {
                            if (dynamicNode.ContainsKey(component) && !actorParameters.ContainsKey(component))
                            {
                                actorParameters.Add(component, BymlUtil.GetValueFromDynamicNode(dynamicNode[component], components[component]));
                            }
                            else
                            {
                                if (actorParameters.ContainsKey(component))
                                {
                                    continue;
                                }

                                var c = components[component];
                                actorParameters.Add(component, c.InitValue);
                            }
                        }
                    }

                    mActorParameters = new PropertyDict(actorParameters);
                }
            }
            else
            {
                InitializeDefaultDynamicParams();
            }

            if (actorNode.ContainsKey("System"))
            {
                var systemParameters = new List<PropertyDict.Entry>();

                foreach (string node in ((BymlHashTable)actorNode["System"]).Keys)
                {
                    BymlHashTable systemNode = ((BymlHashTable)actorNode["System"]);
                    var curNode = systemNode[node];
                    object? data = null;

                    switch (curNode.Id)
                    {
                        case BymlNodeId.Int:
                            data = BymlUtil.GetNodeData<int>(curNode);
                            break;
                        case BymlNodeId.Float:
                            data = BymlUtil.GetNodeData<float>(curNode);
                            break;
                        case BymlNodeId.Bool:
                            data = BymlUtil.GetNodeData<bool>(curNode);
                            break;
                        default:
                            throw new Exception("CourseActor::CourseActor() -- You are the chosen one. You have found a type we don't account for in the system params. WOAH!");
                    }

                    systemParameters.Add(new(node, data));
                }

                mSystemParameters = new PropertyDict(systemParameters);
            }
        }

        public CourseActor(string packName, uint areaHash, string actorLayer)
        {
            mPackName = packName;
            mType = GetActorTypeFromGyaml(packName);
            mAreaHash = areaHash;
            mLayer = actorLayer;
            mName = "";
            mTranslation = new System.Numerics.Vector3(0.0f);
            mRotation = new System.Numerics.Vector3(0.0f);
            mScale = new System.Numerics.Vector3(1.0f);
            mHash = RandomUtil.GetRandom();
            mActorPack = ActorPackCache.Load(mPackName);
            mActorChildRef = mActorPack.ChildActorParamName;
            mCalcDistanceParam = mActorPack.CalcDistanceParam;

            InitializeDefaultDynamicParams();
        }

        public void InitializeDefaultDynamicParams()
        {
            var actorParameters = new Dictionary<string, object>();

            if (ParamDB.HasActorComponents(mPackName))
            {
                List<string> paramList = ParamDB.GetActorComponents(mPackName);

                foreach (string p in paramList)
                {
                    var components = ParamDB.GetComponentParams(p);

                    foreach (string component in components.Keys)
                    {
                        if (actorParameters.ContainsKey(component))
                        {
                            continue;
                        }

                        var c = components[component];
                        actorParameters.Add(component, c.InitValue);
                    }
                }
            }

            mActorParameters = new PropertyDict(actorParameters);
        }
        internal static void DrawActorCollision(LevelViewport viewport, CourseAreaEditContext mEditContext, CourseArea mArea)
        {
            const float pointSize = 8.0f;
            foreach (CourseActor actor in mArea.GetActors())
            {
                if (HiddenActors.Contains(actor.mType.ToString()))
                    continue;

                System.Numerics.Vector3 min = new(-.5f);
                System.Numerics.Vector3 max = new(.5f);
                System.Numerics.Vector3 off = new(0f);
                System.Numerics.Vector3 center = new(0f);
                var drawing = "box";

                if (actor.mActorPack?.ShapeParams != null)
                {
                    var shapes = actor.mActorPack.ShapeParams;
                    var calc = shapes.mCalc;

                    if (((shapes.mSphere?.Count ?? 0) > 0) ||
                        ((shapes.mCapsule?.Count ?? 0) > 0))
                    {
                        drawing = "sphere";
                    }
                    else if ((shapes.mPoly?.Count ?? 0) > 0)
                    {
                        calc = shapes.mCalc;
                    }

                    if (calc != null)
                    {
                        center = calc.mCenter;
                        min = calc.mMin;
                        max = calc.mMax;
                    }

                    // Fix this so that always min < max to avoid negative length sides
                    if (min.X == max.X)
                    {
                        if (min.X == 0)
                        {
                            min.X = -0.5f;
                            max.X = 0.5f;
                        }
                        else
                        {
                            min.X = -Math.Abs(min.X);
                            max.X = Math.Abs(max.X);
                        }
                    }
                    if (min.Y == max.Y)
                    {
                        if (min.Y == 0)
                        {
                            min.Y = -0.5f;
                            max.Y = 0.5f;
                        }
                        else
                        {
                            min.Y = -Math.Abs(min.Y);
                            max.Y = Math.Abs(max.Y);
                        }
                    }
                    if (min.Z == max.Z)
                    {
                        if (min.Z == 0)
                        {
                            min.Z = -0.5f;
                            max.Z = 0.5f;
                        }
                        else
                        {
                            min.Z = -Math.Abs(min.Z);
                            max.Z = Math.Abs(max.Z);
                        }
                    }
                }

                string layer = actor.mLayer;

                if (viewport.mLayersVisibility!.TryGetValue(layer, out bool isVisible) && isVisible && actor.wonderVisible && !viewport.ScreenshotMode)
                {
                    Matrix4x4 transform =
                        Matrix4x4.CreateScale(actor.mScale.X, actor.mScale.Y, actor.mScale.Z
                        ) *
                        Matrix4x4.CreateRotationZ(
                            actor.mRotation.Z
                        ) *
                        Matrix4x4.CreateTranslation(
                            actor.mTranslation.X,
                            actor.mTranslation.Y,
                            actor.mTranslation.Z
                        ); ;

                    // Because fuck consistency I guess.
                    // (It's mostly cause usually AreaObjs use a distance calculation "NoModel_1x1x1", meaning a centered block.
                    //  While some use "SameActor" and "NoModel_1x1x1_Bottom" which place the origin at the bottom. I can't be bothered to
                    //  get this info and since it's only a handful of actors afaik, I'll just hardcode them here. If you got an issue with this,
                    //  feel free to change it.)
                    string[] halfOffsetCDP = {
                        "NoModel_1x1x1_Bottom",
                        "SameArea"
                    };

                    // Changed this cause it still wasn't correct
                    if (actor.mActorPack?.ShapeParams == null && halfOffsetCDP.Contains(actor.mCalcDistanceParam))
                        off = new(0, .5f, 0);

                    //topLeft
                    s_actorRectPolygon[0] = viewport.WorldToScreen(System.Numerics.Vector3.Transform(new System.Numerics.Vector3(min.X, max.Y, 0) + off, transform));
                    //topRight
                    s_actorRectPolygon[1] = viewport.WorldToScreen(System.Numerics.Vector3.Transform(new System.Numerics.Vector3(max.X, max.Y, 0) + off, transform));
                    //bottomRight
                    s_actorRectPolygon[2] = viewport.WorldToScreen(System.Numerics.Vector3.Transform(new System.Numerics.Vector3(max.X, min.Y, 0) + off, transform));
                    //bottomLeft
                    s_actorRectPolygon[3] = viewport.WorldToScreen(System.Numerics.Vector3.Transform(new System.Numerics.Vector3(min.X, min.Y, 0) + off, transform));

                    Vector2 topLeft = s_actorRectPolygon[0];
                    Vector2 bottomRight = s_actorRectPolygon[2];

                    Vector2 tl = s_actorRectPolygon[0];
                    Vector2 tr = s_actorRectPolygon[1];
                    Vector2 br = s_actorRectPolygon[2];
                    Vector2 bl = s_actorRectPolygon[3];

                    if (Sprites.OverrideSize.TryGetValue(actor.mPackName, out var size))
                    {
                        // Scale relative to center
                        Vector2 newCenter = (tl + br) * 0.5f;

                        tl = newCenter + (tl - newCenter) * size;
                        tr = newCenter + (tr - newCenter) * size;
                        br = newCenter + (br - newCenter) * size;
                        bl = newCenter + (bl - newCenter) * size;
                    }

                    if (UserSettings.GetUseSprites())
                    {
                        var key = actor.mPackName;
                        if (Sprites.SpriteAliases.TryGetValue(actor.mPackName, out var alias))
                            key = alias;

                        if (Sprites.ActorSprites.TryGetValue(key, out var tex))
                        {
                            viewport.mDrawList.AddImageQuad(
                                (IntPtr)tex.ID,
                                tl,
                                tr,
                                br,
                                bl,
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(1, 1),
                                new Vector2(0, 1),
                                0xFFFFFFFF
                            );
                        }
                    }

                    uint color = CourseActor.CourseActorColors[CourseActorType.None];
                    CourseActor.CourseActorColors.TryGetValue(actor.mType, out color);

                    bool isHovered = viewport.mHoveredObject == actor;

                    if (!viewport.ScreenshotMode)
                    {
                        switch (drawing)
                        {
                            default:
                                for (int i = 0; i < 4; i++)
                                {
                                    viewport.mDrawList.AddLine(
                                    s_actorRectPolygon[i],
                                    s_actorRectPolygon[(i + 1) % 4],
                                    color, isHovered ? 2.5f : 1.5f);
                                }
                                break;
                            case "sphere":
                                var pos = viewport.WorldToScreen(System.Numerics.Vector3.Transform(center, transform));
                                var scale = Matrix4x4.CreateScale(actor.mScale);
                                Vector2 rad = (viewport.WorldToScreen(System.Numerics.Vector3.Transform(max, scale)) - viewport.WorldToScreen(System.Numerics.Vector3.Transform(min, scale))) / 2;
                                viewport.mDrawList.AddEllipse(pos, Math.Abs(rad.X), Math.Abs(rad.Y), color, -actor.mRotation.Z, 0, isHovered ? 2.5f : 1.5f);

                                break;
                        }
                        if (mEditContext.IsSelected(actor))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                viewport.mDrawList.AddCircleFilled(s_actorRectPolygon[i],
                                    pointSize, color);
                                if (drawing == "sphere")
                                {
                                    viewport.mDrawList.AddLine(
                                    s_actorRectPolygon[i],
                                    s_actorRectPolygon[(i + 1) % 4],
                                    color, isHovered ? 2.5f : 1.5f);
                                }
                            }
                            viewport.mDrawList.AddEllipse(viewport.WorldToScreen(transform.Translation), pointSize * 3, pointSize * 3, color, -actor.mRotation.Z, 4, 2);
                        }
                    }

                    string name = actor.mPackName;
                    isHovered = MathUtil.HitTestConvexPolygonPoint(s_actorRectPolygon, ImGui.GetMousePos());

                    if (name.Contains("Area"))
                    {
                        isHovered = MathUtil.HitTestLineLoopPoint(s_actorRectPolygon, 4f,
                            ImGui.GetMousePos());
                    }

                    if (isHovered)
                    {
                        viewport.mHoveredObject = actor;
                    }

                    if (viewport.mMultiSelecting)
                    {
                        float actorX = actor.mTranslation.X;
                        float actorY = actor.mTranslation.Y;

                        viewport.isInMultiSelectBox(new Vector2(actorX, actorY), actor);
                    }

                }
            }
        }
        public BymlHashTable BuildNode(CourseLinkHolder linkHolder)
        {
            BymlHashTable table = new();
            table.AddNode(BymlNodeId.UInt, BymlUtil.CreateNode<uint>(mAreaHash), "AreaHash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mPackName), "Gyaml");
            table.AddNode(BymlNodeId.UInt64, BymlUtil.CreateNode<ulong>(mHash), "Hash");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mLayer), "Layer");
            table.AddNode(BymlNodeId.String, BymlUtil.CreateNode<string>(mName), "Name");

            if (mActorParameters.Count > 0)
            {
                BymlHashTable dynamicNode = new();

                foreach (PropertyDict.Entry dynParam in mActorParameters)
                {
                    object param = mActorParameters[dynParam.Key];

                    var valueNode = BymlUtil.CreateNode(param);
                    dynamicNode.AddNode(valueNode.Id, valueNode, dynParam.Key);
                }

                table.AddNode(BymlNodeId.Hash, dynamicNode, "Dynamic");
            }

            if (mSystemParameters.Count > 0)
            {
                BymlHashTable sysNode = new();

                foreach (KeyValuePair<string, object> sysParam in mSystemParameters)
                {
                    object param = mSystemParameters[sysParam.Key];

                    var valueNode = BymlUtil.CreateNode(param);
                    sysNode.AddNode(valueNode.Id, valueNode, sysParam.Key);
                }

                table.AddNode(BymlNodeId.Hash, sysNode, "System");
            }

            if (linkHolder.GetSrcHashesFromDest(mHash).Values.Count > 0)
            {
                BymlHashTable inLinksNode = new();

                foreach (var (linkName, links) in linkHolder.GetSrcHashesFromDest(mHash))
                {
                    inLinksNode.AddNode(BymlNodeId.Int, BymlUtil.CreateNode<int>(links.Count), linkName);
                }

                table.AddNode(BymlNodeId.Hash, inLinksNode, "InLinks");
            }

            BymlArrayNode rotateNode = new(3);
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mRotation.X));
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mRotation.Y));
            rotateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mRotation.Z));

            table.AddNode(BymlNodeId.Array, rotateNode, "Rotate");

            BymlArrayNode scaleNode = new(3);
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>(mScale.X));
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>(mScale.Y));
            scaleNode.AddNodeToArray(BymlUtil.CreateNode<float>(mScale.Z));

            table.AddNode(BymlNodeId.Array, scaleNode, "Scale");

            BymlArrayNode translateNode = new(3);
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.X));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.Y));
            translateNode.AddNodeToArray(BymlUtil.CreateNode<float>(mTranslation.Z));

            table.AddNode(BymlNodeId.Array, translateNode, "Translate");

            return table;
        }

        public static CourseActorType GetActorTypeFromGyaml(string gyaml)
        {
            gyaml = gyaml.ToLower();
            if (gyaml.StartsWith("edv"))
                return CourseActorType.DV;
            if (gyaml.StartsWith("richdv"))
                return CourseActorType.DV;
            if (gyaml.StartsWith("camera"))
                return CourseActorType.Camera;
            if (gyaml.StartsWith("cloud"))
                return CourseActorType.Cloud;
            if (gyaml.EndsWith("tag"))
                return CourseActorType.Tag;
            if (gyaml.Contains("area"))
                return CourseActorType.Area;
            if (gyaml.Contains("block"))
                return CourseActorType.Block;
            if (gyaml.StartsWith("bgunit"))
                return CourseActorType.BgUnit;
            if (gyaml.StartsWith("dv"))
                return CourseActorType.DV;
            if (gyaml.StartsWith("enemy"))
                return CourseActorType.Enemy;
            if (gyaml.StartsWith("event"))
                return CourseActorType.Event;
            if (gyaml.StartsWith("item"))
                return CourseActorType.Item;
            if (gyaml.StartsWith("mapobj"))
                return CourseActorType.MapObj;
            if (gyaml.StartsWith("object"))
                return CourseActorType.Object;
            if (gyaml.StartsWith("sound"))
                return CourseActorType.Sound;
            if (gyaml.StartsWith("unit"))
                return CourseActorType.Unit;
            if (gyaml.StartsWith("wmap"))
                return CourseActorType.WMap;
            if (gyaml.StartsWith("wobj"))
                return CourseActorType.WObj;
            if (gyaml.StartsWith("worldmap"))
                return CourseActorType.WorldMap;

            return CourseActorType.None;
        }

        public CourseActor Clone(CourseArea areaTo)
        {
            CourseActor cloned = new(mPackName, mAreaHash, mLayer)
            {
                mPackName = mPackName,
                mName = mName,
                mLayer = mLayer,
                mWonderView = mWonderView,
                wonderVisible = wonderVisible,
                mStartingTrans = mStartingTrans,
                mTranslation = mTranslation,
                mScale = mScale,
                mRotation = mRotation
            };
            cloned.mStartingTrans = mStartingTrans;
            cloned.mStartingRot = mStartingRot;
            cloned.mAreaHash = areaTo.mRootHash;
            cloned.mHash = (ulong)(new Random().NextDouble() * ulong.MaxValue);
            cloned.mActorParameters = mActorParameters.Clone();
            cloned.mSystemParameters = mSystemParameters.Clone();
            cloned.mActorPack = mActorPack;

            return cloned;
        }

        public CourseActor ClonePrefab(CourseArea areaTo)
        {
            CourseActor cloned = new(mPackName, mAreaHash, mLayer)
            {
                mPackName = mPackName,
                mName = mName,
                mLayer = mLayer,
                mWonderView = mWonderView,
                mHash = mHash,
                wonderVisible = wonderVisible,
                mStartingTrans = mStartingTrans,
                mTranslation = mTranslation,
                mScale = mScale,
                mRotation = mRotation
            };
            cloned.mStartingTrans = mStartingTrans;
            cloned.mStartingRot = mStartingRot;
            cloned.mAreaHash = areaTo.mRootHash;
            cloned.mHash = mHash;
            cloned.mActorParameters = mActorParameters.Clone();
            cloned.mSystemParameters = mSystemParameters.Clone();
            cloned.mActorPack = mActorPack;

            return cloned;
        }
        public string mPackName;
        public string mName;
        public string mLayer;
        public WonderViewType mWonderView = WonderViewType.Normal;
        public CourseActorType mType = CourseActorType.None;
        public bool wonderVisible = true;
        public System.Numerics.Vector3 mStartingRot;
        public System.Numerics.Vector3 mRotation;
        public System.Numerics.Vector3 mScale;
        public uint mAreaHash;
        public ulong mHash;
        public PropertyDict mActorParameters = PropertyDict.Empty;
        public PropertyDict mSystemParameters = PropertyDict.Empty;
        public string mCalcDistanceParam;

        public ActorPack mActorPack;

        public string mActorChildRef;

        public static readonly Dictionary<CourseActorType, uint> CourseActorColors = new()
        {
            { CourseActorType.None, ImGui.ColorConvertFloat4ToU32(new(0.125f, 0.988f, 0.561f, 1)) },
            { CourseActorType.Tag, ImGui.ColorConvertFloat4ToU32(new(1, 0.55f, 0, 1)) },
            { CourseActorType.Area, ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, 1)) },
            { CourseActorType.Block, ImGui.ColorConvertFloat4ToU32(new(0.125f, 0.976f, 0.988f, 1)) },
            { CourseActorType.BgUnit, ImGui.ColorConvertFloat4ToU32(new(0.84f, .437f, .437f, 1)) },
            { CourseActorType.Enemy, ImGui.ColorConvertFloat4ToU32(new(0.5f, 1, 0, 1)) },
            { CourseActorType.Event, ImGui.ColorConvertFloat4ToU32(new(1, 0.7f, 0, 1)) },
            { CourseActorType.DV, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0.7f, 1)) },
            { CourseActorType.Item, ImGui.ColorConvertFloat4ToU32(new(0.988f, 0.125f, 0.941f, 1)) },
            { CourseActorType.MapObj, ImGui.ColorConvertFloat4ToU32(new(0.85f, 0.63f, 0.43f, 1)) },
            { CourseActorType.WObj, ImGui.ColorConvertFloat4ToU32(new(0.85f, 0.63f, 0.43f, 1)) },
            { CourseActorType.WMap, ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) },
            { CourseActorType.WorldMap, ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) },
            { CourseActorType.Object, ImGui.ColorConvertFloat4ToU32(new(0.125f, 0.976f, 0.988f, 1)) },
            { CourseActorType.Sound, ImGui.ColorConvertFloat4ToU32(new(0.36f, 0.25f, 0.83f, 1)) },
            { CourseActorType.Camera, ImGui.ColorConvertFloat4ToU32(new(1, 1, 0, 1)) },
            { CourseActorType.Cloud, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0.7f, 1)) },
        };

        public static uint ColorShift(uint color)
        {
            Vector4 vecCol = ImGui.ColorConvertU32ToFloat4(color);
            vecCol.X = EditColorChannel(vecCol.X);
            vecCol.Y = EditColorChannel(vecCol.Y);
            vecCol.Z = EditColorChannel(vecCol.Z);
            return ImGui.ColorConvertFloat4ToU32(vecCol);
        }

        public static float EditColorChannel(float channel)
        {
            return (float)(channel + (1 - channel) * 0.6);
        }
    }

    public class CourseActorHolder
    {
        public CourseActorHolder()
        {

        }

        public CourseActorHolder(BymlArrayNode actorArray)
        {
            foreach (BymlHashTable actor in actorArray.Array)
                mActors.Add(new CourseActor(actor));
        }

        public bool TryGetActor(ulong hash, [NotNullWhen(true)] out CourseActor? actor)
        {
            actor = mActors.Find(x => x.mHash == hash);
            return actor is not null;
        }

        public CourseActor this[ulong hash]
        {
            get
            {
                bool exists = TryGetActor(hash, out CourseActor? actor);
                //Debug.Assert(exists);
                return actor!;
            }
        }

        public BymlArrayNode SerializeToArray(CourseLinkHolder linkHolder)
        {
            BymlArrayNode node = new((uint)mActors.Count);

            foreach (CourseActor actor in mActors)
            {
                node.AddNodeToArray(actor.BuildNode(linkHolder));
            }

            return node;
        }

        public BymlArrayNode SerializePrefab(List<CourseActor> actors, CourseLinkHolder linkHolder)
        {
            BymlArrayNode node = new((uint)actors.Count);

            foreach (CourseActor actor in actors)
            {
                node.AddNodeToArray(actor.BuildNode(linkHolder));
            }

            return node;
        }


        public List<CourseActor> mActors = [];
        public List<CourseActor> mSortedActors = [];
    }

   
    public class CourseActorRender //This can be overridden per actor for individual behavior
    {

        public virtual void Render()
        {
        }
    }
}
