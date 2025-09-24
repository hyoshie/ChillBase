using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

public class MusicController : MonoBehaviour
{
	[Header("Audio")]
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private TrackCatalog catalog;

	[Header("UI - Controls")]
	[SerializeField] private Button prevButton;
	[SerializeField] private Button playPauseButton;
	[SerializeField] private Button nextButton;

	[Header("UI - PlayMode")]
	[SerializeField] private Button playModeButton;   // 再生モード切替
	[SerializeField] private Image playModeIcon;      // アイコン表示
	[SerializeField] private Sprite sequentialSprite; // 順番再生アイコン
	[SerializeField] private Sprite repeatOneSprite;  // 一曲リピートアイコン

	[Header("UI - Display")]
	[SerializeField] private TextMeshProUGUI titleText;
	[SerializeField] private Image playPauseIcon;
	[SerializeField] private Sprite playSprite;
	[SerializeField] private Sprite pauseSprite;

	[SerializeField] private Button openSelectionPanelButton;
	[SerializeField] private MusicSelectPanelController selectionPanel;

	[Header("UI - Debug")]
	[SerializeField] private Button jumpLast5SecButton;

	[Header("Options")]
	[SerializeField] private bool autoPlayOnStart = false;
	[SerializeField] private bool loopPlaylist = true; // 順番再生のときのみ利用

	private int currentIndex = -1;
	private bool isPlaying = false;
	private PlayMode playMode;
	private AsyncOperationHandle<AudioClip> currentHandle;

	private void Awake()
	{
		if (prevButton) prevButton.onClick.AddListener(Prev);
		if (playPauseButton) playPauseButton.onClick.AddListener(TogglePlay);
		if (nextButton) nextButton.onClick.AddListener(Next);
		if (playModeButton) playModeButton.onClick.AddListener(CyclePlayMode);
		if (jumpLast5SecButton) jumpLast5SecButton.onClick.AddListener(JumpToLast5Seconds);
		if (openSelectionPanelButton) openSelectionPanelButton.onClick.AddListener(() =>
		{
			if (selectionPanel) selectionPanel.Show();
		});

		// 保存されたモードをロード
		playMode = PlayModeStore.Load();
	}

	private void Start()
	{
		if (!audioSource) return;
		audioSource.loop = false;
		audioSource.playOnAwake = false;

		if (catalog && catalog.tracks.Length > 0)
			SelectTrack(0, autoPlayOnStart);

		if (selectionPanel) selectionPanel.Init(this);

		UpdateUI();
	}

	private void Update()
	{
		// 曲が終わったらモードごとの挙動
		if (isPlaying && audioSource && audioSource.clip &&
				!audioSource.isPlaying && audioSource.time >= audioSource.clip.length - 0.05f)
		{
			HandleTrackFinished();
		}
	}

	private void OnDestroy()
	{
		if (currentHandle.IsValid()) Addressables.Release(currentHandle);
	}

	// --------------------
	// 再生モード切替
	// --------------------
	private void CyclePlayMode()
	{
		var values = System.Enum.GetValues(typeof(PlayMode)).Cast<PlayMode>().ToArray();
		int idx = System.Array.IndexOf(values, playMode);
		int next = (idx + 1) % values.Length;
		playMode = values[next];

		PlayModeStore.Save(playMode);
		UpdateUI_PlayMode();
	}

	private void HandleTrackFinished()
	{
		switch (playMode)
		{
			case PlayMode.RepeatOne:
				if (currentIndex >= 0) SelectTrack(currentIndex, true);
				else { isPlaying = false; UpdateUI(); }
				break;

			case PlayMode.Sequential:
			default:
				if (loopPlaylist) Next(true);
				else { isPlaying = false; UpdateUI(); }
				break;
		}
	}

	// --------------------
	// 再生コントロール
	// --------------------
	public void TogglePlay()
	{
		if (!audioSource || !audioSource.clip) return;

		if (isPlaying) { audioSource.Pause(); isPlaying = false; }
		else { audioSource.Play(); isPlaying = true; }

		UpdateUI();
	}

	public void Next() => Next(false);

	public void Next(bool autoPlay)
	{
		if (catalog == null || catalog.tracks.Length == 0) return;
		int next = (currentIndex + 1) % catalog.tracks.Length;
		SelectTrack(next, autoPlay || isPlaying);
	}

	public void Prev()
	{
		if (catalog == null || catalog.tracks.Length == 0) return;
		int prev = (currentIndex - 1 + catalog.tracks.Length) % catalog.tracks.Length;
		SelectTrack(prev, isPlaying);
	}

	public void SelectTrack(int index, bool autoPlay = false)
	{
		if (!audioSource || catalog == null || index < 0 || index >= catalog.tracks.Length) return;

		// 前のハンドルを解放
		if (currentHandle.IsValid())
			Addressables.Release(currentHandle);

		currentIndex = index;

		// ロード前から表示名を出す
		titleText.text = catalog.tracks[index].displayName;

		var entry = catalog.tracks[index];
		currentHandle = entry.clip.LoadAssetAsync<AudioClip>();
		currentHandle.Completed += op =>
		{
			if (op.Status == AsyncOperationStatus.Succeeded)
			{
				audioSource.clip = op.Result;
				audioSource.time = 0f;

				if (autoPlay) { audioSource.Play(); isPlaying = true; }
				else { audioSource.Pause(); isPlaying = false; }
			}
			else
			{
				Debug.LogError($"Failed to load {entry.displayName}");
			}

			UpdateUI();
		};
	}

	// --------------------
	// デバッグ: 残り5秒へジャンプ
	// --------------------
	private void JumpToLast5Seconds()
	{
		if (!audioSource || !audioSource.clip) return;

		var clip = audioSource.clip;
		bool wasPlaying = isPlaying || audioSource.isPlaying;

		float backSeconds = (clip.length > 5f) ? 5f : 0.1f;
		int offsetSamples = Mathf.CeilToInt(clip.frequency * backSeconds);
		int targetSamples = Mathf.Clamp(clip.samples - offsetSamples, 0, clip.samples - 1);

		audioSource.Stop();
		audioSource.timeSamples = targetSamples;

		if (wasPlaying)
		{
			audioSource.Play();
			isPlaying = true;
		}

		UpdateUI();
	}

	// --------------------
	// UI更新
	// --------------------
	private void UpdateUI()
	{
		if (titleText && currentIndex >= 0 && catalog && catalog.tracks.Length > currentIndex)
			titleText.text = catalog.tracks[currentIndex].displayName;

		if (playPauseIcon && playSprite && pauseSprite)
			playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;

		bool hasClip = (catalog && catalog.tracks.Length > 0);
		if (prevButton) prevButton.interactable = hasClip;
		if (nextButton) nextButton.interactable = hasClip;
		if (playPauseButton) playPauseButton.interactable = hasClip;

		UpdateUI_PlayMode();
		if (playModeButton) playModeButton.interactable = true;
	}

	private void UpdateUI_PlayMode()
	{
		if (!playModeIcon) return;

		switch (playMode)
		{
			case PlayMode.RepeatOne:
				if (repeatOneSprite) playModeIcon.sprite = repeatOneSprite;
				break;

			case PlayMode.Sequential:
			default:
				if (sequentialSprite) playModeIcon.sprite = sequentialSprite;
				break;
		}
	}
}
