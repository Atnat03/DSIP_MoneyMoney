using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class SFX_SO : MonoBehaviour
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

    public void PlaySFX(int clipID, float volume = 0.5f, float pitch = 1f, bool loop = false)
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.pitch = pitch;
        if (loop)
        {
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(data.clips[clipID], volume);  
        }
    }
}
