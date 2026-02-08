using Unity.Netcode;
using UnityEngine;

public class Radio : NetworkBehaviour
{
    public static Radio instance;

    void Awake()
    {
        instance = this;
    }

    [Header("Buttons")]
    [SerializeField] private GameObject buttonPlay;
    [SerializeField] private GameObject buttonPrevious;
    [SerializeField] private GameObject buttonNext;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] clipsList;
    [SerializeField] private AudioClip clipWhenPlay;
    [SerializeField] private float radioDelay = 0.2f;

    public NetworkVariable<bool> IsPlaying = new(false);
    public NetworkVariable<int> CurrentClip = new(0);
    public NetworkVariable<double> StartTime = new(0);

    void Start()
    {
        IsPlaying.OnValueChanged += OnPlayChanged;
        CurrentClip.OnValueChanged += OnClipChanged;
    }

    void OnDestroy()
    {
        IsPlaying.OnValueChanged -= OnPlayChanged;
        CurrentClip.OnValueChanged -= OnClipChanged;
    }
    
    public void CheckButton(GameObject button)
    {
        if (button == buttonPlay)
            ToggleRadio();
        else if (button == buttonPrevious)
            ChangeClip(false);
        else if (button == buttonNext)
            ChangeClip(true);
    }

    void ToggleRadio()
    {
        ToggleRadioServerRpc();
    }

    void ChangeClip(bool next)
    {
        ChangeClipServerRpc(next);
    }
    
    
    [ServerRpc(RequireOwnership = false)]
    void ToggleRadioServerRpc()
    {
        IsPlaying.Value = !IsPlaying.Value;
        StartTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangeClipServerRpc(bool next)
    {
        if (clipsList.Length == 0) return;

        CurrentClip.Value = next
            ? (CurrentClip.Value + 1) % clipsList.Length
            : (CurrentClip.Value - 1 + clipsList.Length) % clipsList.Length;

        StartTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }

    void OnPlayChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            audioSource.PlayOneShot(clipWhenPlay, 0.25f);
            buttonPlay.GetComponent<MeshRenderer>().material.color = Color.green;

            Invoke(nameof(PlaySynced), radioDelay);
        }
        else
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clipWhenPlay, 0.25f);
            buttonPlay.GetComponent<MeshRenderer>().material.color = Color.red;
        }
    }


    void OnClipChanged(int oldValue, int newValue)
    {
        if (IsPlaying.Value)
            PlaySynced();
    }

    void PlaySynced()
    {
        if (clipsList.Length == 0) return;

        audioSource.clip = clipsList[CurrentClip.Value];

        double elapsed =
            NetworkManager.Singleton.ServerTime.Time - StartTime.Value;

        audioSource.time = (float)(elapsed % audioSource.clip.length);
        audioSource.Play();
    }

}
