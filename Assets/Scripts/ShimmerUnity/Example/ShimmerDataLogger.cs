using System.Collections.Generic;
using ShimmerAPI;
using UnityEngine;

namespace ShimmerDataCollection
{
    /// <summary>
    /// Processes and displays Shimmer sensor data in real-time.
    /// Provides signal monitoring and data visualization capabilities.
    /// </summary>
    public class ShimmerDataLogger : MonoBehaviour
    {
        [Header("Device Connection")]
        
        [SerializeField]
        [Tooltip("Reference to the Shimmer device to monitor")]
        private ShimmerDeviceUnity shimmerDevice;

        [Header("Signal Configuration")]
        
        [SerializeField]
        [Tooltip("List of signals to monitor and display from the device")]
        private List<Signal> signals = new List<Signal>();

        /// <summary>
        /// Signal configuration for monitoring specific sensor data
        /// </summary>
        [System.Serializable]
        public class Signal
        {
            [SerializeField]
            [Tooltip("The signal type to monitor (e.g., ECG, GSR, Accelerometer)")]
            private ShimmerConfig.SignalName name;

            /// <summary>
            /// Gets the signal name/type
            /// </summary>
            public ShimmerConfig.SignalName Name => name;

            [SerializeField]
            [Tooltip("The data format (RAW for uncalibrated, CAL for calibrated values)")]
            private ShimmerConfig.SignalFormat format;

            /// <summary>
            /// Gets the signal format
            /// </summary>
            public ShimmerConfig.SignalFormat Format => format;

            [SerializeField]
            [Tooltip("The measurement units (Automatic for default units)")]
            private ShimmerConfig.SignalUnits unit;

            /// <summary>
            /// Gets the signal units
            /// </summary>
            public ShimmerConfig.SignalUnits Unit => unit;

            [SerializeField]
            [Tooltip("Current signal value (read-only, for monitoring purposes)")]
            private string value = "No Data";

            /// <summary>
            /// Gets or sets the current signal value for display
            /// </summary>
            public string Value
            {
                get => value;
                set => this.value = value;
            }
        }

        void OnEnable()
        {
            // Subscribe to data events when component is enabled
            if (shimmerDevice != null)
            {
                shimmerDevice.OnDataReceived.AddListener(OnDataReceived);
            }
        }

        void OnDisable()
        {
            // Unsubscribe from data events when component is disabled
            if (shimmerDevice != null)
            {
                shimmerDevice.OnDataReceived.RemoveListener(OnDataReceived);
            }
        }

        /// <summary>
        /// Handles incoming sensor data from the Shimmer device
        /// </summary>
        /// <param name="device">The source device</param>
        /// <param name="objectCluster">The sensor data cluster</param>
        private void OnDataReceived(ShimmerDeviceUnity device, ObjectCluster objectCluster)
        {
            // Process each configured signal
            foreach (var signal in signals)
            {
                ProcessSignal(signal, objectCluster);
            }
        }

        /// <summary>
        /// Processes a single signal from the data cluster
        /// </summary>
        /// <param name="signal">The signal configuration</param>
        /// <param name="objectCluster">The data cluster</param>
        private void ProcessSignal(Signal signal, ObjectCluster objectCluster)
        {
            // Get sensor data based on signal configuration
            SensorData data = signal.Unit == ShimmerConfig.SignalUnits.Automatic ?
                objectCluster.GetData(
                    ShimmerConfig.NAME_DICT[signal.Name],
                    ShimmerConfig.FORMAT_DICT[signal.Format]) :
                objectCluster.GetData(
                    ShimmerConfig.NAME_DICT[signal.Name],
                    ShimmerConfig.FORMAT_DICT[signal.Format],
                    ShimmerConfig.UNIT_DICT[signal.Unit]);

            if (data == null)
            {
                signal.Value = "NULL";
                return;
            }

            // Update signal value for display
            signal.Value = $"{data.Data:F3} {data.Unit}";
        }
    }
}
