using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DTXFileLoadTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DTXInputOutput dtxIO = DTXInputOutput.Load("Sing Alive (Full Version)/adv.dtx");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
