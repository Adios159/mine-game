// Player: WASD 이동과 맵 경계 제한. 광원, 줍기, 클릭 처리는 이후 단계에서 추가한다.
using Godot;

public partial class Player : CharacterBody2D
{
	public bool Active = true;
	public float MoveSpeed = GameData.BaseMoveSpeed;

	public override void _Ready()
	{
		MotionMode = MotionModeEnum.Floating;
		CollisionLayer = 0;
		CollisionMask = 2;
		GetNode<CollisionShape2D>("CollisionShape2D").Shape = new CircleShape2D { Radius = GameData.PlayerBodyRadiusPx };
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Active) { Velocity = Vector2.Zero; return; }

		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Velocity = dir * MoveSpeed * GameData.TileSize;
		MoveAndSlide();

		GlobalPosition = new Vector2(
			Mathf.Clamp(GlobalPosition.X, 0, GameData.MapWidthPx),
			Mathf.Clamp(GlobalPosition.Y, 0, GameData.MapHeightPx));
	}
}
