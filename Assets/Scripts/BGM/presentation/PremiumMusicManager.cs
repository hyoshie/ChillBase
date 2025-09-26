using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PremiumMusicManager : MonoBehaviour
{
	[Header("UI (普段は非アクティブでOK)")]
	[SerializeField] private GameObject waitOverlay; // 全画面パネル（半透明でOK）
	[SerializeField] private TMP_Text statusText;     // 任意（メッセージ用・無ければnullでOK）

	[Header("Buttons")]
	[SerializeField] private Button downloadButton;   // ダウンロードだけ
	[SerializeField] private Button memoryButton;     // メモリにロード
	[SerializeField] private Button clearButton;      // キャッシュ削除

	private const string PremiumLabel = "premium";
	private bool isBusy = false;

	// メモリロード時のみ保持（解放用）
	private AsyncOperationHandle<IList<AudioClip>> loadedClipsHandle;
	private bool hasLoadedClips = false;

	private void Awake()
	{
		if (downloadButton) downloadButton.onClick.AddListener(OnDownloadOnly);
		if (memoryButton) memoryButton.onClick.AddListener(OnLoadIntoMemory);
		if (clearButton) clearButton.onClick.AddListener(OnClearCache);

		if (waitOverlay) waitOverlay.SetActive(false);
	}

	private void OnDestroy()
	{
		if (!isBusy && hasLoadedClips && loadedClipsHandle.IsValid())
		{
			Addressables.Release(loadedClipsHandle);
			hasLoadedClips = false;
		}
	}

	// ===== ダウンロードだけ（キャッシュへ保存・メモリ展開しない） =====
	private void OnDownloadOnly()
	{
		if (!BeginBusy("Downloading...")) return;

		Debug.Log("▶ Premium をダウンロード開始（端末キャッシュのみ）");

		// （任意）サイズ確認だけ先に
		var sizeH = Addressables.GetDownloadSizeAsync(PremiumLabel);
		sizeH.Completed += _ =>
		{
			Addressables.Release(sizeH);

			var dl = Addressables.DownloadDependenciesAsync(PremiumLabel, true);
			dl.Completed += d =>
					{
						if (d.Status == AsyncOperationStatus.Succeeded)
							Debug.Log("[Premium] download completed.");
						else
							Debug.LogError($"[Premium] download failed: {d.OperationException}");

						EndBusy("Done");
					};
		};
	}

	// ===== メモリにロード（勝手に再生しない／到着ログのみ） =====
	private void OnLoadIntoMemory()
	{
		if (!BeginBusy("Loading...")) return;

		Debug.Log("▶ Premium をメモリにロード開始（再生はしない）");

		var keys = new List<string> { PremiumLabel };
		loadedClipsHandle = Addressables.LoadAssetsAsync<AudioClip>(
				keys,
				clip => { Debug.Log($"  ✔ Loaded: {clip.name}"); },
				Addressables.MergeMode.Union,
				true  // 1つ失敗で全体失敗＆解放
		);
		hasLoadedClips = true;

		loadedClipsHandle.Completed += h =>
		{
			if (h.Status == AsyncOperationStatus.Succeeded)
				Debug.Log($"[Premium] loaded {h.Result.Count} clips into memory.");
			else
			{
				Debug.LogError($"[Premium] LoadAssets failed: {h.OperationException}");
				hasLoadedClips = false;
			}

			EndBusy("Done");
		};
	}

	// ===== キャッシュ削除（操作中は拒否 → コルーチンで安全に実行） =====
	private void OnClearCache()
	{
		if (isBusy)
		{
			Debug.LogWarning("⏳ 実行中は削除できません。完了後に再試行してください。");
			return;
		}
		StartCoroutine(ClearCacheRoutine());
	}

	private System.Collections.IEnumerator ClearCacheRoutine()
	{
		isBusy = true;
		Debug.Log("▶ Premium キャッシュ削除開始");

		// 1) 再生停止 & 参照を必ず外す（どこかで保持していると失敗します）
		// if (audioSource)
		// {
		// 	if (audioSource.isPlaying) audioSource.Stop();
		// 	audioSource.clip = null;
		// }

		// 2) このクラスで保持しているロードハンドルを解放（保持していれば）
		if (hasLoadedClips && loadedClipsHandle.IsValid())
		{
			Addressables.Release(loadedClipsHandle);
			hasLoadedClips = false;
			Debug.Log("[Premium] released loaded clips from memory.");
		}

		// 3) 参照カウントが下がるのを待つ（重要）
		yield return null; // 1 frame
		yield return null; // 2 frames (保険)

		// 4) 依存キャッシュをラベルでクリア
		// あなたの環境が void 戻り値の場合もあるため、Completedは見ずにログだけ出します
		Addressables.ClearDependencyCacheAsync(PremiumLabel);
		Debug.Log("[Premium] cleared cached bundles for label: premium");

		isBusy = false;
	}

	// ===== 共通：操作ロック / 表示制御 =====
	private bool BeginBusy(string msg)
	{
		if (isBusy) return false;
		isBusy = true;

		if (downloadButton) downloadButton.interactable = false;
		if (memoryButton) memoryButton.interactable = false;
		if (clearButton) clearButton.interactable = false;

		if (waitOverlay) waitOverlay.SetActive(true);
		if (statusText) statusText.text = msg;
		return true;
	}

	private void EndBusy(string msg)
	{
		if (statusText) statusText.text = msg;
		if (waitOverlay) waitOverlay.SetActive(false);

		if (downloadButton) downloadButton.interactable = true;
		if (memoryButton) memoryButton.interactable = true;
		if (clearButton) clearButton.interactable = true;

		isBusy = false;
	}
}
