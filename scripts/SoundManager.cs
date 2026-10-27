// SoundManager.cs - 효과음 재생 오토로드. 플레이어 풀로 같은 소리를 겹쳐 재생하고, Play가 돌려준 번호로 그 소리만 멈추거나 이름으로 같은 소리를 모두 멈춘다.
using Godot;
using System.Collections.Generic;

public partial class SoundManager : Node
{
	private const string SfxDir = "res://assets/audio/effect/";
	private const string SfxExt = ".mp3";
	private const string SfxBus = "SFX";
	private const int PoolSize = 8;

	public static SoundManager Instance { get; private set; }

	private readonly List<AudioStreamPlayer> _pool = new();
	private readonly Dictionary<string, AudioStream> _cache = new();
	private readonly Dictionary<AudioStreamPlayer, int> _playIds = new();
	private int _nextId;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		for (int i = 0; i < PoolSize; i++)
		{
			var player = new AudioStreamPlayer { Bus = SfxBus };
			AddChild(player);
			_pool.Add(player);
		}
	}

	public int Play(string name, float volumeDb = 0f)
	{
		AudioStream stream = GetStream(name);
		if (stream == null) return 0;

		foreach (AudioStreamPlayer player in _pool)
		{
			if (player.Playing) continue;
			player.Stream = stream;
			player.VolumeDb = volumeDb;
			player.Play();
			_playIds[player] = ++_nextId;
			return _nextId;
		}
		return 0;
	}

	public void Stop(int playId)
	{
		if (playId == 0) return;
		foreach (AudioStreamPlayer player in _pool)
		{
			if (_playIds.TryGetValue(player, out int id) && id == playId)
			{
				player.Stop();
				return;
			}
		}
	}

	public void StopAll(string name)
	{
		if (!_cache.TryGetValue(name, out AudioStream stream)) return;
		foreach (AudioStreamPlayer player in _pool)
			if (player.Playing && player.Stream == stream) player.Stop();
	}

	private AudioStream GetStream(string name)
	{
		if (_cache.TryGetValue(name, out AudioStream cached)) return cached;

		string path = SfxDir + name + SfxExt;
		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning($"SoundManager: 효과음을 찾을 수 없음 - {path}");
			return null;
		}

		AudioStream stream = GD.Load<AudioStream>(path);
		_cache[name] = stream;
		return stream;
	}
}
