// Ore: 바닥에 떨어진 광석 하나. 종류에 맞는 SpriteFrames로 애니메이션을 재생하고, 자석이 발동하면 플레이어 쪽으로 끌려간다.
using Godot;

public partial class Ore : Node2D
{
	private const float PullSpeedPx = 320f;
	private const float SpriteScale = 0.35f;

	public OreType Type;
	public bool Taken;
	public Node2D PullTarget;

	public override void _Ready()
	{
		var frames = GD.Load<SpriteFrames>($"res://assets/frames/ore_{Type}.tres");
		var sprite = new AnimatedSprite2D
		{
			SpriteFrames = frames,
			Scale = new Vector2(SpriteScale, SpriteScale)
		};
		AddChild(sprite);
		sprite.Play(frames.GetAnimationNames()[0]);
	}

	public override void _Process(double delta)
	{
		if (PullTarget == null) return;
		GlobalPosition = GlobalPosition.MoveToward(PullTarget.GlobalPosition, PullSpeedPx * (float)delta);
	}
}
