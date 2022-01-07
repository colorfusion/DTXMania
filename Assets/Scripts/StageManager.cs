using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public DTXInputOutput dtxIO;
    public bool IsLoaded;
    public bool IsPlaying;
    public bool IsAutoPlay;
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
        if (IsLoaded)
        {
            if (!IsPlaying && dtxIO.IsSongReady())
            {
                IsPlaying = true;

                if (IsAutoPlay)
                {
                    dtxIO.AutoPlaySong();
                }
            }
        }
    }

    public void PlaySong(SongInfo songInfo, int difficulty, bool autoPlay)
    {
        IsAutoPlay = autoPlay;

        string relativePath = string.Format("{0}\\{1}", songInfo.Path, songInfo.DifficultyList[difficulty].FilePath);
        Debug.Log(string.Format("Playing {0}", relativePath));
        
        dtxIO.LoadFile(relativePath);

        IsLoaded = true;
        IsPlaying = false;
    }
}
