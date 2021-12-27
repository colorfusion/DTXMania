using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Multimedia {

    /// <summary>
    /// Represents the Windows Multimedia MidiHDR structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MidiHeader
    {
        #region MidiHeader Members

        public IntPtr        data;
        public int           bufferLength; 
        public int           bytesRecorded; 
        public int           user; 
        public int           flags; 
        public IntPtr        next; 
        public int           reserved; 
        public int           offset; 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        public int[]         reservedArray; 

        #endregion
    }

    /// <summary>
    /// Represents the basic functionality provided by a device capable of 
    /// receiving Midi messages.
    /// </summary>
    public interface IMidiReceiver
    {
        /// <summary>
        /// Occurs when a channel message is received.
        /// </summary>
        // event ChannelMessageEventHandler ChannelMessageReceived;

        /// <summary>
        /// Occures when a system common message is received.
        /// </summary>
        // event SysCommonEventHandler SysCommonReceived;

        /// <summary>
        /// Occurs when a system exclusive message is received.
        /// </summary>
        // event SysExEventHandler SysExReceived;

        /// <summary>
        /// Occurs when a system realtime message is received.
        /// </summary>
        // event SysRealtimeEventHandler SysRealtimeReceived;

        /// <summary>
        /// Occures when an invalid short message is received.
        /// </summary>
        event InvalidShortMessageEventHandler InvalidShortMessageReceived;

        /// <summary>
        /// Starts receiving Midi messages.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops receiving Midi messages.
        /// </summary> 
        void Stop();
    }

    /// <summary>
    /// Represents the method that will handle the event that occurs when an
    /// invalid short message is received.
    /// </summary>
    public delegate void InvalidShortMessageEventHandler(object sender, InvalidShortMsgEventArgs e);

    /// <summary>
    /// Represents Midi input device capabilities.
    /// </summary>
    public struct MidiInCaps
    {
        #region MidiInCaps Members

        /// <summary>
        /// Manufacturer identifier of the device driver for the Midi output 
        /// device. 
        /// </summary>
        public short mid; 

        /// <summary>
        /// Product identifier of the Midi output device. 
        /// </summary>
        public short pid; 

        /// <summary>
        /// Version number of the device driver for the Midi output device. The 
        /// high-order byte is the major version number, and the low-order byte 
        /// is the minor version number. 
        /// </summary>
        public int driverVersion;

        /// <summary>
        /// Product name.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=32)]
        public byte[] name;         

        /// <summary>
        /// Optional functionality supported by the device. 
        /// </summary>
        public int support; 

        #endregion
    }

    /// <summary>
    /// Represents Midi input devices.
    /// </summary>
    public class InputDevice : System.ComponentModel.Component, IMidiReceiver
    {
        #region InputDevice Members

        #region Delegates

        // Represents the method that handles messages from Windows.
        private delegate void MidiInProc(int handle, int msg, int instance,
            int param1, int param2); 

        #endregion

        #region Win32 Midi Input Functions and Constants

        [DllImport("winmm.dll")]
        private static extern int midiInOpen(ref int handle, int deviceId,
            MidiInProc proc, int instance, int flags);

        [DllImport("winmm.dll")]
        private static extern int midiInClose(int handle);

        [DllImport("winmm.dll")]
        private static extern int midiInStart(int handle);

        [DllImport("winmm.dll")]
        private static extern int midiInReset(int handle);

        [DllImport("winmm.dll")]
        private static extern int midiInPrepareHeader(int handle, 
            IntPtr header, int sizeOfmidiHeader);

        [DllImport("winmm.dll")]
        private static extern int midiInUnprepareHeader(int handle, 
            IntPtr header, int sizeOfmidiHeader);

        [DllImport("winmm.dll")]
        private static extern int midiInAddBuffer(int handle, 
            IntPtr header, int sizeOfmidiHeader);

        [DllImport("winmm.dll")]
        private static extern int midiInGetDevCaps(int handle, 
            ref MidiInCaps caps, int sizeOfmidiInCaps);

        [DllImport("winmm.dll")]
        private static extern int midiInGetNumDevs();

        private const int MMSYSERR_NOERROR = 0;
        private const int CALLBACK_FUNCTION = 0x30000; 
        private const int MIM_DATA = 0x3C3;
        private const int MIM_ERROR = 0x3C5;
        private const int MIM_LONGDATA = 0x3C4;
        private const int MHDR_DONE = 0x00000001;

        #endregion

        #region Constants

        // Number of system exclusive headers to use.
        private const int HeaderCount = 4;

        // System exclusive buffer size.
        private const int SysExBufferSize = 32000;

        #endregion

        #region Fields

        // Device handle.
        private int handle;

        // device Identifier.
        private int deviceId;

        // Indicates whether or not the device is open.
        private bool opened = false;

        // Indicates whether or not the device is recording.
        private bool recording = false;        

        // Represents the method that handles messages from Windows.
        private MidiInProc messageHandler;

        // Thread for managing messagess.
        private Thread messageManager;

        // Event used to signal when the device has received a message.
        private AutoResetEvent resetEvent = new AutoResetEvent(false);        

        // Queue for storing messages from Windows.
        private Queue messageQueue = new Queue();

        // Synchronized queue for messages.
        private Queue syncMsgQueue;

        // Midi headers for storing system exclusive messages.
        private MidiHeader[] headers = new MidiHeader[HeaderCount];

        // Pointers to headers. 
        private IntPtr[] ptrHeaders = new IntPtr[HeaderCount];

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the InputDevice class.
        /// </summary>
        public InputDevice()
        {

            InitializeInputDevice();
        }

        /// <summary>
        /// Initializes a new instance of the InputDevice class with the 
        /// specified device Id.
        /// </summary>
        /// <param name="deviceId">
        /// The device Id.
        /// </param>
        public InputDevice(int deviceId)
        {
            InitializeInputDevice();

            // Open device.
            Open(deviceId);
        }

        #endregion        

        #region Methods

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(IsOpen())
                {
                    if(IsRecording())
                    {
                        Stop();
                    }

                    Close();
                }
            }
            base.Dispose( disposing );
        }

        /// <summary>
        /// Indicate whether or not the input device is open
        /// </summary>
        /// <returns>
        /// true if the device is open; otherwise, false.
        /// </returns>
        public bool IsOpen()
        {
            return opened;
        }   
        
        /// <summary>
        /// Indicates whether or not the device is recording.
        /// </summary>
        /// <returns>
        /// true if the device is recording; otherwise, false.
        /// </returns>
        public bool IsRecording()
        {
            return recording;
        }

        /// <summary>
        /// Gets the input device capabilities.
        /// </summary>
        /// <param name="deviceId">
        /// The device Identifier.
        /// </param>
        /// <exception cref="InputDeviceException">
        /// Thrown if an error occurred while retrieving the input device
        /// capabilities.
        /// </exception>
        /// <returns>
        /// The Midi intput device's capabilities.
        /// </returns>
        public static MidiInCaps GetCapabilities(int deviceId)
        {
            MidiInCaps caps = new MidiInCaps();

            ThrowOnError(midiInGetDevCaps(deviceId, ref caps, 
                Marshal.SizeOf(caps)));

            return caps;
        }

        /// <summary>
        /// Initializes input device.
        /// </summary>
        private void InitializeInputDevice()
        {
            // Create delegate for handling messages from Windows.
            messageHandler = new MidiInProc(OnMessage);            

            // Create synchronized queue for messages.
            syncMsgQueue = Queue.Synchronized(messageQueue);
        }

        /// <summary>
        /// Handles messages from Windows.
        /// </summary>
        private void OnMessage(int handle, int msg, int instance,
            int param1, int param2)
        { 
            // Only respond to messages if the device is in fact recording.
            if(IsRecording())
            {
                if(msg == MIM_DATA || msg == MIM_ERROR || msg == MIM_LONGDATA)
                {
                    syncMsgQueue.Enqueue(new Message(msg, param1, param2));
                    resetEvent.Set();            
                }
            }
        }

        /// <summary>
        /// Throw exception on error.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
        private static void ThrowOnError(int errCode)
        {
            // If an error occurred.
            if(errCode != MMSYSERR_NOERROR)
            {
                // Throw exception.
                throw new InputDeviceException(errCode);
            }
        }

        /// <summary>
        /// Thread method for managing Window messages.
        /// </summary>
        private void ManageMessages()
        {
            // While the device is recording.
            while(IsRecording())
            {
                // Wait for signal that a message has been received.
                resetEvent.WaitOne();

                // While the message queue is not empty.
                while(syncMsgQueue.Count > 0)
                {
                    // Get message from message queue.
                    Message msg = (Message)syncMsgQueue.Dequeue();

                    // Determine the type of message and act accordingly.
                    switch(msg.msg)
                    {
                        case MIM_DATA:
                            DispatchShortMessage(msg.param1, msg.param2);
                            break;

                        case MIM_ERROR:
                            DispatchInvalidShortMsg(msg.param1, msg.param2);
                            break;

                        case MIM_LONGDATA:
                            ManageSysExMessage(msg.param1, msg.param2);
                            break;

                        default:
                            break;
                    }                
                }
            }                
        }

        /// <summary>
        /// Determines the type of message received and triggers the correct
        /// event in response.
        /// </summary>
        /// <param name="message">
        /// The short Midi message received.
        /// </param>
        /// <param name="timeStamp">
        /// Number of milliseconds that have passed since the input device 
        /// began recording.
        /// </param>
        private void DispatchShortMessage(int message, int timeStamp)
        {
            /*
            // Unpack status value.
            int status = ShortMessage.UnpackStatus(message);

            // If a channel message was received.
            if(ChannelMessage.IsChannelMessage(status))
            {
                // If anyone is listening for channel messages.
                if(ChannelMessageReceived != null)
                {
                    // Create channel message.
                    ChannelMessage msg = new ChannelMessage(message);

                    // Create channel message event argument.
                    ChannelMessageEventArgs e = 
                        new ChannelMessageEventArgs(msg, timeStamp);

                    // Trigger channel message received event.
                    ChannelMessageReceived(this, e);
                }
            }
            // Else if a system common message was received
            else if(SysCommonMessage.IsSysCommonMessage(status))
            {
                // If anyone is listening for system common messages
                if(SysCommonReceived != null)
                {
                    // Create system common message.
                    SysCommonMessage msg = new SysCommonMessage(message);

                    // Create system common event argument.
                    SysCommonEventArgs e = new SysCommonEventArgs(msg, timeStamp);

                    // Trigger system common received event.
                    SysCommonReceived(this, e);
                }
            }
            // Else if a system realtime message was received
            else if(SysRealtimeMessage.IsSysRealtimeMessage(status))
            {
                // If anyone is listening for system realtime messages
                if(SysRealtimeReceived != null)
                {
                    // Create system realtime message.
                    SysRealtimeMessage msg = new SysRealtimeMessage(message);

                    // Create system realtime event argument.
                    SysRealtimeEventArgs e = new SysRealtimeEventArgs(msg, timeStamp);

                    // Trigger system realtime received event.
                    SysRealtimeReceived(this, e);
                }
            }
            */
        }

        /// <summary>
        /// Handles triggering the invalid short message received event.
        /// </summary>
        /// <param name="message">
        /// The invalid short message received.
        /// </param>
        /// <param name="timeStamp">
        /// Number of milliseconds that have passed since the input device 
        /// began recording.
        /// </param>
        private void DispatchInvalidShortMsg(int message, int timeStamp)
        {
            if(InvalidShortMessageReceived != null)
            {
                InvalidShortMsgEventArgs e = 
                    new InvalidShortMsgEventArgs(message, timeStamp);

                InvalidShortMessageReceived(this, e);
            }
        }

        /// <summary>
        /// Manages system exclusive messages received by the input device.
        /// </summary>
        /// <param name="param1">
        /// Integer pointer to the header containing the received system
        /// exclusive message.
        /// </param>
        /// <param name="timeStamp">
        /// Number of milliseconds that have passed since the input device 
        /// began recording.
        /// </param>
        private void ManageSysExMessage(int param1, int timeStamp)
        {
            /*
            // Get pointer to header.
            IntPtr ptrHeader = new IntPtr(param1);

            // If anyone is listening for system exclusive messages.
            if(SysExReceived != null)
            {
                // Imprint raw pointer on to structure.
                MidiHeader header = (MidiHeader)Marshal.PtrToStructure(ptrHeader, typeof(MidiHeader));
                
                // Dispatches system exclusive messages.
                DispatchSysExMessage(header, timeStamp);
            }
                
            // Unprepare header.
            ThrowOnError(midiInUnprepareHeader(handle, ptrHeader, 
                Marshal.SizeOf(typeof(MidiHeader))));                     

            // Prepare header to be used again.
            ThrowOnError(midiInPrepareHeader(handle, ptrHeader, 
                Marshal.SizeOf(typeof(MidiHeader)))); 

            // Add header back to buffer.
            ThrowOnError(midiInAddBuffer(handle, ptrHeader, 
                Marshal.SizeOf(typeof(MidiHeader))));
        }

        /// <summary>
        /// Handles triggering the system exclusive message received event.
        /// </summary>
        /// <param name="header">
        /// Midi header containing the system exclusive message.
        /// </param>
        /// <param name="timeStamp">
        /// Number of milliseconds that have passed since the input device 
        /// began recording.
        /// </param>
        private void DispatchSysExMessage(MidiHeader header, int timeStamp)
        {
            /*
            // Create array for holding system exclusive data.
            byte[] data = new byte[header.bytesRecorded - 1];

            // Get status byte.
            byte status = Marshal.ReadByte(header.data);
                
            // Copy system exclusive data into array (status byte is 
            // excluded).
            for(int i = 1; i < header.bytesRecorded; i++)
            {
                data[i - 1] = Marshal.ReadByte(header.data, i);
            }

            // Create message.
            SysExMessage msg = new SysExMessage((SysExType)status, data);

            // Raise event.
            SysExReceived(this, new SysExEventArgs(msg, timeStamp));
            */
        }

        /// <summary>
        /// Create headers for system exclusive messages.
        /// </summary>
        private void CreateHeaders()
        {
            // Create headers.
            for(int i = 0; i < HeaderCount; i++)
            {
                // Initialize headers and allocate memory for system exclusive
                // data.
                headers[i].bufferLength = SysExBufferSize;
                headers[i].data = Marshal.AllocHGlobal(SysExBufferSize);

                // Allocate memory for pointers to headers. This is necessary 
                // to insure that garbage collection doesn't move the memory 
                // for the headers around while the input device is open.
                ptrHeaders[i] = 
                    Marshal.AllocHGlobal(Marshal.SizeOf(typeof(MidiHeader)));
            }
        }

        /// <summary>
        /// Destroy headers.
        /// </summary>
        private void DestroyHeaders()
        {
            // Free memory for headers.
            for(int i = 0; i < HeaderCount; i++)
            {
                Marshal.FreeHGlobal(headers[i].data);
                Marshal.FreeHGlobal(ptrHeaders[i]);
            }
        }

        /// <summary>
        /// Unprepares headers.
        /// </summary>
        private void UnprepareHeaders()
        {
            // Unprepare each Midi header.
            for(int i = 0; i < HeaderCount; i++)
            {
                ThrowOnError(midiInUnprepareHeader(handle, ptrHeaders[i], 
                    Marshal.SizeOf(typeof(MidiHeader))));
            }
        }        

        #endregion

        #region Properties 
        
        /// <summary>
        /// Gets the number of intput devices present in the system.
        /// </summary>
        public static int DeviceCount
        {
            get
            {
                return midiInGetNumDevs();
            }
        }

        #endregion		

        #region Structs

        /// <summary>
        /// Represents a message sent by Windows to the input device.
        /// </summary>
        private struct Message
        {
            public int msg;
            public int param1;
            public int param2;

            public Message(int msg, int param1, int param2)
            {
                this.msg = msg;
                this.param1 = param1;
                this.param2 = param2;
            }
        }

        #endregion

        #endregion

        #region IDevice Members

        #region Methods

        /// <summary>
        /// Opens the InputDevice with the specified device Identifier.
        /// </summary>
        /// <param name="deviceId">
        /// The device Identifier.
        /// </param>
        /// <exception cref="InputDeviceException">
        /// Thrown if an error occurred while opening the input device.
        /// </exception>
        public void Open(int deviceId)
        {
            // If the device is already open.
            if(IsOpen())
            {
                // Close device before attempting to open it again.
                Close();
            }            

            // Open the device.
            ThrowOnError(midiInOpen(ref handle, deviceId, messageHandler, 0, 
                CALLBACK_FUNCTION));

            // Create headers for system exclusive messages.
            CreateHeaders();            

            // Indicate that the device is open.
            opened = true;

            // Keep track of device Identifier.
            this.deviceId = deviceId;
        }     
        
        /// <summary>
        /// Closes the InputDevice.
        /// </summary>
        /// <exception cref="InputDeviceException">
        /// Thrown if an error occurred while closing the input device.
        /// </exception>
        public void Close()
        {
            // If the device is open.
            if(IsOpen())
            {
                // If the device is recording.
                if(IsRecording())
                {
                    // Stop recording before closing the device.
                    Stop();
                }

                // Destroy headers for system exclusive messages.
                DestroyHeaders();                

                // Close the device.
                ThrowOnError(midiInClose(handle));

                // Indicate that the device is closed.
                opened = false;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the device handle.
        /// </summary>
        public int DeviceHandle
        {
            get
            {
                return handle;
            }
        }     

        /// <summary>
        /// Gets the device Identifier.
        /// </summary>
        public int DeviceId
        {
            get 
            {
                return deviceId;
            }
        }

        #endregion

        #endregion

        #region IMidiReceiver

        #region Events

        /// <summary>
        /// Occurs when a channel message is received.
        /// </summary>
        // public event ChannelMessageEventHandler ChannelMessageReceived;

        /// <summary>
        /// Occurs when a system common message is received.
        /// </summary>
        // public event SysCommonEventHandler SysCommonReceived;

        /// <summary>
        /// Occurs when a system exclusive message is received.
        /// </summary>
        // public event SysExEventHandler SysExReceived;

        /// <summary>
        /// Occurs when a system realtime message is received.
        /// </summary>
        // public event SysRealtimeEventHandler SysRealtimeReceived;

        /// <summary>
        /// Occurs when an invalid short message is received.
        /// </summary>
        public event InvalidShortMessageEventHandler InvalidShortMessageReceived;

        #endregion

        #region Methods

        /// <summary>
        /// Starts recording Midi messages.
        /// </summary>
        /// <exception cref="InputDeviceException">
        /// Thrown if there was an error starting the input device.
        /// </exception>
        public void Start()
        {
            // If the device is open and it is not already recording.
            if(IsOpen() && !IsRecording())
            { 
                // Initializes headers for system exclusive messages.
                for(int i = 0; i < HeaderCount; i++)
                { 
                    // Reset flags.
                    headers[i].flags = 0;

                    // Imprint header structure onto raw memory.
                    Marshal.StructureToPtr(headers[i], ptrHeaders[i], false); 

                    // Prepare header.
                    ThrowOnError(midiInPrepareHeader(handle, ptrHeaders[i], 
                        Marshal.SizeOf(typeof(MidiHeader))));

                    // Add header to buffer.
                    ThrowOnError(midiInAddBuffer(handle, ptrHeaders[i], 
                        Marshal.SizeOf(typeof(MidiHeader))));                  
                }

                // Indicate that the device is recording.
                recording = true;

                // Create thread for managing messages.
                messageManager = new Thread(new ThreadStart(ManageMessages));

                // Start thread.
                messageManager.Start();

                // Wait for thread to become active.
                while(!messageManager.IsAlive)
                    continue;

                // Start recording.
                ThrowOnError(midiInStart(handle));
            }
        }

        /// <summary>
        /// Stop recording Midi messages.
        /// </summary>
        public void Stop()
        {
            // If the device is open.
            if(IsOpen())
            {
                // If the device is recording.
                if(IsRecording())
                {
                    // Indicate that the device is not recording.
                    recording = false;

                    // Clear out messages from message queue.
                    syncMsgQueue.Clear();

                    // Signal message thread to finish.
                    resetEvent.Set();

                    // Wait for message thread to finish.
                    messageManager.Join();

                    // Stop recording.
                    ThrowOnError(midiInReset(handle));  

                    // Unprepare headers.
                    UnprepareHeaders();
                }
            }
        }

        #endregion

        #endregion        
    }

    /// <summary>
    /// The exception that is thrown when a error occurs with the InputDevice
    /// class.
    /// </summary>
    public class InputDeviceException : ApplicationException
    {
        #region InputDeviceException Members

        #region Win32 Midi Input Error Function

        [DllImport("winmm.dll")]
        private static extern int midiInGetErrorText(int errCode, 
            StringBuilder errMsg, int sizeOfErrMsg);

        #endregion

        #region Fields

        // Error message.
        private StringBuilder errMsg = new StringBuilder(128);

        #endregion 

        #region Construction

        /// <summary>
        /// Initializes a new instance of the InputDeviceException class with
        /// the specified error code.
        /// </summary>
        /// <param name="errCode">
        /// The error code.
        /// </param>
        public InputDeviceException(int errCode)
        {
            // Get error message.
            midiInGetErrorText(errCode, errMsg, errMsg.Capacity);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return errMsg.ToString();
            }
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Provides data for the InvalidShortMsgEvent event.
    /// </summary>
    public class InvalidShortMsgEventArgs : EventArgs
    {
        #region InvalidShortMsgEventArgs Members

        #region Fields

        private int message;
        private int timeStamp;   
        
        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the InvalidShortMsgEventArgs class 
        /// with the specified message and time stamp.
        /// </summary>
        /// <param name="message">
        /// The invalid short message as an integer. 
        /// </param>
        /// <param name="timeStamp">
        /// Time in milliseconds since the input device began recording.
        /// </param>
        public InvalidShortMsgEventArgs(int message, int timeStamp)
        {
            this.message = message;
            this.timeStamp = timeStamp;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Invalid short message as an integer.
        /// </summary>
        public int Message
        {
            get
            {
                return message;
            }
        }

        /// <summary>
        /// Time in milliseconds since the input device began recording.
        /// </summary>
        public int TimeStamp
        {
            get
            {
                return timeStamp;
            }
        }

        #endregion

        #endregion
    }
}
