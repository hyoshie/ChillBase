using UnityEngine;
using UnityEngine.UI;

public class MenuPanelManager : MonoBehaviour
{
	[Header("Panels")]
	public GameObject pomodoroPanel;
	public RoomItemShopPanel shopPanel;
	public RoomShopPanel roomPanel;

	[Header("Buttons")]
	[SerializeField] private Button pomodoroButton;
	[SerializeField] private Button shopButton;
	[SerializeField] private Button roomButton;

	private void Awake()
	{
		// ボタンにリスナー追加
		if (pomodoroButton) pomodoroButton.onClick.AddListener(TogglePomodoroPanel);
		if (shopButton) shopButton.onClick.AddListener(ToggleShopPanel);
		if (roomButton) roomButton.onClick.AddListener(ToggleRoomPanel);
	}

	// すべてのパネルを閉じる
	private void HideAllPanels()
	{
		if (pomodoroPanel) pomodoroPanel.SetActive(false);
		if (shopPanel) shopPanel.Close();   // ShopPanel_SO.Close() は panelRoot.SetActive(false)
		if (roomPanel) roomPanel.Close();
	}

	public void TogglePomodoroPanel()
	{
		bool isActive = pomodoroPanel && pomodoroPanel.activeSelf;
		HideAllPanels();
		if (pomodoroPanel) pomodoroPanel.SetActive(!isActive);
	}

	public void ToggleShopPanel()
	{
		bool isActive = shopPanel && shopPanel.IsOpen;
		HideAllPanels();
		if (shopPanel && !isActive)
		{
			shopPanel.Open();
		}
	}

	public void ToggleRoomPanel()
	{
		bool isActive = roomPanel && roomPanel.IsOpen;
		HideAllPanels();
		if (roomPanel && !isActive)
		{
			roomPanel.Open();
		}
	}
}
