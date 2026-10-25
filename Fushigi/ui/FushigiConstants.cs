using System;
using System.Text.RegularExpressions;

public class FushigiConstants
{
    readonly string[] mViewMode = [
          "View All Actors",
            "View Normal Actors",
            "View Wonder Actors"];

    public static readonly string[] mLinkTypes = [
        "BasicSignal",
            "Create",
            "CreateRelativePos",
            "CreateAfterDied",
            "Delete",
            "Reference",
            "NextGoTo",
            "NextGoToParallel",
            "Bind",
            "Bind_NoRot",
            "Connection",
            "Follow",
            "PopUp",
            "Contents",
            "NoticeDeath",
            "Relocation",
            "ParamRefForChild",
            "CullingReference",
            "EventJoinMember",
            "EventGuest_04",
            "EventGuest_05",
            "EventGuest_06",
            "EventGuest_08",
            "EventGuest_09",
            "EventGuest_10",
            "EventGuest_11",
            "AreaDirCorres_Up",
            "AreaDirCorres_Down",
            "AreaDirCorres_Left",
            "AreaDirCorres_Right",
        ];

    public static readonly string[] LayerTypes = [
        "None",
            "DvScreen",
            "DvNear2",
            "DvNear1",
            "DecoAreaFront",
            "BgFront",
            "PlayArea",
            "DecoArea",
            "DvPlayArea",
            "DvMiddle1",
            "DvMiddle2",
            "DvFar1",
            "DvFar2",
            "DvFar3",
            "DvFar4",
            "DvFar5",
            "DvFar6",
            "DvFar7",
            "DvFar8",
            "DvFar9",
            "DvFar10"
    ];

    public static readonly string[] BackgroundLayerTypes = [
        "DvScreen",
            "DvNear2",
            "DvNear1",
            "BgFront",
            "DvPlayArea",
            "DvMiddle1",
            "DvMiddle2",
            "DvFar1",
            "DvFar2",
            "DvFar3",
            "DvFar4",
            "DvFar5",
            "DvFar6",
            "DvFar7",
            "DvFar8",
            "DvFar9",
            "DvFar10"
    ];

    public static readonly string[] RailTypes = [
        "Default",
            "NextGoTo",
            "BadgeChallenge",
            "CRing",
            "Scroll",
            "SectionFrame",
            "ShabonMove",
            "SwitchON",
            "SwitchOFF",
            "WorldMapRoute",
            "WorldMapRouteXlu"
    ];

    public static readonly Regex NumberRegex = new(@"\d+");

    public static List<string> layerSortTypes = [
         "DvScreen",
             "DvNear",
             "DecoAreaFront",
             "PlayArea",
             "DvPlayArea",
             "DecoArea",
             "DvMiddle",
             "DvFar"
    ];
}
