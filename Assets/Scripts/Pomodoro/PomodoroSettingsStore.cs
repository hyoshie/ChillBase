using System;
using UnityEngine;

public static class PomodoroSettingsStore
{
	const string KeyTimerSettings = "pomodoro_timer_settings_v1";

	[Serializable]
	class Dto
	{
		public int version = 1;
		public int workMinutes = PomodoroConstants.DefaultWorkMinutes;
		public int breakMinutes = PomodoroConstants.DefaultBreakMinutes;
	}

	static bool _loaded;
	static Dto _dto;

	public static event Action OnChanged;

	// --------------- 基本I/O ---------------
	public static void Load()
	{
		if (_loaded) return;
		_loaded = true;

		var raw = PlayerPrefs.GetString(KeyTimerSettings, "");
		if (string.IsNullOrEmpty(raw))
		{
			_dto = new Dto(); // デフォルト
			return;
		}

		try
		{
			_dto = JsonUtility.FromJson<Dto>(raw) ?? new Dto();
			// バージョン移行等があればここで
			_dto.workMinutes = PomodoroConstants.ClampWork(_dto.workMinutes);
			_dto.breakMinutes = PomodoroConstants.ClampBreak(_dto.breakMinutes);
		}
		catch (Exception e)
		{
			Debug.LogError($"[PomodoroSettings] JSON parse failed: {e}");
			_dto = new Dto();
		}
	}

	static void Save()
	{
		var json = JsonUtility.ToJson(_dto);
		PlayerPrefs.SetString(KeyTimerSettings, json);
		PlayerPrefs.Save();
		OnChanged?.Invoke();
	}

	// --------------- 公開API ---------------
	public static int GetWorkMinutes()
	{
		Load();
		return _dto.workMinutes;
	}

	public static int GetBreakMinutes()
	{
		Load();
		return _dto.breakMinutes;
	}

	public static void SetWorkMinutes(int minutes)
	{
		Load();
		minutes = PomodoroConstants.ClampWork(minutes);
		if (_dto.workMinutes == minutes) return;
		_dto.workMinutes = minutes;
		Save();
	}

	public static void SetBreakMinutes(int minutes)
	{
		Load();
		minutes = PomodoroConstants.ClampBreak(minutes);
		if (_dto.breakMinutes == minutes) return;
		_dto.breakMinutes = minutes;
		Save();
	}

	public static void ResetToDefault()
	{
		_dto = new Dto();
		Save();
		OnChanged?.Invoke();
		Debug.Log("[PomodoroSettingStore] Reset -> cleared time");
	}
}
