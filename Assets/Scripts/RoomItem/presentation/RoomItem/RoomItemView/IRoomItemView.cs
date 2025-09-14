using UnityEngine;

public interface IRoomItemView
{
	void Show(RoomItemVisual visual); // Visualを元に表示
	void Hide();
	GameObject gameObject { get; }
}
