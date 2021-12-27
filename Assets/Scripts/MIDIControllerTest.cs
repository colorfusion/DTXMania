using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using Multimedia.Midi;

public class MIDIControllerTest : MonoBehaviour
{
    InputDevice inputDevice;
    string inputDeviceName;

    // Start is called before the first frame update
    void Start()
    {
        int inputDeviceCount = InputDevice.DeviceCount;
        Debug.Log(inputDeviceCount);

        if (inputDeviceCount >= 1)
        {
            inputDevice = new InputDevice(0);
            inputDevice.Start();

            Debug.Log(string.Format("Device {0} is loaded", inputDevice.DeviceName, 1));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnApplicationQuit()
    {
        if (inputDevice != null)
        {
            inputDevice.Close();
        }
    }
}
