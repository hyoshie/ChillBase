// PomodoroLimits.cs
using UnityEngine;

public static class PomodoroConstants
{
	// デフォルト

	public const int DefaultWorkMinutes = 25;
	public const int DefaultBreakMinutes = 5; // 作業の最大値
																						// 範囲（分）
	public const int MinWorkMinutes = 1;
	public const int MaxWorkMinutes = 120; // 作業の最大値
	public const int MinBreakMinutes = 1;
	public const int MaxBreakMinutes = 30;  // 休憩の最大値

	// 共通クランプ
	public static int ClampWork(int minutes)
		=> Mathf.Clamp(minutes, MinWorkMinutes, MaxWorkMinutes);

	public static int ClampBreak(int minutes)
		=> Mathf.Clamp(minutes, MinBreakMinutes, MaxBreakMinutes);
}
