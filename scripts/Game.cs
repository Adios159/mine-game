// Game: 한 판의 루트. 노드와 시스템을 서로 연결하고, 어두운 화면, 임시 바닥, 시야, HUD, 해금 효과, 폭탄을 만든다.
using Godot;

public partial class Game : Node2D
{
	public override void _Ready()
	{
		RenderingServer.SetDefaultClearColor(Colors.Black);

		var spawner = GetNode<RockSpawner>("Systems/RockSpawner");
		spawner.SpawnRocks(GetNode<Node2D>("Rocks"));

		var floor = new ColorRect
		{
			Color = new Color(0.35f, 0.33f, 0.3f),
			Position = Vector2.Zero,
			Size = new Vector2(GameData.MapWidthPx, GameData.MapHeightPx),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = -10
		};
		AddChild(floor);

		AddChild(new CanvasModulate { Color = Colors.Black });

		var player = GetNode<Player>("Player");
		var vision = new Vision();
		player.AddChild(vision);

		var score = new ScoreSystem();
		var unlock = new UnlockSystem();
		var drops = new DropSystem
		{
			Player = player,
			Ores = GetNode<Node2D>("Ores"),
			Items = GetNode<Node2D>("Items")
		};
		AddChild(score);
		AddChild(unlock);
		AddChild(drops);
		drops.OrePicked += score.OnOrePicked;
		drops.OrePicked += unlock.OnOrePicked;

		var drillController = new DrillController
		{
			Player = player,
			Rocks = GetNode<Node2D>("Rocks"),
			Drills = GetNode<Node2D>("Drills"),
			Drops = drops
		};
		AddChild(drillController);

		AddChild(new BombController
		{
			Player = player,
			Rocks = GetNode<Node2D>("Rocks"),
			Drills = drillController,
			Drops = drops
		});

		unlock.Unlocked += type => ApplyUnlock(type, player, vision, drillController);

		var round = new RoundSystem();
		AddChild(round);

		GetNode<CanvasLayer>("HUD").AddChild(new Hud { Round = round, Score = score, Drills = drillController, Drops = drops });
	}

	private static void ApplyUnlock(OreType type, Player player, Vision vision, DrillController drills)
	{
		switch (type)
		{
			case OreType.A:
				drills.MineSeconds *= 0.75f;
				GD.Print("[효과] 채굴 시간 ", drills.MineSeconds, "초 (새로 붙이는 드릴부터)");
				break;
			case OreType.B:
				player.MoveSpeed += GameData.UnlockMoveBonus;
				GD.Print("[효과] 이동 속도 ", player.MoveSpeed);
				break;
			case OreType.C:
				drills.DrillsLeft += 2;
				GD.Print("[효과] 드릴 +2, 남은 드릴 ", drills.DrillsLeft);
				break;
			case OreType.D:
				vision.Expand(5f, 1.5f);
				drills.ReachTiles += 1.5f;
				GD.Print("[효과] 부채꼴 ", vision.FanLengthTiles, " 원 ", vision.CircleRadiusTiles, " 붙이는 거리 ", drills.ReachTiles);
				break;
		}
	}
}
