# Shimmer Biosignal Unity Package

A Unity package for real-time biosignal data collection from Shimmer sensors, including ECG, GSR, and PPG.

Designed for both researchers and developers, it lets you quickly stream, monitor, and log physiological signals in Unity — with minimal setup required.

This package expands and fixes a major configuration issue from [shimmering-unity](https://github.com/jemmec/shimmering-unity) that slows sampling rates by ~100×.

## Key Features
- **Flexible Shimmer Configuration Options**:
  1. Keep existing Shimmer configurations written through Consensys
  2. Quickly configure devices through Unity Inspector with recommended settings
- **Extensive Inspector Interface**: Select recommended settings based on latest Shimmer documentation
- **Real-time Device State Monitoring**: Visual feedback of connection and streaming status
- **Automatic Recommended Settings**: Device-type specific configurations based on the Shimmer User Guides GSR+ Rev 1.13, Optical Pulse Rev 1.6, and ECG Rev 1.12.
- **CSV Data Logging**: Store collected sensor data with timestamps in CSV format. Logs can be saved to Unity's [persistent data path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html) or custom OS directory.
- **Real-time Signal Monitoring**: Live sensor value display in Unity Inspector.

### Free Implementation of Premium Features
- **ECG to Heart Rate**: Derived from LL-RA (Lead II) data filtered through High-pass and Band-stop filters
- **GSR/PPG to Heart Rate**: Derived from PPG data filtered through High-pass and Low-pass filters.

## Getting Started
1. **Import the Package**: Either clone this project or import this [Unity package](https://github.com/pokemon9757/Shimmer-Unity/releases/latest) into your project. There will be an example scene that you can access. 
2. **Add Device Controller**: Add `ShimmerDeviceUnity` component to a GameObject in your scene
    - **Configure Device**: 
        - Set your device name (e.g., "ShimmerBD01") in the Inspector
        - Set the correct COM port for your Shimmer device. You can find this in Settings -> Bluetooth and Devices -> Devices -> More Bluetooth Settings -> Choose "COM ports" tab -> Find the Outgoing port that has your Shimmer device name. 
    - **Choose Configuration Method**:
        - **Option A**: Enable "Use Default Consensys Settings" to preserve existing device configuration
        - **Option B**: Disable it and select device type (GSR/PPG/ECG) for automatic recommended settings
    - **Enable Data Logging**: 
        - Click the checkbox to store data to CSV and enter the System path that you would like to store to. By default, data will be stored at Unity persistent data path.
3. **Add Heart Rate Monitoring**: 
   - For ECG: Add `ECGShimmerHeartRateMonitor` component
   - For GSR/PPG: Add `GSRPPGShimmerHeartRateMonitor` component
5. **Optional Signal Monitoring**: Add `ShimmerDataLogger` for real-time signal display

## Scripts Overview
- `ShimmerDeviceUnity.cs` Shimmer device controller.
- `ShimmerDataLogger.cs` Visualizes data for debugging.
- `ECGShimmerHeartRateMonitor.cs` Uses Shimmer's ECGToHRAdaptive for real-time heart rate calculation
    - **Filters**:
        - High-pass filter: 0.05 Hz (diagnostic quality) or 0.5 Hz (stable HR in long sessions)
        - Band-stop filter: 50 Hz (Europe) or 60 Hz (US) for mains electrical interference removal
    - **Training Period**: Requires ~10 seconds of data for algorithm training 

- `GSRPPGShimmerHeartRateMonitor.cs` Uses Shimmer's PPGToHRAlgorithm for real-time heart rate calculation
    - **Filters**:
        - Low-pass filter: 1-5 Hz (removes high-frequency noise from GSR/PPG)
        - High-pass filter: 0.5 Hz (removes baseline drift, Shimmer C API default)
    - **Training Period**: Requires ~10 seconds of data for algorithm training
    

### Basic Usage Example

```csharp
using ShimmerDataCollection;
using ShimmerAPI;

public class HeartRateDisplay : MonoBehaviour
{
    [SerializeField] private ShimmerDeviceUnity shimmerDevice;
    [SerializeField] private ECGShimmerHeartRateMonitor ecgMonitor;
    
    void Start()
    {
        // Subscribe to data events
        shimmerDevice.OnDataReceived.AddListener(OnDataReceived);
        
        // Connect and start streaming
        shimmerDevice.Connect();
    }
    
    private void OnDataReceived(ShimmerDeviceUnity device, ObjectCluster data)
    {
        // Access heart rate from ECG monitor, heart rate will take ~10 seconds of measurement to display
        float heartRate = ecgMonitor.HeartRate;
        Debug.Log($"Current Heart Rate: {heartRate} BPM");
        
        // Access specific sensor data
        var ecgData = data.GetData(
            ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.ECG_LL_RA], // Set your signal to get here
            ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL] // Set your signal unit here, CAL is default
        );
        
        if (ecgData != null)
        {
            Debug.Log($"ECG LL-RA: {ecgData.Data:F3} {ecgData.Unit}");
        }
    }
}
```

## Configuration Recommendations

### ECG Setup
- **Device Type**: Select "ECG" in Unity Inspector
- **Sampling Rate**: 512 Hz (automatically set with recommended settings)
- **Enabled Sensors**: EXG1 24-bit + EXG2 24-bit + Internal ADC A13
- **Filter Configuration**:
  - High-pass: 0.05 Hz for diagnostic quality ECG
  - Band-stop: 50 Hz (Europe) or 60 Hz (US) for mains interference


### GSR Setup
- **Device Type**: Select "GSR" in Unity Inspector  
- **Sampling Rate**: 1 Hz (recommended for tonic measurements)
- **Enabled Sensors**: GSR + Internal ADC A13
- **GSR Range**: Auto Range (recommended) or manual selection based on expected conductance
- **Enable Internal Exp Power**: Required for GSR measurements
- **Filter Configurations**:
  - Tonic (slow changes): 0-5 Hz sampling adequate
  - Phasic (fast changes): 0.03-5 Hz for capturing rapid responses

### PPG Setup
- **Device Type**: Select "PPG" in Unity Inspector
- **Sampling Rate**: 128 Hz (recommended minimum 100+ Hz)
- **Enabled Sensors**: GSR + Internal ADC A13 (PPG uses Internal ADC A13)
- **Enable Internal Exp Power**: Required for PPG sensor operation
- **Filter Configurations**:
  - Low-pass: 1-5 Hz for noise reduction
  - High-pass: 0.5 Hz for baseline drift removal

## Upcoming Features (Work in Progress)
- **Event Tracking Integration**: Runtime event logging with timestamps for research applications. For example, when user presses a button, that moment is tracked in the csv data log. 
## License and Attribution

**If you would like to use this package in your projects or research please acknowledge the original authors by citing this repository.**

> https://github.com/pokemon9757/Shimmer-Unity 

## Credits and Acknowledgements

- This project is an expansion of [shimmering-unity](https://github.com/jemmec/shimmering-unity).
- The [ShimmerAPI](https://github.com/ShimmerEngineering/Shimmer-C-API) was created by [Shimmer](https://shimmersensing.com/).
