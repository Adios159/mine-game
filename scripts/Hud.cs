// Hud: 아래 띠에 남은 시간, 점수, 배율, 광석 개수, 드릴 수를 보여 준다. 끝나면 결과 화면을 띄우고 점수를 서버에 제출해 순위를 보여 준다.
using Godot;
using System;
using System.Text;
using System.Threading.Tasks;

public partial class Hud : Node
{
	public RoundSystem Round;
	public ScoreSystem Score;
	public DrillController Drills;
	public DropSystem Drops;

	private Label _top;
	private Label _bottom;
	private ColorRect _result;
	private Label _summary;
	private Label _status;
	private Label _list;
	private Button _action;
	private Action _onAction;
	private ApiClient _api;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		_api = new ApiClient();
		AddChild(_api);

		_top = MakeLabel(this, new Vector2(4, 227), 12);
		_bottom = MakeLabel(this, new Vector2(4, 243), 12);

		_result = new ColorRect
		{
			Color = new Color(0f, 0f, 0f, 1f),
			Position = Vector2.Zero,
			Size = new Vector2(480, 270),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = false
		};
		AddChild(_result);

		_summary = MakeLabel(_result, new Vector2(10, 6), 12);
		_status = MakeLabel(_result, new Vector2(10, 50), 12);
		_list = MakeLabel(_result, new Vector2(10, 88), 12);

		_action = new Button { Position = new Vector2(350, 240), Size = new Vector2(120, 24), Visible = false };
		_action.AddThemeFontSizeOverride("font_size", 12);
		_action.Pressed += () => _onAction?.Invoke();
		_result.AddChild(_action);

		Round.Ended += OnEnded;
	}

	public override void _Process(double delta)
	{
		int sec = Mathf.CeilToInt(Round.Remaining);
		_top.Text = $"TIME {sec / 60}:{sec % 60:00}   SCORE {Score.Score()}   x{Score.BestMultiplier():0.0}";
		int[] c = Score.Counts;
		_bottom.Text = $"A {c[0]}  B {c[1]}  C {c[2]}  D {c[3]}  E {c[4]}   DRILL {Drills.DrillsLeft}   BOMB {(Drops.HasBomb ? 1 : 0)}";
	}

	private async void OnEnded()
	{
		int[] c = Score.Counts;
		int score = Score.Score();
		_summary.Text = $"점수 {score}   x{Score.BestMultiplier():0.0}\n합계 {Score.Sum()}   A{c[0]} B{c[1]} C{c[2]} D{c[3]} E{c[4]}";
		_result.Visible = true;
		GD.Print($"[결과] A{c[0]} B{c[1]} C{c[2]} D{c[3]} E{c[4]} | 합계 {Score.Sum()} | 배율 {Score.BestMultiplier():0.0} | 점수 {score}");

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
		string line = $"내 순위 {rank}위 / {r.GetInt("total_players")}명   최고점 {r.GetInt("best_score")}";
		if (r.GetBool("is_personal_best")) line += "  (최고 기록)";
		line += $"\n남은 횟수 {r.GetInt("plays_remaining")}/3";
		_status.Text = line;

		var sb = new StringBuilder();
		foreach (var e in r.GetArray("top"))
		{
			int rk = ApiResult.Num(e, "rank");
			string mark = rk == rank ? "> " : "  ";
			sb.AppendLine($"{mark}{rk,2}  {Cut(ApiResult.Str(e, "name"), 6)}  {Cut(ApiResult.Str(e, "department"), 10)}  {ApiResult.Num(e, "best_score")}");
		}
		_list.Text = sb.ToString();
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

	private static string Cut(string s, int n)
	{
		return s.Length <= n ? s : s.Substring(0, n);
	}

	private static Label MakeLabel(Node parent, Vector2 pos, int fontSize)
	{
		var label = new Label { Position = pos, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		parent.AddChild(label);
		return label;
	}
}
