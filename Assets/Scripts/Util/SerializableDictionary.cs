using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> :
		Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
	[SerializeField] private List<TKey> keys = new();
	[SerializeField] private List<TValue> values = new();

	public SerializableDictionary() : base() { }

	// ★ 追加：コピーコンストラクタ
	public SerializableDictionary(IDictionary<TKey, TValue> src) : base(src) { }

	public void OnBeforeSerialize()
	{
		keys.Clear(); values.Clear();
		foreach (var kv in this) { keys.Add(kv.Key); values.Add(kv.Value); }
	}

	public void OnAfterDeserialize()
	{
		this.Clear();
		int count = Math.Min(keys.Count, values.Count);
		for (int i = 0; i < count; i++) this[keys[i]] = values[i];
	}
}
