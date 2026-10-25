// FloorItem: 바닥에 떨어진 1회용 아이템 하나. 폭탄은 빨간 네모, 자석은 하늘색 네모로 그린다.
using Godot;

public partial class FloorItem : Node2D
{
	public bool IsBomb;
	public bool Taken;

	public override void _Draw()
	{
		Color c = IsBomb ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.3f, 0.9f, 0.95f);
		DrawRect(new Rect2(-3, -3, 6, 6), c);
		DrawRect(new Rect2(-3, -3, 6, 6), Colors.White, false, 1f);
	}
}
