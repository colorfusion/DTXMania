using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    #region Internal Struct
    public class AudioSourceMixer
    {
        public double scheduledTime;
        public AudioSource audioSource;

        public double GetCompletedTime()
        {
            return scheduledTime + audioSource.clip.length;
        }

        public bool IsDone(double currentTime) 
        {
            return currentTime > GetCompletedTime();
        }
    }

    public struct AudioArgs
    {
        public double scheduledTime;
        public AudioClip audioClip;
        public float volume;
        public float pan;
    }
    #endregion

    #region Fields
    public List<AudioSourceMixer> audioSourceMixers;
    public Queue<AudioSource> audioSourcePool;

    public int initialPoolCapacity = 32;
    public float poolIncrementMultiplier = 2.0f;
    public int currentPoolSize;
    public int currentPoolCapacity = 0;

    public double currentDSPTime;
    #endregion

    #region Methods
    // Start is called before the first frame update
    void Start()
    {
        audioSourceMixers = new List<AudioSourceMixer>();

        SetupAudioSourcePool();
    }

    // Update is called once per frame
    void Update()
    {
        currentDSPTime = AudioSettings.dspTime;
        double currentTime = AudioSettings.dspTime;

        for(int i = audioSourceMixers.Count - 1; i >= 0; --i)
        {
            AudioSourceMixer mixer = audioSourceMixers[i];

            if (!mixer.IsDone(currentTime))
            {
                // ignore mixer if it is not done playing yet
                continue;
            }

            mixer.audioSource.clip = null;
            mixer.audioSource.enabled = false;

            audioSourcePool.Enqueue(mixer.audioSource);
            audioSourceMixers.RemoveAt(i);
        }
        currentPoolSize = audioSourcePool.Count;
    }

    public void SetupAudioSourcePool()
    {
        audioSourcePool = new Queue<AudioSource>();
        UpdatePoolSize(initialPoolCapacity);
    }

    public void UpdatePoolSize(int newPoolSize)
    {
        int newElementCount = newPoolSize - currentPoolCapacity;
        for (int i = 0; i < newElementCount; ++i)
        {
            AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
            newAudioSource.playOnAwake = false;
            newAudioSource.enabled = false;
            audioSourcePool.Enqueue(newAudioSource);
        }

        currentPoolSize = audioSourcePool.Count;

        currentPoolCapacity = newPoolSize;
    }

    public void PlayAudio(AudioArgs args)
    {
        // double pool size if it is empty currently
        if (audioSourcePool.Count == 0)
        {
            UpdatePoolSize((int)(currentPoolCapacity * poolIncrementMultiplier));
        }

        AudioSourceMixer newMixer = new AudioSourceMixer();
        AudioSource audioSource = audioSourcePool.Dequeue();
        audioSource.enabled = true;
        audioSource.clip = args.audioClip;
        audioSource.volume = args.volume;
        audioSource.panStereo = args.pan;
        audioSource.PlayScheduled(args.scheduledTime);

        newMixer.audioSource = audioSource;
        newMixer.scheduledTime = args.scheduledTime;
        audioSourceMixers.Add(newMixer);
    }
    #endregion
}
