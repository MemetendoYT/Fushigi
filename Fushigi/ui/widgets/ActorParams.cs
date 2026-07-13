using Fushigi.course;
using Fushigi.ui.widgets;
using ImGuiNET;

public class ActorParams
{
    //Rail Move Param
    public static readonly List<string> AccelType = new()
    {
        "None",
        "Weak",
        "Strong"
    };

    public static readonly Dictionary<string, string> CameraVibrationStrength = new Dictionary<string, string>()
    {
        { "Weak", "-1" },
        { "Normal", "0" },
        {"Strong", "1" },
        { "Very Strong", "2"}
    };


    public static readonly Dictionary<string, string> RailSpeedTypes = new Dictionary<string, string>()
    {
        { "Slowest", "-4" },
        { "Even Slower", "-3" },
        { "Slower", "-2" },
        { "Slow", "-1"},
        { "Normal", "0"},
        { "Medium Fast", "1"},
        { "Fast", "2"},
        { "Faster", "3"},
        { "Even Faster", "4"},
        { "Very Fast", "5"},
        { "Extremely Fast", "6"},
        { "Fastest", "7" }
    };


    // Request Wonder Item
    public static readonly Dictionary<string, string> WonderEffects = new Dictionary<string, string>()
    {
        { "None", "0" },
        { "Top-Down", "1" },
        { "Slide", "2" },
        { "Sky-Dive", "3" },
        { "Disable Badge", "4"},
        { "Goo", "5"},
        { "Invincible Lava", "6"},
        { "Unused: Lower Jumps / Slower Speed", "7"},
        { "Bowser Jr: Mega Mario Effect", "8"},
        { "Bowser Jr: Mega Mini Effect", "9"},
        { "Unused", "10"},
        { "Metal Mario", "11"},
        { "???", "12" }
    };

    public static readonly Dictionary<string, string> WonderMorphs = new Dictionary<string, string>()
    {
        { "None", "0" },
        { "Wubba", "1" },
        { "Spike-Ball", "2" },
        { "Balloon", "3" },
        { "Goomba", "4"},
        { "Puffy Lift", "5"},
        { "Hoppycat", "6"},
        { "Sproing/Stretch", "7"},
    };

    public static readonly Dictionary<string, string> ActorParamNames = new Dictionary<string, string>()
    {
        // Rail Move Param
        { "HasRailMovePreMoveAction", "Rail Pre-Move Action" },
        { "HasRailMoveArriveAction", "Rail Arrive Action" },
        { "IsEmitXLink", "Play Sound Effect" },
        { "RailSpeedType", "Rail Speed" },
        { "CameraVibrationStrength", "Camera Vibration Strength" },
        { "AccelLengthType", "Rail Acceleration" },
        { "DecelLengthType", "Rail Deceleration"},
        { "IsAccDecOnlyEdge", "Decelerate at Edge" },
        // RequestWonderItem
        {"IsPlayerWonderAll", "Wonder All" },
        {"IsUsePostWonder", "Post Wonder" },
        {"PlayerWonderType", "Wonder Effect" },
        {"MorphPlayerType", "Wonder Morph" }
    };

    public static readonly Dictionary<string, string> Tooltips = new Dictionary<string, string>()
    {
        // Rail Move Param
        { "HasRailMovePreMoveAction", "TBD" },
        { "HasRailMoveArriveAction", "TBD" },
        { "IsEmitXLink", "If available, actor will play a sound effect while moving." },
        { "RailSpeedType", "Sets the speed the actor moves along the rail. Will be ignored if the rail's section speed is not zero."},
        { "CameraVibrationStrength", "TBD" },
        { "AccelLengthType", "How quickly the actor accelerates from standing still." },
        { "DecelLengthType", "How quickly the actor decelerates to standing still."},
        { "IsAccDecOnlyEdge", "Acceleration/Deceleration only when the actor turns around. " +
                              "\nIf disabled, the actor will accelerate/decelerate on every rail point." },
        // Request Wonder Item
        {"IsPlayerWonderAll", "Wonder All" },
        {"IsUsePostWonder", "Post Wonder" },
        {"PlayerWonderType", "WOOOOOOOOOONDAAAAAAAAAAAAAAAAAAAAAAAH" },
        {"MorphPlayerType", "Wonder Morph" }
    };

    public static void DrawParamText(string ActorParam)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        string text = ActorParamNames[ActorParam];
        ImGui.Text(text);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Tooltips[ActorParam]);
    }


    public static void DrawParamBool(CourseActor actor, string ActorParam)
    {
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
        bool selected = (bool)actor.mActorParameters[ActorParam];

        if (ImGui.Checkbox($"##{ActorParam}", ref selected))
            actor.mActorParameters[ActorParam] = selected;
        
        ImGui.PopItemWidth();
    }
    internal static void DrawParam(List<string> list, CourseActor actor, string ActorParam)
    {
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
        int selected = list.IndexOf(actor.mActorParameters[ActorParam].ToString());

        if (ImGui.Combo($"##{ActorParam}", ref selected, list.ToArray(), list.Count))
            actor.mActorParameters[ActorParam] = list[selected];


        ImGui.PopItemWidth();
    }

    internal static void DrawParam(Dictionary<string, string> list, CourseActor actor, string ActorParam, CourseScene CourseScene = null)
    {
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
        int selected = list.Values.ToList().IndexOf(actor.mActorParameters[ActorParam].ToString());

        if (ImGui.Combo($"##{ActorParam}", ref selected, list.Keys.ToArray(), list.Count))
        {
            actor.mActorParameters[ActorParam] = list.Values.ToArray()[selected];

            if (ActorParam == "MorphPlayerType")
                CourseScene.course.mCourseInfo.CoursePlayerMorphType = CourseSettings.PlayerMorphTypeReverse[list.Keys.ToArray()[selected]];
        }
        ImGui.PopItemWidth();
    }
}
