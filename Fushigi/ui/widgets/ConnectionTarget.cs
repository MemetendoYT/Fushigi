namespace OpenAbility.ImGui.Nodes;

public struct ConnectionTarget
{
	public readonly int TargetNode;
	public readonly int TargetPin;
	
	public ConnectionTarget(int targetNode, int targetPin)
	{
		TargetNode = targetNode;
		TargetPin = targetPin;
	}
}
