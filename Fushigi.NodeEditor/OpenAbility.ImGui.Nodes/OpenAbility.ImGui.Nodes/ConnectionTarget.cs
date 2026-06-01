namespace OpenAbility.ImGui.Nodes;

public struct ConnectionTarget
{
	public readonly uint TargetNode;
	public readonly uint TargetPin;
	
	public ConnectionTarget(uint targetNode, uint targetPin)
	{
		TargetNode = targetNode;
		TargetPin = targetPin;
	}
}
