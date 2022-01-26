using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public DTXInputOutput dtxIO;
    private bool isSongSelected = false;
    // Start is called before the first frame update
    void Start()
    {
        dtxIO = GetComponent<DTXInputOutput>();
        if (dtxIO == null)
        {
            dtxIO = gameObject.AddComponent<DTXInputOutput>();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlaySong(SongInfo songInfo, int difficulty, bool autoPlay)
    {
        isSongSelected = true;
        StartCoroutine(LoadAndPlaySelectedSong(songInfo, difficulty, autoPlay));
    }

    private IEnumerator LoadAndPlaySelectedSong(SongInfo songInfo, int difficulty, bool autoPlay)
    {
        string relativePath = string.Format("{0}\\{1}", songInfo.Path, songInfo.DifficultyList[difficulty].FilePath);
        Debug.Log(string.Format("Playing {0}", relativePath));
        
        dtxIO.LoadFile(relativePath);

        if (!dtxIO.IsSongReady())
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (autoPlay)
        {
            dtxIO.PlaySong(autoPlay);
        }
    }
}
