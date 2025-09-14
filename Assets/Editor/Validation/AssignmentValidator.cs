// Assets/Scripts/Editor/Validation/ScriptableObjectValidator.cs
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;

[InitializeOnLoad]
public static class AssignmentValidator
{
	// --- 設定 ---
	// 検索対象フォルダ（SO/Prefab アセットの検証対象）
	static readonly string[] SearchFolders = { "Assets/GameConfig", "Assets/Data", "Assets/Prefabs" };

	// 自作アセンブリの「接頭辞」ホワイトリスト
	// 例: "ScriptsAssembly" -> "ScriptsAssembly" と "ScriptsAssembly.Editor" を両方許可
	static readonly string[] AllowedAssemblyPrefixes = {
				"Assembly-CSharp",   // asmdef未使用の自作コード
        "ScriptsAssembly",   // ★ あなたの asmdef
    };

	// デバウンス
	static double _nextRunAt = 0;
	const double DebounceSec = 0.3;

	struct Issue { public string message; public UnityEngine.Object context; }

	static AssignmentValidator()
	{
		// プロジェクトのアセット変更→軽い検証（SO/Prefabのみ）
		EditorApplication.projectChanged += DebouncedRun;

		// Play直前：SO/Prefab + シーン上MBを全部検証、未設定なら停止ダイアログ
		EditorApplication.playModeStateChanged += state =>
		{
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				var issues = new List<Issue>();
				issues.AddRange(ValidateAllScriptableObjects());
				issues.AddRange(ValidateAllPrefabs());
				issues.AddRange(ValidateSceneMonoBehaviours());

				if (issues.Count > 0)
				{
					if (EditorUtility.DisplayDialog(
									"Validation Failed",
									$"未割り当ての参照が {issues.Count} 件見つかりました。\nPlayを中止しますか？",
									"中止する", "続行"))
					{
						EditorApplication.isPlaying = false;
						foreach (var i in issues) Debug.LogWarning(i.message, i.context);
					}
				}
			}
		};
	}

	// 手動メニュー：総合検証
	[MenuItem("Tools/Validation/Validate All(Scene,SO,Prefab)")]
	public static void RunManual()
	{
		var issues = new List<Issue>();
		issues.AddRange(ValidateAllScriptableObjects());
		issues.AddRange(ValidateAllPrefabs());
		issues.AddRange(ValidateSceneMonoBehaviours());

		if (issues.Count == 0) Debug.Log("[Validation] All good!");
		else foreach (var i in issues) Debug.LogWarning(i.message, i.context);
	}

	// 手動メニュー：診断（何のアセンブリが対象候補か可視化）
	// [MenuItem("Tools/Validation/Diagnostics/Print Detected Assemblies")]
	public static void PrintDetectedAssemblies()
	{
		var set = new HashSet<string>();

		// シーン上
		foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
		{
			if (mb == null) continue;
			set.Add(mb.GetType().Assembly.GetName().Name);
		}

		// SO
		foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", SearchFolders))
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
			if (so == null) continue;
			set.Add(so.GetType().Assembly.GetName().Name);
		}

		// Prefab 中の MB
		foreach (var guid in AssetDatabase.FindAssets("t:Prefab", SearchFolders))
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (go == null) continue;
			foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
			{
				if (mb == null) continue;
				set.Add(mb.GetType().Assembly.GetName().Name);
			}
		}

		Debug.Log($"[Validation] Assemblies found:\n- " + string.Join("\n- ", set.OrderBy(s => s)));
	}

	// =============== 内部実装 ===============

	static void DebouncedRun()
	{
		_nextRunAt = EditorApplication.timeSinceStartup + DebounceSec;
		EditorApplication.update -= MaybeRunOnUpdate;
		EditorApplication.update += MaybeRunOnUpdate;
	}

	static void MaybeRunOnUpdate()
	{
		if (EditorApplication.timeSinceStartup < _nextRunAt) return;
		EditorApplication.update -= MaybeRunOnUpdate;

		// 変更時はアセット（SO/Prefab）のみ軽くチェック
		var issues = new List<Issue>();
		issues.AddRange(ValidateAllScriptableObjects());
		issues.AddRange(ValidateAllPrefabs());
		foreach (var i in issues) Debug.LogWarning(i.message, i.context);
	}

	// ScriptableObject（アセット）検証
	static List<Issue> ValidateAllScriptableObjects()
	{
		var list = new List<Issue>();
		var guids = AssetDatabase.FindAssets("t:ScriptableObject", SearchFolders);

		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
			if (so == null) continue;
			if (!IsMyTargetType(so.GetType())) continue; // ★ 自作のみ（接頭辞マッチ）

			ValidateObjectFields(so, list, labelPrefix: so.name);
		}
		return list;
	}

	// Prefab（アセット）検証：中の MonoBehaviour を対象
	static List<Issue> ValidateAllPrefabs()
	{
		var list = new List<Issue>();
		var guids = AssetDatabase.FindAssets("t:Prefab", SearchFolders);

		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (go == null) continue;

			var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
			foreach (var mb in behaviours)
			{
				if (mb == null) continue; // Missing Script
				if (!IsMyTargetType(mb.GetType())) continue; // ★ 自作のみ

				ValidateObjectFields(mb, list, labelPrefix: $"Prefab:{go.name}");
			}
		}
		return list;
	}

	// シーン上の MonoBehaviour 検証（FindObjectsByType を使用）
	static List<Issue> ValidateSceneMonoBehaviours()
	{
		var list = new List<Issue>();
		var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

		foreach (var mb in behaviours)
		{
			if (mb == null) continue; // Missing Script
			if (!IsMyTargetType(mb.GetType())) continue; // ★ 自作のみ

			ValidateObjectFields(mb, list, labelPrefix: $"Scene:{mb.gameObject.name}");
		}
		return list;
	}

	// 共通：対象フィールドの null チェック
	static void ValidateObjectFields(UnityEngine.Object target, List<Issue> sink, string labelPrefix)
	{
		foreach (var f in GetTargetFields(target.GetType()))
		{
			var value = f.GetValue(target) as UnityEngine.Object;
			if (value == null)
			{
				sink.Add(new Issue
				{
					message = $"[{labelPrefix}] {target.GetType().Name}.{f.Name} が未割り当てです。",
					context = target
				});
			}
		}
	}

	// 検証対象フィールド抽出
	static FieldInfo[] GetTargetFields(Type t)
	{
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		return t.GetFields(flags)
				.Where(f =>
						// Unity参照型のみ
						typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType) &&
						// インスペクタに出るもの（public or [SerializeField]）
						(f.IsPublic || f.GetCustomAttribute<SerializeField>() != null) &&
						// 明示的に許可された null は除外
						f.GetCustomAttribute<AllowNullAttribute>() == null
				)
				.ToArray();
	}

	// 自作型か判定：アセンブリ名が許可接頭辞のいずれかで始まるか
	static bool IsMyTargetType(Type t)
	{
		var asmName = t.Assembly.GetName().Name;
		// Debug.Log($"{asmName}");
		foreach (var prefix in AllowedAssemblyPrefixes)
		{
			if (!string.IsNullOrEmpty(prefix) && asmName.StartsWith(prefix, StringComparison.Ordinal)) return true;
		}
		return false;
	}
}
