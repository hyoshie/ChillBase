using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Music/TrackEntry")]
public class TrackEntry : ScriptableObject
{
	public string displayName; // ユーザーに見せたい名前
	public AssetReferenceT<AudioClip> clip; // 実データ参照
}
