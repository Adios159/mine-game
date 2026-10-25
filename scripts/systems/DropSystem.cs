// DropSystem: 캔 돌에서 광석과 아이템을 만들어 돌덩이 주변에 떨어뜨리고, 플레이어가 닿으면 줍는다. 자석은 줍는 즉시 광석을 끌어당긴다.
using Godot;
using System;

public partial class DropSystem : Node
{
	private const float DropRadiusPx = 13f;
	private const float PickupRadiusPx = 10f;
	private const float BombChance = 0.05f;
	private const float MagnetChance = 0.10f;

	public Node2D Player;
	public Node2D Ores;
	public Node2D Items;
	public bool HasBomb;
	public event Action<OreType> OrePicked;

	public void SpawnFromStone(Vector2 rockPos, bool shiny)
	{
		SpawnOre(rockPos, shiny);
		if (GD.Randf() < BombChance) SpawnItem(rockPos, true);
		if (GD.Randf() < MagnetChance) SpawnItem(rockPos, false);
	}

	public void SpawnOre(Vector2 rockPos, bool shiny)
	{
		OreType type = OreTable.Roll(shiny);
		var ore = new Ore { Type = type };
		Ores.AddChild(ore);
		ore.GlobalPosition = Around(rockPos);
		GD.Print("광석 ", type, shiny ? " (반짝)" : "");
	}

	private void SpawnItem(Vector2 rockPos, bool bomb)
	{
		var item = new FloorItem { IsBomb = bomb };
		Items.AddChild(item);
		item.GlobalPosition = Around(rockPos);
		GD.Print("아이템 ", bomb ? "폭탄" : "자석");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Ores == null || Player == null || Items == null) return;

		foreach (Node n in Ores.GetChildren())
		{
			if (n is not Ore o || o.Taken) continue;
			if (o.GlobalPosition.DistanceTo(Player.GlobalPosition) > PickupRadiusPx) continue;
			o.Taken = true;
			OrePicked?.Invoke(o.Type);
			o.QueueFree();
		}

		foreach (Node n in Items.GetChildren())
		{
			if (n is not FloorItem it || it.Taken) continue;
			if (it.GlobalPosition.DistanceTo(Player.GlobalPosition) > PickupRadiusPx) continue;

			if (it.IsBomb)
			{
				if (HasBomb) continue;
				HasBomb = true;
				GD.Print("폭탄 획득");
			}
			else
			{
				PullAllOres();
				GD.Print("자석 발동");
			}
			it.Taken = true;
			it.QueueFree();
		}
	}

	private void PullAllOres()
	{
		foreach (Node n in Ores.GetChildren())
			if (n is Ore o && !o.Taken) o.PullTarget = Player;
	}

	private static Vector2 Around(Vector2 center)
	{
		return center + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * DropRadiusPx;
	}
}
