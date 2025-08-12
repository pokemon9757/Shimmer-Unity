using System.Collections.Generic;
using System.IO;
using ShimmerAPI;
using UnityEngine;

namespace ShimmerDataCollection
{
    /// <summary>
    /// Processes and displays Shimmer sensor data in real-time.
    /// Logs sensor data to CSV files with event markers for synchronization.
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

        [Header("Logging to CSV Configuration")]
        [SerializeField]
        [Tooltip("Enable CSV data logging to file")]
        private bool enableDataLogging = true;
        [SerializeField]
        [Tooltip("Directory path for CSV log files (leave empty for default persistent data path)")]
        private string logFileDirectory = "";
        [SerializeField] private string fileName = "";
        [SerializeField]
        [Tooltip("Allow overwriting existing CSV files. If false, will append timestamp to avoid overwriting")]
        private bool allowOverwrite = true;

        // Enum for CSV delimiter options
        public enum Delimiter
        {
            [InspectorName("Space ( )")]
            Space = 0,
            [InspectorName("Comma (,)")]
            Comma = 1,
            [InspectorName("Semicolon (;)")]
            Semicolon = 2,
            [InspectorName("Tab (\\t)")]
            Tab = 3
        }

        [SerializeField]
        [Tooltip("Character used to separate values in the CSV file")]
        private Delimiter delimiter = Delimiter.Comma;

        /// <summary>
        /// Cached delimiter string for performance
        /// </summary>
        private string _delimiterString = ",";
        private bool _firstWrite = true;              // Flag to track first CSV write for header
        private StreamWriter PCsvFile;                // CSV file writer instance
        private string _pendingEventMarker = "";      // Event marker to write with next data row

        void OnEnable()
        {
            // Subscribe to data events when component is enabled
            if (shimmerDevice != null)
            {
                shimmerDevice.OnDataReceived.AddListener(OnDataReceived);
                InitializeLogging();
            }
        }

        public void InitializeLogging()
        {
            try
            {
                // Cache the delimiter string once for performance
                _delimiterString = GetDelimiterString();

                // Set up log directory and file name
                string logDirectory = string.IsNullOrEmpty(logFileDirectory)
                    ? Application.persistentDataPath
                    : logFileDirectory;
                string fileName = string.IsNullOrEmpty(this.fileName) ? $"{shimmerDevice.DeviceName}_shimmer_data.csv" : this.fileName;
                string fullPath = System.IO.Path.Combine(logDirectory, fileName);

                // Handle file overwrite behavior
                if (!allowOverwrite && File.Exists(fullPath))
                {
                    // Create unique filename with timestamp to avoid overwriting
                    string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
                    string extension = System.IO.Path.GetExtension(fileName);
                    string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string uniqueFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
                    fullPath = System.IO.Path.Combine(logDirectory, uniqueFileName);
                    Debug.Log($"File exists and overwrite is disabled. Using unique filename: {uniqueFileName}");
                }

                // Create new CSV file (overwrites existing if allowOverwrite is true)
                PCsvFile = new StreamWriter(fullPath, append: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize Shimmer CSV logging: {ex.Message}");
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

            // Write data to CSV file
            WriteData(objectCluster);
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

        public void WriteData(ObjectCluster obj)
        {
            // Write CSV header on first data write
            if (_firstWrite)
            {
                WriteHeader(obj);
                _firstWrite = false;
            }

            // Write all sensor data values
            double[] array = obj.GetData().ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                PCsvFile.Write(array[i] + _delimiterString);
            }

            // Write the event marker column
            PCsvFile.Write(_pendingEventMarker + _delimiterString);

            // Clear the pending event marker after writing
            _pendingEventMarker = "";

            PCsvFile.WriteLine();
        }

        private void WriteHeader(ObjectCluster obj)
        {
            ObjectCluster objectCluster = new ObjectCluster(obj);
            List<string> names = objectCluster.GetNames();
            List<string> formats = objectCluster.GetFormats();
            List<string> units = objectCluster.GetUnits();
            List<double> data = objectCluster.GetData();
            string shimmerID = objectCluster.GetShimmerID();

            // Write Shimmer ID row
            for (int i = 0; i < data.Count; i++)
            {
                PCsvFile.Write(shimmerID + _delimiterString);
            }
            // Add header for event marker column
            PCsvFile.Write(shimmerID + _delimiterString);

            PCsvFile.WriteLine();

            // Write signal names row
            for (int j = 0; j < data.Count; j++)
            {
                PCsvFile.Write(names[j] + _delimiterString);
            }
            // Add column name for event markers
            PCsvFile.Write("Event_Marker" + _delimiterString);

            PCsvFile.WriteLine();

            // Write format types row
            for (int k = 0; k < data.Count; k++)
            {
                PCsvFile.Write(formats[k] + _delimiterString);
            }
            // Add format for event marker column
            PCsvFile.Write("STRING" + _delimiterString);

            PCsvFile.WriteLine();

            // Write units row
            for (int l = 0; l < data.Count; l++)
            {
                PCsvFile.Write(units[l] + _delimiterString);
            }
            // Add units for event marker column
            PCsvFile.Write("EVENT" + _delimiterString);

            PCsvFile.WriteLine();
        }

        /// <summary>
        /// Adds an event marker that will be written to the CSV file with the next data row.
        /// This allows synchronizing events with sensor data timestamps.
        /// </summary>
        /// <param name="eventName">The name or description of the event to mark</param>
        public void AddEventMarker(string eventName)
        {
            if (!string.IsNullOrEmpty(eventName))
            {
                // Warn if overwriting an existing pending marker
                if (!string.IsNullOrEmpty(_pendingEventMarker))
                {
                    Debug.LogError($"An event marker is already pending. Overwriting {_pendingEventMarker} with new marker: {eventName}");
                }
                _pendingEventMarker = eventName;
                Debug.Log($"Event marker queued: {eventName}");
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
        /// Gets the string representation of the delimiter
        /// </summary>
        public string GetDelimiterString()
        {
            return delimiter switch
            {
                Delimiter.Space => " ",
                Delimiter.Comma => ",",
                Delimiter.Semicolon => ";",
                Delimiter.Tab => "\t",
                _ => ","
            };
        }

        void OnApplicationQuit()
        {
            // Clean up resources on application exit
            if (PCsvFile != null)
            {
                PCsvFile.Close();
                PCsvFile = null;
            }
        }
    }
}
