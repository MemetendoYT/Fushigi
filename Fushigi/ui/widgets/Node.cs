using ImGuiNET;
using System.Numerics;

namespace OpenAbility.ImGui.Nodes;

using ImGui = ImGuiNET.ImGui;
public class Node
{
	
	public static Vector2 NodeWindowPadding = new Vector2(8, 8);

	public int ID;
	public string name;
	private readonly Dictionary<int, NodePin> pins = new Dictionary<int, NodePin>();
	private readonly Dictionary<int, NodePin> inputs = new Dictionary<int, NodePin>();
	private readonly Dictionary<int, NodePin> outputs = new Dictionary<int, NodePin>();

	public Vector2 Position;
	internal Vector2 Size;

	public NodePin AddInput(PinType pinType)
	{
		return AddInput(pinType.ID);
	}
	
	public NodePin AddOutput(PinType pinType)
	{
		return AddOutput(pinType.ID);
	}

	public NodePin AddInput(int pinType)
	{
		NodePin pin = new NodePin(this, pinType, PinMode.Input);
		pins[pin.ID] = pin;
		inputs[pin.ID] = pin;
		return pin;
	}
	
	public NodePin AddOutput(int pinType)
	{
		NodePin pin = new NodePin(this, pinType, PinMode.Output);
		pins[pin.ID] = pin;
		outputs[pin.ID] = pin;
		return pin;
	}

	public NodePin[] GetPins()
	{
		return pins.Values.ToArray();
	}

	public Vector2 GetPinPosition(int pin)
	{
		return pins[pin].PinMode == PinMode.Input ? GetInputPinPosition(GetInputPinIndex(pin)) :
			GetOutputPinPosition(GetOutputPinIndex(pin));
	}

	public int GetPinIndex(int pin)
	{
		if (pins[pin].PinMode == PinMode.Input)
			return GetInputPinIndex(pin);
		return GetOutputPinIndex(pin);
	}

	public int GetInputPinIndex(int pin)
	{
		return inputs.Keys.TakeWhile(x => x != pin).Count();
	}
	
	public int GetOutputPinIndex(int pin)
	{
		return outputs.Keys.TakeWhile(x => x != pin).Count();
	}
	
	public Vector2 GetInputPinPosition(int index)
	{
		return Position with
		{
			Y = Position.Y + Size.Y * ((float)index + 1) / ((float)inputs.Count + 1)
		};
	}
	
	
	public Vector2 GetOutputPinPosition(int index)
	{
		return new Vector2(Position.X + Size.X, 
			Position.Y + Size.Y * ((float)index + 1) / ((float)outputs.Count + 1));
	}

	public NodePin GetPin(int id)
	{
		return pins[id];
	}

    public void Render()
    {
		ImGui.Text(name);
    }

}
