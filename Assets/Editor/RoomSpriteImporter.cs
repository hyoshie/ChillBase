// 特定のパスに置かれた画像にfullRect版のspriteを自動で追加する
// 元々置かれている場合はreimportすれば反映される
// 右クリックで呼び出すようにした方がいいかも
// https://qiita.com/RyotaMurohoshi/items/b01e3cdb91fea96f4574#%E4%B8%80%E5%BA%A6sprite%E3%81%B8%E3%81%AE%E5%A4%89%E6%9B%B4%E3%82%92%E5%8F%8D%E6%98%A0%E3%81%95%E3%81%9B%E3%82%8B
// Assets/Editor/RoomSpriteImporter.cs
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;

/// <summary>
/// 指定フォルダ（Assets/Art/Sprites/Rooms/）配下のテクスチャについて、
/// - Sprite Import Mode を Multiple に（必要時のみ）
/// - _FullRect スプライト（テクスチャ全体）を追加（未作成時のみ）
/// - Filter Mode = Point (no filter)
/// - Compression = None
/// を自動適用します。
/// 無限リインポートを避けるため、必要変更があった場合のみ delayCall で再インポートします。
/// </summary>
public class RoomSpriteImporter : AssetPostprocessor
{
	// 対象フォルダ（必要なら複数対応に拡張してOK）
	private const string TARGET_ROOT = "Assets/Art/Sprites/Rooms/";

	// 1回だけ処理したいときのフラグ
	private const string FLAG_PROCESSED = "[RoomSpriteImporter_v1]";
	// 再インポートを遅延実行するためのフラグ
	private const string FLAG_PENDING = "[RoomSpriteImporter_Pending]";

	static void OnPostprocessAllAssets(
			string[] imported, string[] deleted, string[] moved, string[] movedFrom)
	{
		// まとめて遅延実行するためのキュー
		var reimportQueue = new List<string>();

		foreach (var path in imported)
		{
			var norm = path.Replace("\\", "/");
			if (!norm.StartsWith(TARGET_ROOT)) continue;

			var importer = AssetImporter.GetAtPath(norm) as TextureImporter;
			if (importer == null) continue;
			if (importer.textureType != TextureImporterType.Sprite) continue;

			// 直前の再インポートで呼び戻された場合は、ここでフラグ剥がしてスキップ（無限ループ防止）
			var ud = importer.userData ?? string.Empty;
			if (ud.Contains(FLAG_PROCESSED))
			{
				importer.userData = ud.Replace(FLAG_PROCESSED, string.Empty);
				continue;
			}

			// 実際の変更を試みる（この関数では SaveAndReimport しない）
			bool changed = EnsureFullRectAndSettings(importer, norm);

			// 変更があったら、PENDING を立てて遅延Reimportを予約
			if (changed && !importer.userData.Contains(FLAG_PENDING))
			{
				importer.userData = (importer.userData ?? "") + ";" + FLAG_PENDING + ";" + FLAG_PROCESSED;
				reimportQueue.Add(norm);
			}
		}

		if (reimportQueue.Count > 0)
		{
			// Editorアイドル時に安全に再インポート（他サブシステムとの競合を避ける）
			EditorApplication.delayCall += () =>
			{
				AssetDatabase.StartAssetEditing();
				try
				{
					foreach (var p in reimportQueue)
					{
						var imp = AssetImporter.GetAtPath(p) as TextureImporter;
						if (imp == null) continue;

						// PENDING を剥がしてから Reimport
						var ud = imp.userData ?? string.Empty;
						if (ud.Contains(FLAG_PENDING))
						{
							ud = ud.Replace(FLAG_PENDING, string.Empty);
							imp.userData = ud;
						}

						// ここで1回だけ Reimport（FLAG_PROCESSED により次ターンはスキップされる）
						imp.SaveAndReimport();
					}
				}
				finally
				{
					AssetDatabase.StopAssetEditing();
				}
			};
		}
	}

	/// <summary>
	/// Importerに対して、Multiple化／_FullRect追加／Filter/Compression変更を行う。
	/// この中では SaveAndReimport しない。何か変更したら true を返す。
	/// </summary>
	private static bool EnsureFullRectAndSettings(TextureImporter importer, string assetPath)
	{
		bool changed = false;

		// Import Mode: Multiple（必要時のみ）
		if (importer.spriteImportMode != SpriteImportMode.Multiple)
		{
			importer.spriteImportMode = SpriteImportMode.Multiple;
			changed = true;
		}

		// DataProvider 取得（Unity 6: Factory->FromObject）
		var factory = new SpriteDataProviderFactories();
		factory.Init();
		var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
		if (dataProvider != null)
		{
			dataProvider.InitSpriteEditorDataProvider();

			// 既存スプライト一覧
			var list = new List<SpriteRect>(dataProvider.GetSpriteRects());
			string fullName = System.IO.Path.GetFileNameWithoutExtension(assetPath) + "_FullRect";

			// まだ _FullRect が無ければ追加
			if (!list.Exists(r => r.name == fullName))
			{
				var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
				if (tex != null)
				{
					var full = new SpriteRect
					{
						name = fullName,
						rect = new Rect(0, 0, tex.width, tex.height),
						alignment = (int)SpriteAlignment.Center,
						pivot = new Vector2(0.5f, 0.5f),
						spriteID = GUID.Generate(),
					};

					list.Add(full);
					dataProvider.SetSpriteRects(list.ToArray());

					// 名前とGUIDの対応を更新（必須）
					var nameId = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
					if (nameId != null)
					{
						var pairs = new List<SpriteNameFileIdPair>(nameId.GetNameFileIdPairs());
						pairs.RemoveAll(p => p.name == full.name);
						pairs.Add(new SpriteNameFileIdPair(full.name, full.spriteID));
						nameId.SetNameFileIdPairs(pairs);
					}

					dataProvider.Apply();
					changed = true;
				}
			}
		}

		// Filter Mode: Point (no filter)
		if (importer.filterMode != FilterMode.Point)
		{
			importer.filterMode = FilterMode.Point;
			changed = true;
		}

		// Compression: None
		if (importer.textureCompression != TextureImporterCompression.Uncompressed)
		{
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			changed = true;
		}

		return changed;
	}
}
