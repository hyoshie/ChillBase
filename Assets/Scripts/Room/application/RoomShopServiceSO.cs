using UnityEngine;

[CreateAssetMenu(menuName = "Game/RoomShopService", fileName = "RoomShopService")]
public class RoomShopServiceSO : ScriptableObject
{
	[SerializeField] RoomStateSO roomState; // 現在ID（UI用の状態SO）

	public string CurrentId =>
			roomState ? roomState.CurrentId : RoomRepository.LoadCurrent();

	public bool IsOwned(string roomId)
	{
		if (string.IsNullOrEmpty(roomId)) return false;
		return RoomRepository.LoadOwned().Contains(roomId);
	}

	public bool TryBuy(string roomId, int price)
	{
		if (string.IsNullOrEmpty(roomId)) return false;
		if (price <= 0) return false;

		var owned = RoomRepository.LoadOwned();
		if (owned.Contains(roomId)) return false;              // 既に所有

		if (!CurrencyManager.SpendCoins(price)) return false;   // 残高チェック＆減算

		owned.Add(roomId);                                     // 所有に追加（保存）
		RoomRepository.SaveOwned(owned);
		return true;
	}

	public void Use(string roomId)
	{
		if (string.IsNullOrEmpty(roomId)) return;

		var current = RoomRepository.LoadCurrent();
		if (current == roomId) return;                         // 変化なし

		RoomRepository.SaveCurrent(roomId);                   // 保存
		roomState.Set(roomId);                // SO一本化後はこれが主
	}

	/// 起動時の初期化（デフォルト付与＆選択確定、通知まで）
	public void EnsureDefault(string defaultRoomId)
	{
		var owned = RoomRepository.LoadOwned();
		var cur = RoomRepository.LoadCurrent();

		// デフォルト未所有なら付与
		if (!string.IsNullOrEmpty(defaultRoomId) && !owned.Contains(defaultRoomId))
		{
			Debug.Log($"[Ensure Default]: Buy {defaultRoomId}");
			owned.Add(defaultRoomId);
			RoomRepository.SaveOwned(owned);
		}

		// 未選択ならデフォルトを選択
		if (string.IsNullOrEmpty(cur) && !string.IsNullOrEmpty(defaultRoomId))
		{
			Debug.Log($"[Ensure Default]: Save Current {defaultRoomId}");
			cur = defaultRoomId;
			RoomRepository.SaveCurrent(cur);
		}

		// 通知・SO反映
		if (!string.IsNullOrEmpty(cur))
		{
			Debug.Log($"[Ensure Default]: Set Current {defaultRoomId}");
			roomState.Set(cur);
		}
	}

	// テスト/初期化差し込み用
	public void InjectState(RoomStateSO state) { roomState = state; }
}
