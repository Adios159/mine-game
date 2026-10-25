// Title: 시작 화면. 서버 연결을 확인하고 학번, 이름, 학과를 받아 입력 확인과 판 시작을 처리한 뒤 게임 씬으로 넘어간다.
using Godot;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class Title : Control
{
	private ApiClient _api;
	private Label _server;
	private Label _message;
	private LineEdit _id;
	private LineEdit _name;
	private LineEdit _dept;
	private Button _retry;
	private Button _check;
	private Button _start;
	private bool _busy;
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

		var title = MakeLabel("광물 채굴 게임", new Vector2(0, 4), 20);
		title.Size = new Vector2(480, 26);
		title.HorizontalAlignment = HorizontalAlignment.Center;

		_server = MakeLabel("", new Vector2(10, 36), 12);
		_retry = MakeButton("다시 시도", new Vector2(380, 32), new Vector2(90, 22));
		_retry.Visible = false;

		MakeLabel("학번", new Vector2(60, 64), 14);
		MakeLabel("이름", new Vector2(60, 94), 14);
		MakeLabel("학과", new Vector2(60, 124), 14);
		_id = MakeInput("숫자 8자리", new Vector2(120, 60), 8);
		_name = MakeInput("이름", new Vector2(120, 90), 30);
		_dept = MakeInput("학과", new Vector2(120, 120), 50);

		_check = MakeButton("입력 확인", new Vector2(120, 156), new Vector2(110, 24));
		_start = MakeButton("게임 시작", new Vector2(250, 156), new Vector2(110, 24));
		_message = MakeLabel("", new Vector2(40, 190), 14);
		_message.Size = new Vector2(400, 70);
		_message.HorizontalAlignment = HorizontalAlignment.Center;
		_message.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		_retry.Pressed += () => { _ = CheckServer(); };
		_check.Pressed += OnCheck;
		_start.Pressed += OnStart;
		_id.TextChanged += OnEdited;
		_name.TextChanged += OnEdited;
		_dept.TextChanged += OnEdited;
		_id.TextSubmitted += _ => _name.GrabFocus();
		_name.TextSubmitted += _ => _dept.GrabFocus();
		_dept.TextSubmitted += _ => OnCheck();

		_ = CheckServer();
	}

	private async Task CheckServer()
	{
		_serverOk = false;
		_retry.Visible = false;
		SetInputsEnabled(false);
		_server.Text = "서버 연결 확인 중... (처음에는 1분쯤 걸릴 수 있습니다)";

		ApiResult r = await _api.Health();
		if (r.Success)
		{
			_serverOk = true;
			_server.Text = "서버 연결됨";
			SetInputsEnabled(true);
			_id.GrabFocus();
			return;
		}

		GD.Print("[서버] 연결 실패: ", Session.BaseUrl);
		_server.Text = "서버에 연결할 수 없습니다.";
		_retry.Visible = true;
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

	private Label MakeLabel(string text, Vector2 pos, int fontSize)
	{
		var label = new Label { Text = text, Position = pos, MouseFilter = MouseFilterEnum.Ignore };
		label.AddThemeFontSizeOverride("font_size", fontSize);
		AddChild(label);
		return label;
	}

	private LineEdit MakeInput(string placeholder, Vector2 pos, int maxLength)
	{
		var edit = new LineEdit { PlaceholderText = placeholder, Position = pos, Size = new Vector2(240, 26), MaxLength = maxLength };
		edit.AddThemeFontSizeOverride("font_size", 14);
		AddChild(edit);
		return edit;
	}

	private Button MakeButton(string text, Vector2 pos, Vector2 size)
	{
		var button = new Button { Text = text, Position = pos, Size = size };
		button.AddThemeFontSizeOverride("font_size", 14);
		AddChild(button);
		return button;
	}
}
