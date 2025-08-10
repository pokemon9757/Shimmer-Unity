using UnityEngine;
using ShimmerAPI;
using ShimmerLibrary;
namespace ShimmerDataCollection
{
    /// <summary>
    /// Convert ECG LL-RA signals to heart rate, will take around 10 seconds of data to start displaying data.
    /// </summary>
    public class ECGShimmerHeartRateMonitor : MonoBehaviour
    {
        [SerializeField] private ShimmerDeviceUnity shimmerDevice;
        [Tooltip("0.05 Hz is recommended for diagnostic ECG, 0.5 Hz for stable HR in long sessions")]
        [SerializeField] private double highPassFilterCutoffFrequency = 0.05;

        [Tooltip("Optional to avoid interference from mains electricity, default is 50Hz, only set to 60 Hz if you are in the US")]
        [SerializeField] private double bandStopFilterFrequency = 50;
        [SerializeField] private double heartRate;
        private ECGToHRAdaptive _ECGtoHRCalculation;
        private Filter _bandStopFilter_ECG;
        private Filter _highPassFilter_ECG;
        private bool _firstTime = true;

        private void InitializeECGProcessing()
        {
            if (_firstTime)
            {
                if (shimmerDevice == null)
                    shimmerDevice = FindFirstObjectByType<ShimmerDeviceUnity>();
                double samplingRate = shimmerDevice.Shimmer.GetSamplingRate();
                Debug.Log($"Initializing ECG heart rate processing with sampling rate: {samplingRate} Hz, recommended should be 512 Hz for ECG.");
                _ECGtoHRCalculation = new ECGToHRAdaptive(samplingRate);
                _highPassFilter_ECG = new Filter(Filter.HIGH_PASS, samplingRate, new double[] { highPassFilterCutoffFrequency });
                _bandStopFilter_ECG = new Filter(Filter.BAND_STOP, samplingRate, new double[] { 0, bandStopFilterFrequency });
                _firstTime = false;
            }
        }

        private void OnDataReceived(ShimmerDeviceUnity device, ObjectCluster objectCluster)
        {
            //Create the heart rate algorithms 
            InitializeECGProcessing();

            //Get heart rate data - using LL-RA lead (Lead II)
            SensorData dataLead2 = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.ECG_LL_RA],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            //Get system  timestamp data
            SensorData dataTS = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.TIMESTAMP],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );

            // Early out if either sensor data is null
            if (dataLead2 == null || dataTS == null)
                return;

            //Calculate the heart rate
            double dataFiltered = _highPassFilter_ECG.filterData(dataLead2.Data);
            dataFiltered = _bandStopFilter_ECG.filterData(dataFiltered);
            ECGToHRAdaptive.DataECGToHROutput output = _ECGtoHRCalculation.ecgToHrConversion(dataFiltered, dataTS.Data);
            heartRate = output.getHeartRate();
        }

        private void OnEnable()
        {
            shimmerDevice.OnDataReceived.AddListener(OnDataReceived);
        }

        private void OnDisable()
        {
            shimmerDevice.OnDataReceived.RemoveListener(OnDataReceived);
        }
    }
}