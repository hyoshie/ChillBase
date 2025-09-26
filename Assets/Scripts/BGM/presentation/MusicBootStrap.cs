using UnityEngine;

public class MusicBootstrap : MonoBehaviour
{
	[SerializeField] private MusicServiceSO service;
	[SerializeField] private AudioSource audioSourceInScene;
	[SerializeField] private bool autoPlayOnStart = false;

	private void Awake()
	{
		if (service == null || audioSourceInScene == null)
		{
			Debug.LogError("[MusicBootstrap] Assign MusicServiceSO and AudioSource.");
			return;
		}

		service.BindRuntime(audioSourceInScene);
		service.Initialize(autoPlayOnStart);

		// 共有運用したいなら
		// DontDestroyOnLoad(audioSourceInScene.gameObject);
		// DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		service?.Tick();
	}

	private void OnDestroy()
	{
		// シーン限定ならここで破棄。共有運用なら別の終了ポイントで
		service?.Dispose();
	}
}
