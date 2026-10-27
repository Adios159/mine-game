// RoundSystem: 한 판의 제한 시간을 센다. 남은 시간 5초부터 1초마다 경고음을 한 번씩 울리고, 시간이 다 되면 게임을 멈추고 끝났다고 알린다.
using Godot;
using System;

public partial class RoundSystem : Node
{
	public const float RoundSeconds = 120f;
	private const int WarningSeconds = 5;

	public float Remaining = RoundSeconds;
	public bool Finished;
	private int _nextWarning = WarningSeconds;
	public event Action Ended;

	public override void _Process(double delta)
	{
		if (Finished) return;

		Remaining -= (float)delta;
		if (_nextWarning > 0 && Remaining <= _nextWarning)
		{
			_nextWarning--;
			SoundManager.Instance.Play("sfx_time_warning");
		}
		if (Remaining <= 0f)
		{
			Remaining = 0f;
			Finished = true;
			GetTree().Paused = true;
			GD.Print("시간 종료");
			Ended?.Invoke();
		}
	}
}
