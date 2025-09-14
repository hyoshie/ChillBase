using UnityEngine;
using UnityEngine.UI;

public class StaticSpriteView : MonoBehaviour, IRoomItemView
{
	[SerializeField] private Image image;

	public void Show(RoomItemVisual visual)
	{
		if (visual == null) { Hide(); return; }
		image.sprite = visual.sprite;
		image.enabled = (visual.sprite != null);
		image.preserveAspect = true;
		image.raycastTarget = false;
		gameObject.SetActive(true);
	}

	public void Hide()
	{
		image.enabled = false;
		gameObject.SetActive(false);
	}
}
