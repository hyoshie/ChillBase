// Assets/Editor/ClothesSlotsBuilderWindow.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ClothesSlotsBuilderWindow : EditorWindow
{
	[MenuItem("Tools/Shop/Clothes Slots Builder")]
	static void Open() => GetWindow<ClothesSlotsBuilderWindow>("Clothes Slots Builder");

	// 入力
	string prefix = "usa"; // 例: usa → GO名: USAUpperBodySlot / USALowerBodySlot, SlotID: usa_upper_body / usa_lower_body
	string slotsFolder = "Assets/Data/Shop/ItemSlots/Test";

	// 親（必須: Canvas配下のRectTransform）
	RectTransform slotsParent;

	// 登録先 RAC（未指定なら Auto で親から探索）
	RoomAppearanceController targetAppearance;

	// 定数
	const string UPPER_SUFFIX = "upper_body";
	const string LOWER_SUFFIX = "lower_body";

	void OnGUI()
	{
		EditorGUILayout.LabelField("Create clothing slots (Upper/Lower) as GameObjects and bind to RoomAppearanceController", EditorStyles.boldLabel);

		prefix = EditorGUILayout.TextField("Prefix", prefix);
		slotsFolder = EditorGUILayout.TextField("Slots Folder", slotsFolder);

		slotsParent = (RectTransform)EditorGUILayout.ObjectField("Slots Parent (UI RectTransform)", slotsParent, typeof(RectTransform), true);

		using (new EditorGUILayout.HorizontalScope())
		{
			targetAppearance = (RoomAppearanceController)EditorGUILayout.ObjectField("RoomAppearanceController", targetAppearance, typeof(RoomAppearanceController), true);
			if (GUILayout.Button("Auto", GUILayout.Width(60)))
			{
				if (slotsParent) targetAppearance = slotsParent.GetComponentInParent<RoomAppearanceController>();
			}
		}

		EditorGUILayout.Space(8);
		using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(prefix) || slotsParent == null))
		{
			if (GUILayout.Button("Create & Bind Upper/Lower Slots"))
			{
				CreateAndBind();
			}
		}
	}

	void CreateAndBind()
	{
		if (slotsParent == null)
		{
			EditorUtility.DisplayDialog("Error", "Slots Parent を指定してください。", "OK");
			return;
		}
		if (slotsParent.GetComponentInParent<Canvas>() == null)
		{
			if (!EditorUtility.DisplayDialog("Warning",
					"指定した Slots Parent が Canvas 配下ではありません。続行しますか？",
					"続行", "中止")) return;
		}

		EnsureFolder(slotsFolder);

		// SlotDef を prefix 付きで準備
		var upperId = $"{prefix}_{UPPER_SUFFIX}";
		var lowerId = $"{prefix}_{LOWER_SUFFIX}";
		var upperSlot = EnsureSlotAsset(upperId);
		var lowerSlot = EnsureSlotAsset(lowerId);

		Undo.RegisterFullObjectHierarchyUndo(slotsParent.gameObject, "Create Clothes Slots");

		// Upper
		var upperCtrl = CreateOrUpdateSceneSlot(slotsParent, BuildGoName(prefix, true));
		// Lower
		var lowerCtrl = CreateOrUpdateSceneSlot(slotsParent, BuildGoName(prefix, false));

		// RoomAppearanceController へ登録
		int bound = 0;
		if (targetAppearance == null)
			targetAppearance = slotsParent.GetComponentInParent<RoomAppearanceController>();

		if (targetAppearance != null)
		{
			if (AddOrUpdateBinding(targetAppearance, upperSlot, upperCtrl)) bound++;
			if (AddOrUpdateBinding(targetAppearance, lowerSlot, lowerCtrl)) bound++;
			EditorUtility.SetDirty(targetAppearance);
		}

		EditorGUIUtility.PingObject(slotsParent);
		EditorUtility.DisplayDialog("Done",
				$"Created/Updated:\n- {BuildGoName(prefix, true)}  (slot: {upperId})\n- {BuildGoName(prefix, false)} (slot: {lowerId})\n" +
				$"Bindings updated: {bound}",
				"OK");
	}

	// --------- Scene生成 ---------
	// 名前規則: "USAUpperBodySlot" / "USALowerBodySlot"
	static string BuildGoName(string prefix, bool isUpper)
	{
		var head = string.IsNullOrEmpty(prefix) ? "" : prefix.ToUpperInvariant();
		return head + (isUpper ? "UpperBodySlot" : "LowerBodySlot");
	}

	static RectTransform FindChild(RectTransform parent, string name)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			var rt = parent.GetChild(i) as RectTransform;
			if (rt && rt.name == name) return rt;
		}
		return null;
	}

	RoomItemSlotController CreateOrUpdateSceneSlot(RectTransform parent, string name)
	{
		var rt = FindChild(parent, name);
		if (rt == null)
		{
			var go = new GameObject(name, typeof(RectTransform));
			rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
		}

		// RectTransform: Stretch/Stretch + Offsets 0
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
		rt.pivot = new Vector2(0.5f, 0.5f);
		rt.localScale = Vector3.one;
		rt.localRotation = Quaternion.identity;

		// SlotController 付与＋viewRoot 自身
		var sc = rt.GetComponent<RoomItemSlotController>();
		if (!sc) sc = rt.gameObject.AddComponent<RoomItemSlotController>();
		sc.viewRoot = rt;

		return sc;
	}

	// --------- RAC.slots へ (slot, controller) を追加/更新 ---------
	static bool AddOrUpdateBinding(RoomAppearanceController rac, RoomItemSlotDef slotDef, RoomItemSlotController controller)
	{
		if (!rac || !slotDef || !controller) return false;

		var so = new SerializedObject(rac);
		var slotsProp = so.FindProperty("slots"); // List<SlotBinding> フィールド名は "slots" を想定
		if (slotsProp == null || !slotsProp.isArray)
		{
			Debug.LogWarning("[ClothesSlotsBuilder] RoomAppearanceController に 'slots' 配列が見つかりません。");
			return false;
		}

		// 既存検索（slot 一致）
		int found = -1;
		for (int i = 0; i < slotsProp.arraySize; i++)
		{
			var elem = slotsProp.GetArrayElementAtIndex(i);
			var slotField = elem.FindPropertyRelative("slot");
			if (slotField != null && slotField.objectReferenceValue == slotDef)
			{
				found = i; break;
			}
		}

		if (found >= 0)
		{
			var elem = slotsProp.GetArrayElementAtIndex(found);
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
			Undo.RecordObject(rac, "Add Slot Binding");
			slotsProp.InsertArrayElementAtIndex(slotsProp.arraySize);
			var elem = slotsProp.GetArrayElementAtIndex(slotsProp.arraySize - 1);
			var slotField = elem.FindPropertyRelative("slot");
			var ctrlField = elem.FindPropertyRelative("controller");
			if (slotField == null || ctrlField == null)
			{
				Debug.LogWarning("[ClothesSlotsBuilder] SlotBinding の 'slot' / 'controller' が見つかりません。");
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

	// --------- Asset生成 ---------
	RoomItemSlotDef EnsureSlotAsset(string id)
	{
		var path = $"{slotsFolder}/{id}.asset";
		var slot = AssetDatabase.LoadAssetAtPath<RoomItemSlotDef>(path);
		if (slot == null)
		{
			EnsureFolder(slotsFolder);
			slot = ScriptableObject.CreateInstance<RoomItemSlotDef>();
			slot.id = id;
			AssetDatabase.CreateAsset(slot, path);
		}
		else
		{
			if (slot.id != id) { slot.id = id; EditorUtility.SetDirty(slot); }
		}
		EditorUtility.SetDirty(slot);
		return slot;
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
}
