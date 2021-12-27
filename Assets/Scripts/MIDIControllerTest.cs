using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Multimedia;

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
            MidiInCaps inputCap = InputDevice.GetCapabilities(0);
            inputDeviceName = Encoding.UTF8.GetString(inputCap.name);

            Debug.Log(string.Format("Loading {0}",inputDeviceName));
            inputDevice = new InputDevice(0);
            inputDevice.Start();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        if (inputDevice != null)
        {
            inputDevice.Dispose();
        }
    }
}
