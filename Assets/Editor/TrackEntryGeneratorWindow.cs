// Assets/Editor/TrackEntryGeneratorWindow.cs
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public class TrackEntryGeneratorWindow : EditorWindow
{
	[SerializeField] private DefaultAsset sourceFolder;       // 走査元（AudioClip）
	[SerializeField] private DefaultAsset destinationFolder;  // 生成先（ここ直下に Catalog、配下の Entry/ に TrackEntry）
	[SerializeField] private bool createCatalog = false;
	[SerializeField] private string catalogAssetName = "TrackCatalog_Generated";

	[MenuItem("Tools/Music/Generate TrackEntries from Addressable AudioClips")]
	public static void Open()
	{
		GetWindow<TrackEntryGeneratorWindow>("TrackEntry Generator");
	}

	private void OnGUI()
	{
		GUILayout.Label("Generate TrackEntry from Addressable AudioClips", EditorStyles.boldLabel);
		EditorGUILayout.Space();

		sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
				new GUIContent("Source Folder", "走査対象（AudioClip を探す）"),
				sourceFolder, typeof(DefaultAsset), false);

		destinationFolder = (DefaultAsset)EditorGUILayout.ObjectField(
				new GUIContent("Destination Folder", "Catalog はここ、TrackEntry はこの配下の Entry/ に保存"),
				destinationFolder, typeof(DefaultAsset), false);

		EditorGUILayout.Space();
		createCatalog = EditorGUILayout.ToggleLeft(new GUIContent("Also create TrackCatalog", "生成した TrackEntry を集めたカタログを同時に作成"), createCatalog);
		if (createCatalog)
		{
			catalogAssetName = EditorGUILayout.TextField(new GUIContent("Catalog Asset Name"), catalogAssetName);
		}

		EditorGUILayout.Space();

		using (new EditorGUI.DisabledScope(sourceFolder == null || destinationFolder == null))
		{
			if (GUILayout.Button("Scan & Generate"))
			{
				Generate();
			}
		}

		EditorGUILayout.HelpBox(
				"・対象は Addressables に登録済みの AudioClip のみ\n" +
				"・displayName とファイル名は AudioClip 名を使用\n" +
				"・TrackEntry は Destination/Entry/ に保存、Catalog は Destination/ 直下に保存\n" +
				"・重複名は自動回避（GenerateUniqueAssetPath）",
				MessageType.Info);
	}

	private void Generate()
	{
		var settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings == null)
		{
			EditorUtility.DisplayDialog("Addressables Settings not found",
					"AddressableAssetSettings が見つかりません。Addressables を初期化してください。", "OK");
			return;
		}

		string srcPath = AssetDatabase.GetAssetPath(sourceFolder);
		string dstPath = AssetDatabase.GetAssetPath(destinationFolder);

		if (string.IsNullOrEmpty(srcPath) || string.IsNullOrEmpty(dstPath) ||
				!AssetDatabase.IsValidFolder(srcPath) || !AssetDatabase.IsValidFolder(dstPath))
		{
			EditorUtility.DisplayDialog("Invalid Folders", "有効な Source/Destination フォルダを指定してください。", "OK");
			return;
		}

		// Entry サブフォルダを用意（なければ作成）
		string entriesDirPath = dstPath.TrimEnd('/') + "/Entry";
		if (!AssetDatabase.IsValidFolder(entriesDirPath))
		{
			AssetDatabase.CreateFolder(dstPath, "Entry"); // 自動作成
		}

		string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { srcPath });
		if (audioGuids.Length == 0)
		{
			EditorUtility.DisplayDialog("No AudioClips", "指定フォルダ内に AudioClip が見つかりません。", "OK");
			return;
		}

		var createdEntries = new List<TrackEntry>();
		int createdCount = 0;
		int skippedNotAddressable = 0;

		AssetDatabase.StartAssetEditing();
		try
		{
			foreach (var guid in audioGuids)
			{
				// Addressables 登録確認
				var addrEntry = settings.FindAssetEntry(guid);
				if (addrEntry == null)
				{
					skippedNotAddressable++;
					continue; // Addressable でないものはスキップ
				}

				string clipPath = AssetDatabase.GUIDToAssetPath(guid);
				string name = Path.GetFileNameWithoutExtension(clipPath);

				// TrackEntry 生成
				var trackEntry = ScriptableObject.CreateInstance<TrackEntry>();
				trackEntry.displayName = name;
				trackEntry.clip = new AssetReferenceT<AudioClip>(guid);

				string savePath = AssetDatabase.GenerateUniqueAssetPath(entriesDirPath + "/" + name + ".asset");
				AssetDatabase.CreateAsset(trackEntry, savePath);
				createdEntries.Add(trackEntry);
				createdCount++;
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		// カタログ（Destination 直下）
		TrackCatalog catalog = null;
		if (createCatalog && createdEntries.Count > 0)
		{
			catalog = ScriptableObject.CreateInstance<TrackCatalog>();
			catalog.tracks = createdEntries.ToArray();

			string catalogPath = AssetDatabase.GenerateUniqueAssetPath(dstPath.TrimEnd('/') + "/" + catalogAssetName + ".asset");
			AssetDatabase.CreateAsset(catalog, catalogPath);
			EditorUtility.SetDirty(catalog);
			AssetDatabase.SaveAssets();
		}

		string msg =
				$"生成: {createdCount}\n" +
				$"スキップ（Addressable 未登録）: {skippedNotAddressable}\n" +
				$"保存先（TrackEntry）: {entriesDirPath}\n" +
				(catalog != null ? $"カタログ: {catalog.name}（{dstPath} 直下）\n" : "");
		EditorUtility.DisplayDialog("TrackEntry Generation Complete", msg, "OK");
	}
}
