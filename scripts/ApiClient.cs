// ApiClient: 랭킹 서버에 JSON 요청을 보낸다. 요청마다 5초 제한을 두고, 연결 실패와 5xx는 최대 2번 다시 보낸다.
using Godot;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiResult
{
	public bool Connected;
	public int Status;
	public JsonElement Body;
	public string ErrorCode = "";

	public bool Success => Connected && Status >= 200 && Status < 300;
	public bool Retryable => !Connected || Status >= 500;

	public static string Str(JsonElement e, string name)
	{
		return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String
			? v.GetString() : "";
	}

	public static int Num(JsonElement e, string name)
	{
		return e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number
			? v.GetInt32() : 0;
	}

	public bool HasValue(string name)
	{
		return Body.ValueKind == JsonValueKind.Object
			&& Body.TryGetProperty(name, out JsonElement v)
			&& v.ValueKind != JsonValueKind.Null;
	}

	public int GetInt(string name) { return Num(Body, name); }

	public string GetString(string name) { return Str(Body, name); }

	public bool GetBool(string name)
	{
		return Body.ValueKind == JsonValueKind.Object
			&& Body.TryGetProperty(name, out JsonElement v)
			&& v.ValueKind == JsonValueKind.True;
	}

	public JsonElement GetObject(string name)
	{
		return Body.ValueKind == JsonValueKind.Object
			&& Body.TryGetProperty(name, out JsonElement v)
			&& v.ValueKind == JsonValueKind.Object
			? v : default;
	}

	public List<JsonElement> GetArray(string name)
	{
		var list = new List<JsonElement>();
		if (Body.ValueKind == JsonValueKind.Object
			&& Body.TryGetProperty(name, out JsonElement v)
			&& v.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement e in v.EnumerateArray()) list.Add(e);
		}
		return list;
	}

	public string Message()
	{
		if (!Connected) return "서버에 연결할 수 없습니다.";
		switch (ErrorCode)
		{
			case "INVALID_INPUT": return "입력 형식을 확인해 주세요.";
			case "PLAYER_INFO_MISMATCH": return "이미 등록된 학번이며 이름 또는 학과가 다릅니다.";
			case "PLAY_LIMIT_REACHED": return "플레이 횟수를 모두 사용했습니다.";
			case "PLAY_NOT_FOUND":
			case "INVALID_TOKEN":
			case "PLAY_ALREADY_FINISHED": return "판 정보 오류로 점수를 기록하지 못했습니다.";
			case "SCORE_INVALID":
			case "TOO_EARLY": return "점수 검증에 실패해 기록하지 못했습니다.";
		}
		if (Status >= 500) return "서버 오류가 발생했습니다.";
		return "알 수 없는 오류입니다. (" + Status + " " + ErrorCode + ")";
	}
}

public partial class ApiClient : Node
{
	private const float TimeoutSeconds = 5f;
	private const int Attempts = 3;

	public Task<ApiResult> Health()
	{
		return Send(Godot.HttpClient.Method.Get, "/health", null, null, 15f, 75f);
	}

	public Task<ApiResult> Ranking(string id = null)
	{
		string path = "/ranking?limit=500";
		if (!string.IsNullOrEmpty(id)) path += "&student_id=" + id;
		return Send(Godot.HttpClient.Method.Get, path, null, null, 15f, 60f);
	}

	public Task<ApiResult> Check(string id, string name, string dept)
	{
		return Send(Godot.HttpClient.Method.Post, "/players/check",
			new { student_id = id, name = name, department = dept });
	}

	public Task<ApiResult> StartPlay(string id, string name, string dept, string requestId)
	{
		return Send(Godot.HttpClient.Method.Post, "/plays",
			new { student_id = id, name = name, department = dept, client_request_id = requestId, client_version = Session.ClientVersion });
	}

	public Task<ApiResult> Finish(string playId, string token, int score, int[] counts)
	{
		var ore = new Dictionary<string, int>
		{
			{ "A", counts[0] }, { "B", counts[1] }, { "C", counts[2] }, { "D", counts[3] }, { "E", counts[4] }
		};
		return Send(Godot.HttpClient.Method.Post, "/plays/" + playId + "/finish",
			new { score = score, ore_counts = ore, client_version = Session.ClientVersion }, token);
	}

	private async Task<ApiResult> Send(Godot.HttpClient.Method method, string path, object body = null, string token = null, float timeout = TimeoutSeconds, float patience = 0f)
	{
		string json = body == null ? "" : JsonSerializer.Serialize(body);
		var headers = new List<string> { "Content-Type: application/json" };
		if (token != null) headers.Add("X-Play-Token: " + token);

		ulong begin = Godot.Time.GetTicksMsec();
		var result = new ApiResult();
		for (int attempt = 0; ; attempt++)
		{
			result = await Once(method, path, json, headers.ToArray(), timeout);
			if (!result.Retryable) return result;

			bool more = patience > 0f
				? (Godot.Time.GetTicksMsec() - begin) / 1000f < patience
				: attempt < Attempts - 1;
			if (!more) break;
			await ToSignal(GetTree().CreateTimer(patience > 0f ? 3.0 : 0.5), SceneTreeTimer.SignalName.Timeout);
		}
		GD.Print("[API] 실패 ", path, " status=", result.Status);
		return result;
	}

	private async Task<ApiResult> Once(Godot.HttpClient.Method method, string path, string json, string[] headers, float timeout)
	{
		var result = new ApiResult();
		var http = new HttpRequest { Timeout = timeout };
		AddChild(http);

		Error err = http.Request(Session.BaseUrl + path, headers, method, json);
		if (err != Error.Ok)
		{
			http.QueueFree();
			return result;
		}

		Variant[] r = await ToSignal(http, HttpRequest.SignalName.RequestCompleted);
		http.QueueFree();

		if (r[0].AsInt32() != (int)HttpRequest.Result.Success) return result;

		result.Connected = true;
		result.Status = r[1].AsInt32();
		string text = Encoding.UTF8.GetString(r[3].AsByteArray());
		if (text.Length > 0)
		{
			try
			{
				using JsonDocument doc = JsonDocument.Parse(text);
				result.Body = doc.RootElement.Clone();
			}
			catch (JsonException) { }
		}
		if (result.Body.ValueKind == JsonValueKind.Object && result.Body.TryGetProperty("error", out JsonElement e))
			result.ErrorCode = ApiResult.Str(e, "code");
		return result;
	}
}