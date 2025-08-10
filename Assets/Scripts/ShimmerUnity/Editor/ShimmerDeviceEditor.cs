using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShimmerDataCollection
{
    /// <summary>
    /// Custom inspector for the Shimmer device
    /// </summary>
    [CustomEditor(typeof(ShimmerDeviceUnity), true)]
    public class ShimmerDeviceEditor : Editor
    {
        private SerializedProperty autoConnectAndStreamProperty;
        private SerializedProperty devNameProperty;
        private SerializedProperty comPortProperty;
        private SerializedProperty deviceTypeProperty;
        private SerializedProperty useDefaultConsensysSettingsProperty;
        private SerializedProperty useRecommendedSettingsProperty;
        private SerializedProperty enabledSensorsProperty;
        private SerializedProperty samplingRateProperty;
        private SerializedProperty accelerometerRangeProperty;
        private SerializedProperty gyroscopeRangeProperty;
        private SerializedProperty magnetometerRangeProperty;
        private SerializedProperty enableLowPowerAccelProperty;
        private SerializedProperty enableLowPowerGyroProperty;
        private SerializedProperty enableLowPowerMagProperty;
        private SerializedProperty enableInternalExpPowerProperty;
        private SerializedProperty gsrRangeProperty;
        private SerializedProperty enableDataLoggingProperty;
        private SerializedProperty logFileDirectoryProperty;
        private SerializedProperty onDataReceivedProperty;
        private ShimmerConfig.ShimmerDeviceType previousDeviceType;
        private bool previousUseRecommendedSettings;

        public override void OnInspectorGUI()
        {
            ShimmerDeviceUnity shimmerDevice = (ShimmerDeviceUnity)target;
            GUIStyle style = new GUIStyle();
            style.richText = true;
            if (Application.isPlaying)
                GUILayout.Label($"<size=24><color=#00ff00>Current State: {shimmerDevice.CurrentState.ToString()}</color></size>", style);
            else
                GUILayout.Label($"<size=24><color=#ff0000>App not running.</color></size>", style);

            if (Application.isPlaying && GUILayout.Button("Connect"))
            {
                shimmerDevice.Connect();
            }
            if (Application.isPlaying && GUILayout.Button("Disconnect"))
            {
                shimmerDevice.Disconnect();
            }
            if (Application.isPlaying && GUILayout.Button("Start Streaming"))
            {
                shimmerDevice.StartStreaming();
            }
            if (Application.isPlaying && GUILayout.Button("Stop Streaming"))
            {
                shimmerDevice.StopStreaming();
            }
            if (Application.isPlaying && GUILayout.Button("Force Abort Thread"))
            {
                shimmerDevice.ForceAbortThread();
            }

            if (EditorApplication.isPlaying)
                Repaint();

            serializedObject.Update();

            // Draw Connection Settings Header
            EditorGUILayout.LabelField("Connection Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoConnectAndStreamProperty);
            EditorGUILayout.PropertyField(devNameProperty);
            EditorGUILayout.PropertyField(comPortProperty);
            EditorGUILayout.PropertyField(deviceTypeProperty);
            EditorGUILayout.PropertyField(useDefaultConsensysSettingsProperty);

            EditorGUILayout.Space();

            // Only show custom settings if useDefaultConsensysSettings is false
            bool useDefaultSettings = useDefaultConsensysSettingsProperty.boolValue;
            if (!useDefaultSettings)
            {
                EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);
                // Check if useRecommendedSettings is enabled and device type has changed
                bool useRecommended = useRecommendedSettingsProperty.boolValue;
                ShimmerConfig.ShimmerDeviceType currentDeviceType = (ShimmerConfig.ShimmerDeviceType)deviceTypeProperty.enumValueIndex;

                if ((useRecommended && currentDeviceType != previousDeviceType) ||
                    (useRecommended && previousUseRecommendedSettings != useRecommended))
                {
                    ApplyRecommendedSettingsInEditor(currentDeviceType);
                    previousDeviceType = currentDeviceType;
                    previousUseRecommendedSettings = useRecommended;
                }

                // Display a helpful info box when recommended settings are enabled
                if (useRecommended)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox($"Recommended settings are active for {currentDeviceType}. " +
                        "Settings will be automatically configured according to Shimmer User Guides.", MessageType.Info);

                    // Show current recommended values
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("Current Recommended Settings:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Sampling Rate: {GetRecommendedSamplingRate(currentDeviceType)} Hz");
                    EditorGUILayout.LabelField($"Enabled Sensors: {GetRecommendedSensors(currentDeviceType)}");
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("Recommended settings are not enabled. " +
                        "You can enable them to automatically configure settings based on the device type.", MessageType.Info);
                }
                EditorGUILayout.PropertyField(useRecommendedSettingsProperty);
                EditorGUILayout.PropertyField(enabledSensorsProperty);
                EditorGUILayout.PropertyField(samplingRateProperty);
                EditorGUILayout.PropertyField(accelerometerRangeProperty);
                EditorGUILayout.PropertyField(gyroscopeRangeProperty);
                EditorGUILayout.PropertyField(magnetometerRangeProperty);
                EditorGUILayout.PropertyField(enableLowPowerAccelProperty);
                EditorGUILayout.PropertyField(enableLowPowerGyroProperty);
                EditorGUILayout.PropertyField(enableLowPowerMagProperty);
                EditorGUILayout.PropertyField(enableInternalExpPowerProperty);
                EditorGUILayout.PropertyField(gsrRangeProperty);

                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.HelpBox("Using default Consensys settings. Custom settings are hidden.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Draw Data Logging Settings Header
            EditorGUILayout.LabelField("Data Logging", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enable this to log sensor data to a CSV file. Will record to Persistent Data Path if left empty", MessageType.Info);
            EditorGUILayout.PropertyField(enableDataLoggingProperty);
            EditorGUILayout.PropertyField(logFileDirectoryProperty);

            EditorGUILayout.Space();

            // Draw Events Header
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This event is triggered when new data is received from the device.", MessageType.Info);
            EditorGUILayout.PropertyField(onDataReceivedProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            autoConnectAndStreamProperty = serializedObject.FindProperty("autoConnectAndStream");
            devNameProperty = serializedObject.FindProperty("devName");
            comPortProperty = serializedObject.FindProperty("comPort");
            deviceTypeProperty = serializedObject.FindProperty("deviceType");
            useDefaultConsensysSettingsProperty = serializedObject.FindProperty("useDefaultConsensysSettings");
            useRecommendedSettingsProperty = serializedObject.FindProperty("useRecommendedSettings");
            enabledSensorsProperty = serializedObject.FindProperty("enabledSensors");
            samplingRateProperty = serializedObject.FindProperty("samplingRate");
            accelerometerRangeProperty = serializedObject.FindProperty("accelerometerRange");
            gyroscopeRangeProperty = serializedObject.FindProperty("gyroscopeRange");
            magnetometerRangeProperty = serializedObject.FindProperty("magnetometerRange");
            enableLowPowerAccelProperty = serializedObject.FindProperty("enableLowPowerAccel");
            enableLowPowerGyroProperty = serializedObject.FindProperty("enableLowPowerGyro");
            enableLowPowerMagProperty = serializedObject.FindProperty("enableLowPowerMag");
            enableInternalExpPowerProperty = serializedObject.FindProperty("enableInternalExpPower");
            gsrRangeProperty = serializedObject.FindProperty("gsrRange");
            enableDataLoggingProperty = serializedObject.FindProperty("enableDataLogging");
            logFileDirectoryProperty = serializedObject.FindProperty("logFileDirectory");
            onDataReceivedProperty = serializedObject.FindProperty("onDataReceived");

            previousDeviceType = (ShimmerConfig.ShimmerDeviceType)deviceTypeProperty.enumValueIndex;
            previousUseRecommendedSettings = useRecommendedSettingsProperty.boolValue;
        }

        private void ApplyRecommendedSettingsInEditor(ShimmerConfig.ShimmerDeviceType deviceType)
        {
            switch (deviceType)
            {
                case ShimmerConfig.ShimmerDeviceType.GSR:
                    ApplyGSRRecommendedSettings();
                    break;
                case ShimmerConfig.ShimmerDeviceType.PPG:
                    ApplyPPGRecommendedSettings();
                    break;
                case ShimmerConfig.ShimmerDeviceType.ECG:
                    ApplyECGRecommendedSettings();
                    break;
                default:
                    Debug.LogWarning("Unsupported device type. Using default settings.");
                    break;
            }
        }

        private void ApplyGSRRecommendedSettings()
        {
            // GSR settings based on Shimmer User Guide GSR+ Rev 1.13
            // For tonic measurements: 0-5 Hz is suggested
            // For phasic measurements: 0.03-5 Hz is adequate
            enabledSensorsProperty.enumValueFlag = (int)(ShimmerConfig.SensorBitmap.SENSOR_GSR | ShimmerConfig.SensorBitmap.SENSOR_INT_A13);
            samplingRateProperty.floatValue = 1.0f; // 1 Hz - good middle ground for both tonic and phasic
            accelerometerRangeProperty.enumValueIndex = (int)ShimmerConfig.AccelerometerRange.zero; // ± 2g
            gyroscopeRangeProperty.enumValueIndex = (int)ShimmerConfig.GyroscopeRange.zero; // 250 dps
            magnetometerRangeProperty.enumValueIndex = (int)ShimmerConfig.MagnetometerRange.one; // 1.3Ga
            enableLowPowerAccelProperty.boolValue = false;
            enableLowPowerGyroProperty.boolValue = false;
            enableLowPowerMagProperty.boolValue = false;
            enableInternalExpPowerProperty.boolValue = true;
            gsrRangeProperty.enumValueIndex = (int)ShimmerConfig.GSRRangeSetting.four; // Auto Range
        }

        private void ApplyPPGRecommendedSettings()
        {
            // PPG settings based on Shimmer User Guide Optical Pulse Rev 1.6
            // 100 Hz or greater is suggested for PPG
            enabledSensorsProperty.enumValueFlag = (int)(ShimmerConfig.SensorBitmap.SENSOR_GSR | ShimmerConfig.SensorBitmap.SENSOR_INT_A13);
            samplingRateProperty.floatValue = 128.0f; // 128 Hz - common power-of-2 rate above 100 Hz
            accelerometerRangeProperty.enumValueIndex = (int)ShimmerConfig.AccelerometerRange.zero; // ± 2g
            gyroscopeRangeProperty.enumValueIndex = (int)ShimmerConfig.GyroscopeRange.zero; // 250 dps
            magnetometerRangeProperty.enumValueIndex = (int)ShimmerConfig.MagnetometerRange.one; // 1.3Ga
            enableLowPowerAccelProperty.boolValue = false;
            enableLowPowerGyroProperty.boolValue = false;
            enableLowPowerMagProperty.boolValue = false;
            enableInternalExpPowerProperty.boolValue = true;
            gsrRangeProperty.enumValueIndex = (int)ShimmerConfig.GSRRangeSetting.four; // Auto Range
        }

        private void ApplyECGRecommendedSettings()
        {
            // ECG settings based on Shimmer User Guide ECG Rev 1.12
            // 512 Hz is suggested for ECG
            enabledSensorsProperty.enumValueFlag = (int)(ShimmerConfig.SensorBitmap.SENSOR_EXG1_24BIT |
                                                          ShimmerConfig.SensorBitmap.SENSOR_EXG2_24BIT |
                                                          ShimmerConfig.SensorBitmap.SENSOR_INT_A13);
            samplingRateProperty.floatValue = 512.0f; // 512 Hz as recommended
            accelerometerRangeProperty.enumValueIndex = (int)ShimmerConfig.AccelerometerRange.zero; // ± 2g
            gyroscopeRangeProperty.enumValueIndex = (int)ShimmerConfig.GyroscopeRange.zero; // 250 dps
            magnetometerRangeProperty.enumValueIndex = (int)ShimmerConfig.MagnetometerRange.one; // 1.3Ga
            enableLowPowerAccelProperty.boolValue = false;
            enableLowPowerGyroProperty.boolValue = false;
            enableLowPowerMagProperty.boolValue = false;
            enableInternalExpPowerProperty.boolValue = true;
            gsrRangeProperty.enumValueIndex = (int)ShimmerConfig.GSRRangeSetting.four; // Auto Range
        }

        private float GetRecommendedSamplingRate(ShimmerConfig.ShimmerDeviceType deviceType)
        {
            return deviceType switch
            {
                ShimmerConfig.ShimmerDeviceType.GSR => 1.0f,
                ShimmerConfig.ShimmerDeviceType.PPG => 128.0f,
                ShimmerConfig.ShimmerDeviceType.ECG => 512.0f,
                _ => 128.0f
            };
        }

        private string GetRecommendedSensors(ShimmerConfig.ShimmerDeviceType deviceType)
        {
            return deviceType switch
            {
                ShimmerConfig.ShimmerDeviceType.GSR => "GSR + Internal ADC A13",
                ShimmerConfig.ShimmerDeviceType.PPG => "GSR + Internal ADC A13",
                ShimmerConfig.ShimmerDeviceType.ECG => "EXG1 24-bit + EXG2 24-bit + Internal ADC A13",
                _ => "Default sensors"
            };
        }
    }
}