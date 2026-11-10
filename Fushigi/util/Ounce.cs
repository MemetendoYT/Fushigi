using System;

public class Ounce
{
	public static bool isOunce = false;

    // Switch 2 Edition: Tilesets
    public static readonly Dictionary<uint, string> SkinNames = new()
    {
        { 4136595072, "CastleFactory" },
        { 4021319721, "CastleSiro" },
        { 2302139999, "CommonSenkan" },
        { 3279395142, "CommonSora" },
        { 3176762065, "CommonSuityuAsase" },
        { 189387638,  "CommonToride" },
        { 125838187,  "CommonTorideB" },
        { 3994172016, "CommonYashiki" },
        { 263342392,  "HajimariChika" },
        { 720228302,  "HajimariKaigan" },
        { 3534951549, "HajimariMori" },
        { 2735387052, "HajimariSougen" },
        { 2067926728, "KinMori" },
        { 3676605899, "KinMoriAkarui" },
        { 276828319,  "KinSlime" },
        { 2311223552, "MiniCourseA" },
        { 483026818,  "MushiChika" },
        { 1471966022, "MushiMachi" },
        { 2778936643, "NettaiResort" },
        { 350820293,  "NettaiResortDark" },
        { 1912335456, "NettaiYogan" },
        { 1532148658, "SabakuChijou" },
        { 831109002,  "SabakuChika" },
        { 2163076660, "SabakuIseki" },
        { 1686814971, "Savanna" },
        { 3559001279, "SavannaChitei" },
        { 3922647108, "SavannaSabaku" },
        { 3333229237, "Undefined" },
        { 2753907545, "WaKaigan" },
        { 3064819885, "YamaDokutu" },
        { 3023323263, "YamaJyoku" },
        { 214294947,  "YamaTijyo" },
        { 2442115663, "NewWorldChika" }
    };

    // Switch 2 Edition: actor pack fields Fushigi reads
    public static readonly Dictionary<uint, string> ActorNames = new()
    {
        // ModelExpandParam: CalcType
        { 768430711,  "ActorScale" },
        { 2123540343, "ActorScaleDiv2" },
        { 1886508163, "ActorScaleDiv4" },
        { 3750794661, "ActorScaleMinus1" },
        { 3445933589, "ActorScaleMinus2" },
        { 2877733055, "ZeroWhenActorScaleOne" },
        { 3069107721, "None" },

        // ModelExpandParam: ScalingType
        { 2382293923, "XAxisOnly" },
        { 3805439596, "YAxisOnly" },
        { 210441115,  "XYAxis" },

        // BgUnitInfo: UnitType
        { 1091859522, "Bridge" },
        { 211986545,  "HalfHit" },
        { 1945809998, "NoHit" },

        // BgUnitInfo: AttrList[].DrawAttr
        { 2155127678, "Belt" },
        { 470423374,  "Edge" },
        { 741904279,  "Wall" },

    };

    // Switch 2 Edition: CourseInfo
    public static readonly Dictionary<uint, string> CourseInfoNames = new()
{
    // CourseDifficulty
    { 4286663369, "Easy" }, { 957022509, "Normal" }, { 1783839311, "Hard" },
    { 1665742699, "VeryHard" }, { 3193779625, "ExtraHard" }, { 3069107721, "None" },

    // CourseKind
    { 1735804353, "Arena" }, { 1650454231, "BadgeChallenge" }, { 3658543906, "BadgeHouse" },
    { 3558135859, "BadgeMedley" }, { 4204245454, "Bonus" }, { 2574306800, "DemoCourse" },
    { 1925705911, "GeneralFacility" }, { 213915958, "Mini" }, { 776425516, "NormalSpWorld" },
    { 3867376820, "Opening" }, { 3601096682, "PlayableFacility" }, { 1237557158, "Race" },
    { 617457906, "SecretSquare" }, { 2106881388, "StaffCredit" }, { 3691091410, "StoryTeller" },
    { 405867837, "WonderPalace" },

    // CoursePlayerMorphType
    { 1960045545, "Ball" }, { 4027419620, "BalloonKiller" }, { 3112181344, "Biyon" },
    { 2303770865, "Hoppin" }, { 563066243, "Kuribo" }, { 2635266350, "SinkBlock" },
    { 3657896309, "Slime" },

    // CourseTimerType / misc
    { 734798978, "ArenaTimer" }, { 2117934662, "Invalid" },

    // RaceCourseType
    { 2477028039, "Race1" }, { 700459112, "Race2" }, { 4024541989, "Race3" },

    // TipsInfo Cond
    { 2347219829, "ExperienceBubble" }, { 2497684700, "ExperienceElp" },

    // Badges
    { 1808541051, "BadgeId00" }, { 3925309594, "BadgeId04" }, { 1655071909, "BadgeId14" },
    { 556060222, "BadgeId19" }, { 711473776, "BadgeId29" }, { 3826981562, "BadgeId34" },
    { 2833801300, "BadgeId35" }, { 1419143334, "BadgeId38" }, { 2294382717, "BadgeId39" },
    { 2306498944, "BadgeId47" }, { 628482444, "BadgeId53" },

    // Tips tags
    { 569784139, "Tag01" }, { 4053486080, "Tag02" }, { 1642576892, "Tag03" },
    { 1154127870, "Tag04" }, { 4057426759, "Tag05" }, { 145134545, "Tag06" },
    { 2187765004, "Tag07" }, { 3885784309, "Tag08" }, { 983418195, "Tag09" },
    { 2892917434, "Tag10" }, { 3456495243, "Tag11" }, { 1136095803, "Tag12" },
    { 2022883363, "Tag13" }, { 1559380794, "Tag14" }, { 3150801712, "Tag16" },
    { 1777348448, "Tag17" }, { 2087287855, "Tag18" }, { 2724814330, "Tag19" },
    { 3557381960, "Tag21" }, { 698605751, "Tag22" }, { 2517144106, "Tag23" },
    { 1572903417, "Tag25" }, { 292061392, "Tag26" }, { 1613351450, "Tag27" },
    { 167961008, "Tag28" }, { 3546860970, "Tag29" }, { 1904662407, "Tag30" },
    { 1677811632, "Tag48" }, { 3391556828, "Tag49" }, { 2659696905, "Tag50" },
    { 245302376, "Tag51" }, { 1853485040, "Tag52" }, { 397242324, "Tag53" },
    { 4048810149, "Tag54" }, { 3358858758, "Tag55" }, { 1743144153, "Tag57" },
};
    public static string Convert(object value, Dictionary<uint, string> table)
    {
        if (value is uint id && table.TryGetValue(id, out var name))
            return name;
        
        return value?.ToString() ?? "";
    }
}
