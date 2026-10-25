// Session: 씬이 바뀌어도 유지되는 현재 판 정보와 서버 주소. 서버 주소는 실행 파일 옆의 server_url.txt로 바꿀 수 있다.
using Godot;

public static class Session
{
	public const string ClientVersion = "0.1.0";
	public const string TitleScene = "res://scenes/Title.tscn";
	public const string GameScene = "res://scenes/Game.tscn";
	private const string DefaultBaseUrl = "https://mine-game-server.onrender.com/api/v1";

	public static string BaseUrl = DefaultBaseUrl;
	public static string PlayId = "";
	public static string PlayToken = "";
	public static int PlayNo;
	public static int PlaysRemaining;

	public static bool HasPlay => PlayId != "";

	public static void Reset()
	{
		PlayId = "";
		PlayToken = "";
		PlayNo = 0;
		PlaysRemaining = 0;
	}

	public static void LoadConfig()
	{
		string file = OS.GetExecutablePath().GetBaseDir().PathJoin("server_url.txt");
		if (!Godot.FileAccess.FileExists(file)) return;
		using var f = Godot.FileAccess.Open(file, Godot.FileAccess.ModeFlags.Read);
		if (f == null) return;
		string line = f.GetLine().Trim();
		if (line.StartsWith("http")) BaseUrl = line.TrimEnd('/');
	}
}
