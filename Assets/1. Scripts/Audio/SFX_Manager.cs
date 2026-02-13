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
    
    public AudioSource audioSource;
    public AudioSource loopAudioSource;
    public SFX_SO data;

    private void Awake()
    {
        instance = this;
    }

    public void PlaySFX(int clipID, float volume = 0.5f, float pitch = 1f, bool loop = false)
    {
        audioSource.pitch = pitch;
        if (loop)
        {
            print("sfx looping");

            if (loopAudioSource.clip != data.clips[clipID])
            {
                loopAudioSource.clip = data.clips[clipID];
                loopAudioSource.volume = volume;
                loopAudioSource.Play();
            }
           
        }
        else
        {
            audioSource.PlayOneShot(data.clips[clipID], volume);  
        }
    }
    
    public void StopSFX()
    {
        print("sfx stopped");

        loopAudioSource.clip = null;
        loopAudioSource.Stop();
    }
}
