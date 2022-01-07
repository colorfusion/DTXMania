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
        dtxIO.LoadFile("Sing Alive (Full Version)/mstr.dtx");
    }

    // Update is called once per frame
    void Update()
    {
        if (!isSongReady)
        {
            if (dtxIO.IsSongReady())
            {
                isSongReady = true;
                // dtxIO.AutoPlaySong();
            }
            else
            {
                
            }
        }
    }
}
