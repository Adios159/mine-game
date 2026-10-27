// DrillController: 좌클릭으로 사거리 안의 돌덩이에 드릴을 붙이고, 끝난 드릴의 결과 처리와 회수를 맡는다.
using Godot;

public partial class DrillController : Node
{
	private const int InitialDrills = 3;
	private const float PickRadiusPx = 10f;
	private const float PickupRadiusPx = 10f;
	private const float ShinyChance = 0.15f;

	public float MineSeconds = 5f;
	public float ReachTiles = 1.5f;

	public Node2D Player;
	public Node2D Rocks;
	public Node2D Drills;
	public DropSystem Drops;
	public int DrillsLeft = InitialDrills;

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			TryAttach(Player.GetGlobalMousePosition());
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (Node n in Drills.GetChildren())
		{
			if (n is Drill d && d.Dropped && !d.Collected && d.GlobalPosition.DistanceTo(Player.GlobalPosition) <= PickupRadiusPx)
			{
				d.Collected = true;
				SoundManager.Instance.Play("sfx_drill_pickup");
				DrillsLeft++;
				d.QueueFree();
				GD.Print("드릴 회수, 남은 드릴 ", DrillsLeft);
			}
		}
	}

	private void TryAttach(Vector2 mouse)
	{
		if (DrillsLeft <= 0) return;

		Rock best = null;
		float bestDist = PickRadiusPx;
		foreach (Node n in Rocks.GetChildren())
		{
			if (n is not Rock r || r.Drilling || r.Bombed) continue;
			float d = r.GlobalPosition.DistanceTo(mouse);
			if (d < bestDist) { best = r; bestDist = d; }
		}
		if (best == null) return;

		float reach = ReachTiles * GameData.TileSize;
		if (best.GlobalPosition.DistanceTo(Player.GlobalPosition) > reach) return;

		DrillsLeft--;
		best.Drilling = true;

		var drill = new Drill();
		Drills.AddChild(drill);
		drill.Finished += OnDrillFinished;
		drill.Start(best, GD.Randf() < ShinyChance, MineSeconds);
		best.AttachedDrill = drill;
		SoundManager.Instance.Play("sfx_drill_attach");
		GD.Print("드릴 부착, 남은 드릴 ", DrillsLeft, drill.Shiny ? " (반짝)" : "");
	}

	private void OnDrillFinished(Drill drill)
	{
		Rock rock = drill.Target;
		rock.Stones--;
		rock.Drilling = false;
		rock.AttachedDrill = null;
		Drops.SpawnFromStone(rock.GlobalPosition, drill.Shiny);
		SoundManager.Instance.Play("sfx_mining_done");
		GD.Print("채굴 완료, 돌덩이에 남은 돌 ", rock.Stones);
		if (rock.Stones <= 0)
		{
			GD.Print("돌덩이 소멸");
			rock.QueueFree();
		}
	}
}
