using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PomodoroTimer : MonoBehaviour
{
	[Header("UI: 表示")]
	public TextMeshProUGUI sessionLabel;
	public TextMeshProUGUI timerText;

	[Header("UI: 操作")]
	public Button startButton;
	public Button stopButton;
	public Button resetButton;

	[Header("UI: 設定")]
	public Button settingsButton;
	public Button closeSettingsButton;
	[Header("デバッグ")]
	public Button debugSkipButton;
	public Button debugSettingResetButton;

	public GameObject settingsPanel;
	public Slider workMinutesSlider;
	public Slider breakMinutesSlider;
	public TextMeshProUGUI workMinutesLabel;
	public TextMeshProUGUI breakMinutesLabel;

	[Header("サウンド")]
	public AudioSource chimeSource;
	public AudioClip chimeClip;

	[Header("時間（秒）")]
	public int workDuration = PomodoroConstants.DefaultWorkMinutes * 60;
	public int breakDuration = PomodoroConstants.DefaultBreakMinutes * 60;

	[Header("報酬")]
	public int coinsPerMinute = 1;
	public bool rewardOnWorkComplete = true;

	private int currentTime;
	private bool isRunning = false;
	private bool isWorkSession = true;

	void Awake()
	{
		PomodoroSettingsStore.Load();
	}

	void OnEnable()
	{
		PomodoroSettingsStore.OnChanged += HandleSettingsChanged;
	}

	void OnDisable()
	{
		PomodoroSettingsStore.OnChanged -= HandleSettingsChanged;
	}

	// 設定変更イベントを受けてUIと内部値を即反映
	void HandleSettingsChanged()
	{
		// ストアから再取得（クランプはストア側が保証）
		int w = PomodoroSettingsStore.GetWorkMinutes();
		int b = PomodoroSettingsStore.GetBreakMinutes();

		workDuration = w * 60;
		breakDuration = b * 60;

		// スライダーがあれば表示だけ静かに合わせる
		if (workMinutesSlider != null)
			workMinutesSlider.SetValueWithoutNotify(w);
		if (breakMinutesSlider != null)
			breakMinutesSlider.SetValueWithoutNotify(b);

		UpdateWorkLabel(w);
		UpdateBreakLabel(b);

		// 停止中のみ残り時間を現在のセッションに合わせて再計算
		if (!isRunning)
		{
			currentTime = isWorkSession ? workDuration : breakDuration;
			UpdateTimerText();
		}
	}

	void Start()
	{
		CurrencyManager.LoadCoins();

		// 保存値を取得→上限内にクランプ→必要なら再保存
		int savedWorkMin = PomodoroConstants.ClampWork(PomodoroSettingsStore.GetWorkMinutes());
		int savedBreakMin = PomodoroConstants.ClampBreak(PomodoroSettingsStore.GetBreakMinutes());

		if (savedWorkMin != PomodoroSettingsStore.GetWorkMinutes())
			PomodoroSettingsStore.SetWorkMinutes(savedWorkMin);
		if (savedBreakMin != PomodoroSettingsStore.GetBreakMinutes())
			PomodoroSettingsStore.SetBreakMinutes(savedBreakMin);

		workDuration = savedWorkMin * 60;
		breakDuration = savedBreakMin * 60;

		currentTime = workDuration;
		UpdateTimerText();

		startButton.onClick.AddListener(StartTimer);
		stopButton.onClick.AddListener(StopTimer);
		resetButton.onClick.AddListener(ResetTimer);

		if (settingsPanel != null) settingsPanel.SetActive(false);

		if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
		if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
		if (debugSkipButton != null) debugSkipButton.onClick.AddListener(SetRemainFiveSeconds);
		if (debugSettingResetButton != null) debugSettingResetButton.onClick.AddListener(ResetSetting);

		if (workMinutesSlider != null)
		{
			workMinutesSlider.minValue = PomodoroConstants.MinWorkMinutes;
			workMinutesSlider.maxValue = PomodoroConstants.MaxWorkMinutes;
			workMinutesSlider.value = savedWorkMin;
			workMinutesSlider.onValueChanged.AddListener(OnWorkSliderChanged);
		}

		if (breakMinutesSlider != null)
		{
			breakMinutesSlider.minValue = PomodoroConstants.MinBreakMinutes;
			breakMinutesSlider.maxValue = PomodoroConstants.MaxBreakMinutes;
			breakMinutesSlider.value = savedBreakMin;
			breakMinutesSlider.onValueChanged.AddListener(OnBreakSliderChanged);
		}

		UpdateWorkLabel(savedWorkMin);
		UpdateBreakLabel(savedBreakMin);
	}

	void StartTimer()
	{
		if (isRunning) return;
		isRunning = true;
		if (settingsButton) settingsButton.interactable = false;
		InvokeRepeating(nameof(Tick), 1f, 1f);
	}

	void StopTimer()
	{
		isRunning = false;
		if (settingsButton) settingsButton.interactable = true;
		CancelInvoke(nameof(Tick));
	}

	void ResetTimer()
	{
		StopTimer();
		currentTime = isWorkSession ? workDuration : breakDuration;
		UpdateTimerText();
	}

	void Tick()
	{
		currentTime--;
		if (currentTime > 0)
		{
			UpdateTimerText();
			return;
		}

		bool finishedWork = isWorkSession;
		if (finishedWork && rewardOnWorkComplete)
		{
			int minutes = Mathf.Max(1, workDuration / 60);
			int reward = Mathf.Max(1, minutes * Mathf.Max(1, coinsPerMinute));
			CurrencyManager.AddCoins(reward);
		}

		isWorkSession = !isWorkSession;
		currentTime = isWorkSession ? workDuration : breakDuration;

		if (chimeSource != null && chimeClip != null)
			chimeSource.PlayOneShot(chimeClip);

		UpdateTimerText();
	}

	void UpdateTimerText()
	{
		if (timerText != null)
			timerText.text = FormatTime(currentTime);

		if (sessionLabel != null)
			sessionLabel.text = isWorkSession ? "Work" : "Break";
	}

	string FormatTime(int totalSeconds)
	{
		int clamped = Mathf.Max(0, totalSeconds);
		int minutes = clamped / 60;
		int seconds = clamped % 60;
		return $"{minutes:D2}:{seconds:D2}";
	}

	void OpenSettings()
	{
		if (isRunning) return;

		if (settingsPanel) settingsPanel.SetActive(true);
		if (workMinutesSlider) workMinutesSlider.value = PomodoroConstants.ClampWork(PomodoroSettingsStore.GetWorkMinutes());
		if (breakMinutesSlider) breakMinutesSlider.value = PomodoroConstants.ClampBreak(PomodoroSettingsStore.GetBreakMinutes());
	}

	void CloseSettings()
	{
		if (settingsPanel) settingsPanel.SetActive(false);
	}

	void OnWorkSliderChanged(float value)
	{
		if (isRunning) return;
		int minutes = PomodoroConstants.ClampWork(Mathf.RoundToInt(value));

		workDuration = minutes * 60;
		if (isWorkSession) currentTime = workDuration;

		PomodoroSettingsStore.SetWorkMinutes(minutes);
		UpdateTimerText();
		UpdateWorkLabel(minutes);
	}

	void OnBreakSliderChanged(float value)
	{
		if (isRunning) return;
		int minutes = PomodoroConstants.ClampBreak(Mathf.RoundToInt(value));

		breakDuration = minutes * 60;
		if (!isWorkSession) currentTime = breakDuration;

		PomodoroSettingsStore.SetBreakMinutes(minutes);
		UpdateTimerText();
		UpdateBreakLabel(minutes);
	}

	void UpdateWorkLabel(int minutes)
	{
		if (workMinutesLabel != null)
			workMinutesLabel.text = $"Work: {minutes} min";
	}

	void UpdateBreakLabel(int minutes)
	{
		if (breakMinutesLabel != null)
			breakMinutesLabel.text = $"Break: {minutes} min";
	}

	void SetRemainFiveSeconds()
	{
		currentTime = 5;
		UpdateTimerText();
	}

	void ResetSetting()
	{
		PomodoroSettingsStore.ResetToDefault();
	}
}
