namespace OpenAbility.ImGui.Nodes;

public struct PinType
{

	public static PinType GetPinType(int id)
	{
		return PinTypes[id];
	}

	private static readonly Dictionary<int, PinType> PinTypes = new Dictionary<int, PinType>();
	
	public readonly int ID;
	public readonly string Name;
	public readonly uint Colour;

	public PinType(string name, byte r, byte g, byte b)
	{
		Name = name;
		Colour = ImUtil.ImCol32(r, g, b, 255);

		PinTypes[ID] = this;
	}
}
