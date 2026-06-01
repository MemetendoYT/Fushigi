using ImGuiNET;
using System.Numerics;

namespace OpenAbility.ImGui.Nodes;

using ImGui = ImGuiNET.ImGui;

public class NodeEditor
{
	public string Name;
	public bool Grid = true;
	public Vector2 Scrolling = Vector2.Zero;
	
	private readonly Dictionary<uint, Node> nodes = new Dictionary<uint, Node>();

	private uint hoveredNode;
	private uint selectedNode;

	private uint selectedConnectionPin;
	private uint selectedConnectionNode;

	public Action DrawContext = () =>
	{
		ImGui.Text("Set NodeEditor.DrawContext!");
	};

	public NodeEditor(string name)
	{
		Name = name;
	}
	
	public void Draw()
	{
		hoveredNode = 0;
		if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
			selectedNode = 0;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		ImGui.BeginChild(Name, ImGui.GetWindowSize(), true, 
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		
		// Time to draw all of our stuff
		ImGui.BeginGroup();
		
		drawList.PushClipRect(ImGui.GetCursorScreenPos(), ImGui.GetWindowSize());

		Vector2 offset = ImGui.GetCursorScreenPos() + Scrolling;

		if (Grid)
		{
			uint gridColour = ImUtil.ImCol32(200, 200, 200, 40);
			const float gridSize = 64.0f;

			Vector2 winPos = ImGui.GetCursorScreenPos();
			Vector2 winSize = ImGui.GetWindowSize();
			
			for (float x = Scrolling.X % gridSize; x < winSize.X; x += gridSize)
				drawList.AddLine(new Vector2(x, 0) + winPos, new Vector2(x, winSize.Y) + winPos, gridColour);
			for (float y = Scrolling.Y % gridSize; y < winSize.Y; y += gridSize)
				drawList.AddLine(new Vector2(0, y) + winPos, new Vector2(winSize.X, y) + winPos, gridColour);
		}
		
		drawList.ChannelsSplit(2);
		drawList.ChannelsSetCurrent(0); // Background

		foreach (var node in nodes.Values)
		{
			foreach (var pin in node.GetPins())
			{
				int pinIndex = node.GetOutputPinIndex(pin.ID);
				Vector2 o0 = offset + node.GetOutputPinPosition(pinIndex);
				Vector2 o1 = o0 + new Vector2(50, 0);

				foreach (var connection in pin.GetConnections())
				{
					Node target = nodes[connection.TargetNode];
					NodePin targetPin = target.GetPin(connection.TargetPin);
					
					int targetPinIndex = target.GetInputPinIndex(targetPin.ID);

					Vector2 i0 = offset + target.GetInputPinPosition(targetPinIndex);
					Vector2 i1 = i0 + new Vector2(-50, 0);
					
					drawList.AddBezierCubic(o0, o1, i1, i0, pin.PinType.Colour, 3.0f);
				}
			}
		}

		foreach (var node in nodes.Values)
		{
			ImGui.PushID(new IntPtr(node.ID));

			Vector2 nodeRectMin = offset + node.Position;
			
			drawList.ChannelsSetCurrent(1);
			bool oldAnyActive = ImGui.IsAnyItemActive();
			ImGui.SetCursorScreenPos(nodeRectMin + Node.NodeWindowPadding);
			ImGui.BeginGroup();
			node.Render();
			ImGui.EndGroup();
			
			// Is the node being used
			bool isNodeAnyActive = (!oldAnyActive && ImGui.IsAnyItemActive());
			node.Size = ImGui.GetItemRectSize() + Node.NodeWindowPadding * 2;
			Vector2 nodeRectMax = nodeRectMin + node.Size;
			
			drawList.ChannelsSetCurrent(0);
			ImGui.SetCursorScreenPos(nodeRectMin);
			ImGui.InvisibleButton("node", node.Size);
			if (ImGui.IsItemHovered())
			{
				hoveredNode = node.ID;

				if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
					selectedNode = node.ID;
			}
			
			bool moving = ImGui.IsItemActive();
			if (moving && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
			{
				node.Position += ImGui.GetIO().MouseDelta;
			}

			uint colour = ImUtil.ImCol32(50, 50, 50, 255);
			if(selectedNode == node.ID || isNodeAnyActive)
				colour = ImUtil.ImCol32(75, 75, 75, 255);
			else if (hoveredNode == node.ID)
				colour = ImUtil.ImCol32(60, 60, 60, 255);
			
			drawList.AddRectFilled(nodeRectMin, nodeRectMax, colour, 4.0f);
			drawList.AddRect(nodeRectMin, nodeRectMax, ImUtil.ImCol32(100, 100, 100, 255), 4.0f);

			foreach (var pin in node.GetPins())
			{
				Vector2 position;
				if (pin.PinMode == PinMode.Input)
					position = offset + node.GetInputPinPosition(node.GetInputPinIndex(pin.ID));
				else
					position = offset + node.GetOutputPinPosition(node.GetOutputPinIndex(pin.ID));
				
				drawList.AddCircleFilled(position, NodePin.Radius, pin.PinType.Colour);
				
				if ((ImGui.GetMousePos() - position).Length() < NodePin.SelectRadius)
				{
					if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					{
						Console.WriteLine("Hover!");
						selectedConnectionNode = node.ID;
						selectedConnectionPin = pin.ID;
					} else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && selectedConnectionNode != 0 && selectedConnectionPin != 0)
					{
						NodePin target = nodes[selectedConnectionNode].GetPin(selectedConnectionPin);
						if(pin.Connected(target))
							pin.Disconnect(target);
						else
							pin.Connect(target);
					}
				}
			}
			
			ImGui.PopID();
		}
		
		// Draw the current connection
		if (selectedConnectionNode != 0 && selectedConnectionPin != 0)
		{
			NodePin currentSelectedPin = nodes[selectedConnectionNode].GetPin(selectedConnectionPin);
			drawList.ChannelsSetCurrent(1);
			
			Vector2 o0 = offset + nodes[selectedConnectionNode].GetPinPosition(selectedConnectionPin);
			Vector2 o1 = o0 + new Vector2(currentSelectedPin.PinMode == PinMode.Input ? -50 : 50, 0);

			Vector2 i0;

			NodePin? closest = null;
			float closestDistance = Single.MaxValue;

			foreach (var node in nodes.Values)
			{
				foreach (var pin in node.GetPins())
				{
					if(pin.PinMode == currentSelectedPin.PinMode)
						continue;
					if(pin.Node == selectedConnectionNode)
						continue;
					if(pin.PinTypeID != currentSelectedPin.PinTypeID)
						continue;

					Vector2 delta = ImGui.GetMousePos() - (offset + node.GetPinPosition(pin.ID));
					float distance = delta.Length();

					if(distance > NodePin.SelectRadius)
						continue;
					
					if (distance >= closestDistance)
						continue;
					
					closestDistance = distance;
					closest = pin;
				}
			}
			

			i0 = closest != null ? offset + nodes[closest.Node].GetPinPosition(closest.ID) : ImGui.GetMousePos();
			
			Vector2 i1 = i0 + new Vector2(currentSelectedPin.PinMode == PinMode.Input ? 50 : -50, 0);
			
			
			drawList.AddBezierCubic(o0, o1, i1, i0, 
				nodes[selectedConnectionNode].GetPin(selectedConnectionPin).PinType.Colour, 3.0f);
			
		}

		drawList.ChannelsMerge();
		drawList.PopClipRect();
		
		if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) || !ImGui.IsAnyItemHovered())
			{
				ImGui.OpenPopup(Name + "_context");
			}

		if (ImGui.BeginPopup(Name + "_context"))
		{
			ContextMenu();
			ImGui.EndPopup();
		}
		
		if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0.0f))
			Scrolling += ImGui.GetIO().MouseDelta;
		
		ImGui.EndGroup();

		if (selectedNode != 0)
		{
			if (ImGui.IsKeyPressed(ImGuiKey.Delete))
			{
				nodes.Remove(selectedNode);
				selectedNode = 0;
			}
		}
		
		if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
		{
			selectedConnectionNode = 0;
			selectedConnectionPin = 0;
		}
	}

	public virtual void ContextMenu()
	{
		DrawContext();
	}
	
	
	
	private static uint currentID = 1;
	internal static uint GetID()
	{
		return currentID++;
	}
	
	public void AddNode(Node node)
	{
		nodes.Add(node.ID, node);
	}
}
