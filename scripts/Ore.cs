// Ore: 바닥에 떨어진 광석 하나. 종류에 맞는 색의 임시 점으로 그린다.
using Godot;

public partial class Ore : Node2D
{
	public OreType Type;

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 3f, OreTable.Tint[(int)Type]);
	}
}
