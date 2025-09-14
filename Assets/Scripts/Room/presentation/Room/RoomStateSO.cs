using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/RoomState")]
public class RoomStateSO : ScriptableObject
{
	[SerializeField] string _currentId = "";
	public string CurrentId => _currentId;

	public event Action<string> OnChanged;

	public void Set(string id)
	{
		id ??= "";
		if (_currentId == id) return;
		_currentId = id;
		OnChanged?.Invoke(_currentId);
	}
}
