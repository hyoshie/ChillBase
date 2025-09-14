using UnityEngine;
using System;

// repositoryとapplicationが混じってる。後でリファクタ
public class CurrencyManager : MonoBehaviour
{
	private static CurrencyManager _instance;
	public static CurrencyManager Instance => _instance;
	// ① RoomItemServiceSO を直参照
	[SerializeField] RoomItemServiceSO roomItemService;

	public static int coins { get; private set; }
	public static event Action<int> OnCoinsChanged;

	[Header("Defaults")]
	[SerializeField] int defaultCoins = 100; // ★リセット時の初期コイン枚数

	void Awake()
	{
		if (_instance != null && _instance != this) { Destroy(gameObject); return; }
		_instance = this;
		DontDestroyOnLoad(gameObject);
		LoadCoins();
	}

	public static void LoadCoins()
	{
		coins = PlayerPrefs.GetInt("coins", 0);
		Debug.Log($"[Currency] Loaded. coins={coins}");
		OnCoinsChanged?.Invoke(coins);
	}

	public static void AddCoins(int amount)
	{
		if (amount <= 0) return;
		int before = coins;
		coins += amount;
		PlayerPrefs.SetInt("coins", coins);
		PlayerPrefs.Save();
		Debug.Log($"[Currency] +{amount} -> {before} => {coins}");
		OnCoinsChanged?.Invoke(coins);
	}

	public static bool SpendCoins(int amount)
	{
		if (amount <= 0) return true;
		if (coins < amount) { Debug.Log($"[Currency] Not enough. need={amount}, have={coins}"); return false; }
		int before = coins;
		coins -= amount;
		PlayerPrefs.SetInt("coins", coins);
		PlayerPrefs.Save();
		Debug.Log($"[Currency] -{amount} -> {before} => {coins}");
		OnCoinsChanged?.Invoke(coins);
		return true;
	}

	// ★直接値をセット（UI再描画イベントも発火）
	public static void SetCoins(int value)
	{
		coins = Mathf.Max(0, value);
		PlayerPrefs.SetInt("coins", coins);
		PlayerPrefs.Save();
		Debug.Log($"[Currency] Set -> {coins}");
		OnCoinsChanged?.Invoke(coins);
	}

	// ★リセット（静的）：任意の値に
	public static void ResetCoins(int to = 100) => SetCoins(to);

	// ★UIボタンから呼び出しやすい“インスタンスメソッド”
	//    ・Inventory を未所持化
	//    ・コインを defaultCoins に
	public void ResetAllToDefault()
	{
		// Inventory.ResetAll();
		// RoomItemFacade.ResetAll();
		roomItemService.ResetAll();
		ResetCoins(defaultCoins);
		Debug.Log($"[Currency] ResetAllToDefault -> coins={defaultCoins}");
	}
}
