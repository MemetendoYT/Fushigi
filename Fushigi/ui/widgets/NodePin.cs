using System.Numerics;

namespace OpenAbility.ImGui.Nodes;

public class NodePin
{
	public readonly int ID;
	private Dictionary<int, ConnectionTarget> connections = new Dictionary<int, ConnectionTarget>();

	public readonly int Node;
	public readonly int PinTypeID;

	public readonly PinMode PinMode;
	public static float Radius = 4.0f;
	public static float RadiusSelectRatio = 2f;
    private static int nextPinID = 1;
    public static float SelectRadius
	{
		get
		{
			return Radius * RadiusSelectRatio;
		}
	}

	public PinType PinType
	{
		get
		{
			return PinType.GetPinType(PinTypeID);
		}
	}


	internal NodePin(Node node, int pinTypeID, PinMode mode)
	{
        this.ID = nextPinID++;
        this.Node = node.ID;
		this.PinTypeID = pinTypeID;
		PinMode = mode;
	}

	public bool Connected(NodePin pin)
	{
		if (pin.ID == ID || pin.Node == Node || pin.PinMode == PinMode)
			return false;
		if (PinMode == PinMode.Input)
			return pin.Connected(this);
		return connections.Any(c => c.Value.TargetPin == pin.ID);
	}

	public void Disconnect(NodePin pin)
	{
		if (pin.ID == ID || pin.Node == Node || pin.PinMode == PinMode)
			return;

		if (PinMode == PinMode.Input)
			pin.Disconnect(this);
		else if (Connected(pin))
		{
			connections.Remove(connections.First(c => c.Value.TargetPin == pin.ID).Key);
		}
	}

	/// <summary>
	/// Connect to another pin
	/// </summary>
	/// <param name="pin">The pin to connect to</param>
	/// <returns>The connection ID, or 0 if the connection is invalid(same node or same pin or same mode)</returns>
	public int Connect(NodePin pin, int index)
	{
		if (pin.ID == ID || pin.Node == Node || pin.PinMode == PinMode)
			return 0;

		if (PinMode == PinMode.Input)
			return pin.Connect(this, index);

		int id = index;
		connections.Add(id, new ConnectionTarget(pin.Node, pin.ID));
		return id;
	}

	public ConnectionTarget[] GetConnections()
	{
		return connections.Values.ToArray();
	}
}

public enum PinMode
{
	Input,
	Output
}
