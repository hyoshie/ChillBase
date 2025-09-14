// Assets/Editor/ClothesItemFactoryWindow.cs
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ClothesItemFactoryWindow : EditorWindow
{
	[MenuItem("Tools/Shop/Clothes Item Factory")]
	static void Open() => GetWindow<ClothesItemFactoryWindow>("Clothes Item Factory");

	// 入力
	string sourceFolder = "Assets/Art/Sprites/Rooms/usa-prod/Item/Clothes"; // 服スプライトのあるフォルダ（サブフォルダ除外）
	string prefix = "usa";       // id の接頭辞（最終 id は "usa_<key>" になる）
	string subfolderUnderPrefix = "clothes"; // ★ 追加: prefix配下のサブフォルダ
	int defaultPrice = 10;
	Sprite defaultShopIcon;

	// 保存先（ベース）
	string itemsFolder = "Assets/Data/Shop/ItemShop/Items";
	string slotsFolder = "Assets/Data/Shop/ItemSlots";

	// 固定スロット名（接尾辞）
	const string UPPER_SUFFIX = "upper_body";
	const string LOWER_SUFFIX = "lower_body";

	// DB
	RoomItemShopCatalog database;

	// Allowed Rooms（チェックボックス）
	Vector2 roomScroll;
	HashSet<RoomDef> selectedRooms = new HashSet<RoomDef>();
	RoomDef[] allRooms;

	static readonly string[] kImageExt = { ".png", ".psd", ".tif", ".tiff", ".jpg", ".jpeg" };

	void OnEnable()
	{
		var guids = AssetDatabase.FindAssets("t:RoomDef");
		allRooms = guids.Select(g => AssetDatabase.LoadAssetAtPath<RoomDef>(AssetDatabase.GUIDToAssetPath(g)))
										.Where(r => r != null)
										.OrderBy(r => r.name)
										.ToArray();
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("Make one Item per (prefix) pairing of *-up* & *-low* files", EditorStyles.boldLabel);

		sourceFolder = FolderField("Source Folder (sprites)", sourceFolder);
		prefix = EditorGUILayout.TextField("ID Prefix", prefix);
		subfolderUnderPrefix = EditorGUILayout.TextField("Subfolder under Prefix", subfolderUnderPrefix); // ★ 追加

		EditorGUILayout.Space(6);
		defaultPrice = EditorGUILayout.IntField("Default Price", defaultPrice);
		defaultShopIcon = (Sprite)EditorGUILayout.ObjectField("Fallback Shop Icon", defaultShopIcon, typeof(Sprite), false);

		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField("Output Folders");
		itemsFolder = EditorGUILayout.TextField("Items Folder (base)", itemsFolder);
		slotsFolder = EditorGUILayout.TextField("Slots Folder (base)", slotsFolder);

		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField("Allowed Rooms", EditorStyles.boldLabel);
		roomScroll = EditorGUILayout.BeginScrollView(roomScroll, GUILayout.Height(120));
		if (allRooms != null && allRooms.Length > 0)
		{
			foreach (var room in allRooms)
			{
				bool isSel = selectedRooms.Contains(room);
				bool newSel = EditorGUILayout.ToggleLeft(room.name, isSel);
				if (newSel && !isSel) selectedRooms.Add(room);
				else if (!newSel && isSel) selectedRooms.Remove(room);
			}
		}
		else
		{
			EditorGUILayout.LabelField("(No RoomDef assets found)");
		}
		EditorGUILayout.EndScrollView();

		EditorGUILayout.Space(6);
		database = (RoomItemShopCatalog)EditorGUILayout.ObjectField("Shop Database", database, typeof(RoomItemShopCatalog), false);

		using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(sourceFolder) || !AssetDatabase.IsValidFolder(sourceFolder)))
		{
			if (GUILayout.Button("Scan & Create"))
				CreateAllFromFolder();
		}
	}

	string FolderField(string label, string path)
	{
		EditorGUILayout.BeginHorizontal();
		path = EditorGUILayout.TextField(label, path);
		if (GUILayout.Button("…", GUILayout.Width(28)))
		{
			var selected = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
			if (!string.IsNullOrEmpty(selected))
			{
				if (selected.StartsWith(Application.dataPath))
					path = "Assets" + selected.Substring(Application.dataPath.Length);
				else
					EditorUtility.DisplayDialog("Invalid Folder", "プロジェクト内(Assets配下)を選んでください。", "OK");
			}
		}
		EditorGUILayout.EndHorizontal();
		return path;
	}

	void CreateAllFromFolder()
	{
		// DBが未指定なら自動検出
		if (database == null)
		{
			var guids = AssetDatabase.FindAssets("t:ItemShopDatabase");
			if (guids.Length > 0)
			{
				var p = AssetDatabase.GUIDToAssetPath(guids[0]);
				database = AssetDatabase.LoadAssetAtPath<RoomItemShopCatalog>(p);
			}
		}

		// ---- 保存先: base / prefix / subfolder の3段構成にする ----
		var itemsOut = CombineUnityPath(itemsFolder, prefix);
		var slotsOut = CombineUnityPath(slotsFolder, prefix);
		if (!string.IsNullOrEmpty(subfolderUnderPrefix))
		{
			itemsOut = CombineUnityPath(itemsOut, subfolderUnderPrefix);
			slotsOut = CombineUnityPath(slotsOut, subfolderUnderPrefix);
		}
		EnsureFolder(itemsOut);
		EnsureFolder(slotsOut);
		// --------------------------------------------------------

		// 上下2スロットの確保（prefixを付けたIDにする）→ 保存先も slotsOut
		var upperSlot = EnsureSlotAsset($"{prefix}_{UPPER_SUFFIX}", slotsOut);
		var lowerSlot = EnsureSlotAsset($"{prefix}_{LOWER_SUFFIX}", slotsOut);

		// 直下ファイルのみ対象
		var absSrc = Application.dataPath + sourceFolder.Substring("Assets".Length);
		if (!Directory.Exists(absSrc))
		{
			EditorUtility.DisplayDialog("Error", $"Source folder not found:\n{sourceFolder}", "OK");
			return;
		}

		var files = Directory.GetFiles(absSrc, "*.*", SearchOption.TopDirectoryOnly)
												 .Where(p => kImageExt.Contains(Path.GetExtension(p).ToLowerInvariant()))
												 .Select(p => "Assets" + p.Substring(Application.dataPath.Length))
												 .ToArray();

		if (files.Length == 0)
		{
			EditorUtility.DisplayDialog("No Sprites", $"対象画像ファイルが見つかりませんでした。\n{sourceFolder}", "OK");
			return;
		}

		// キー（共通接頭辞）でグルーピング：例）"c1-up1" → "c1"（最初の '-' の手前）
		var groups = new Dictionary<string, List<string>>();
		foreach (var path in files)
		{
			var file = Path.GetFileNameWithoutExtension(path);
			var key = file;
			var idx = file.IndexOf('-');
			if (idx >= 0) key = file.Substring(0, idx);
			if (!groups.TryGetValue(key, out var list))
				groups[key] = list = new List<string>();
			list.Add(path);
		}

		int made = 0, updated = 0;
		try
		{
			AssetDatabase.StartAssetEditing();

			foreach (var kv in groups)
			{
				var key = kv.Key;            // 例: "c1"
				var paths = kv.Value;        // up/low の入った複数候補

				// up / low を判定（ファイル名に "up" / "low" を含む）
				string upPath = paths.FirstOrDefault(p => NameOf(p).Contains("up"));
				string lowPath = paths.FirstOrDefault(p => NameOf(p).Contains("low"));

				// スプライト取得
				var upSprites = LoadSprites(upPath);
				var lowSprites = LoadSprites(lowPath);

				// *_FullRect / *_0 の抽出
				Sprite upFull = upSprites?.FirstOrDefault(s => s.name.EndsWith("_FullRect"));
				Sprite lowFull = lowSprites?.FirstOrDefault(s => s.name.EndsWith("_FullRect"));

				Sprite upIcon = upSprites?.FirstOrDefault(s => s.name.EndsWith("_0"));
				Sprite lowIcon = lowSprites?.FirstOrDefault(s => s.name.EndsWith("_0"));

				// shopIcon は up優先 → 無ければ low → 無ければ fallback
				var shopIcon = upIcon ?? lowIcon ?? defaultShopIcon;

				// 最終ID
				var normKey = NormalizeId(key);
				var idName = $"{prefix}_{normKey}";

				// RoomItemDef の作成/更新（★ itemsOut を使用）
				var itemPath = $"{itemsOut}/{idName}.asset";
				var item = AssetDatabase.LoadAssetAtPath<RoomItemDef>(itemPath);
				if (item == null)
				{
					item = ScriptableObject.CreateInstance<RoomItemDef>();
					item.id = idName;
					item.displayName = ObjectNames.NicifyVariableName(normKey.Replace('_', ' '));
					item.price = defaultPrice;
					item.shopIcon = shopIcon;

					var visualsList = new List<RoomItemVisual>
										{
												new RoomItemVisual { slot = upperSlot, sprite = upFull },
												new RoomItemVisual { slot = lowerSlot, sprite = lowFull }
										};
					item.visuals = visualsList.ToArray();
					item.allowedRooms = selectedRooms.ToList();

					AssetDatabase.CreateAsset(item, itemPath);
					made++;
				}
				else
				{
					var list = item.visuals != null ? item.visuals.ToList() : new List<RoomItemVisual>();

					var vUp = list.FirstOrDefault(v => v != null && v.slot == upperSlot);
					if (vUp == null) list.Add(new RoomItemVisual { slot = upperSlot, sprite = upFull });
					else vUp.sprite = upFull;

					var vLow = list.FirstOrDefault(v => v != null && v.slot == lowerSlot);
					if (vLow == null) list.Add(new RoomItemVisual { slot = lowerSlot, sprite = lowFull });
					else vLow.sprite = lowFull;

					item.visuals = list.ToArray();

					if (item.price <= 0) item.price = defaultPrice;
					if (item.shopIcon == null) item.shopIcon = shopIcon;
					item.allowedRooms = selectedRooms.ToList();

					EditorUtility.SetDirty(item);
					updated++;
				}

				// DB登録
				if (database != null)
				{
					var list = (database.items != null) ? database.items.ToList() : new List<RoomItemDef>();
					if (!list.Contains(item))
					{
						list.Add(item);
						database.items = list.ToArray();
						EditorUtility.SetDirty(database);
					}
				}
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		var dbPath = database ? AssetDatabase.GetAssetPath(database) : "(not set)";
		EditorUtility.DisplayDialog("Done",
				$"Items created: {made}, updated: {updated}\n" +
				$"Items Folder: {itemsOut}\nSlots Folder: {slotsOut}\nDB: {dbPath}",
				"OK");
	}

	// ---- Helpers ----

	static string NameOf(string assetPath)
			=> string.IsNullOrEmpty(assetPath) ? "" : Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

	static Sprite[] LoadSprites(string assetPath)
	{
		if (string.IsNullOrEmpty(assetPath)) return null;
		var subs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
		return subs?.OfType<Sprite>().ToArray();
	}

	// ★ slotsFolder を引数化して、その配下に作成
	RoomItemSlotDef EnsureSlotAsset(string id, string slotsFolderOut)
	{
		var path = $"{slotsFolderOut}/{id}.asset";
		var slot = AssetDatabase.LoadAssetAtPath<RoomItemSlotDef>(path);
		if (slot == null)
		{
			slot = ScriptableObject.CreateInstance<RoomItemSlotDef>();
			slot.id = id;
			EnsureFolder(slotsFolderOut);
			AssetDatabase.CreateAsset(slot, path);
			EditorUtility.SetDirty(slot);
		}
		else
		{
			if (slot.id != id) { slot.id = id; EditorUtility.SetDirty(slot); }
		}
		return slot;
	}

	static string NormalizeId(string s)
	{
		if (string.IsNullOrEmpty(s)) return s;
		s = s.Replace("-", "_").Replace(" ", "_");
		s = Regex.Replace(s, @"[^0-9A-Za-z_]+", "_"); // 連続する非英数字を _
		s = Regex.Replace(s, @"_+", "_");
		return s.Trim('_');
	}

	static void EnsureFolder(string folder)
	{
		if (AssetDatabase.IsValidFolder(folder)) return;
		var parts = folder.Split('/');
		var cur = "";
		for (int i = 0; i < parts.Length; i++)
		{
			if (i == 0) { cur = parts[0]; continue; } // "Assets"
			var next = $"{cur}/{parts[i]}";
			if (!AssetDatabase.IsValidFolder(next))
				AssetDatabase.CreateFolder(cur, parts[i]);
			cur = next;
		}
	}

	static string CombineUnityPath(string a, string b)
	{
		if (string.IsNullOrEmpty(a)) return b ?? "";
		if (string.IsNullOrEmpty(b)) return a ?? "";
		a = a.TrimEnd('/', '\\');
		b = b.TrimStart('/', '\\');
		return $"{a}/{b}";
	}
}
