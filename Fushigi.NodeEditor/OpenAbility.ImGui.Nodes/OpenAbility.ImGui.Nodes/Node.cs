using System.Numerics;

namespace OpenAbility.ImGui.Nodes;

public abstract class Node
{
	
	public static Vector2 NodeWindowPadding = new Vector2(8, 8);
	
	public readonly uint ID = NodeEditor.GetID();
	private readonly Dictionary<uint, NodePin> pins = new Dictionary<uint, NodePin>();
	
	private readonly Dictionary<uint, NodePin> inputs = new Dictionary<uint, NodePin>();
	private readonly Dictionary<uint, NodePin> outputs = new Dictionary<uint, NodePin>();

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

	public NodePin AddInput(uint pinType)
	{
		NodePin pin = new NodePin(this, pinType, PinMode.Input);
		pins[pin.ID] = pin;
		inputs[pin.ID] = pin;
		return pin;
	}
	
	public NodePin AddOutput(uint pinType)
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

	public Vector2 GetPinPosition(uint pin)
	{
		return pins[pin].PinMode == PinMode.Input ? GetInputPinPosition(GetInputPinIndex(pin)) :
			GetOutputPinPosition(GetOutputPinIndex(pin));
	}

	public int GetPinIndex(uint pin)
	{
		if (pins[pin].PinMode == PinMode.Input)
			return GetInputPinIndex(pin);
		return GetOutputPinIndex(pin);
	}

	public int GetInputPinIndex(uint pin)
	{
		return inputs.Keys.TakeWhile(x => x != pin).Count();
	}
	
	public int GetOutputPinIndex(uint pin)
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

	public NodePin GetPin(uint id)
	{
		return pins[id];
	}

	public abstract void Render();
}
