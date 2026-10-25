// RoundSystem: 한 판의 제한 시간을 센다. 시간이 다 되면 게임을 멈추고 끝났다고 알린다.
using Godot;
using System;

public partial class RoundSystem : Node
{
	public const float RoundSeconds = 120f;

	public float Remaining = RoundSeconds;
	public bool Finished;
	public event Action Ended;

	public override void _Process(double delta)
	{
		if (Finished) return;

		Remaining -= (float)delta;
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
