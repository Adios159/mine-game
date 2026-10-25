// Drill: 돌덩이에 붙어 일정 시간 뒤 돌 1개를 캐고, 끝나면 돌덩이 주변에 떨어진다. 폭탄이 설치되면 채굴을 취소하고 바로 떨어진다.
using Godot;

public partial class Drill : Node2D
{
	[Signal] public delegate void FinishedEventHandler(Drill drill);

	public Rock Target;
	public bool Shiny;
	public bool Dropped;
	public bool Collected;
	private Timer _timer;

	public void Start(Rock target, bool shiny, float seconds)
	{
		Target = target;
		Shiny = shiny;
		GlobalPosition = target.GlobalPosition;

		_timer = new Timer { OneShot = true, WaitTime = seconds };
		AddChild(_timer);
		_timer.Timeout += OnTimeout;
		_timer.Start();
		QueueRedraw();
	}

	public void Cancel()
	{
		if (Dropped) return;
		_timer.Stop();
		Dropped = true;
		GlobalPosition = Target.GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 13f;
		QueueRedraw();
	}

	private void OnTimeout()
	{
		Dropped = true;
		EmitSignal(SignalName.Finished, this);
		GlobalPosition = Target.GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 13f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(-3, -3, 6, 6), Dropped ? new Color(0.5f, 0.5f, 0.5f) : Colors.Yellow);
		if (Shiny && !Dropped)
			DrawRect(new Rect2(-1, -7, 2, 2), Colors.White);
	}
}
