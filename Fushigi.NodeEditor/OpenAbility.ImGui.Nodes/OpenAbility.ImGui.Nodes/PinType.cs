namespace OpenAbility.ImGui.Nodes;

public struct PinType
{

	public static PinType GetPinType(uint id)
	{
		return PinTypes[id];
	}

	private static readonly Dictionary<uint, PinType> PinTypes = new Dictionary<uint, PinType>();
	
	public readonly uint ID;
	public readonly string Name;
	public readonly uint Colour;

	public PinType(string name, byte r, byte g, byte b)
	{
		ID = NodeEditor.GetID();
		Name = name;
		Colour = ImUtil.ImCol32(r, g, b, 255);

		PinTypes[ID] = this;
	}
}
