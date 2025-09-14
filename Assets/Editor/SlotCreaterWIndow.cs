// Assets/Editor/SlotCreatorWindow.cs
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SlotCreatorWindow : EditorWindow
{
	[MenuItem("Tools/Shop/Slot Creator")]
	static void Open() => GetWindow<SlotCreatorWindow>("Slot Creator");

	// 親（必須: UI RectTransform）
	RectTransform slotsParent;

	// 生成後に slots にバインドしたい RoomAppearanceController（任意）
	RoomAppearanceController targetAppearance;

	// 一覧
	Vector2 scroll;
	RoomItemSlotDef[] allSlots = new RoomItemSlotDef[0];
	HashSet<RoomItemSlotDef> selected = new HashSet<RoomItemSlotDef>();

	// 簡易検索
	string search = "";

	void OnEnable()
	{
		RefreshList();
	}

	void RefreshList()
	{
		var guids = AssetDatabase.FindAssets("t:ItemSlotDef");
		allSlots = guids
				.Select(g => AssetDatabase.LoadAssetAtPath<RoomItemSlotDef>(AssetDatabase.GUIDToAssetPath(g)))
				.Where(a => a != null)
				.OrderBy(a => a.name)
				.ToArray();
	}

	void OnGUI()
	{
		EditorGUILayout.LabelField("Create Slot GameObjects under a parent", EditorStyles.boldLabel);

		slotsParent = (RectTransform)EditorGUILayout.ObjectField("Slots Parent (UI RectTransform)", slotsParent, typeof(RectTransform), true);

		// RoomAppearanceController の指定（未指定時は slotsParent から自動推測ボタンあり）
		using (new EditorGUILayout.HorizontalScope())
		{
			targetAppearance = (RoomAppearanceController)EditorGUILayout.ObjectField("Bind to RoomAppearanceController", targetAppearance, typeof(RoomAppearanceController), true);
			if (GUILayout.Button("Auto", GUILayout.Width(60)))
			{
				if (slotsParent != null)
					targetAppearance = slotsParent.GetComponentInParent<RoomAppearanceController>();
			}
		}

		using (new EditorGUILayout.HorizontalScope())
		{
			search = EditorGUILayout.TextField("Search", search);
			if (GUILayout.Button("Refresh", GUILayout.Width(80)))
				RefreshList();
		}

		EditorGUILayout.Space(4);
		using (var sv = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.MinHeight(180)))
		{
			scroll = sv.scrollPosition;
			if (allSlots.Length == 0)
			{
				EditorGUILayout.HelpBox("No ItemSlotDef assets found.", MessageType.Info);
			}
			else
			{
				foreach (var slot in allSlots)
				{
					if (!string.IsNullOrEmpty(search) && !slot.name.ToLowerInvariant().Contains(search.ToLowerInvariant()))
						continue;

					bool isSel = selected.Contains(slot);
					bool newSel = EditorGUILayout.ToggleLeft($"{slot.name}", isSel);
					if (newSel && !isSel) selected.Add(slot);
					else if (!newSel && isSel) selected.Remove(slot);
				}
			}
		}

		EditorGUILayout.Space(8);
		using (new EditorGUI.DisabledScope(slotsParent == null || selected.Count == 0))
		{
			if (GUILayout.Button($"Create & Bind {selected.Count} Slot GameObject(s)"))
			{
				CreateSlotsAndBind();
			}
		}
	}

	void CreateSlotsAndBind()
	{
		if (slotsParent == null)
		{
			EditorUtility.DisplayDialog("Error", "Slots Parent を指定してください。", "OK");
			return;
		}

		if (slotsParent.GetComponentInParent<Canvas>() == null)
		{
			if (!EditorUtility.DisplayDialog("Warning",
					"指定した Slots Parent が Canvas 配下ではありません。UI 要素としては不適切な可能性があります。\n続行しますか？",
					"続行", "中止"))
			{
				return;
			}
		}

		Undo.RegisterFullObjectHierarchyUndo(slotsParent.gameObject, "Create Slots");

		int created = 0, updated = 0, bound = 0;
		foreach (var slotDef in selected)
		{
			string goName = BuildSlotObjectName(slotDef.name);
			var child = FindChild(slotsParent, goName);
			bool isNew = false;
			if (child == null)
			{
				var go = new GameObject(goName, typeof(RectTransform));
				child = go.GetComponent<RectTransform>();
				child.SetParent(slotsParent, false);
				isNew = true;
			}

			// RectTransform: Stretch/Stretch, offsets=0, pivot=0.5
			child.anchorMin = Vector2.zero;
			child.anchorMax = Vector2.one;
			child.offsetMin = Vector2.zero;
			child.offsetMax = Vector2.zero;
			child.pivot = new Vector2(0.5f, 0.5f);
			child.localScale = Vector3.one;
			child.localRotation = Quaternion.identity;

			// SlotController を付け、自身を viewRoot に
			var sc = child.GetComponent<RoomItemSlotController>();
			if (sc == null)
				sc = child.gameObject.AddComponent<RoomItemSlotController>();
			sc.viewRoot = child;

			if (isNew) created++; else updated++;

			// ----- RoomAppearanceController.slots にバインド（任意） -----
			if (targetAppearance != null)
			{
				if (AddOrUpdateBinding(targetAppearance, slotDef, sc))
					bound++;
			}
		}

		if (targetAppearance != null)
		{
			EditorUtility.SetDirty(targetAppearance);
		}

		EditorGUIUtility.PingObject(slotsParent);
		EditorUtility.DisplayDialog("Done",
				$"Created: {created}\nUpdated: {updated}\nBound to RoomAppearanceController: {bound}\nParent: {GetFullPath(slotsParent)}",
				"OK");
	}

	// ---- Binding: RoomAppearanceController.slots へ (SlotDef + SlotController) を追加/更新 ----
	static bool AddOrUpdateBinding(RoomAppearanceController rac, RoomItemSlotDef slotDef, RoomItemSlotController controller)
	{
		if (rac == null || slotDef == null || controller == null) return false;

		// Serialized 経由で slots(List<SlotBinding>) を安全に編集
		var so = new SerializedObject(rac);
		var slotsProp = so.FindProperty("slots");   // ← フィールド名が "slots" である前提
		if (slotsProp == null || !slotsProp.isArray)
		{
			Debug.LogWarning("[SlotCreator] RoomAppearanceController に 'slots' 配列が見つかりません。フィールド名を確認してください。");
			return false;
		}

		// 既存に同じ slot があるか確認（slot 比較）
		int foundIndex = -1;
		for (int i = 0; i < slotsProp.arraySize; i++)
		{
			var elem = slotsProp.GetArrayElementAtIndex(i);
			var slotField = elem.FindPropertyRelative("slot");
			if (slotField != null && slotField.objectReferenceValue == slotDef)
			{
				foundIndex = i;
				break;
			}
		}

		if (foundIndex >= 0)
		{
			// 既存の controller を差し替え（上書き）
			var elem = slotsProp.GetArrayElementAtIndex(foundIndex);
			var ctrlField = elem.FindPropertyRelative("controller");
			if (ctrlField != null)
			{
				Undo.RecordObject(rac, "Update Slot Binding");
				ctrlField.objectReferenceValue = controller;
				so.ApplyModifiedProperties();
				return true;
			}
			return false;
		}
		else
		{
			// 新規追加
			Undo.RecordObject(rac, "Add Slot Binding");
			slotsProp.InsertArrayElementAtIndex(slotsProp.arraySize);
			var elem = slotsProp.GetArrayElementAtIndex(slotsProp.arraySize - 1);
			var slotField = elem.FindPropertyRelative("slot");
			var ctrlField = elem.FindPropertyRelative("controller");
			if (slotField == null || ctrlField == null)
			{
				Debug.LogWarning("[SlotCreator] SlotBinding のフィールド名 'slot' / 'controller' が見つかりません。クラス定義を確認してください。");
				// 失敗した追加要素をクリーンアップ
				slotsProp.DeleteArrayElementAtIndex(slotsProp.arraySize - 1);
				so.ApplyModifiedProperties();
				return false;
			}
			slotField.objectReferenceValue = slotDef;
			ctrlField.objectReferenceValue = controller;
			so.ApplyModifiedProperties();
			return true;
		}
	}

	// ---- Helpers ----

	static RectTransform FindChild(RectTransform parent, string name)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			var rt = parent.GetChild(i) as RectTransform;
			if (rt != null && rt.name == name) return rt;
		}
		return null;
	}

	// 例: "usa_coffee1" → "USACoffee1Slot"
	static string BuildSlotObjectName(string slotDefName)
	{
		if (string.IsNullOrEmpty(slotDefName)) return "Slot";

		// normalize: - と space を _
		var normalized = slotDefName.Replace("-", "_").Replace(" ", "_");

		var parts = normalized.Split(new[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return "Slot";

		var sb = new StringBuilder();

		// 最初の部分は全部大文字
		// sb.Append(parts[0].ToUpperInvariant());

		// 2つ目以降は PascalCase
		for (int i = 1; i < parts.Length; i++)
		{
			var p = parts[i];
			if (p.Length == 0) continue;
			sb.Append(char.ToUpperInvariant(p[0]));
			if (p.Length > 1) sb.Append(p.Substring(1));
		}

		sb.Append("Slot");
		return sb.ToString();
	}

	static string GetFullPath(Transform t)
	{
		if (t == null) return "(null)";
		var names = new List<string>();
		while (t != null) { names.Add(t.name); t = t.parent; }
		names.Reverse();
		return string.Join("/", names);
	}
}
