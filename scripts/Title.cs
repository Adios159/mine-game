using Godot;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class Title : Control
{
	private const string HelpText = @"[목표]
2분 안에 광석을 최대한 많이, 좋은 조합으로 모아 점수를 겨룹니다.

[조작]
WASD: 이동
마우스: 시야(부채꼴)가 마우스 방향으로 돕니다.
좌클릭: 마우스가 가리키는 돌덩이에 드릴 붙이기 (가까이 있을 때만)
우클릭: 폭탄 설치 (폭탄이 있을 때, 5초 뒤 폭발)

[채굴]
드릴은 3개로 시작합니다. 돌덩이 하나에는 드릴을 하나만 붙일 수 있고, 5초마다 돌 1개가 광석이 됩니다.
광석은 바닥에 떨어지니 직접 가서 주워야 점수가 됩니다. 끝난 드릴도 주워야 다시 쓸 수 있습니다.

[광석 가치]
A 5,000 / B 3,000 / C 1,000 / D 500 / E 250

[해금] (광석을 주운 개수 기준)
A 1개: 이후 붙이는 드릴이 25% 빨라집니다.
B 2개: 이동 속도가 빨라집니다.
C 5개: 드릴이 2개 늘어납니다.
D 7개: 시야가 넓어집니다.

[아이템]
폭탄: 설치한 돌덩이가 터지며 돌이 한꺼번에 나옵니다.
자석: 얻는 즉시 바닥의 광석이 모두 끌려옵니다.

[점수]
점수 = 가장 높은 조합 배율 x (광석 개수 x 가치의 합)
D+E x1.4 / C+D x1.6 / B+C+D+E x2.6 / A+C+D+E x3.0 / A+B+C+E x3.3
조합에 들어간 광석을 각각 1개 이상 주워야 달성됩니다.

[랭킹]
학번당 최대 3번 플레이할 수 있고 가장 높은 점수만 순위에 반영됩니다.
중간에 끈 판도 횟수가 차감됩니다. 점수가 같으면 먼저 달성한 사람이 위입니다.";

	private static readonly int[] RankColumns = { 36, 80, 150, 70, 80 };

	private ApiClient _api;

	private Control _main;
	private Control _entry;
	private Control _rank;
	private Control _help;

	private Label _server;
	private Button _retry;
	private Button _startMain;

	private Label _message;
	private LineEdit _id;
	private LineEdit _name;
	private LineEdit _dept;
	private Button _check;
	private Button _start;

	private LineEdit _rankId;
	private Button _rankLoad;
	private Label _rankStatus;
	private ScrollContainer _rankScroll;
	private GridContainer _rankGrid;

	private bool _busy;
	private bool _rankBusy;
	private bool _serverOk;
	private string _pendingRequestId;

	public override void _Ready()
	{
		GetTree().Paused = false;
		Session.Reset();
		Session.LoadConfig();
		RenderingServer.SetDefaultClearColor(new Color(0.1f, 0.1f, 0.14f));

		_api = new ApiClient();
		AddChild(_api);

		BuildMain();
		BuildEntry();
		BuildRank();
		BuildHelp();
		ShowPanel(_main);

		_ = CheckServer();
	}

	private void BuildMain()
	{
		_main = MakePanel();

		var title = MakeLabel(_main, "광물 채굴 게임", new Vector2(0, 36), 24);
		title.Size = new Vector2(480, 32);
		title.HorizontalAlignment = HorizontalAlignment.Center;

		_startMain = MakeButton(_main, "게임 시작", new Vector2(170, 92), new Vector2(140, 28));
		var rank = MakeButton(_main, "랭킹 보기", new Vector2(170, 128), new Vector2(140, 28));
		var help = MakeButton(_main, "게임 설명", new Vector2(170, 164), new Vector2(140, 28));
		_startMain.Disabled = true;

		_server = MakeLabel(_main, "", new Vector2(10, 250), 12);
		_retry = MakeButton(_main, "다시 시도", new Vector2(380, 244), new Vector2(90, 22));
		_retry.Visible = false;

		_startMain.Pressed += OpenEntry;
		rank.Pressed += OpenRank;
		help.Pressed += () => ShowPanel(_help);
		_retry.Pressed += () => { _ = CheckServer(); };
	}

	private void BuildEntry()
	{
		_entry = MakePanel();

		var title = MakeLabel(_entry, "플레이어 정보 입력", new Vector2(0, 14), 18);
		title.Size = new Vector2(480, 24);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		var back = MakeButton(_entry, "뒤로", new Vector2(10, 4), new Vector2(60, 22));

		MakeLabel(_entry, "학번", new Vector2(60, 64), 14);
		MakeLabel(_entry, "이름", new Vector2(60, 94), 14);
		MakeLabel(_entry, "학과", new Vector2(60, 124), 14);
		_id = MakeInput(_entry, "숫자 8자리", new Vector2(120, 60), new Vector2(240, 26), 8);
		_name = MakeInput(_entry, "이름", new Vector2(120, 90), new Vector2(240, 26), 30);
		_dept = MakeInput(_entry, "학과", new Vector2(120, 120), new Vector2(240, 26), 50);

		_check = MakeButton(_entry, "입력 확인", new Vector2(120, 156), new Vector2(110, 24));
		_start = MakeButton(_entry, "게임 시작", new Vector2(250, 156), new Vector2(110, 24));
		_start.Disabled = true;
		_message = MakeLabel(_entry, "", new Vector2(40, 190), 14);
		_message.Size = new Vector2(400, 70);
		_message.HorizontalAlignment = HorizontalAlignment.Center;
		_message.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		back.Pressed += () => ShowPanel(_main);
		_check.Pressed += OnCheck;
		_start.Pressed += OnStart;
		_id.TextChanged += OnEdited;
		_name.TextChanged += OnEdited;
		_dept.TextChanged += OnEdited;
		_id.TextSubmitted += _ => _name.GrabFocus();
		_name.TextSubmitted += _ => _dept.GrabFocus();
		_dept.TextSubmitted += _ => OnCheck();
	}

	private void BuildRank()
	{
		_rank = MakePanel();

		var title = MakeLabel(_rank, "랭킹", new Vector2(0, 5), 16);
		title.Size = new Vector2(480, 22);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		var back = MakeButton(_rank, "뒤로", new Vector2(10, 4), new Vector2(60, 22));

		MakeLabel(_rank, "학번", new Vector2(10, 32), 12);
		_rankId = MakeInput(_rank, "내 순위를 보려면 학번 입력", new Vector2(44, 28), new Vector2(190, 22), 8);
		_rankLoad = MakeButton(_rank, "조회", new Vector2(240, 28), new Vector2(60, 22));

		var header = new GridContainer { Columns = RankColumns.Length, Position = new Vector2(10, 54) };
		header.AddThemeConstantOverride("h_separation", 6);
		_rank.AddChild(header);
		AddRankRow(header, new[] { "순위", "이름", "학과", "학번", "점수" }, false);

		_rankScroll = new ScrollContainer
		{
			Position = new Vector2(10, 70),
			Size = new Vector2(460, 160),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		_rankGrid = new GridContainer { Columns = RankColumns.Length };
		_rankGrid.AddThemeConstantOverride("h_separation", 6);
		_rankGrid.AddThemeConstantOverride("v_separation", 2);
		_rankScroll.AddChild(_rankGrid);
		_rank.AddChild(_rankScroll);

		_rankStatus = MakeLabel(_rank, "", new Vector2(10, 232), 11);
		_rankStatus.Size = new Vector2(460, 36);
		_rankStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		back.Pressed += () => ShowPanel(_main);
		_rankLoad.Pressed += () => { _ = LoadRanking(); };
		_rankId.TextSubmitted += text => { _ = LoadRanking(); };
	}

	private void BuildHelp()
	{
		_help = MakePanel();

		var title = MakeLabel(_help, "게임 설명", new Vector2(0, 5), 16);
		title.Size = new Vector2(480, 22);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		var back = MakeButton(_help, "뒤로", new Vector2(10, 4), new Vector2(60, 22));
		back.Pressed += () => ShowPanel(_main);

		var scroll = new ScrollContainer
		{
			Position = new Vector2(10, 32),
			Size = new Vector2(460, 230),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
		};
		var text = new Label
		{
			Text = HelpText,
			CustomMinimumSize = new Vector2(440, 0),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore
		};
		text.AddThemeFontSizeOverride("font_size", 12);
		scroll.AddChild(text);
		_help.AddChild(scroll);
	}

	private void ShowPanel(Control panel)
	{
		_main.Visible = panel == _main;
		_entry.Visible = panel == _entry;
		_rank.Visible = panel == _rank;
		_help.Visible = panel == _help;
	}

	private void OpenEntry()
	{
		if (!_serverOk) return;
		ShowPanel(_entry);
		_id.GrabFocus();
	}

	private void OpenRank()
	{
		ShowPanel(_rank);
		_ = LoadRanking();
	}

	private async Task CheckServer()
	{
		_serverOk = false;
		_retry.Visible = false;
		_startMain.Disabled = true;
		SetInputsEnabled(false);
		_server.Text = "서버 연결 확인 중... (처음에는 1분쯤 걸릴 수 있습니다)";

		ApiResult r = await _api.Health();
		if (r.Success)
		{
			_serverOk = true;
			_server.Text = "서버 연결됨";
			_startMain.Disabled = false;
			SetInputsEnabled(true);
			return;
		}

		GD.Print("[서버] 연결 실패: ", Session.BaseUrl);
		_server.Text = "서버에 연결할 수 없습니다.";
		_retry.Visible = true;
	}

	private async Task LoadRanking()
	{
		if (_rankBusy) return;

		string id = _rankId.Text.Trim();
		if (id.Length > 0 && !Regex.IsMatch(id, "^[0-9]{8}$"))
		{
			_rankStatus.Text = "학번은 숫자 8자리로 입력해 주세요.";
			return;
		}

		_rankBusy = true;
		_rankLoad.Disabled = true;
		_rankStatus.Text = "순위를 불러오는 중... (처음에는 1분쯤 걸릴 수 있습니다)";
		ApiResult r = await _api.Ranking(id.Length > 0 ? id : null);
		_rankBusy = false;
		_rankLoad.Disabled = false;

		if (!r.Success)
		{
			_rankStatus.Text = r.Message() + "\n조회 버튼으로 다시 시도해 주세요.";
			return;
		}
		ShowRankList(r, id.Length > 0);
	}

	private void ShowRankList(ApiResult r, bool askedMe)
	{
		foreach (Node child in _rankGrid.GetChildren())
		{
			_rankGrid.RemoveChild(child);
			child.QueueFree();
		}

		int myRank = 0;
		int myBest = 0;
		JsonElement me = r.GetObject("me");
		if (me.ValueKind == JsonValueKind.Object)
		{
			myRank = ApiResult.Num(me, "rank");
			myBest = ApiResult.Num(me, "best_score");
		}

		int shown = 0;
		foreach (JsonElement e in r.GetArray("top"))
		{
			int rank = ApiResult.Num(e, "rank");
			AddRankRow(_rankGrid, new[]
			{
				rank.ToString(),
				ApiResult.Str(e, "name"),
				ApiResult.Str(e, "department"),
				ApiResult.Str(e, "student_id_masked"),
				Comma(ApiResult.Num(e, "best_score"))
			}, myRank > 0 && rank == myRank);
			shown++;
		}
		_rankScroll.ScrollVertical = 0;

		int total = r.GetInt("total_players");
		if (total == 0)
		{
			_rankStatus.Text = "아직 순위가 없습니다.";
			return;
		}

		string line = $"총 {total}명 참가";
		if (shown < total) line += $"  (상위 {shown}명 표시)";
		if (myRank > 0) line += $"\n내 순위 {myRank}위   최고점 {Comma(myBest)}";
		else if (askedMe) line += "\n입력한 학번의 순위가 없습니다. 점수를 기록한 판이 있어야 순위가 생깁니다.";
		_rankStatus.Text = line;
	}

	private void AddRankRow(GridContainer grid, string[] cells, bool mine)
	{
		for (int i = 0; i < cells.Length; i++)
		{
			var label = new Label
			{
				Text = cells[i],
				CustomMinimumSize = new Vector2(RankColumns[i], 0),
				ClipText = true,
				MouseFilter = MouseFilterEnum.Ignore
			};
			label.AddThemeFontSizeOverride("font_size", 11);
			if (i == 0 || i == 4) label.HorizontalAlignment = HorizontalAlignment.Right;
			if (mine) label.Modulate = new Color(1f, 0.85f, 0.3f);
			grid.AddChild(label);
		}
	}

	private async void OnCheck()
	{
		if (_busy || !_serverOk) return;
		if (!TryRead(out string id, out string name, out string dept)) return;

		SetBusy(true, "확인 중...");
		ApiResult r = await _api.Check(id, name, dept);
		SetBusy(false);

		if (!r.Success)
		{
			_message.Text = r.Message();
			return;
		}

		int remaining = r.GetInt("plays_remaining");
		string line = r.GetBool("registered") ? "" : "처음 등록하는 학번입니다.\n";
		line += $"남은 횟수 {remaining}/3";
		if (r.HasValue("best_score"))
			line += $"\n최고점 {r.GetInt("best_score")}   현재 순위 {r.GetInt("rank")}위";
		if (remaining <= 0) line += "\n플레이 횟수를 모두 사용했습니다.";
		_message.Text = line;
		_start.Disabled = remaining <= 0;
		if (remaining > 0) _start.GrabFocus();
	}

	private async void OnStart()
	{
		if (_busy || !_serverOk) return;
		if (!TryRead(out string id, out string name, out string dept)) return;

		_pendingRequestId ??= Guid.NewGuid().ToString();
		SetBusy(true, "게임을 시작하는 중...");
		ApiResult r = await _api.StartPlay(id, name, dept, _pendingRequestId);
		SetBusy(false);

		if (r.Success)
		{
			Session.PlayId = r.GetString("play_id");
			Session.PlayToken = r.GetString("play_token");
			Session.PlayNo = r.GetInt("play_no");
			Session.PlaysRemaining = r.GetInt("plays_remaining");
			_pendingRequestId = null;

			if (!ResourceLoader.Exists(Session.GameScene))
			{
				_message.Text = "게임 씬 경로를 찾을 수 없습니다: " + Session.GameScene;
				return;
			}
			GetTree().ChangeSceneToFile(Session.GameScene);
			return;
		}

		if (!r.Retryable) _pendingRequestId = null;
		_message.Text = r.Message() + (r.Retryable ? "\n게임 시작을 다시 눌러 주세요." : "");
		if (r.ErrorCode == "PLAY_LIMIT_REACHED") _start.Disabled = true;
	}

	private void OnEdited(string text)
	{
		_pendingRequestId = null;
		_start.Disabled = true;
		_message.Text = "";
	}

	private bool TryRead(out string id, out string name, out string dept)
	{
		id = _id.Text.Trim().Normalize();
		name = _name.Text.Trim().Normalize();
		dept = _dept.Text.Trim().Normalize();

		if (!Regex.IsMatch(id, "^[0-9]{8}$")) { _message.Text = "학번은 숫자 8자리로 입력해 주세요."; return false; }
		if (name.Length < 1 || name.Length > 30) { _message.Text = "이름은 1~30자로 입력해 주세요."; return false; }
		if (dept.Length < 1 || dept.Length > 50) { _message.Text = "학과는 1~50자로 입력해 주세요."; return false; }
		return true;
	}

	private void SetInputsEnabled(bool on)
	{
		_id.Editable = on;
		_name.Editable = on;
		_dept.Editable = on;
		_check.Disabled = !on;
		_start.Disabled = true;
	}

	private void SetBusy(bool busy, string message = null)
	{
		_busy = busy;
		_id.Editable = !busy && _serverOk;
		_name.Editable = !busy && _serverOk;
		_dept.Editable = !busy && _serverOk;
		_check.Disabled = busy || !_serverOk;
		if (busy)
		{
			_start.Disabled = true;
			_message.Text = message ?? "";
		}
	}

	private static string Comma(int n)
	{
		return n.ToString("N0", CultureInfo.InvariantCulture);
	}

	private Control MakePanel()
	{
		var panel = new Control
		{
			Position = Vector2.Zero,
			Size = new Vector2(480, 270),
			MouseFilter = MouseFilterEnum.Ignore,
			Visible = false
		};
		AddChild(panel);
		return panel;
	}

	private static Label MakeLabel(Control parent, string text, Vector2 pos, int fontSize)
	{
		var label = new Label { Text = text, Position = pos, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		parent.AddChild(label);
		return label;
	}

	private static LineEdit MakeInput(Control parent, string placeholder, Vector2 pos, Vector2 size, int maxLength)
	{
		var edit = new LineEdit { PlaceholderText = placeholder, Position = pos, Size = size, MaxLength = maxLength };
		edit.AddThemeFontSizeOverride("font_size", 12);
		parent.AddChild(edit);
		return edit;
	}

	private static Button MakeButton(Control parent, string text, Vector2 pos, Vector2 size)
	{
		var button = new Button { Text = text, Position = pos, Size = size };
		button.AddThemeFontSizeOverride("font_size", 14);
		parent.AddChild(button);
		return button;
	}
}