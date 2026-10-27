// FloorItem: 바닥에 떨어진 1회용 아이템 하나. 폭탄과 자석을 각자의 이미지로 그린다.
using Godot;

public partial class FloorItem : Node2D
{
	private const float SpriteScale = 0.35f;

	public bool IsBomb;
	public bool Taken;

	public override void _Ready()
	{
		string path = IsBomb ? "res://assets/sprites/item/bomb.png" : "res://assets/sprites/item/magnet.png";
		var sprite = new Sprite2D
		{
			Texture = GD.Load<Texture2D>(path),
			Scale = new Vector2(SpriteScale, SpriteScale)
		};
		AddChild(sprite);
	}
}
