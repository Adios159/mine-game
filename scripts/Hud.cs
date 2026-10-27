// Hud: 아래 띠에 남은 시간, 점수, 배율, 드릴 수와 광석 다이아몬드~철광석 해금 게이지 목록을 보여 준다. 시작 때 카운트다운을 돌리고, 끝나면 '게임 종료' 문구 뒤에 결과 화면(왼쪽 점수·순위, 오른쪽 점수 구간 그림)을 띄워 점수를 서버에 제출한다.
using Godot;
using System;
using System.Globalization;
using System.Threading.Tasks;

public partial class Hud : Node
{
	private const string OreListDir = "res://assets/sprites/ui/ore_list/";
	private const string UnlockDir = "res://assets/sprites/ui/unlock/";
	private static readonly string[] UnlockEffect = { "mine_speed", "move_speed", "drill_count", "vision" };
	private const float SpriteScale = 0.25f;
	private const float GaugeLeftSrc = 68f;
	private const float RowHeightSrc = 32f;
	private const float CellW = 64f;
	private const float CellH = 16f;
	private const float CellGap = 2f;
	private const float OreListX = 480f - 4f - CellW * 2 - CellGap;
	private const float OreListY = 230f;
	private static readonly Vector2 RowOffset = new(0, 4);
	private static readonly Vector2 NumberOffset = new(8, 2);
	private static readonly Vector2 BadgeOffset = new(40, 0);
	private const float ResultHalf = 240f;
	private const float ResultPad = 6f;
	private const float TierTextY = 28f;
	private const int OresPerLine = 3;
	private static readonly Vector2 TierPicPos = new(13, 46);
	private static readonly Vector2 TierPicSize = new(214, 178);
	private static readonly Vector2 ActionPos = new(300, 236);
	private static readonly int[] TopColumns = { 18, 60, 84, 48 };

	public RoundSystem Round;
	public ScoreSystem Score;
	public DrillController Drills;
	public DropSystem Drops;

	private Label _top;
	private Label _bottom;
	private Label _center;
	private ColorRect _result;
	private Label _summary;
	private Label _status;
	private GridContainer _top10;
	private Label _tierText;
	private TextureRect _tierPic;
	private Button _action;
	private Action _onAction;
	private ApiClient _api;
	private readonly Texture2D[][] _stages = new Texture2D[UnlockEffect.Length][];
	private readonly Sprite2D[] _rows = new Sprite2D[UnlockEffect.Length];
	private readonly Sprite2D[] _badges = new Sprite2D[UnlockEffect.Length];
	private readonly Label[] _nums = new Label[UnlockEffect.Length];
	private readonly int[] _shown = new int[UnlockEffect.Length];

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_api = new ApiClient();
		AddChild(_api);

		_top = MakeLabel(this, new Vector2(4, OreListY + NumberOffset.Y), 12);
		_bottom = MakeLabel(this, new Vector2(4, OreListY + CellH + CellGap + NumberOffset.Y), 12);
		MakeOreList();

		_center = MakeLabel(this, Vector2.Zero, 24);
		_center.Size = new Vector2(GameData.MapWidthPx, GameData.MapHeightPx);
		_center.HorizontalAlignment = HorizontalAlignment.Center;
		_center.VerticalAlignment = VerticalAlignment.Center;

		_result = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 1f),
			Position = Vector2.Zero,
			Size = new Vector2(480, 270),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false
		};
		AddChild(_result);

		var info = new VBoxContainer
		{
			Position = new Vector2(ResultPad, ResultPad),
			Size = new Vector2(ResultHalf - ResultPad * 2, 270 - ResultPad * 2),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		info.AddThemeConstantOverride("separation", 4);
		_result.AddChild(info);
		_summary = MakeWrapLabel(info);
		_status = MakeWrapLabel(info);
		_top10 = new GridContainer { Columns = TopColumns.Length, MouseFilter = Control.MouseFilterEnum.Ignore };
		_top10.AddThemeConstantOverride("h_separation", 4);
		_top10.AddThemeConstantOverride("v_separation", 0);
		info.AddChild(_top10);

		var picSide = new ColorRect
		{
			Color = Colors.White,
			Position = new Vector2(ResultHalf, 0),
			Size = new Vector2(480 - ResultHalf, 270),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		_result.AddChild(picSide);
		_tierText = MakeLabel(picSide, new Vector2(0, TierTextY), 12);
		_tierText.Size = new Vector2(480 - ResultHalf, 16);
		_tierText.HorizontalAlignment = HorizontalAlignment.Center;
		_tierText.AddThemeColorOverride("font_color", Colors.Black);
		_tierPic = new TextureRect
		{
			Position = TierPicPos,
			Size = TierPicSize,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		picSide.AddChild(_tierPic);

		_action = new Button { Position = ActionPos, Size = new Vector2(120, 24), Visible = false };
		_action.AddThemeFontSizeOverride("font_size", 12);
		_action.Pressed += () => _onAction?.Invoke();
		_result.AddChild(_action);

		Round.Ended += OnEnded;
		StartCountdown();
	}

	public override void _Process(double delta)
	{
		int sec = Mathf.CeilToInt(Round.Remaining);
		_top.Text = $"TIME {sec / 60}:{sec % 60:00}   SCORE {Score.Score()}   x{Score.BestMultiplier():0.0}";
		int[] c = Score.Counts;
		_bottom.Text = $"DRILL {Drills.DrillsLeft}   BOMB {(Drops.HasBomb ? 1 : 0)}   {GameData.OreNames[4]} {c[4]}";
		for (int i = 0; i < _rows.Length; i++)
		{
			if (c[i] == _shown[i]) continue;
			_shown[i] = c[i];
			ShowOre(i, c[i]);
		}
	}

	private void MakeOreList()
	{
		for (int i = 0; i < _rows.Length; i++)
		{
			int need = GameData.UnlockNeed[i];
			string key = GameData.OreKeys[i];
			_stages[i] = new Texture2D[need];
			for (int n = 0; n < need; n++)
				_stages[i][n] = GD.Load<Texture2D>(i == 0 ? $"{OreListDir}list_{key}.png" : $"{OreListDir}list_{key}_{n + 1}.png");

			var cell = new Vector2(OreListX + i / 2 * (CellW + CellGap), OreListY + i % 2 * (CellH + CellGap));
			_rows[i] = new Sprite2D
			{
				Centered = false,
				Scale = Vector2.One * SpriteScale,
				Position = cell + RowOffset,
				RegionRect = new Rect2(0, 0, Mathf.Min(GaugeLeftSrc, _stages[i][0].GetWidth()), RowHeightSrc)
			};
			AddChild(_rows[i]);

			_badges[i] = new Sprite2D
			{
				Centered = false,
				Scale = Vector2.One * SpriteScale,
				Position = cell + BadgeOffset,
				Texture = GD.Load<Texture2D>($"{UnlockDir}unlock_{key}_{UnlockEffect[i]}.png")
			};
			AddChild(_badges[i]);

			_nums[i] = MakeLabel(this, cell + NumberOffset, 12);
			_nums[i].AddThemeColorOverride("font_color", Colors.White);
			_nums[i].AddThemeColorOverride("font_outline_color", Colors.Black);
			_nums[i].AddThemeConstantOverride("outline_size", 2);

			ShowOre(i, 0);
		}
	}

	private void ShowOre(int i, int count)
	{
		int need = GameData.UnlockNeed[i];
		bool unlocked = count >= need;
		_rows[i].Texture = _stages[i][Mathf.Min(count, need - 1)];
		_rows[i].RegionEnabled = unlocked;
		_badges[i].Visible = unlocked;
		_nums[i].Text = count.ToString();
	}

	// 트리를 멈춘 채 3, 2, 1을 센 뒤 풀어 준다. 타이머는 멈춘 트리에서도 돌아가야 해서 기본값(processAlways)을 그대로 쓴다.
	private async void StartCountdown()
	{
		GetTree().Paused = true;
		for (int n = GameData.CountdownSeconds; n >= 1; n--)
		{
			_center.Text = $"준비\n{n}";
			await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
		}
		_center.Text = "시작!";
		GetTree().Paused = false;
		await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
		_center.Text = "";
	}

	private async void OnEnded()
	{
		int[] c = Score.Counts;
		int score = Score.Score();
		GD.Print($"[결과] A{c[0]} B{c[1]} C{c[2]} D{c[3]} E{c[4]} | 합계 {Score.Sum()} | 배율 {Score.BestMultiplier():0.0} | 점수 {score}");

		_center.Text = "게임 종료";
		SoundManager.Instance.Play("sfx_game_over");
		await ToSignal(GetTree().CreateTimer(GameData.EndBannerSeconds), SceneTreeTimer.SignalName.Timeout);
		_center.Text = "";

		_summary.Text = $"점수 {Comma(score)}  (합계 {Comma(Score.Sum())} x{Score.BestMultiplier():0.0})\n조합 {ComboLine(c)}\n{OreLine(c)}";
		ShowTier(GameData.ResultTier(score));
		_result.Visible = true;

		if (!Session.HasPlay)
		{
			_status.Text = "서버 없이 시작한 테스트 판이라 점수를 보내지 않았습니다.";
			SetAction("다시 시작", Restart);
			return;
		}
		await Submit();
	}

	private async Task Submit()
	{
		_action.Visible = false;
		_status.Text = "점수 전송 중...";
		ApiResult r = await _api.Finish(Session.PlayId, Session.PlayToken, Score.Score(), Score.Counts);

		if (r.Success)
		{
			ShowRanking(r);
			SetAction("처음으로", GoTitle);
			return;
		}

		GD.Print($"[전송 실패] status={r.Status} code={r.ErrorCode}");
		if (r.Retryable)
		{
			_status.Text = r.Message() + "\n점수는 이 화면에만 있습니다. 게임을 끄면 사라집니다.";
			SetAction("다시 전송", () => { _ = Submit(); });
		}
		else
		{
			_status.Text = r.Message();
			SetAction("처음으로", GoTitle);
		}
	}

	private void ShowRanking(ApiResult r)
	{
		int rank = r.GetInt("rank");
		string line = $"내 순위 {rank}위 / {r.GetInt("total_players")}명  남은 횟수 {r.GetInt("plays_remaining")}/3";
		line += $"\n최고점 {Comma(r.GetInt("best_score"))}";
		if (r.GetBool("is_personal_best")) line += "  (최고 기록)";
		_status.Text = line;

		foreach (Node child in _top10.GetChildren())
		{
			_top10.RemoveChild(child);
			child.QueueFree();
		}
		foreach (var e in r.GetArray("top"))
		{
			int rk = ApiResult.Num(e, "rank");
			AddTopRow(new[] { rk.ToString(), ApiResult.Str(e, "name"), ApiResult.Str(e, "department"), Comma(ApiResult.Num(e, "best_score")) }, rk == rank);
		}
	}

	private void AddTopRow(string[] cells, bool mine)
	{
		for (int i = 0; i < cells.Length; i++)
		{
			var label = new Label
			{
				Text = cells[i],
				CustomMinimumSize = new Vector2(TopColumns[i], 0),
				ClipText = true,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			label.AddThemeFontSizeOverride("font_size", 12);
			if (i == 0 || i == cells.Length - 1) label.HorizontalAlignment = HorizontalAlignment.Right;
			if (mine) label.Modulate = new Color(1f, 0.85f, 0.3f);
			_top10.AddChild(label);
		}
	}

	private void ShowTier(int tier)
	{
		string path = string.Format(GameData.ResultTierPath, tier);
		_tierText.Text = GameData.ResultTierTexts[tier - 1];
		_tierPic.Texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
		if (_tierPic.Texture == null) GD.PushWarning($"[결과] 그림을 읽지 못했습니다: {path}");
	}

	private void SetAction(string text, Action action)
	{
		_action.Text = text;
		_onAction = action;
		_action.Visible = true;
	}

	private void Restart()
	{
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	private void GoTitle()
	{
		GetTree().Paused = false;
		Session.Reset();
		GetTree().ChangeSceneToFile(Session.TitleScene);
	}

	private static string OreLine(int[] c)
	{
		var parts = new string[c.Length];
		for (int i = 0; i < c.Length; i++)
			parts[i] = $"{GameData.OreNames[i]} {c[i]}";
		return string.Join("  ", parts, 0, OresPerLine) + "\n" + string.Join("  ", parts, OresPerLine, parts.Length - OresPerLine);
	}

	private static string ComboLine(int[] c)
	{
		int best = 0;
		int mask = 0;
		foreach (var combo in GameData.Combos)
		{
			bool ok = true;
			for (int i = 0; i < c.Length; i++)
				if ((combo.Mask & (1 << i)) != 0 && c[i] < 1) ok = false;
			if (ok && combo.Tenths > best) { best = combo.Tenths; mask = combo.Mask; }
		}
		return mask == 0 ? "없음" : GameData.ComboName(mask);
	}

	private static string Comma(int n)
	{
		return n.ToString("N0", CultureInfo.InvariantCulture);
	}

	private static Label MakeWrapLabel(Node parent)
	{
		var label = MakeLabel(parent, Vector2.Zero, 12);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		return label;
	}

	private static Label MakeLabel(Node parent, Vector2 pos, int fontSize)
	{
		var label = new Label { Position = pos, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		parent.AddChild(label);
		return label;
	}
}
