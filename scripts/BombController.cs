// BombController: 우클릭으로 사거리 안의 돌덩이에 폭탄을 설치한다. 5초 뒤 돌덩이를 없애고 남은 돌 최대 6개를 바로 광석으로 만든다.
using Godot;

public partial class BombController : Node
{
	private const float FuseSeconds = 5f;
	private const int BombStones = 6;
	private const float PickRadiusPx = 10f;
	private const float ShinyChance = 0.15f;

	public Node2D Player;
	public Node2D Rocks;
	public DrillController Drills;
	public DropSystem Drops;

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
			TryPlace(Player.GetGlobalMousePosition());
	}

	private void TryPlace(Vector2 mouse)
	{
		if (!Drops.HasBomb) return;

		Rock best = null;
		float bestDist = PickRadiusPx;
		foreach (Node n in Rocks.GetChildren())
		{
			if (n is not Rock r || r.Bombed) continue;
			float d = r.GlobalPosition.DistanceTo(mouse);
			if (d < bestDist) { best = r; bestDist = d; }
		}
		if (best == null) return;
		if (best.GlobalPosition.DistanceTo(Player.GlobalPosition) > Drills.ReachTiles * GameData.TileSize) return;

		Drops.HasBomb = false;
		best.Bombed = true;
		SoundManager.Instance.Play("sfx_bomb_place");
		best.Modulate = new Color(1f, 0.4f, 0.4f);

		if (best.AttachedDrill != null)
		{
			best.AttachedDrill.Cancel();
			best.AttachedDrill = null;
			best.Drilling = false;
		}

		GD.Print("폭탄 설치, 돌덩이에 남은 돌 ", best.Stones);
		Rock rock = best;
		GetTree().CreateTimer(FuseSeconds, false).Timeout += () => Explode(rock);
	}

	private void Explode(Rock rock)
	{
		if (!IsInstanceValid(rock)) return;

		int count = Mathf.Min(BombStones, rock.Stones);
		Vector2 pos = rock.GlobalPosition;
		rock.QueueFree();
		SoundManager.Instance.Play("sfx_bomb_explode");
		for (int i = 0; i < count; i++)
			Drops.SpawnOre(pos, GD.Randf() < ShinyChance);
		GD.Print("폭탄 폭발, 광석 ", count, "개");
	}
}
