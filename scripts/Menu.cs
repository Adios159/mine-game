using Godot;

public partial class Menu : Control
{
	private const string GameScenePath = "res://scenes/game.tscn";

	public void OnStartButtonPressed()
	{
		GetTree().ChangeSceneToFile(GameScenePath);
	}
}
