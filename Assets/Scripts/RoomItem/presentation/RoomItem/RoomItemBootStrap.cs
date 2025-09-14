// Assets/Scripts/Item/application/RoomItemBootstrap.cs
using UnityEngine;

public class RoomItemBootstrap : MonoBehaviour
{
	[SerializeField] RoomItemServiceSO service;

	void Awake()
	{
		if (service == null)
		{
			Debug.LogError("[RoomItemBootstrap] RoomItemServiceSO is not assigned.");
			return;
		}

		// RoomItemFacade.Configure(service);
		service.ReloadAllFromStorage(); // 起動時にロード
	}
}
