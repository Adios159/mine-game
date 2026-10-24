// DropSystem: 캔 돌에서 광석을 만들어 돌덩이 주변에 떨어뜨리고, 플레이어가 닿으면 주운 것으로 알린다.
using Godot;
using System;

public partial class DropSystem : Node
{
	private const float DropRadiusPx = 13f;
	private const float PickupRadiusPx = 10f;

	public Node2D Player;
	public Node2D Ores;
	public event Action<OreType> OrePicked;

	public void SpawnFromStone(Vector2 rockPos, bool shiny)
	{
		OreType type = OreTable.Roll(shiny);
		var ore = new Ore { Type = type };
		Ores.AddChild(ore);
		ore.GlobalPosition = rockPos + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * DropRadiusPx;
		GD.Print("광석 ", type, shiny ? " (반짝)" : "");
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (Node n in Ores.GetChildren())
		{
			if (n is Ore o && o.GlobalPosition.DistanceTo(Player.GlobalPosition) <= PickupRadiusPx)
			{
				OrePicked?.Invoke(o.Type);
				o.QueueFree();
			}
		}
	}
}
