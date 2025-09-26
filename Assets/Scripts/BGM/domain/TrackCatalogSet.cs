// Assets/Scripts/Music/TrackCatalogSet.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Music/TrackCatalogSet")]
public class TrackCatalogSet : ScriptableObject
{
	[Serializable]
	public class CatalogItem
	{
		public string displayName;       // タブ名（例：Free / Premium など）
		public TrackCatalog catalog;     // 既存の TrackCatalog（TrackEntry[]）
		public Sprite tabIcon;           // 任意：タブ用のアイコン
	}

	public List<CatalogItem> catalogs = new(); // 将来3つ以上もOK
}
