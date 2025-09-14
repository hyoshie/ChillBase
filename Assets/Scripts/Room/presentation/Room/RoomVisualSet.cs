using System.Collections.Generic;
using UnityEngine;

public class RoomVisualSet : MonoBehaviour
{
	[System.Serializable] public struct Entry { public string roomId; public GameObject root; }

	[Tooltip("手動登録したいときに使う。空なら子の名前をroomIdとして自動スキャン")]
	public Entry[] entries;

	Dictionary<string, GameObject> _map;
	[SerializeField] RoomStateSO roomState;

	void Awake()
	{
		// マップ構築のみ（ここでは購読しない）
		_map = new Dictionary<string, GameObject>();

		if (entries != null && entries.Length > 0)
		{
			foreach (var e in entries)
				if (e.root) _map[e.roomId] = e.root;
		}
		else
		{
			foreach (Transform child in transform)
				_map[child.name] = child.gameObject;
		}
	}

	void OnEnable()
	{
		roomState.OnChanged += Apply;
		Apply(roomState.CurrentId); // 初期反映
	}

	void OnDisable()
	{
		roomState.OnChanged -= Apply;
	}

	public void Apply(string roomId)
	{
		foreach (var kv in _map)
			kv.Value.SetActive(kv.Key == roomId && !string.IsNullOrEmpty(roomId));
	}
}
