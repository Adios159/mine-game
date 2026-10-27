// Player: WASD 이동, 맵 경계 제한, 이동에 따른 idle/walk 애니메이션과 좌우 반전. 광원, 줍기, 클릭 처리는 이후 단계에서 추가한다.
using Godot;

public partial class Player : CharacterBody2D
{
	public bool Active = true;
	public float MoveSpeed = GameData.BaseMoveSpeed;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		MotionMode = MotionModeEnum.Floating;
		CollisionLayer = 0;
		CollisionMask = 2;
		GetNode<CollisionShape2D>("CollisionShape2D").Shape = new CircleShape2D { Radius = GameData.PlayerBodyRadiusPx };
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("idle");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Active)
		{
			Velocity = Vector2.Zero;
			_sprite.Play("idle");
			return;
		}

		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = dir * MoveSpeed * GameData.TileSize;
		MoveAndSlide();

		GlobalPosition = new Vector2(
			Mathf.Clamp(GlobalPosition.X, 0, GameData.MapWidthPx),
			Mathf.Clamp(GlobalPosition.Y, 0, GameData.MapHeightPx));

		if (dir == Vector2.Zero)
		{
			_sprite.Play("idle");
			return;
		}

		_sprite.Play("walk");
		if (dir.X != 0) _sprite.FlipH = dir.X < 0;
	}
}
