using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomSoundClip : MonoBehaviour
{
    public List<AudioClip> clips = new List<AudioClip>();
    public bool playOnAwake = true;

    private void Awake()
    {
        int clipIndex = Random.Range(0, clips.Count - 1);
        AudioSource source = GetComponent<AudioSource>();
        source.Stop();
        source.clip = clips[clipIndex];
        if (playOnAwake)
        {
            source.Play();
        }
    }
}
