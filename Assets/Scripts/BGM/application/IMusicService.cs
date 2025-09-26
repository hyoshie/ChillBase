// IMusicService.cs
using System;
using UnityEngine;

public interface IMusicService : IDisposable
{
	event Action<MusicSnapshot> OnChanged;

	PlayMode PlayMode { get; }
	TrackCatalogSet CatalogSet { get; }   // 複数カタログ前提
	int CurrentCatalogIndex { get; }

	// ランタイム初期化
	void BindRuntime(AudioSource audioSource);   // シーンの AudioSource をバインド
	void Initialize(bool autoPlayOnStart);

	// 再生制御
	void TogglePlay();
	void Next(bool autoPlayOverride = false);  // カタログ横断で次へ
	void Prev();                               // カタログ横断で前へ
	void SelectTrack(int catalogIndex, int trackIndex, bool autoPlay = false);
	void SetCatalog(int catalogIndex, bool keepTrackIndex = false, bool autoPlay = false);
	void CyclePlayMode();
	void JumpToLast5Seconds();

	// 毎フレーム更新（自然終了検知など）
	void Tick();
}
