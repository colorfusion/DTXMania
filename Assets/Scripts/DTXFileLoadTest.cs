using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DTXFileLoadTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DTXInputOutput dtxIO = GetComponent<DTXInputOutput>();
        dtxIO.LoadFile("Sing Alive (Full Version)/adv.dtx");

        AudioSource audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.Log("Adding audio source");
            // add audio source if it does not exist
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        StartCoroutine(DTXHelper.GetAudioClip(dtxIO.musicInfo.PreviewSound, (audioClip) => {
            // Debug.Log(string.Format("Playing {0}", dtxIO.musicInfo.PreviewSound));
            // audioSource.PlayOneShot(audioClip);
        }, (errorMsg) => {
            
        }));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
