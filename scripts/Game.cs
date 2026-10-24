// Game: 한 판의 루트. 노드와 시스템을 서로 연결하고 어두운 화면, 임시 바닥, 시야를 만든다.
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

		AddChild(new CanvasModulate { Color = new Color(0.03f, 0.03f, 0.06f) });

		var player = GetNode<Node2D>("Player");
		player.AddChild(new Vision());

		var score = new ScoreSystem();
		var unlock = new UnlockSystem();
		var drops = new DropSystem { Player = player, Ores = GetNode<Node2D>("Ores") };
		AddChild(score);
		AddChild(unlock);
		AddChild(drops);
		drops.OrePicked += score.OnOrePicked;
		drops.OrePicked += unlock.OnOrePicked;

		AddChild(new DrillController
		{
			Player = player,
			Rocks = GetNode<Node2D>("Rocks"),
			Drills = GetNode<Node2D>("Drills"),
			Drops = drops
		});
	}
}
