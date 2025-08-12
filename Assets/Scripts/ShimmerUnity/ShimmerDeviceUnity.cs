using UnityEngine;
using ShimmerAPI;
using System;
using System.Threading;
using UnityEngine.Events;

namespace ShimmerDataCollection
{
    public class ShimmerDeviceUnity : MonoBehaviour
    {
        /// <summary>
        /// The possible connection states of the shimmer device
        /// </summary>
        public enum State
        {
            /// <summary>
            /// No connection, device not connected
            /// </summary>
            None,
            /// <summary>
            /// Device is currently establishing bluetooth connection
            /// </summary>
            Connecting,
            /// <summary>
            /// Device has successfully connected via bluetooth
            /// </summary>
            Connected,
            /// <summary>
            /// Device has been disconnected
            /// </summary>
            Disconnected,
            /// <summary>
            /// Device is actively streaming sensor data
            /// </summary>
            Streaming
        }

        /// <summary>
        /// Current connection state of this shimmer device
        /// </summary>
        public State CurrentState { get; private set; }

        [SerializeField]
        [Tooltip("Automatically connect and start streaming when the scene starts")]
        private bool autoConnectAndStream = true;

        [SerializeField]
        [Tooltip("Device identifier name for this Shimmer device")]
        private string devName = "ShimmerBD01";

        /// <summary>
        /// Gets or sets the device name for this Shimmer device
        /// </summary>
        public string DeviceName
        {
            get => devName;
            set => devName = value;
        }

        [SerializeField]
        [Tooltip("COM port that the Shimmer device is connected to (e.g. COM6, COM8)")]
        private string comPort = "COM6";

        /// <summary>
        /// Gets or sets the COM port for bluetooth communication
        /// </summary>
        public string COMPort
        {
            get => comPort;
            set => comPort = value;
        }
        [SerializeField]
        [Tooltip("Select the type of shimmer you are using.")]
        private ShimmerConfig.ShimmerDeviceType deviceType = ShimmerConfig.ShimmerDeviceType.GSR;

        [SerializeField]
        [Tooltip("Enable this if you wish to use default Consensys settings.")]
        private bool useDefaultConsensysSettings = false;

        # region Custom Settings, only shown if useDefaultConsensysSettings is False
        [SerializeField]
        [Tooltip("Enable recommended settings that follows Shimmer User Guides GSR+ Rev 1.13, Optical Pulse Rev 1.6, and ECG Rev 1.12. Untick to use custom settings.")]
        private bool useRecommendedSettings = true;

        [SerializeField]
        [Tooltip("Select the sensors you want to enable during connection.")]
        private ShimmerConfig.SensorBitmap enabledSensors;

        public ShimmerConfig.SensorBitmap EnabledSensors
        {
            get => enabledSensors;
            set => enabledSensors = value;
        }

        [SerializeField]
        [Tooltip("The sampling rate for the device (For GSR, 0-5 Hz is suggested for tonic measurements, with 0.03-5 Hz for phasic measurements; For PPG, 100 Hz or greater is suggested; For ECG, 512 Hz is suggested).")]
        private float samplingRate;
        public float SamplingRate
        {
            get => samplingRate;
            set => samplingRate = value;
        }

        [SerializeField]
        [Tooltip("The range for the accelerometer.")]
        private ShimmerConfig.AccelerometerRange accelerometerRange;

        public ShimmerConfig.AccelerometerRange AccelerometerRange
        {
            get => accelerometerRange;
            set => accelerometerRange = value;
        }

        [SerializeField]
        [Tooltip("The range for the gyroscope.")]
        private ShimmerConfig.GyroscopeRange gyroscopeRange;

        public ShimmerConfig.GyroscopeRange GyroscopeRange
        {
            get => gyroscopeRange;
            set => gyroscopeRange = value;
        }

        [SerializeField]
        [Tooltip("The range for the magnetometer.")]
        private ShimmerConfig.MagnetometerRange magnetometerRange;

        public ShimmerConfig.MagnetometerRange MagnetometerRange
        {
            get => magnetometerRange;
            set => magnetometerRange = value;
        }

        [SerializeField]
        private bool enableLowPowerAccel = false;

        public bool EnableLowPowerAccel
        {
            get => enableLowPowerAccel;
            set => enableLowPowerAccel = value;
        }

        [SerializeField]
        private bool enableLowPowerGyro = false;

        public bool EnableLowPowerGyro
        {
            get => enableLowPowerGyro;
            set => enableLowPowerGyro = value;
        }

        [SerializeField]
        private bool enableLowPowerMag = false;

        public bool EnableLowPowerMag
        {
            get => enableLowPowerMag;
            set => enableLowPowerMag = value;
        }

        [SerializeField]
        [Tooltip("Enables the internal ADC pins on the shimmer3.")]
        private bool enableInternalExpPower = true;

        public bool EnableInternalExpPower
        {
            get => enableInternalExpPower;
            set => enableInternalExpPower = value;
        }

        [SerializeField]
        [Tooltip("The range for the GSR. For GSR, Settings 2 and 3 provide the best match for typical tonic skin conductance values")]
        private ShimmerConfig.GSRRangeSetting gsrRange;

        public ShimmerConfig.GSRRangeSetting GSRRange
        {
            get => gsrRange;
            set => gsrRange = value;
        }
        #endregion

        [SerializeField]
        [Tooltip("Event triggered when new sensor data is received from the device")]
        private DataReceivedEvent onDataReceived = new DataReceivedEvent();
        /// <summary>
        /// Unity event triggered when sensor data is received
        /// </summary>
        public DataReceivedEvent OnDataReceived => onDataReceived;

        // Private members
        private Thread shimmerThread = null;
        private ShimmerBluetooth shimmer;

        /// <summary>
        /// Gets the underlying ShimmerBluetooth instance for direct API access
        /// </summary>
        public ShimmerBluetooth Shimmer { get => shimmer; }

        /// <summary>
        /// Unity timestamp when the device successfully connected and started streaming
        /// </summary>
        void Start()
        {
            // Auto-connect if enabled
            if (autoConnectAndStream)
                Connect();
        }


        void OnApplicationQuit()
        {
            Disconnect();
        }

 

        /// <summary>
        /// Attempts to connect to the Shimmer3 bluetooth device using current configuration
        /// </summary>
        public void Connect()
        {
            if (CurrentState == State.None || CurrentState == State.Disconnected)
            {
                shimmerThread = new Thread(ConnectionThread);
                shimmerThread.Start();
            }
        }

        /// <summary>
        /// Disconnects from the currently connected Shimmer device
        /// </summary>
        public void Disconnect()
        {
            if (CurrentState == State.Connected ||
                CurrentState == State.Connecting ||
                CurrentState == State.Streaming)
            {
                if (shimmer != null)
                {
                    // Run disconnect in separate thread to avoid blocking Unity
                    new Thread(() =>
                    {
                        shimmer.Disconnect();
                    }).Start();
                }
            }
        }

        /// <summary>
        /// Starts data streaming on the connected device
        /// </summary>
        public void StartStreaming()
        {
            if (CurrentState != State.Connected || shimmer == null)
            {
                Debug.LogError("Cannot start streaming: device is not connected");
                return;
            }

            new Thread(() =>
            {
                Thread.Sleep(5000);
                /// MUST HAVE THIS LINE TO SET THE SAMPLING RATE BEFORE STARTING STREAMING
                shimmer.WriteSamplingRate(shimmer.GetSamplingRate());
                shimmer.StartStreaming();
            }).Start();
        }

        /// <summary>
        /// Stops data streaming on the connected device
        /// </summary>
        public void StopStreaming()
        {
            if (CurrentState == State.Streaming && shimmer != null)
            {
                new Thread(() =>
                {
                    shimmer.StopStreaming();
                }).Start();
            }
        }

        /// <summary>
        /// Displays the current configuration settings for the EXG sensors.
        /// </summary>
        public void DisplayEXGConfigurations()
        {
            shimmer.ReadEXGConfigurations(1);
            shimmer.ReadEXGConfigurations(2);

            Debug.Log("EXG CHIP 1 CONFIGURATION");
            for (int i = 0; i < 10; i++)
            {
                Debug.Log(shimmer.GetEXG1RegisterContents()[i] + " ");
            }
            Debug.Log("EXG CHIP 2 CONFIGURATION");
            for (int i = 0; i < 10; i++)
            {
                Debug.Log(shimmer.GetEXG2RegisterContents()[i] + " ");
            }
        }

        #region Shimmer Thread Operations
        /// <summary>
        /// Forces the connection thread to abort (use with caution)
        /// </summary>
        public void ForceAbortThread()
        {
            if (shimmerThread != null)
            {
                shimmerThread.Abort();
            }
        }


        /// <summary>
        /// Creates and configures the Shimmer device connection.
        /// Override this method in derived classes for device-specific configuration.
        /// </summary>
        private void ConnectionThread()
        {
            Debug.Log("THREAD: Starting shimmer device connection...");
            // Create basic shimmer connection (override in derived classes for specific configurations)
            if (useDefaultConsensysSettings) shimmer = new ShimmerLogAndStreamSystemSerialPort(devName, comPort);
            else
            {
                var exg1configuration = deviceType == ShimmerConfig.ShimmerDeviceType.ECG ? Shimmer3Configuration.EXG_ECG_CONFIGURATION_CHIP1 : Shimmer3Configuration.EXG_EMG_CONFIGURATION_CHIP1;
                var exg2configuration = deviceType == ShimmerConfig.ShimmerDeviceType.ECG ? Shimmer3Configuration.EXG_ECG_CONFIGURATION_CHIP2 : Shimmer3Configuration.EXG_EMG_CONFIGURATION_CHIP2;
                shimmer = new ShimmerLogAndStreamSystemSerialPort(
                    devName: devName,
                    bComPort: comPort,
                    samplingRate: samplingRate,
                    accelRange: (int)accelerometerRange,
                    gsrRange: (int)gsrRange,
                    gyroRange: (int)gyroscopeRange,
                    magRange: (int)magnetometerRange,
                    setEnabledSensors: (int)enabledSensors,
                    enableLowPowerAccel: enableLowPowerAccel,
                    enableLowPowerGyro: enableLowPowerGyro,
                    enableLowPowerMag: enableLowPowerMag,
                    exg1configuration: exg1configuration,
                    exg2configuration: exg2configuration,
                    internalexppower: enableInternalExpPower
                );
            }
            shimmer.UICallback += HandleEvent;
            shimmer.Connect();
        }

        /// <summary>
        /// Handles events from the Shimmer device on a separate thread.
        /// This method processes state changes and data packets.
        /// </summary>
        private void HandleEvent(object sender, EventArgs args)
        {
            CustomEventArgs eventArgs = (CustomEventArgs)args;
            int indicator = eventArgs.getIndicator();

            switch (indicator)
            {
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_STATE_CHANGE:
                    HandleStateChange(eventArgs);
                    break;

                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_NOTIFICATION_MESSAGE:
                    // Handle notification messages if needed
                    break;

                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_DATA_PACKET:
                    HandleDataPacket(eventArgs);
                    break;
            }
        }

        /// <summary>
        /// Handles state change events from the Shimmer device
        /// </summary>
        private void HandleStateChange(CustomEventArgs eventArgs)
        {
            int state = (int)eventArgs.getObject();
            string deviceName = shimmer?.GetDeviceName() ?? "Unknown";

            switch (state)
            {
                case (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTED:
                    Debug.Log($"THREAD: {deviceName} Connected");
                    CurrentState = State.Connected;

                    if (autoConnectAndStream)
                        StartStreaming();
                    break;

                case (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTING:
                    Debug.Log($"THREAD: {deviceName} Connecting");
                    CurrentState = State.Connecting;
                    break;

                case (int)ShimmerBluetooth.SHIMMER_STATE_NONE:
                    Debug.Log($"THREAD: {deviceName} Disconnected");
                    shimmer.UICallback -= HandleEvent;
                    CurrentState = State.Disconnected;
                    break;

                case (int)ShimmerBluetooth.SHIMMER_STATE_STREAMING:
                    Debug.Log($"THREAD: {deviceName} Streaming");
                    CurrentState = State.Streaming;
                    break;
            }
        }

        /// <summary>
        /// Handles incoming data packets from the Shimmer device
        /// </summary>
        private void HandleDataPacket(CustomEventArgs eventArgs)
        {
            ObjectCluster objectCluster = (ObjectCluster)eventArgs.getObject();

            // Trigger Unity event on main thread (Unity handles cross-thread calls automatically for UnityEvents)
            OnDataReceived.Invoke(this, objectCluster);
        }

        #endregion
    }

    /// <summary>
    /// Unity event for capturing sensor data from the Shimmer device
    /// </summary>
    [System.Serializable]
    public class DataReceivedEvent : UnityEvent<ShimmerDeviceUnity, ObjectCluster> { }

    /// <summary>
    /// Unity event for listening to state changes on the Shimmer device
    /// </summary>
    [System.Serializable]
    public class StateChangeEvent : UnityEvent<ShimmerDeviceUnity, ShimmerDeviceUnity.State> { }
}