using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public class TrackEntry
{
	public string displayName; // ユーザーに見せたい名前
	public AssetReferenceT<AudioClip> clip; // 実データ参照
}

[CreateAssetMenu(menuName = "Music/TrackCatalog")]
public class TrackCatalog : ScriptableObject
{
	public TrackEntry[] tracks;
}
