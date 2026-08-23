using Fushigi.course;
using Fushigi.param;
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

    public static readonly Dictionary<string, string> ChildParameters = new Dictionary<string, string>()
    {
        { "None", "None" },
        { "ItemMushroom_All", "Super Mushroom" },
        { "ItemDrillSuit_All", "Drill Suit" },
        { "ItemElephantSuit_All", "Elephant Suit" },
        { "ItemMushroomOrAwaFlower_All", "Mushroom/Bubble Flower" },
        { "ItemMushroomOrCoinYellow_All", "Mushroom/Coin" },
        { "ItemMushroomOrDrillSuit_All", "Mushroom/Drill Suit" },
        { "ItemMushroomOrElephantSuit_All", "Mushroom/Elephant Suit" },
        { "EnemyIronBall_Single", "Iron Ball" },
        { "ItemContinueStar_All", "Star Continue" },
        { "ItemStar_All", "Star" },
        { "ObjectCoinYellow_Single", "1 Coin" },
        { "ObjectPropellerFlower", "Propeller Flower" },
        { "ObjectScatterRandomCoin_Five", "5 Purple Coins" }
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

    public static readonly Dictionary<string, string> SwitchHitType = new Dictionary<string, string>()
    {
        { "Only Players", "0" },
        { "All referenced actors needed", "1" },
        { "Any referenced actor", "2" },
        { "Anything", "3"},
        { "Only crown player", "4"}
    };

    public static readonly Dictionary<string, string> SwitchOnOffType = new Dictionary<string, string>()
    {
        { "Once", "0" },
        { "While Conditions Are Satisfied", "1" },
        { "Unknown", "2" }
    };

    public static readonly Dictionary<string, string> ActorSituation = new Dictionary<string, string>()
    {
        { "Always", "0" },
        { "Only when actor/player is on ground", "1" },
        { "Unknown", "2" }
    };

    public static readonly Dictionary<string, string> InitDir = new Dictionary<string, string>()
    {
        { "Right", "0" },
        { "Left", "1" },
        { "Up", "2" },
        { "Down", "3" },
        { "Towards Player", "4" },
    };

    public static readonly Dictionary<string, string> EdgeTypes = new Dictionary<string, string>()
    {
        { "Reverse", "0" },
        { "Open End", "1" },
        { "Warp to first point", "2" },
        { "Stop at end", "3" }
    };

    public static readonly Dictionary<string, string> WonderVisibility = new Dictionary<string, string>()
    {
        { "Exists at All times", "0" },
        { "Only exists in Wonder Effects", "1" },
    };

    public static readonly Dictionary<string, string> MovableDir = new Dictionary<string, string>()
    {
        { "Towards Point 0", "-1" },
        { "Any", "0" },
        { "Away from Point 0", "1" }
    };

    public static readonly Dictionary<string, object> MapParams = new()
    {
        // Rail move Param
        { "RailSpeedType", RailSpeedTypes },
        { "AccelLengthType", AccelType },
        { "DecelLengthType", AccelType },

        // Request Wonder Item 
        { "PlayerWonderType", WonderEffects },
        { "MorphPlayerType", WonderMorphs },

        // AreaTargetTypeSelect 
        { "SwitchHitType", SwitchHitType },
        { "SwitchOnOffType", SwitchOnOffType },
        { "ActorSituation", ActorSituation },

        // TurnCompnentCommon
        { "InitDir", InitDir },

        // Default Rail
        { "StartEdgeType", EdgeTypes },
        { "EndEdgeType", EdgeTypes },
        { "WonderVisibilityType", WonderVisibility },
        { "MovableDir", MovableDir },

        // ChildActorSelectName
        { "ChildActorSelectName", ChildParameters }
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
        { "IsPlayerWonderAll", "Wonder All" },
        { "IsUsePostWonder", "Post Wonder" },
        { "PlayerWonderType", "Wonder Effect" },
        { "MorphPlayerType", "Wonder Morph" },

        // AreaTargetTypeSelect 
        { "IsAllLocalPlayer",  "Is Local Player" },
        { "IsReferenceLinkName", "Reference Link Name" },
        { "SwitchHitType", "Switch Hit Type" },
        { "SwitchOnOffType", "Switch On Off Type" },
        { "ActorSituation", "Actor Situation" },

        // TurnCompnentCommon
        { "InitDir", "Initial Direction" },

        // Default Rail
        { "StartEdgeType", "Start Behavior" },
        { "IsEnableRailChain", "Rail Chain" },
        { "EndEdgeType", "End Behavior" },
        { "IsEntity", "Is Visible" },
        { "WonderVisibilityType", "Wonder Visibility" },
        { "MovableDir", "Direction" },

        // ChildActorSelectName
        { "ChildActorSelectName", "Contents" }
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
        {"MorphPlayerType", "Wonder Morph" },

        // AreaTargetTypeSelect 
        { "IsAllLocalPlayer",  "All players needed to activate?" },
        { "IsReferenceLinkName", "Allows all actors that are the same type as the " +
                                 "\nreferenced actors to trigger the area." },
        { "SwitchHitType", "Sets what triggers the area." },
        { "SwitchOnOffType", "Sets the behaviour of the area." },
        { "ActorSituation", "What the actor has to do to trigger the area." },

        // Default Rail 
        { "IsEnableRailChain", "Allows actors to fall onto rail from a different rail" },
        { "StartEdgeType", "Actor behaviour when they reach the start of the rail." },
        { "EndEdgeType", "Actor behaviour when they reach the end of the rail." },
        { "IsEntity", "If set to true, the rail will be visible for the player." +
                      "\nThe look will depend on the rail type" },
        { "WonderVisibilityType", "If the rail exists during wonder effects." },
        { "MovableDir", "What direction actors can move along the rail" }
    };

    public static void DrawParamText(string ActorParam)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        if (ActorParamNames.TryGetValue(ActorParam, out var name))
            ImGui.Text(name);
        else
            ImGui.Text(ActorParam);

        if (ImGui.IsItemHovered())
        {
            if (Tooltips.TryGetValue(ActorParam, out var tooltip))
                ImGui.SetTooltip(tooltip);
            else
                ImGui.SetTooltip("No tooltip available.");

        }
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
    }
    public static void DrawFloat(CourseActor actor, string ActorParam)
    {
        float selected = (float)actor.mActorParameters[ActorParam];
        if (ImGui.InputFloat($"##{ActorParam}", ref selected))
            actor.mActorParameters[ActorParam] = selected;
        ImGui.PopItemWidth();
    }
    public static void DrawInt(CourseActor actor, string ActorParam)
    {
        int selected = (int)actor.mActorParameters[ActorParam];
        if (ImGui.InputInt($"##{ActorParam}", ref selected))
            actor.mActorParameters[ActorParam] = selected;
        ImGui.PopItemWidth();
    }

    public static void DrawIntRail(CourseRail rail, string RailParam)
    {
        int selected = (int)rail.mParameters[RailParam];
        if (ImGui.InputInt($"##{RailParam}", ref selected))
            rail.mParameters[RailParam] = selected;
        ImGui.PopItemWidth();
    }
    public static void DrawParamBool(CourseActor actor, string ActorParam)
    {
        bool selected = (bool)actor.mActorParameters[ActorParam];

        if (ImGui.Checkbox($"##{ActorParam}", ref selected))
            actor.mActorParameters[ActorParam] = selected;

        ImGui.PopItemWidth();
    }

    public static void DrawString(CourseActor actor, string ActorParam)
    {
        string selected = (string)actor.mActorParameters[ActorParam];

        if (ImGui.InputText($"##{ActorParam}", ref selected, 0x100))
            actor.mActorParameters[ActorParam] = selected;

        ImGui.PopItemWidth();
    }

    internal static void DrawParam(List<string> list, CourseActor actor, string ActorParam)
    {
        object original = actor.mActorParameters[ActorParam];
        Type originalType = original.GetType();

        int selected = list.IndexOf(original.ToString());
        if (ImGui.Combo($"##{ActorParam}", ref selected, list.ToArray(), list.Count))
            actor.mActorParameters[ActorParam] = Convert.ChangeType(list[selected], originalType);

        ImGui.PopItemWidth();
    }

    internal static void DrawRailParam(Dictionary<string, string> list, CourseRail rail, string RailParam)
    {
        object original = rail.mParameters[RailParam];
        Type originalType = original.GetType();

        int selected = list.Values.ToList().IndexOf(original.ToString());
        if (ImGui.Combo($"##{RailParam}", ref selected, list.Keys.ToArray(), list.Count))
            rail.mParameters[RailParam] = Convert.ChangeType(list.Values.ToArray()[selected], originalType);

        ImGui.PopItemWidth();
    }
    internal static void DrawParam(Dictionary<string, string> list, CourseActor actor, string ActorParam, CourseScene CourseScene)
    {
        object original = actor.mActorParameters[ActorParam];
        Type originalType = original.GetType();

        int selected = list.Values.ToList().IndexOf(original.ToString());
        if (ImGui.Combo($"##{ActorParam}", ref selected, list.Keys.ToArray(), list.Count))
        {
            actor.mActorParameters[ActorParam] = Convert.ChangeType(list.Values.ToArray()[selected], originalType);
            if (ActorParam == "MorphPlayerType")
                CourseScene.course.mCourseInfo.CoursePlayerMorphType = CourseSettings.PlayerMorphTypeReverse[list.Keys.ToArray()[selected]];
        }
        ImGui.PopItemWidth();
    }

    internal static void DrawChildParameters(CourseActor actor, string param)
    {
        try
        {
            string id = $"##{param}";
            List<string> list = ChildActorParam.GetActorParams(actor.mActorChildRef);
            int selected = list.IndexOf(actor.mActorParameters[param].ToString());

            List<string> translated = new List<string>();
            foreach (var child in list)
            {
                if (ChildParameters.TryGetValue(child, out var translatedName))
                {
                    translated.Add(translatedName);
                }
                else
                {
                    translated.Add(child);
                }
            }

            ImGui.Text("ChildParameters");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);
            if (ImGui.Combo("##Parameters", ref selected, translated.ToArray(), translated.Count))
            {
                actor.mActorParameters[param] = list[selected];
            }
            ImGui.PopItemWidth();
        }
        catch (Exception ex) 
        {
                Console.WriteLine(ex.Message);
            string id = $"##{param}";

            ImGui.AlignTextToFramePadding();
            ImGui.Text(param);
            ImGui.TableNextColumn();

            ImGui.PushItemWidth(ImGui.GetColumnWidth() - ImGui.GetStyle().ScrollbarSize);

            string val_string = actor.mActorParameters[param].ToString();
            if (ImGui.InputText(id, ref val_string, 1024))
            {
                actor.mActorParameters[param] = val_string;
            }
        }
    }
    internal static void DrawRailParams(CourseRail mSelectedRail)
    {
        foreach (KeyValuePair<string, object> param in mSelectedRail.mParameters)
        {

            string type = param.Value.GetType().ToString();
            DrawParamText(param.Key);

            if (MapParams.TryGetValue(param.Key, out var options))
            {
                switch (options)
                {
                    case Dictionary<string, string> dictOptions:
                        DrawRailParam(dictOptions, mSelectedRail, param.Key);
                        break;
                        //case List<string> listOptions:
                        //    DrawParam(listOptions, actor, paramName);
                        //    break;
                }
            }
            else
            {
                switch (type)
                {
                    case "System.Int32":
                        int int_val = (int)param.Value;
                        if (ImGui.InputInt($"##{param.Key}", ref int_val))
                        {
                            mSelectedRail.mParameters[param.Key] = int_val;
                        }
                        break;
                    case "System.Boolean":
                        bool bool_val = (bool)param.Value;
                        if (ImGui.Checkbox($"##{param.Key}", ref bool_val))
                        {
                            mSelectedRail.mParameters[param.Key] = bool_val;
                        }
                        break;
                }
            }
            ImGui.TableNextColumn();

        }
    }
    internal static void DrawActorParams(CourseActor actor, string param, CourseScene CourseScene = null)
    {
        if (param == "ChildActorSelectName" && actor.mActorChildRef != null)
            DrawChildParameters(actor, param);
        else
        {
            foreach (KeyValuePair<string, ParamDB.ComponentParam> pair in ParamDB.GetComponentParams(param))
            {
                string paramName = pair.Key;

                if (!actor.mActorParameters.TryGetValue(paramName, out var value))
                    continue;

                DrawParamText(paramName);

                if (MapParams.TryGetValue(paramName, out var options))
                {
                    switch (options)
                    {
                        case Dictionary<string, string> dictOptions:
                            DrawParam(dictOptions, actor, paramName, CourseScene);
                            break;
                        case List<string> listOptions:
                            DrawParam(listOptions, actor, paramName);
                            break;
                    }
                }
                else if (value is bool)
                {
                    DrawParamBool(actor, paramName);
                }
                else if (value is int)
                {
                    DrawInt(actor, paramName);
                }
                else if (value is float)
                {
                    DrawFloat(actor, paramName);
                }
                else if (value is string)
                {
                    DrawString(actor, paramName);
                }
            }
        }
    }
}
