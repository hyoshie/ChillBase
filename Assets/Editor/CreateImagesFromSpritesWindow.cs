// Assets/Editor/CreateImagesFromSpritesWindow.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateImagesFromSpritesWindow : EditorWindow
{
	[SerializeField] private GameObject parent;
	[SerializeField] private DefaultAsset folder;

	[SerializeField] private bool createContainer = false;
	[SerializeField] private string containerName = "";

	private const string MenuPath = "Tools/UI/フォルダからImageを作成";
	private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };

	[MenuItem(MenuPath)]
	public static void ShowWindow()
	{
		var window = GetWindow<CreateImagesFromSpritesWindow>();
		window.titleContent = new GUIContent("フォルダからImageを作成");
		window.minSize = new Vector2(460, 190);
		window.Show();
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("指定したフォルダ内のスプライトからUI Imageをまとめて生成します", EditorStyles.boldLabel);
		EditorGUILayout.Space(6);

		parent = (GameObject)EditorGUILayout.ObjectField("親オブジェクト", parent, typeof(GameObject), true);
		folder = (DefaultAsset)EditorGUILayout.ObjectField("対象フォルダ", folder, typeof(DefaultAsset), false);

		EditorGUILayout.Space(8);
		createContainer = EditorGUILayout.ToggleLeft("新しいコンテナを作成してその配下に配置する", createContainer);
		using (new EditorGUI.DisabledScope(!createContainer))
		{
			EditorGUI.indentLevel++;
			containerName = EditorGUILayout.TextField("コンテナ名（例: usa, jpn）", containerName);
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Space(8);
		using (new EditorGUI.DisabledScope(parent == null || folder == null || (createContainer && string.IsNullOrWhiteSpace(containerName))))
		{
			if (GUILayout.Button("生成", GUILayout.Height(32)))
			{
				try
				{
					CreateImages();
				}
				catch (Exception e)
				{
					Debug.LogError($"[フォルダからImageを作成] エラー: {e}");
				}
			}
		}

		EditorGUILayout.HelpBox(
				"仕様:\n" +
				"・サブフォルダは対象外（直下のみ）\n" +
				"・子オブジェクト名: \n" +
				"   - コンテナなし → 親名(大文字) + ファイル名（パスカルケース）\n" +
				"   - コンテナあり → コンテナ名(大文字) + ファイル名（パスカルケース）\n" +
				"・SpriteMode=Multiple を想定。「*_FullRect」で終わるSpriteをImageに割り当て。見つからなければ空\n" +
				"・RectTransformは stretch/stretch、left/top/right/bottom=0、posZ=0",
				MessageType.Info);
	}

	private void CreateImages()
	{
		if (parent == null || folder == null) return;

		var folderPath = AssetDatabase.GetAssetPath(folder);
		if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
		{
			Debug.LogWarning("[フォルダからImageを作成] 選択したオブジェクトは有効なフォルダではありません。");
			return;
		}

		var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
		if (guids == null || guids.Length == 0)
		{
			Debug.Log($"[フォルダからImageを作成] フォルダに画像が見つかりません: {folderPath}");
			return;
		}

		Transform baseParent = parent.transform;
		string namePrefix;

		if (createContainer)
		{
			string upperContainer = containerName.ToUpperInvariant();
			baseParent = EnsureContainer(parent.transform, upperContainer);
			namePrefix = upperContainer;
		}
		else
		{
			namePrefix = parent.name.ToUpperInvariant();
		}

		int createdCount = 0;
		Undo.RegisterFullObjectHierarchyUndo(parent, "フォルダからImageを作成");

		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var dirOfAsset = Path.GetDirectoryName(path)?.Replace("\\", "/");
			if (!string.Equals(dirOfAsset, folderPath, StringComparison.Ordinal)) continue;

			var ext = Path.GetExtension(path)?.ToLowerInvariant();
			if (string.IsNullOrEmpty(ext) || !ImageExtensions.Contains(ext)) continue;

			var fileBase = Path.GetFileNameWithoutExtension(path);
			// string childName = $"{namePrefix}{ToPascalCase(fileBase)}";
			string childName = $"{ToPascalCase(fileBase)}";

			if (baseParent.Cast<Transform>().Any(t => t.name == childName))
			{
				Debug.Log($"[フォルダからImageを作成] 既に存在するためスキップ: {childName}");
				continue;
			}

			Sprite targetSprite = LoadFullRectSpriteAtPath(path);

			var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			Undo.RegisterCreatedObjectUndo(go, "Imageを作成");
			go.transform.SetParent(baseParent, false);

			var rt = go.GetComponent<RectTransform>();
			ApplyFullStretch(rt);

			var img = go.GetComponent<Image>();
			img.sprite = targetSprite;

			createdCount++;
		}

		if (createdCount > 0)
		{
			EditorUtility.SetDirty(parent);
			Debug.Log($"[フォルダからImageを作成] {createdCount} 個のImageを生成しました。配置先: '{baseParent.name}'");
		}
		else
		{
			Debug.Log("[フォルダからImageを作成] 生成対象がありませんでした。");
		}
	}

	private static Transform EnsureContainer(Transform parent, string name)
	{
		var existing = parent.Cast<Transform>().FirstOrDefault(t => t.name == name);
		if (existing != null)
		{
			var rtExisting = existing.GetComponent<RectTransform>();
			if (rtExisting == null) rtExisting = existing.gameObject.AddComponent<RectTransform>();
			ApplyFullStretch(rtExisting);
			return existing;
		}

		var go = new GameObject(name, typeof(RectTransform));
		Undo.RegisterCreatedObjectUndo(go, "コンテナを作成");
		go.transform.SetParent(parent, false);

		var rt = go.GetComponent<RectTransform>();
		ApplyFullStretch(rt);

		return go.transform;
	}

	private static void ApplyFullStretch(RectTransform rt)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
		var ap3 = rt.anchoredPosition3D;
		ap3.z = 0f;
		rt.anchoredPosition3D = ap3;
		rt.localScale = Vector3.one;
		rt.localRotation = Quaternion.identity;
	}

	private static Sprite LoadFullRectSpriteAtPath(string assetPath)
	{
		var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
		if (assets == null || assets.Length == 0) return null;

		var fullRect = assets.OfType<Sprite>()
				.FirstOrDefault(s => s != null && s.name.EndsWith("_FullRect", StringComparison.Ordinal));

		return fullRect;
	}

	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return string.Empty;
		var tokens = Regex.Split(input.Trim(), @"[\s_\-]+").Where(t => !string.IsNullOrEmpty(t));

		string UpperFirst(string s)
		{
			if (string.IsNullOrEmpty(s)) return s;
			if (s.Length == 1) return char.ToUpperInvariant(s[0]).ToString();
			return char.ToUpperInvariant(s[0]) + s.Substring(1);
		}
		return string.Concat(tokens.Select(UpperFirst));
	}
}
