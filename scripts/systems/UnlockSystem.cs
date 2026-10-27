// UnlockSystem: 광석별 누적 습득 개수를 세고 해금 조건을 채우면 알린다. 실제 효과 적용은 이후 단계에서 붙인다.
using Godot;
using System;

public partial class UnlockSystem : Node
{
	private static readonly int[] Need = { 1, 2, 5, 7, 0 };
	private static readonly string[] Effect = { "채굴 시간 -25%", "이동 속도 +5", "드릴 +2", "시야 확대", "" };

	private readonly int[] _counts = new int[5];
	private readonly bool[] _unlocked = new bool[5];

	public event Action<OreType> Unlocked;

	public void OnOrePicked(OreType type)
	{
		int i = (int)type;
		_counts[i]++;
		if (Need[i] > 0 && !_unlocked[i] && _counts[i] >= Need[i])
		{
			_unlocked[i] = true;
			SoundManager.Instance.Play("sfx_unlock");
			GD.Print("해금: ", type, " -> ", Effect[i]);
			Unlocked?.Invoke(type);
		}
	}
}
