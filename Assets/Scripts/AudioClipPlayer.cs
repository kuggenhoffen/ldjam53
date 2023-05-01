using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioClipPlayer : MonoBehaviour
{
    
    [SerializeField]
    AudioClip[] audioClips;
    [SerializeField]
    bool playOnStart;
    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (playOnStart)
            PlayAudioClip();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayAudioClip()
    {
        if (audioClips.Length > 0)
        {
            int index = Random.Range(0, audioClips.Length);
            audioSource.PlayOneShot(audioClips[index]);
        }
    }
}
