using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DTXFileLoadTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DTXInputOutput dtxIO = DTXInputOutput.Load("Sing Alive (Full Version)/adv.dtx");
        StartCoroutine(PlayPreview(dtxIO.musicInfo.PreviewSound));
    }

    IEnumerator PlayPreview(string audioPath)
    {
        AudioSource audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.Log("Adding audio source");
            // add audio source if it does not exist
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        using(UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.OGGVORBIS))
        {
            yield return uwr.SendWebRequest();
            
            if (uwr.result != UnityWebRequest.Result.Success) {
                Debug.LogError(uwr.error);
                yield break;
            }

            Debug.Log(string.Format("Playing {0}", audioPath));
            AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
            // use audio clip
            audioSource.PlayOneShot(clip);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
