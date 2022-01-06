using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DTXFileLoadTest : MonoBehaviour
{
    DTXInputOutput dtxIO;
    bool isSongReady = false;

    // Start is called before the first frame update
    void Start()
    {
        dtxIO = GetComponent<DTXInputOutput>();
        dtxIO.LoadFile("Sing Alive (Full Version)/test.dtx");

        AudioSource audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.Log("Adding audio source");
            // add audio source if it does not exist
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        string filePath = dtxIO.GetFileAbsolutePath(dtxIO.musicInfo.PreviewSound);
        StartCoroutine(DTXHelper.GetAudioClip(filePath, (audioClip) => {
            // Debug.Log(string.Format("Playing {0}", dtxIO.musicInfo.PreviewSound));
            // audioSource.PlayOneShot(audioClip);
        }, (_) => {
            
        }));
    }

    // Update is called once per frame
    void Update()
    {
        if (!isSongReady)
        {
            if (dtxIO.IsSongReady())
            {
                isSongReady = true;
                dtxIO.AutoPlaySong();
            }
            else
            {
                
            }
        }
    }
}
