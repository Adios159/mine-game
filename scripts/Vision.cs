// Vision: 열쇠 구멍 시야. 플레이어 주위의 원 조명과 마우스 방향 부채꼴 조명을 코드로 만든다.
using Godot;

public partial class Vision : Node2D
{
	private const int TexSize = 256;
	private const float CircleRadiusTiles = 1.5f;
	private const float FanLengthTiles = 3f;
	private const float FanAngleDeg = 70f;

	private PointLight2D _fan;

	public override void _Ready()
	{
		float half = TexSize / 2f;
		float tile = (float)GameData.TileSize;

		var circle = new PointLight2D
		{
			Texture = MakeTexture(false),
			TextureScale = CircleRadiusTiles * tile / half,
			Energy = 1f
		};
		AddChild(circle);

		_fan = new PointLight2D
		{
			Texture = MakeTexture(true),
			TextureScale = FanLengthTiles * tile / half,
			Energy = 1f
		};
		AddChild(_fan);
	}

	public override void _Process(double delta)
	{
		Vector2 dir = GetGlobalMousePosition() - GlobalPosition;
		if (dir.LengthSquared() > 0.0001f)
			_fan.GlobalRotation = dir.Angle();
	}

	private static ImageTexture MakeTexture(bool fan)
	{
		var img = Image.CreateEmpty(TexSize, TexSize, false, Image.Format.Rgba8);
		float half = TexSize / 2f;
		float halfAngle = Mathf.DegToRad(FanAngleDeg) / 2f;

		for (int y = 0; y < TexSize; y++)
		{
			for (int x = 0; x < TexSize; x++)
			{
				float dx = x + 0.5f - half;
				float dy = y + 0.5f - half;
				float d = Mathf.Sqrt(dx * dx + dy * dy);

				float a = Mathf.Clamp((half - d) / (half * 0.12f), 0f, 1f);
				if (fan)
				{
					float ang = Mathf.Abs(Mathf.Atan2(dy, dx));
					a *= Mathf.Clamp((halfAngle - ang) / 0.08f, 0f, 1f);
				}
				img.SetPixel(x, y, new Color(a, a, a, a));
			}
		}
		return ImageTexture.CreateFromImage(img);
	}
}
