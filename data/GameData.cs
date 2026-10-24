using Godot;

public enum OreType { A = 0, B = 1, C = 2, D = 3, E = 4 }
public enum ItemType { Bomb = 0, Magnet = 1 }


public static class GameData
{
	public const string ClientVersion = "0.1.0";  // 서버로 보내는 기록용 버전


	public const int TileSize = 16;               // 타일 한 칸의 원본 픽셀
	public const int MapWidthPx = 480;            // 30칸 × 16
	public const int MapHeightPx = 224;           // 14칸 × 16


	public const float GameSeconds = 120f;
	public const int RockCount = 22;
	public const int StonesPerRock = 10;
	public const float MinRockSpacingTiles = 2f;
	public const float RockBodyRadiusPx = 7f;     // 돌덩이 몸통(충돌) 반지름
	public const float PlayerBodyRadiusPx = 5f;   // 플레이어 몸통 반지름
	public const float DropMarginPx = 5f;         // 떨어진 광석, 드릴이 맵 가장자리에서 떨어져야 하는 거리(HUD 띠에 가려지지 않게)
	public const float AttachBonusTiles = 0f;     // 붙이는 거리에 더하는 칸 수. 0이 기획서 규칙(원 반지름). 바꾸려면 기획서를 먼저 고친다
	public const float ShinyChance = 0.15f;       // 돌마다 반짝이는 돌일 확률


	public const float BombChance = 0.05f;        // 돌을 캘 때마다 따로 굴림
	public const float MagnetChance = 0.10f;
	public const int BombStones = 6;
	public const float BombFuseSeconds = 5f;


	public const int StartDrills = 3;
	public const float BaseMineSeconds = 5f;
	public const float BaseMoveSpeed = 3f;        // 칸/초
	public const float BaseFanLength = 3f;        // 칸
	public const float BaseCircleRadius = 1.5f;   // 칸
	public const float FanAngleDeg = 70f;


	// 광석 가치 A..E
	public static readonly int[] OreValue = { 5000, 3000, 1000, 500, 250 };


	// 돌 1개에서 A..E가 나올 확률 (각 합계 1.0)
	public static readonly float[] NormalTable = { 0.005f, 0.05f, 0.145f, 0.30f, 0.50f };
	public static readonly float[] ShinyTable  = { 0.03f,  0.10f, 0.22f,  0.30f, 0.35f };


	// 해금에 필요한 누적 개수 A..E (0은 해금 없음)
	public static readonly int[] UnlockNeed = { 1, 2, 5, 7, 0 };


	// 조합: 광석 비트마스크(A=1, B=2, C=4, D=8, E=16)와 배율×10
	// 배율을 10배 한 정수로 두면 소수 오차가 없고 서버와 점수가 같게 계산된다.
	public static readonly (int Mask, int Tenths)[] Combos =
	{
		(24, 14),  // D+E        ×1.4
		(12, 16),  // C+D        ×1.6
		(30, 26),  // B+C+D+E    ×2.6
		(29, 30),  // A+C+D+E    ×3.0
		(23, 33),  // A+B+C+E    ×3.3
	};


	// 마스크를 "B+C+D+E" 같은 글자로 바꾼다. 조합이 없으면 빈 문자열
	public static string ComboName(int mask)
	{
		string name = "";
		for (int i = 0; i < 5; i++)
			if ((mask & (1 << i)) != 0)
				name += (name == "" ? "" : "+") + "ABCDE"[i];
		return name;
	}
}
