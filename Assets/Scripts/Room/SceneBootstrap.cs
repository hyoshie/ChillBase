using UnityEngine;

[DefaultExecutionOrder(-1100)]
public class RoomBootstrap : MonoBehaviour
{
	[SerializeField] RoomShopServiceSO roomService;

	// 非シリアライズ：常に定数を参照
	private string DefaultRoomId => RoomConstants.DefaultRoomId;

	void Awake()
	{
		Debug.Log($"[RoomBootstrap] DefaultRoomId={DefaultRoomId}");
		roomService?.EnsureDefault(DefaultRoomId);
	}
}
