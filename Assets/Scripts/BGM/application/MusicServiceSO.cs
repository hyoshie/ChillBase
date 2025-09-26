using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class MusicSnapshot
{
	public int CurrentIndex { get; }
	public bool IsPlaying { get; }
	public string Title { get; }
	public bool HasAnyTrack { get; }
	public PlayMode PlayMode { get; }

	public MusicSnapshot(int currentIndex, bool isPlaying, string title, bool hasAnyTrack, PlayMode playMode)
	{
		CurrentIndex = currentIndex;
		IsPlaying = isPlaying;
		Title = title;
		HasAnyTrack = hasAnyTrack;
		PlayMode = playMode;
	}
}

public interface IMusicService : IDisposable
{
	event Action<MusicSnapshot> OnChanged;

	PlayMode PlayMode { get; }
	TrackCatalog Catalog { get; }   // ← 追加（読み取り専用）

	void Initialize(bool autoPlayOnStart);
	void TogglePlay();
	void Next(bool autoPlayOverride = false);
	void Prev();
	void SelectTrack(int index, bool autoPlay = false);
	void CyclePlayMode();
	void JumpToLast5Seconds();
	void Tick(); // 毎フレーム呼ぶ
}

[CreateAssetMenu(menuName = "Music/MusicServiceSO")]
public class MusicServiceSO : ScriptableObject, IMusicService
{
	[Header("Config (Data)")]
	[SerializeField] private TrackCatalog catalog;
	public TrackCatalog Catalog => catalog;  //
	[SerializeField] private bool loopPlaylist = true;

	// シーン側のAudioSourceはSOに直シリアライズできないので、起動時にバインド
	[NonSerialized] private AudioSource _audioSource;

	// 状態（元クラスの内容をそのまま保持）
	private int _currentIndex = -1;
	private bool _isPlaying = false;
	public PlayMode PlayMode { get; private set; }

	private AsyncOperationHandle<AudioClip> _currentHandle;

	public event Action<MusicSnapshot> OnChanged;

	/// <summary>シーンのAudioSourceを渡してバインド。起動時に1回。</summary>
	public void BindRuntime(AudioSource audio)
	{
		_audioSource = audio;

		if (_audioSource)
		{
			_audioSource.loop = false;
			_audioSource.playOnAwake = false;
		}

		// 保存されたモードをロード
		PlayMode = PlayModeStore.Load();

		PushChanged();
	}

	// ------ IMusicService 実装（元のロジックをSOに移植） ------

	public void Initialize(bool autoPlayOnStart)
	{
		if (catalog && catalog.tracks.Length > 0)
		{
			SelectTrack(0, autoPlayOnStart);
		}
		else
		{
			PushChanged();
		}
	}

	public void TogglePlay()
	{
		if (!_audioSource || !_audioSource.clip) return;

		if (_isPlaying)
		{
			_audioSource.Pause();
			_isPlaying = false;
		}
		else
		{
			_audioSource.Play();
			_isPlaying = true;
		}

		PushChanged();
	}

	public void Next(bool autoPlayOverride = false)
	{
		if (catalog == null || catalog.tracks.Length == 0) return;
		int next = (_currentIndex + 1) % catalog.tracks.Length;
		SelectTrack(next, autoPlayOverride || _isPlaying);
	}

	public void Prev()
	{
		if (catalog == null || catalog.tracks.Length == 0) return;
		int prev = (_currentIndex - 1 + catalog.tracks.Length) % catalog.tracks.Length;
		SelectTrack(prev, _isPlaying);
	}

	public void SelectTrack(int index, bool autoPlay = false)
	{
		if (!_audioSource || catalog == null || index < 0 || index >= catalog.tracks.Length) return;

		// 以前のハンドル解放
		if (_currentHandle.IsValid())
			Addressables.Release(_currentHandle);

		_currentIndex = index;

		var entry = catalog.tracks[index];
		_currentHandle = entry.clip.LoadAssetAsync<AudioClip>();
		_currentHandle.Completed += op =>
		{
			if (op.Status == AsyncOperationStatus.Succeeded)
			{
				_audioSource.clip = op.Result;
				_audioSource.time = 0f;

				if (autoPlay)
				{
					_audioSource.Play();
					_isPlaying = true;
				}
				else
				{
					_audioSource.Pause();
					_isPlaying = false;
				}
			}
			else
			{
				Debug.LogError($"[MusicServiceSO] Failed to load {entry.displayName}");
			}

			PushChanged();
		};

		// ロード前でもタイトルは即時反映
		PushChanged();
	}

	public void CyclePlayMode()
	{
		var values = Enum.GetValues(typeof(PlayMode)).Cast<PlayMode>().ToArray();
		int idx = Array.IndexOf(values, PlayMode);
		int next = (idx + 1) % values.Length;
		PlayMode = values[next];

		PlayModeStore.Save(PlayMode);
		PushChanged();
	}

	public void JumpToLast5Seconds()
	{
		if (!_audioSource || !_audioSource.clip) return;

		var clip = _audioSource.clip;
		bool wasPlaying = _isPlaying || _audioSource.isPlaying;

		float backSeconds = (clip.length > 5f) ? 5f : 0.1f;
		int offsetSamples = Mathf.CeilToInt(clip.frequency * backSeconds);
		int targetSamples = Mathf.Clamp(clip.samples - offsetSamples, 0, clip.samples - 1);

		_audioSource.Stop();
		_audioSource.timeSamples = targetSamples;

		if (wasPlaying)
		{
			_audioSource.Play();
			_isPlaying = true;
		}

		PushChanged();
	}

	public void Tick()
	{
		if (!_audioSource || !_audioSource.clip) return;

		// 曲が終わったらモードごとの挙動
		if (_isPlaying && !_audioSource.isPlaying &&
				_audioSource.time >= _audioSource.clip.length - 0.05f)
		{
			HandleTrackFinished();
		}
	}

	private void HandleTrackFinished()
	{
		switch (PlayMode)
		{
			case PlayMode.RepeatOne:
				if (_currentIndex >= 0) SelectTrack(_currentIndex, true);
				else { _isPlaying = false; PushChanged(); }
				break;

			case PlayMode.Sequential:
			default:
				if (loopPlaylist) Next(true);
				else { _isPlaying = false; PushChanged(); }
				break;
		}
	}

	private void PushChanged()
	{
		string title = (catalog && _currentIndex >= 0 && _currentIndex < (catalog?.tracks.Length ?? 0))
				? catalog.tracks[_currentIndex].displayName
				: string.Empty;

		bool hasAny = catalog && catalog.tracks.Length > 0;

		OnChanged?.Invoke(new MusicSnapshot(
				_currentIndex,
				_isPlaying,
				title,
				hasAny,
				PlayMode
		));
	}

	public void Dispose()
	{
		if (_currentHandle.IsValid())
			Addressables.Release(_currentHandle);
		_currentIndex = -1;
		_isPlaying = false;
		_audioSource = null;
	}

#if UNITY_EDITOR
	// ドメインリロード時のリーク対策をしたいなら適宜
	private void OnDisable()
	{
		// Dispose(); // 必要なら有効化
	}
#endif
}
