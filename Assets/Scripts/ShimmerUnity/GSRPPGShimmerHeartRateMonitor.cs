using UnityEngine;
using ShimmerAPI;
using ShimmerLibrary;
using System;

namespace ShimmerDataCollection
{
    /// <summary>
    /// Basic example of measuring heart rate from the shimmer device
    /// Ensure the ShimmerDevice has internalExpPower enabled and the
    /// INTERNAL_ADC_A13 sensor enabled. Also ensure the correct
    /// sampling rate is set before running the application.
    /// </summary>
    public class GSRPPGShimmerHeartRateMonitor : MonoBehaviour
    {
        [SerializeField] private ShimmerDeviceUnity shimmerDevice;
        [Tooltip("1-5 Hz is recommended for GSR to remove high frequency noise")]
        [SerializeField] private double lowPassFilterCutoffFrequency = 5;
        [Tooltip("Default value from ShimmerCAPI example is 0.5, use at your own discretion...")]
        [SerializeField] private double highPassFilterCutoffFrequency = 0.5;
        [SerializeField] private int heartRate;
        private Filter _lowPassFilter_PPG;
        private Filter _highPassFilter_PPG;
        private PPGToHRAlgorithm _PPGtoHeartRateCalculation;
        // Shimmer C API default values for PPG heart rate calculation
        private const int NumberOfHeartBeatsToAverage = 1;
        private const int TrainingPeriodPPG = 10;
        private bool _firstTime = true;

        private void InitializePPGProcessing()
        {
            if (_firstTime)
            {
                if (shimmerDevice == null)
                    shimmerDevice = FindFirstObjectByType<ShimmerDeviceUnity>();
                double samplingRate = shimmerDevice.Shimmer.GetSamplingRate();
                Debug.Log($"Initializing GSR/PPG heart rate processing with sampling rate: {samplingRate} Hz. For GSR, 0-5 Hz is suggested for tonic measurements, with 0.03-5 Hz for phasic measurements; For PPG, 100 Hz or greater is suggested;");

                //Create the heart rate algorithms 
                _PPGtoHeartRateCalculation = new PPGToHRAlgorithm(samplingRate, NumberOfHeartBeatsToAverage, TrainingPeriodPPG);
                _lowPassFilter_PPG = new Filter(Filter.LOW_PASS, samplingRate, new double[] { lowPassFilterCutoffFrequency });
                _highPassFilter_PPG = new Filter(Filter.HIGH_PASS, samplingRate, new double[] { highPassFilterCutoffFrequency });
                _firstTime = false;
            }
        }
        private void OnDataReceived(ShimmerDeviceUnity device, ObjectCluster objectCluster)
        {
            //Create the heart rate algorithms
            InitializePPGProcessing();

            //Get PPG data - using internal ADC A13
            SensorData dataPPG = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.INTERNAL_ADC_A13],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            //Get system  timestamp data
            SensorData dataTS = objectCluster.GetData(
                ShimmerConfig.NAME_DICT[ShimmerConfig.SignalName.SYSTEM_TIMESTAMP],
                ShimmerConfig.FORMAT_DICT[ShimmerConfig.SignalFormat.CAL]
            );
            
            // Early out if either sensor data is null
            if (dataPPG == null || dataTS == null)
                return;

            //Calculate the heart rate
            double dataFiltered = _lowPassFilter_PPG.filterData(dataPPG.Data);
            dataFiltered = _highPassFilter_PPG.filterData(dataFiltered);
            heartRate = (int)Math.Round(_PPGtoHeartRateCalculation.ppgToHrConversion(dataFiltered, dataTS.Data));
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