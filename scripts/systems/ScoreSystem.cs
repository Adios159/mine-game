// ScoreSystem: 주운 광석 개수를 세고, 달성한 최고 조합 배율로 점수를 계산한다.
using Godot;

public partial class ScoreSystem : Node
{
	private static readonly (OreType[] Ores, float Mult)[] Combos =
	{
		(new[] { OreType.Iron, OreType.Coal }, 1.4f),
		(new[] { OreType.Gold, OreType.Iron }, 1.6f),
		(new[] { OreType.Emerald, OreType.Gold, OreType.Iron, OreType.Coal }, 2.6f),
		(new[] { OreType.Diamond, OreType.Gold, OreType.Iron, OreType.Coal }, 3.0f),
		(new[] { OreType.Diamond, OreType.Emerald, OreType.Gold, OreType.Coal }, 3.3f)
	};

	public int[] Counts = new int[5];

	public void OnOrePicked(OreType type)
	{
		Counts[(int)type]++;
		GD.Print("광석 획득 ", type, " | 합계 ", Sum(), " x 배율 ", BestMultiplier(), " = 점수 ", Score());
	}

	public int Sum()
	{
		int sum = 0;
		for (int i = 0; i < Counts.Length; i++)
			sum += Counts[i] * OreTable.Value[i];
		return sum;
	}

	public float BestMultiplier()
	{
		float best = 1f;
		foreach (var combo in Combos)
		{
			bool ok = true;
			foreach (OreType o in combo.Ores)
				if (Counts[(int)o] < 1) { ok = false; break; }
			if (ok && combo.Mult > best) best = combo.Mult;
		}
		return best;
	}

	public int Score()
	{
		return Mathf.RoundToInt(BestMultiplier() * Sum());
	}
}
