using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu]
public class SFX_SO : ScriptableObject
{
    public List<AudioClip> clips;
}

public class SFX_Manager : NetworkBehaviour
{
    public static SFX_Manager instance;
    
    public AudioSource audioSource;
    public AudioSource loopAudioSource;
    public SFX_SO data;

    private void Awake()
    {
        instance = this;
    }

    public void PlaySFX(int clipID, float volume = 0.5f, float pitch = 1f, bool loop = false)
    {
        if (IsServer)
        {
            PlaySoundClientRpc(clipID, volume, pitch, loop);
        }
        else
        {
            RequestPlaySFXServerRpc(clipID, volume, pitch, loop);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestPlaySFXServerRpc(int clipID, float volume = 0.5f, float pitch = 1f, bool loop = false)
    {
        PlaySoundClientRpc(clipID, volume, pitch, loop);
    }


    [ClientRpc]
    void PlaySoundClientRpc(int clipID, float volume = 0.5f, float pitch = 1f, bool loop = false)
    {

    }
}
