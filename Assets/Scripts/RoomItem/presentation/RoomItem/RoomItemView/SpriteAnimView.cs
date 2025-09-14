// using UnityEngine;
// using UnityEngine.UI;
// public class SpriteAnimView : MonoBehaviour, IItemView
// {
// 	[SerializeField] private Image image;
// 	[SerializeField] private Animator animator;

// 	public void Show(RoomItemVisual visual)
// 	{
// 		if (visual == null) { Hide(); return; }

// 		image.sprite = visual.sprite;
// 		image.enabled = (image.sprite != null);
// 		image.preserveAspect = true;
// 		image.raycastTarget = false;

// 		animator.runtimeAnimatorController = visual.animator;
// 		animator.cullingMode = AnimatorCullingMode.CullCompletely;

// 		animator.enabled = (visual.animator != null);
// 		if (animator.enabled)
// 			animator.Play(0, 0, 0f);

// 		gameObject.SetActive(true);
// 		Debug.Log($"{visual.animator}");
// 		Debug.Log($"[AnimView] ctrl={(animator.runtimeAnimatorController ? animator.runtimeAnimatorController.name : "null")}, " +
// 					$"enabled={animator.enabled}, active={gameObject.activeInHierarchy}");
// 	}

// 	public void Hide()
// 	{
// 		animator.enabled = false;
// 		gameObject.SetActive(false);
// 	}
// }

using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimView : MonoBehaviour, IRoomItemView
{
	[SerializeField] private Image image;
	[SerializeField] private Animator animator;

	// 参照が未設定なら自動解決（同一GO or 子）
	void Awake()
	{
		if (!image) image = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);
		if (!animator) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
	}

	public void Show(RoomItemVisual visual)
	{
		if (!image || !animator || visual == null) { Hide(); return; }

		// ベース画像（任意）
		image.sprite = visual.sprite;
		image.enabled = (image.sprite != null);
		image.preserveAspect = true;
		image.raycastTarget = false;

		// Animator にコントローラを差し替え
		animator.runtimeAnimatorController = visual.animator;

		if (visual.animator)
		{
			// 初期化を強めに（差し替え直後の固着対策）
			animator.updateMode = AnimatorUpdateMode.Normal;
			animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // 確認が済んだら CullCompletely に戻してOK
			animator.speed = 1f;
			animator.enabled = true;
			animator.Rebind();
			animator.Update(0f);
			// 必要なら特定ステートを明示再生: animator.Play(0, 0, 0f);
		}
		else
		{
			animator.enabled = false;
		}

		gameObject.SetActive(true);
	}

	public void Hide()
	{
		if (animator) animator.enabled = false;
		if (image) image.enabled = false;
		gameObject.SetActive(false);
	}
}
