using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class SFX_SO : ScriptableObject
{
    public List<AudioClip> clips;
}

public class SFX_Manager : MonoBehaviour
{
    public static SFX_Manager instance;
    
    AudioSource audioSource;
    public SFX_SO data;

    private void Awake()
    {
        instance = this;
    }

    public void PlaySFX(int clipID, float volume, float pitch)
    {
        audioSource = GetComponent<AudioSource>();
        
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(data.clips[clipID], volume);
    }
}
