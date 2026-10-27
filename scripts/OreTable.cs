// OreTable: 광석 5종의 가치, 확률표, 임시 색을 모아 두고 확률대로 종류를 뽑는다.
using Godot;

public static class OreTable
{
	public static readonly int[] Value = { 5000, 3000, 1000, 500, 250 };
	private static readonly float[] Normal = { 0.5f, 5f, 14.5f, 30f, 50f };
	private static readonly float[] Shiny = { 3f, 10f, 22f, 30f, 35f };
	public static readonly Color[] Tint =
	{
		new Color(1f, 0.3f, 0.3f),
		new Color(0.95f, 0.75f, 0.2f),
		new Color(0.3f, 0.8f, 0.4f),
		new Color(0.3f, 0.6f, 1f),
		new Color(0.75f, 0.75f, 0.75f)
	};

	public static OreType Roll(bool shiny)
	{
		float[] table = shiny ? Shiny : Normal;
		float r = GD.Randf() * 100f;
		float acc = 0f;
		for (int i = 0; i < table.Length; i++)
		{
			acc += table[i];
			if (r < acc) return (OreType)i;
		}
		return OreType.Coal;
	}
}
