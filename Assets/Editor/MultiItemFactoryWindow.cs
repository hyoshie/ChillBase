// Assets/Editor/MultiItemFactoryWindow.cs
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiItemFactoryWindow : EditorWindow
{
	[MenuItem("Tools/Shop/Multi Item Factory")]
	static void Open() => GetWindow<MultiItemFactoryWindow>("Multi Item Factory");

	// 入力
	string sourceFolder = "Assets/Art/Items";
	int defaultPrice = 10;
	Sprite defaultShopIcon;
	string prefix = "usa";   // 接頭辞

	// 出力先（ベース）
	string itemsFolder = "Assets/Data/Shop/ItemShop/Items";
	string slotsFolder = "Assets/Data/Shop/ItemSlots";

	// DB
	RoomItemShopCatalog database;

	// Allowed Rooms
	Vector2 roomScroll;
	HashSet<RoomDef> selectedRooms = new HashSet<RoomDef>();
	RoomDef[] allRooms;

	static readonly string[] kImageExt = { ".png", ".psd", ".tif", ".tiff", ".jpg", ".jpeg" };

	void OnEnable()
	{
		var guids = AssetDatabase.FindAssets("t:RoomDef");
		allRooms = guids.Select(g => AssetDatabase.LoadAssetAtPath<RoomDef>(AssetDatabase.GUIDToAssetPath(g)))
										.Where(r => r != null).ToArray();
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("Batch Create Item + Slot + Link to DB", EditorStyles.boldLabel);

		sourceFolder = FolderField("Source Folder (sprites)", sourceFolder);

		EditorGUILayout.Space(6);
		prefix = EditorGUILayout.TextField("ID Prefix", prefix);

		EditorGUILayout.Space(6);
		defaultPrice = EditorGUILayout.IntField("Default Price", defaultPrice);
		defaultShopIcon = (Sprite)EditorGUILayout.ObjectField("Fallback Shop Icon", defaultShopIcon, typeof(Sprite), false);

		EditorGUILayout.Space(6);
		EditorGUILayout.LabelField("Allowed Rooms", EditorStyles.boldLabel);

		roomScroll = EditorGUILayout.BeginScrollView(roomScroll, GUILayout.Height(120));
		if (allRooms != null && allRooms.Length > 0)
		{
			foreach (var room in allRooms)
			{
				bool isSelected = selectedRooms.Contains(room);
				bool newSelected = EditorGUILayout.ToggleLeft(room.name, isSelected);
				if (newSelected && !isSelected) selectedRooms.Add(room);
				else if (!newSelected && isSelected) selectedRooms.Remove(room);
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
			{
				CreateAllFromFolder();
			}
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
		if (database == null)
		{
			var guids = AssetDatabase.FindAssets("t:ItemShopDatabase");
			if (guids.Length > 0)
			{
				var p = AssetDatabase.GUIDToAssetPath(guids[0]);
				database = AssetDatabase.LoadAssetAtPath<RoomItemShopCatalog>(p);
			}
		}

		// --- ここから：prefix サブフォルダを用意してそこに保存 ---
		var itemsFolderWithPrefix = CombineUnityPath(itemsFolder, prefix);
		var slotsFolderWithPrefix = CombineUnityPath(slotsFolder, prefix);
		EnsureFolder(itemsFolderWithPrefix);
		EnsureFolder(slotsFolderWithPrefix);
		// ------------------------------------------------------

		var absSrc = Application.dataPath + sourceFolder.Substring("Assets".Length);
		if (!Directory.Exists(absSrc))
		{
			EditorUtility.DisplayDialog("Error", $"Source folder not found:\n{sourceFolder}", "OK");
			return;
		}

		var targetFiles = Directory.GetFiles(absSrc, "*.*", SearchOption.TopDirectoryOnly)
															 .Where(p => kImageExt.Contains(Path.GetExtension(p).ToLowerInvariant()))
															 .ToArray();

		if (targetFiles.Length == 0)
		{
			EditorUtility.DisplayDialog("No Sprites", $"対象画像ファイルが見つかりませんでした。\n{sourceFolder}", "OK");
			return;
		}

		var created = new List<string>();
		try
		{
			AssetDatabase.StartAssetEditing();

			foreach (var absPath in targetFiles)
			{
				var assetPath = "Assets" + absPath.Substring(Application.dataPath.Length);
				var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
				var sprites = subAssets.OfType<Sprite>().ToArray();

				var fullRect = sprites.FirstOrDefault(s => s != null && s.name.EndsWith("_FullRect"));
				var shop0 = sprites.FirstOrDefault(s => s != null && s.name.EndsWith("_0"));

				var baseName = Path.GetFileNameWithoutExtension(assetPath);
				baseName = baseName.Replace("-", "_").Replace(" ", "_");
				var idName = $"{prefix}_{baseName}";

				// 1) SlotDef -> prefix サブフォルダ配下
				var slotPath = $"{slotsFolderWithPrefix}/{idName}.asset";
				var slot = AssetDatabase.LoadAssetAtPath<RoomItemSlotDef>(slotPath);
				if (slot == null)
				{
					slot = ScriptableObject.CreateInstance<RoomItemSlotDef>();
					slot.id = idName;
					AssetDatabase.CreateAsset(slot, slotPath);
					EditorUtility.SetDirty(slot);
				}

				// 2) RoomItemDef -> prefix サブフォルダ配下
				var itemPath = $"{itemsFolderWithPrefix}/{idName}.asset";
				var item = AssetDatabase.LoadAssetAtPath<RoomItemDef>(itemPath);
				if (item == null)
				{
					item = ScriptableObject.CreateInstance<RoomItemDef>();
					item.id = idName;
					item.displayName = ObjectNames.NicifyVariableName(baseName.Replace('_', ' '));
					item.price = defaultPrice;
					item.shopIcon = shop0 ?? defaultShopIcon;
					item.visuals = new RoomItemVisual[]
					{
												new RoomItemVisual { slot = slot, sprite = fullRect }
					};
					item.allowedRooms = selectedRooms.ToList();
					AssetDatabase.CreateAsset(item, itemPath);
				}
				else
				{
					var list = item.visuals != null ? item.visuals.ToList() : new List<RoomItemVisual>();
					var v = list.FirstOrDefault(x => x != null && x.slot == slot);
					if (v == null)
						list.Add(new RoomItemVisual { slot = slot, sprite = fullRect });
					else
						v.sprite = fullRect;
					item.visuals = list.ToArray();

					if (item.price <= 0) item.price = defaultPrice;
					if (item.shopIcon == null) item.shopIcon = shop0 ?? defaultShopIcon;

					item.allowedRooms = selectedRooms.ToList();

					EditorUtility.SetDirty(item);
				}

				// 3) DBに追加
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

				created.Add(idName);
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		var dbPath = database ? AssetDatabase.GetAssetPath(database) : "(not set)";
		EditorUtility.DisplayDialog(
				"Done",
				$"Processed {created.Count} files under:\n- {sourceFolder}\n\n" +
				$"Created/Updated Items: {itemsFolderWithPrefix}\nSlots: {slotsFolderWithPrefix}\nDB: {dbPath}",
				"OK"
		);

		if (database) { Selection.activeObject = database; EditorGUIUtility.PingObject(database); }
	}

	// --- helpers ---
	static string CombineUnityPath(string a, string b)
	{
		if (string.IsNullOrEmpty(a)) return b ?? "";
		if (string.IsNullOrEmpty(b)) return a ?? "";
		a = a.TrimEnd('/', '\\');
		b = b.TrimStart('/', '\\');
		return $"{a}/{b}";
	}

	static void EnsureFolder(string folder)
	{
		if (AssetDatabase.IsValidFolder(folder)) return;
		var parts = folder.Split('/');
		var cur = "";
		for (int i = 0; i < parts.Length; i++)
		{
			if (i == 0) { cur = parts[0]; continue; }
			var next = $"{cur}/{parts[i]}";
			if (!AssetDatabase.IsValidFolder(next))
				AssetDatabase.CreateFolder(cur, parts[i]);
			cur = next;
		}
	}
}
