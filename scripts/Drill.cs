// Drill: 돌덩이에 붙어 일정 시간 뒤 돌 1개를 캐고, 끝나면 돌덩이 주변에 떨어진다. 폭탄이 설치되면 채굴을 취소하고 바로 떨어진다. 작동 중에는 work, 떨어지면 dropped 애니메이션을 보여 주고, 반짝이는 돌이면 작동 중에만 반짝이를 겹쳐 재생한다.
using Godot;

public partial class Drill : Node2D
{
	[Signal] public delegate void FinishedEventHandler(Drill drill);

	private const float SpriteScale = 0.25f;
	private const float DropDistancePx = 13f;

	public Rock Target;
	public bool Shiny;
	public bool Dropped;
	public bool Collected;
	private Timer _timer;
	private AnimatedSprite2D _sprite;
	private AnimatedSprite2D _sparkle;
	private int _runSound;

	public override void _Ready()
	{
		_sprite = new AnimatedSprite2D
		{
			SpriteFrames = GD.Load<SpriteFrames>("res://assets/frames/drill.tres"),
			Scale = new Vector2(SpriteScale, SpriteScale)
		};
		AddChild(_sprite);

		var sparkleFrames = GD.Load<SpriteFrames>("res://assets/frames/sparkle.tres");
		_sparkle = new AnimatedSprite2D
		{
			SpriteFrames = sparkleFrames,
			Scale = new Vector2(SpriteScale, SpriteScale),
			Visible = false
		};
		AddChild(_sparkle);
	}

	public void Start(Rock target, bool shiny, float seconds)
	{
		Target = target;
		Shiny = shiny;
		GlobalPosition = target.GlobalPosition;

		_sprite.Play("work");
		if (Shiny)
		{
			_sparkle.Visible = true;
			_sparkle.Play(_sparkle.SpriteFrames.GetAnimationNames()[0]);
		}

		_timer = new Timer { OneShot = true, WaitTime = seconds };
		AddChild(_timer);
		_timer.Timeout += OnTimeout;
		_timer.Start();
		_runSound = SoundManager.Instance.Play("sfx_drill_run_alt", -6f);
	}

	public void Cancel()
	{
		if (Dropped) return;
		_timer.Stop();
		SoundManager.Instance.Stop(_runSound);
		DropAround();
	}

	private void OnTimeout()
	{
		EmitSignal(SignalName.Finished, this);
		DropAround();
	}

	private void DropAround()
	{
		Dropped = true;
		_sparkle.Stop();
		_sparkle.Visible = false;
		_sprite.Animation = "dropped";
		_sprite.Frame = 0;
		_sprite.Stop();
		GlobalPosition = Target.GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * DropDistancePx;
	}
}
