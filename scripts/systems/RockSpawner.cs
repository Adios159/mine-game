// RockSpawner: 시작할 때 돌덩이 22개를 규칙에 맞게 배치한다. 최소 간격, 맵 안, 시작 원 안 1개를 지킨다.
using Godot;
using System.Collections.Generic;

public partial class RockSpawner : Node
{
	[Export] public PackedScene RockScene;

	public void SpawnRocks(Node2D container)
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		float margin = GameData.TileSize;
		float minDist = GameData.MinRockSpacingTiles * GameData.TileSize;
		var center = new Vector2(GameData.MapWidthPx / 2f, GameData.MapHeightPx / 2f);
		var points = new List<Vector2>();

		float angle = rng.RandfRange(0f, Mathf.Tau);
		float dist = rng.RandfRange(1.0f, 1.4f) * GameData.TileSize;
		points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist);

		int tries = 0;
		while (points.Count < GameData.RockCount && tries++ < 10000)
		{
			var p = new Vector2(
				rng.RandfRange(margin, GameData.MapWidthPx - margin),
				rng.RandfRange(margin, GameData.MapHeightPx - margin));
			bool ok = p.DistanceTo(center) >= GameData.TileSize;
			foreach (var q in points)
				if (q.DistanceTo(p) < minDist) { ok = false; break; }
			if (ok) points.Add(p);
		}

		foreach (var p in points)
		{
			var rock = RockScene.Instantiate<Node2D>();
			container.AddChild(rock);
			rock.GlobalPosition = p;
		}

		GD.Print("돌덩이 ", points.Count, "개 생성");
	}
}
