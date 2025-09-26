
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(menuName = "Music/MusicServiceSO")]
public class MusicServiceSO : ScriptableObject, IMusicService
{
	[Header("Config (Data)")]
	[SerializeField] private TrackCatalogSet catalogSet;       // 複数カタログ前提
	[SerializeField] private bool loopPlaylist = true;

	[NonSerialized] private AudioSource _audioSource;

	private int _currentCatalogIndex = 0;
	private int _currentIndex = -1;
	private bool _isPlaying = false;

	public event Action<MusicSnapshot> OnChanged;
	public PlayMode PlayMode { get; private set; }
	public TrackCatalogSet CatalogSet => catalogSet;
	public int CurrentCatalogIndex => _currentCatalogIndex;

	private AsyncOperationHandle<AudioClip> _currentHandle;

	// ========= ランタイム・セットアップ =========

	public void BindRuntime(AudioSource audio)
	{
		_audioSource = audio;
		if (_audioSource)
		{
			_audioSource.loop = false;
			_audioSource.playOnAwake = false;
		}
		PlayMode = PlayModeStore.Load();
		PushChanged();
	}

	public void Initialize(bool autoPlayOnStart)
	{
		// 最初の非空カタログに合わせる
		int first = catalogSet?.FindNextNonEmptyCatalog(-1, +1) ?? -1;
		if (first >= 0)
		{
			_currentCatalogIndex = first;
			SelectTrack(_currentCatalogIndex, 0, autoPlayOnStart);
		}
		else
		{
			_currentIndex = -1;
			_isPlaying = false;
			PushChanged();
		}
	}

	// ========= 再生制御 =========

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
		if (!(catalogSet?.TryGetNextAcross(_currentCatalogIndex, _currentIndex, out int nextCat, out int nextTrack) ?? false))
		{
			if (loopPlaylist)
			{
				int first = catalogSet.FindNextNonEmptyCatalog(-1, +1);
				if (first >= 0) SelectTrack(first, 0, autoPlayOverride || _isPlaying);
				else { _isPlaying = false; PushChanged(); }
			}
			else { _isPlaying = false; PushChanged(); }
			return;
		}
		SelectTrack(nextCat, nextTrack, autoPlayOverride || _isPlaying);
	}

	public void Prev()
	{
		if (!(catalogSet?.TryGetPrevAcross(_currentCatalogIndex, _currentIndex, out int prevCat, out int prevTrack) ?? false))
		{
			if (loopPlaylist && catalogSet.TryGetLastNonEmpty(out int lastCat))
			{
				var c = catalogSet.GetCatalog(lastCat);
				int t = Mathf.Max(0, (c?.tracks?.Length ?? 1) - 1);
				SelectTrack(lastCat, t, _isPlaying);
			}
			else { _isPlaying = false; PushChanged(); }
			return;
		}

		SelectTrack(prevCat, prevTrack, _isPlaying);
	}

	public void SelectTrack(int catalogIndex, int trackIndex, bool autoPlay = false)
	{
		if (catalogSet == null || catalogSet.catalogs == null || catalogSet.catalogs.Count == 0) return;
		if (catalogIndex < 0 || catalogIndex >= catalogSet.catalogs.Count) return;

		var cat = catalogSet.GetCatalog(catalogIndex);
		if (cat == null || cat.tracks == null || cat.tracks.Length == 0) return;
		if (trackIndex < 0 || trackIndex >= cat.tracks.Length) return;

		if (_currentHandle.IsValid())
			Addressables.Release(_currentHandle);

		_currentCatalogIndex = catalogIndex;
		_currentIndex = trackIndex;

		var entry = cat.tracks[trackIndex];
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

		// ロード前でも状態を即時通知
		PushChanged();
	}

	public void SetCatalog(int catalogIndex, bool keepTrackIndex = false, bool autoPlay = false)
	{
		if (catalogSet == null || catalogSet.catalogs == null || catalogSet.catalogs.Count == 0) return;
		catalogIndex = Mathf.Clamp(catalogIndex, 0, catalogSet.catalogs.Count - 1);
		var cat = catalogSet.GetCatalog(catalogIndex);
		if (cat == null || cat.tracks == null || cat.tracks.Length == 0) return;

		int nextTrack = keepTrackIndex
			? Mathf.Clamp(_currentIndex, 0, cat.tracks.Length - 1)
			: 0;

		SelectTrack(catalogIndex, nextTrack, autoPlay);
	}

	public void CyclePlayMode()
	{
		var values = Enum.GetValues(typeof(PlayMode)).Cast<PlayMode>().ToArray();
		int idx = Array.IndexOf(values, PlayMode);
		PlayMode = values[(idx + 1) % values.Length];
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
				if (_currentIndex >= 0) SelectTrack(_currentCatalogIndex, _currentIndex, true);
				else { _isPlaying = false; PushChanged(); }
				break;

			case PlayMode.Sequential:
			default:
				// カタログ横断のNext
				Next(true);
				break;
		}
	}

	// ========= 内部ユーティリティ =========
	private void PushChanged()
	{
		var cat = catalogSet.GetCatalog(_currentCatalogIndex);
		string title = (cat != null && _currentIndex >= 0 && _currentIndex < (cat.tracks?.Length ?? 0))
			? cat.tracks[_currentIndex].displayName
			: string.Empty;

		bool hasAny = cat != null && cat.tracks != null && cat.tracks.Length > 0;
		int trackCount = cat?.tracks?.Length ?? 0;

		OnChanged?.Invoke(new MusicSnapshot(
			_currentIndex,
			_isPlaying,
			title,
			hasAny,
			PlayMode,
			_currentCatalogIndex,
			catalogSet.GetDisplayName(_currentCatalogIndex),
			trackCount
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
}
