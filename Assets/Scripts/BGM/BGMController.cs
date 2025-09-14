using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BGMController : MonoBehaviour
{
    public AudioSource audioSource;
    public TMP_Dropdown trackDropdown;
    public Button playToggleButton;
    public TextMeshProUGUI playButtonText;
    public List<AudioClip> audioClips;

    private int currentIndex = 0;
    private bool isPlaying = false;

    void Start()
    {
        playToggleButton.onClick.AddListener(OnTogglePlay);
        trackDropdown.onValueChanged.AddListener(OnTrackSelected);

        SetupDropdown();
        SelectTrack(0);
    }

    void SetupDropdown()
    {
        List<string> trackNames = new List<string>();
        foreach (AudioClip clip in audioClips)
        {
            trackNames.Add(clip.name);
        }

        trackDropdown.ClearOptions();
        trackDropdown.AddOptions(trackNames);
        trackDropdown.value = 0;
        trackDropdown.RefreshShownValue();
    }

    void OnTogglePlay()
    {
        if (isPlaying)
        {
            audioSource.Pause();
            isPlaying = false;
        }
        else
        {
            audioSource.Play();
            isPlaying = true;
        }

        UpdatePlayButtonLabel();
    }

    void OnTrackSelected(int index)
    {
        if (index == currentIndex) return;

        currentIndex = index;
        SelectTrack(index);

        if (isPlaying)
        {
            audioSource.Play();
        }
    }

    void SelectTrack(int index)
    {
        if (index < 0 || index >= audioClips.Count) return;

        audioSource.clip = audioClips[index];
        currentIndex = index;
    }

    void UpdatePlayButtonLabel()
    {
        playButtonText.text = isPlaying ? "Stop BGM" : "Play BGM";
    }
}
