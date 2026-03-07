using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using ColbyO.CardioSim.Engine;
using ColbyO.CardioSim.Settings;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    Heart.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim
{
    /// <summary>
    /// Main class to handle simulation of ECG. Rendering is handled by the ECG Visualizer. 
    /// </summary>
    public class Heart : MonoBehaviour
    {
        [Tooltip("A refernece to a ECGVisualizer where the ECG will be displayed.")]
        [SerializeField] private ECGVisualizer _ecgVisualizer;

        [Header("Simulation Settings")]
        [Tooltip("Simulation quality, controls the sample rate and engine timestep.")]
        [SerializeField, Range(0.0f, 1f)] float _quality = 0.20f;
        [Tooltip("Optional. A reference to a database that contains different pathologies ECG profiles.")]
        [SerializeField] private ECGProfileDatabase _profileDatabase;
        [Tooltip("The default ECG profile.")]
        [SerializeField] private ECGProfile _profile;

        [Header("General Settings")]
        [Tooltip("The target heart rate of the heart.")]
        [SerializeField] private float _targetHeartRate = 60f;
        [Tooltip("Strength of the overall ECG signal.")]
        [SerializeField, Range(0.2f, 1f)] private float _gain = 1.0f;
        [Tooltip("Strength of random noise added to the ECG signal.")]
        [SerializeField, Range(0f, 1f)] private float _patientMovement = 0.0f;
        [Tooltip("Controls the width of the QRS complex (and J wave).")]
        [SerializeField, Range(0.5f, 2.0f)] private float _waveSharpness = 1.0f;
        [Tooltip("Controls T wave gain. The default value is 1.0f.")]
        [SerializeField, Range(-1f, 3f)] private float _tWaveStress = 1.0f;
        [Tooltip("Controls J (Osborn) wave gain.")]
        [SerializeField, Range(0f, 1f)] private float _jWaveStress = 0.0f;
        [Tooltip("Controls U wave gain.")]
        [SerializeField, Range(0f, 1f)] private float _uWaveStress = 0.0f;

        [Header("Heart Rate Variability Settings")]
        [Tooltip("Controls the strength of the Heart Rate Varibility engine. A value of 0.0f means the heart will always beat at the Target Heart Rate.")]
        [SerializeField, Range(0f, 1f)] private float _hrvGain = 0.2f;
        [Tooltip("The power scaling factor for the LF band. Doesn't change at runtime unless 'RefreshHRV()' is called.")]
        [SerializeField] private float _hrvLFAmplitude = 0.01f;
        [Tooltip("The power scaling factor for the HF band. Doesn't change at runtime unless 'RefreshHRV()' is called.")]
        [SerializeField] private float _hrvHFAmplitude = 0.02f;
        
        [Header("Respiration Settings")]
        [Tooltip("Controls the streangth of the voltage offset due to respiration.")]
        [SerializeField, Range(0f, 1f)] private float _respirationGain = 0.1f;
        [Tooltip("Controls the respiration rate.")]
        [SerializeField] private float _respirationRate = 0.25f;

        [Header("Audio Settings")]
        [Tooltip("A flag to diable audio.")]
        [SerializeField] private bool _playAudio = true;
        [Tooltip("A reference to a AudioSource where the heart sounds will come from.")]
        [SerializeField] private AudioSource _as;
        [Tooltip("AudioClip for a ECG beep.")]
        [SerializeField] private AudioClip _ecgBeepClip;
        [Tooltip("AudioClip for a ECG flatline.")]
        [SerializeField] private AudioClip _flatlineClip;

        private ECGEngine _engine;
        private ECGState _state;
        private float _time = 0f;
        private float _accumulator = 0.0f;

        private float[] _hrvBuffer;
        private const int BUFFER_SIZE = 2048;
        private const float HRV_SAMPLE_RATE = 100f;

        public float Voltage { get; private set; }
        private float _currentECG;
        public Queue<float> SampleBuffer = new Queue<float>();

        private SignalState _signal;

        private List<float> _prevBeats = new List<float>();
        private float _prevRWaveTime;
        private float _k1, _k2, _k3, _k4;

        private Coroutine _morphCoroutine;

        public float Quailty { get => _quality; set => _quality = Mathf.Clamp01(value); }

        public float EngineDt { get; private set; }
        public int SampleRate { get; private set; }

        public bool EnableAudio { get => _playAudio; set => _playAudio = value; }

        public float PatientMovement { get => _patientMovement; set => _patientMovement = value; }
        public float HRVGain { get => _hrvGain; set => _hrvGain = value; }
        public float RespirationGain { get => _respirationGain; set => _respirationGain= value; }
        public float RespirationRate { get => _respirationRate; set => _respirationRate = value; }
        public float Gain { get => _gain; set => _gain = value; }
        public float WaveShaprness { get => _waveSharpness; set => _waveSharpness = value; }
        public float TWaveStress { get => _tWaveStress; set => _tWaveStress = value; }
        public float JWaveStress { get => _jWaveStress; set => _jWaveStress = value; }
        public float UWaveStress { get => _uWaveStress; set => _uWaveStress = value; }

        public float TargetHeartRate { get => _targetHeartRate; }
        public float HeartRate { get; private set; }
        public float SystolicBloodPressure { get; private set; }
        public float DiastolicBloodPressure { get; private set; }
        public float PulseTransitTime { get; private set; }

        public bool IsArrested { get; private set; }

        [ContextMenu("TriggerCardiacArrest")]
        public void TriggerCardiacArrest()
        {
            if (IsArrested) return;
            IsArrested = true;

            HeartRate = 0;
            DiastolicBloodPressure = 0;
            SystolicBloodPressure = 0;
            PulseTransitTime = Mathf.Infinity;

            if (_morphCoroutine != null) StopCoroutine(_morphCoroutine);
            SetCondition("Asystole", bpm: 0);
        }

        public void RestartHeart(string condition = "Normal", float bpm = 60.0f, float duration = 2f)
        {
            IsArrested = false;

            if (_as && _as.isPlaying)
            {
                _as.Stop();
            }

            SetCondition(condition);
            SetTargetHeartRate(Mathf.Min(bpm, 40.0f));
            SetTargetHeartRate(bpm, duration);
        }

        [ContextMenu("RestartHeart")]
        public void RestartHeart()
        {
            RestartHeart(condition: "Normal", bpm: 60.0f, duration: 2f);
        }

        private void Awake()
        {
            if (_profileDatabase == null) _profileDatabase = Resources.Load<ECGProfileDatabase>("ECGDatabase");
            if (!_ecgVisualizer) _ecgVisualizer = GetComponent<ECGVisualizer>();

            _profileDatabase.InitDatabase();

            _engine = new ECGEngine();
            _state = new ECGState { x = 1.0f, y = 0.0f };

            _prevBeats = new List<float>(Enumerable.Repeat(0.8f, 8));

            _k1 = Random.Range(100f, 140f);
            _k2 = Random.Range(0.5f, 1.5f);
            _k3 = Random.Range(70f, 100f);
            _k4 = Random.Range(0.2f, 0.8f);

            RefreshHRV();

            UpdateSimulationQuaility();

            float warmUpTime = 7.5f;
            _time = 0.0f;
            while (_time < warmUpTime)
            {
                float omega = GetOmega(_time);

                _state = _engine.Step(_state, EngineDt, _time, omega, _respirationRate, Mathf.Lerp(0f, 10f, _respirationGain), _profile, _waveSharpness);
                float val = _engine.CalculateVoltage(_state, _profile, _gain, _targetHeartRate, 0.0f, _tWaveStress, _jWaveStress, _uWaveStress);
                _signal.RunningAvg = Mathf.Lerp(_signal.RunningAvg, val, 0.01f);
                _signal.RunningVar = Mathf.Lerp(_signal.RunningVar, Mathf.Abs(val - _signal.RunningAvg), 0.005f);
                _time += EngineDt;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            _profile.SyncArrays();
#endif
            UpdateSimulationQuaility();

            if (_playAudio && _as && !_as.isPlaying && _flatlineClip && IsArrested)
            {
                _as.clip = _flatlineClip;
                _as.Play();
            }
            else if (!_playAudio && _as && _as.isPlaying && _flatlineClip && IsArrested)
            {
                _as.Stop();
            }

            int downsampleRate = SampleRate;
            _accumulator += Time.deltaTime;

            while (_accumulator >= EngineDt)
            {
                _accumulator -= EngineDt;

                float omega = GetOmega(_time);

                _state = _engine.Step(_state, EngineDt, _time, omega, _respirationRate, Mathf.Lerp(0f, 2.0f, _respirationGain), _profile, _waveSharpness);

                _currentECG = _engine.CalculateVoltage(_state, _profile, _gain, _targetHeartRate, 0.0f, _tWaveStress, _jWaveStress, _uWaveStress);

                _signal.PendingMax = Mathf.Max(_signal.PendingMax, _currentECG);
                _signal.PendingMin = Mathf.Min(_signal.PendingMin, _currentECG);

                _signal.SubStepCounter++;
                _time += EngineDt;

                if (_signal.SubStepCounter >= downsampleRate)
                {
                    ProcessOutputSample();
                    _signal.SubStepCounter = 0;
                    _signal.ResetPending();
                }
            }
        }

        private void UpdateSimulationQuaility()
        {
            float quality = Mathf.Lerp(0.01f, 5.0f, _quality);
            EngineDt = 0.0001f / quality;
            float secondsToDisplay = (_ecgVisualizer) ? _ecgVisualizer.GetSecondsToDisplay() : 1f;
            SampleRate = Mathf.Min(Mathf.Max(Mathf.RoundToInt(secondsToDisplay * 10f * quality), 1), 2500);
        }

        public void TransitionToCondition(ECGProfile targetProfile, float targetBPM, float duration)
        {
            if (targetProfile == null) return;
            if (_morphCoroutine != null) StopCoroutine(_morphCoroutine);
            _morphCoroutine = StartCoroutine(EvolvePatientCondition(targetProfile, targetBPM, duration));
        }

        private IEnumerator EvolvePatientCondition(ECGProfile target, float targetBPM, float duration)
        {
            float startBPM = _targetHeartRate;

            ECGProfile startState = Instantiate(_profile);
            ECGProfile workingProfile = Instantiate(_profile);
            _profile = workingProfile;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);

                SetTargetHeartRate(Mathf.Lerp(startBPM, targetBPM, t));
                ECGProfile.Lerp(startState, target, ref workingProfile, t);

                yield return null;
            }

            _profile = target;
            _targetHeartRate = targetBPM;
            _morphCoroutine = null;

            Destroy(startState);
            Destroy(workingProfile);
        }

        private void SetCondition(ECGProfile newProfile, float bpm)
        {
            if (newProfile == null) return;

            SetTargetHeartRate(bpm);
            _profile = newProfile;

#if UNITY_EDITOR
            _profile.SyncArrays();
#endif
        }

        public void SetCondition(string name, float duration, float? bpm = null)
        {
            if (_profileDatabase != null)
            {
                ECGProfile target = _profileDatabase.GetEntry(name);
                if (target != null)
                {
                    TransitionToCondition(target, bpm ?? _targetHeartRate, duration);
                }
            }
        }

        public void SetCondition(string name, float? bpm = null)
        {
            if (_profileDatabase != null)
            {
                ECGProfile target = _profileDatabase.GetEntry(name);
                SetCondition(target, bpm ?? _targetHeartRate);
            }
        }

        public void SetTargetHeartRate(float bpm)
        {
            _targetHeartRate = bpm;
        }

        public void SetTargetHeartRate(float bpm, float duration)
        {
            TransitionToCondition(_profile, bpm, duration);
        }

        private float GetOmega(float t)
        {
            float baseOmega = _targetHeartRate / 60f;
            int index = Mathf.FloorToInt(t * HRV_SAMPLE_RATE) % BUFFER_SIZE;
            float variation = _hrvBuffer[index];

            return (baseOmega + Mathf.Lerp(0f, 1000f, _hrvGain) * variation) * 2f * Mathf.PI;
        }

        private void ProcessOutputSample()
        {
            float jitter = (Random.value - 0.5f) * Mathf.Lerp(0.0f, 0.3f, _patientMovement);
            float rawVal = (Mathf.Abs(_signal.PendingMax) > Mathf.Abs(_signal.PendingMin)) ? _signal.PendingMax : _signal.PendingMin;
            float val = rawVal + jitter;

            Voltage = val;

            if (!float.IsFinite(val)) return;

            if (IsArrested)
            {
                _signal.IsPeakState = false;
                SampleBuffer.Enqueue(val);
                return;
            }

            _signal.RunningAvg = Mathf.Lerp(_signal.RunningAvg, rawVal, 0.01f);
            _signal.RunningVar = Mathf.Lerp(_signal.RunningVar, Mathf.Abs(rawVal - _signal.RunningAvg), 0.005f);
            float threshold = _signal.RunningAvg + (_signal.RunningVar * 3.0f);

            _signal.LockoutTimer += EngineDt * SampleRate;

            if (rawVal > threshold && !_signal.IsPeakState && _signal.LockoutTimer >= 60.0f / Mathf.Max(_targetHeartRate, 0.01f) / 2f)
            {
                OnRPeak();
                _signal.IsPeakState = true;
                _signal.LockoutTimer = 0;
            }
            else if (rawVal < threshold)
            {
                _signal.IsPeakState = false;
            }

            SampleBuffer.Enqueue(val);
            if (SampleBuffer.Count > 1000) SampleBuffer.Dequeue();
        }

        private void OnRPeak()
        {
            if (_playAudio && _as && _ecgBeepClip) _as.PlayOneShot(_ecgBeepClip);

            float now = Time.time;
            PulseTransitTime = now - _prevRWaveTime;
            _prevRWaveTime = now;

            SystolicBloodPressure = (_k1 - _k2 * PulseTransitTime);
            DiastolicBloodPressure = (_k3 - _k4 * PulseTransitTime);

            _prevBeats.Add(PulseTransitTime);
            if (_prevBeats.Count > 8) _prevBeats.RemoveAt(0);

            float sum = 0;
            for (int i = 0; i < _prevBeats.Count; i++) sum += _prevBeats[i];
            HeartRate = 60.0f / (sum / _prevBeats.Count);
        }

        [ContextMenu("Refresh HRV")]
        public void RefreshHRV()
        {
            HRVGenerator gen = new HRVGenerator();
            _hrvBuffer = gen.GenerateHRVBuffer(BUFFER_SIZE, HRV_SAMPLE_RATE, _hrvLFAmplitude, _hrvHFAmplitude);
        }

        public void RefreshHRV(float hrvLFAmplitude, float hrvHFAmplitude)
        {
            _hrvLFAmplitude = hrvLFAmplitude;
            _hrvHFAmplitude = hrvHFAmplitude;

            HRVGenerator gen = new HRVGenerator();
            _hrvBuffer = gen.GenerateHRVBuffer(BUFFER_SIZE, HRV_SAMPLE_RATE, _hrvLFAmplitude, _hrvHFAmplitude);
        }

        private struct SignalState
        {
            public float RunningAvg;
            public float RunningVar;
            public float LockoutTimer;
            public bool IsPeakState;
            public float PendingMax;
            public float PendingMin;
            public int SubStepCounter;

            public void ResetPending()
            {
                PendingMax = float.MinValue;
                PendingMin = float.MaxValue;
            }
        }
    }
}