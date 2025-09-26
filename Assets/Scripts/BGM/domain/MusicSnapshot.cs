// MusicSnapshot.cs
using System;

public sealed class MusicSnapshot
{
	public int CurrentIndex { get; }
	public bool IsPlaying { get; }
	public string Title { get; }
	public bool HasAnyTrack { get; }
	public PlayMode PlayMode { get; }

	// 複数カタログ用
	public int CurrentCatalogIndex { get; }
	public string CurrentCatalogDisplayName { get; }
	public int CurrentCatalogTrackCount { get; }

	public MusicSnapshot(
			int currentIndex,
			bool isPlaying,
			string title,
			bool hasAnyTrack,
			PlayMode playMode,
			int currentCatalogIndex,
			string currentCatalogDisplayName,
			int currentCatalogTrackCount)
	{
		CurrentIndex = currentIndex;
		IsPlaying = isPlaying;
		Title = title;
		HasAnyTrack = hasAnyTrack;
		PlayMode = playMode;

		CurrentCatalogIndex = currentCatalogIndex;
		CurrentCatalogDisplayName = currentCatalogDisplayName;
		CurrentCatalogTrackCount = currentCatalogTrackCount;
	}
}
