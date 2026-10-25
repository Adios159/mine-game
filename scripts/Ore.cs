// Ore: 바닥에 떨어진 광석 하나. 종류에 맞는 색의 임시 점으로 그리고, 자석이 발동하면 플레이어 쪽으로 끌려간다.
using Godot;

public partial class Ore : Node2D
{
	private const float PullSpeedPx = 320f;

	public OreType Type;
	public bool Taken;
	public Node2D PullTarget;

	public override void _Process(double delta)
	{
		if (PullTarget == null) return;
		GlobalPosition = GlobalPosition.MoveToward(PullTarget.GlobalPosition, PullSpeedPx * (float)delta);
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 3f, OreTable.Tint[(int)Type]);
	}
}
