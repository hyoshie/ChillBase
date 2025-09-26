using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Music/TrackCatalogSet")]
public class TrackCatalogSet : ScriptableObject
{
	[Serializable]
	public class CatalogItem
	{
		public string displayName;   // タブ名（例：Free / Premium など）
		public TrackCatalog catalog; // 既存の TrackCatalog（TrackEntry[]）
		public Sprite tabIcon;       // 任意：タブ用のアイコン
	}

	public List<CatalogItem> catalogs = new();

	public bool IsValidCatalogIndex(int index)
			=> catalogs != null && index >= 0 && index < catalogs.Count;

	public TrackCatalog GetCatalog(int index)
			=> IsValidCatalogIndex(index) ? catalogs[index]?.catalog : null;

	public string GetDisplayName(int index)
	{
		if (!IsValidCatalogIndex(index)) return string.Empty;
		var item = catalogs[index];
		return string.IsNullOrEmpty(item?.displayName) ? $"Catalog {index}" : item.displayName;
	}

	public int GetTrackCount(int catalogIndex)
			=> GetCatalog(catalogIndex)?.tracks?.Length ?? 0;

	public bool HasAnyTracks(int catalogIndex)
			=> GetTrackCount(catalogIndex) > 0;

	/// <summary>
	/// from から dir(±1)方向に、次の「非空」カタログを探す。見つからなければ -1。
	/// </summary>
	public int FindNextNonEmptyCatalog(int from, int dir)
	{
		if (catalogs == null || catalogs.Count == 0) return -1;
		int n = catalogs.Count;
		for (int step = 1; step <= n; step++)
		{
			int idx = (from + dir * step + n) % n;
			if (HasAnyTracks(idx)) return idx;
		}
		return -1;
	}

	public bool TryGetFirstNonEmpty(out int index)
	{
		index = FindNextNonEmptyCatalog(-1, +1);
		return index >= 0;
	}

	public bool TryGetLastNonEmpty(out int index)
	{
		// 0 から -1 方向にたどる
		index = FindNextNonEmptyCatalog(0, -1);
		return index >= 0;
	}

	/// <summary>
	/// 現在位置から「次」を求める（カタログ横断）。同一カタログ末尾なら、次の非空カタログ先頭。
	/// </summary>
	public bool TryGetNextAcross(int currentCatalog, int currentTrack, out int nextCatalog, out int nextTrack)
	{
		nextCatalog = currentCatalog;
		nextTrack = currentTrack;

		var cat = GetCatalog(currentCatalog);
		var len = cat?.tracks?.Length ?? 0;
		if (len == 0) return false;

		if (currentTrack + 1 < len)
		{
			nextTrack = currentTrack + 1;
			return true;
		}

		int idx = FindNextNonEmptyCatalog(currentCatalog, +1);
		if (idx >= 0)
		{
			nextCatalog = idx;
			nextTrack = 0;
			return true;
		}
		return false;
	}

	/// <summary>
	/// 現在位置から「前」を求める（カタログ横断）。同一カタログ先頭なら、前の非空カタログ末尾。
	/// </summary>
	public bool TryGetPrevAcross(int currentCatalog, int currentTrack, out int prevCatalog, out int prevTrack)
	{
		prevCatalog = currentCatalog;
		prevTrack = currentTrack;

		var cat = GetCatalog(currentCatalog);
		var len = cat?.tracks?.Length ?? 0;
		if (len == 0) return false;

		if (currentTrack - 1 >= 0)
		{
			prevTrack = currentTrack - 1;
			return true;
		}

		int idx = FindNextNonEmptyCatalog(currentCatalog, -1);
		if (idx >= 0)
		{
			var c = GetCatalog(idx);
			int last = Mathf.Max(0, (c?.tracks?.Length ?? 1) - 1);
			prevCatalog = idx;
			prevTrack = last;
			return true;
		}
		return false;
	}
}
