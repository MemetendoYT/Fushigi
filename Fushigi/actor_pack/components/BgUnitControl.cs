using Fushigi.Byml.Serializer;

[Serializable]
public class BgUnitControl
{
    [BymlProperty("UnitType", DefaultValue = "")]
    public string UnitType { get; set; }
}
