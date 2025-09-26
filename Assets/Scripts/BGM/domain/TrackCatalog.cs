using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Music/TrackCatalog")]
public class TrackCatalog : ScriptableObject
{
	public TrackEntry[] tracks;
}
