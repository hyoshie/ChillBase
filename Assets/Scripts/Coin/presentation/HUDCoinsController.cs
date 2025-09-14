using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDCoinsController : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private Image coinIcon;           // 任意
	[SerializeField] private TextMeshProUGUI coinsText;

	[Header("Format")]
	[SerializeField] private string format = "{0:n0}"; // 12,345 表記
	[SerializeField] private bool showSuffix = false;   // 1.2k / 3.4M 表記にするならtrue

	void OnEnable()
	{
		CurrencyManager.OnCoinsChanged += OnCoinsChanged;
		OnCoinsChanged(CurrencyManager.coins); // 初期反映
	}

	void OnDisable()
	{
		CurrencyManager.OnCoinsChanged -= OnCoinsChanged;
	}

	void OnCoinsChanged(int value)
	{
		if (!coinsText) return;
		coinsText.text = showSuffix ? FormatWithSuffix(value) : string.Format(format, value);

		// ちょい演出（任意）：数値をポンっと拡大
		var rt = coinsText.rectTransform;
		if (!rt) return;
		// LeanTween.cancel(rt);
		rt.localScale = Vector3.one;                 // LeanTween使ってない場合は削除
																								 // LeanTween.scale(rt, Vector3.one * 1.12f, 0.08f).setIgnoreTimeScale(true)
																								 //  .setOnComplete(() => LeanTween.scale(rt, Vector3.one, 0.12f).setIgnoreTimeScale(true));
	}

	string FormatWithSuffix(int n)
	{
		if (n >= 1_000_000) return (n / 1_000_000f).ToString("0.0") + "M";
		if (n >= 1_000) return (n / 1_000f).ToString("0.0") + "k";
		return n.ToString("n0");
	}
}
