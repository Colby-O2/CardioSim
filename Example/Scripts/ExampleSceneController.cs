using UnityEngine;
using UnityEngine.UI;

using TMPro;

namespace ColbyO.CardioSim.Example
{
    internal class ExampleSceneController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _ecgPannel;
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Heart _heart;

        [SerializeField] private TMP_Text _heartRate;
        [SerializeField] private TMP_Text _bloodPressure;
        [SerializeField] private TMP_Text _ptt;

        [SerializeField] private Button _killButton;
        [SerializeField] private Button _reviveButton;

        [SerializeField] private Toggle _audioControl;
        [SerializeField] private Slider _heartRateControl;

        [SerializeField] private Slider _patientMovementControl;
        [SerializeField] private Slider _tStressControl;
        [SerializeField] private Slider _jWaveControl;
        [SerializeField] private Slider _uWaveControl;

        [SerializeField] private Slider _hrvControl;

        private void Start()
        {
            if (!_targetCamera) _targetCamera = Camera.main;

            _killButton.onClick.AddListener(() => _heart.TriggerCardiacArrest());
            _reviveButton.onClick.AddListener(() => _heart.RestartHeart(bpm: Mathf.Lerp(40f, 180f, _heartRateControl.value)));
            _heartRateControl.onValueChanged.AddListener((val) =>
            {
                _heart.SetTargetHeartRate(Mathf.Lerp(40f, 180f, val));
            });

            _heartRateControl.value = Mathf.InverseLerp(40f, 180f, _heart.TargetHeartRate);

            _audioControl.onValueChanged.AddListener((val) =>
            {
                _heart.EnableAudio = val;
            });

            _patientMovementControl.onValueChanged.AddListener(val =>
            {
                _heart.PatientMovement = val;
            });

            _patientMovementControl.value = _heart.PatientMovement;

            _tStressControl.onValueChanged.AddListener((val) =>
            {
                _heart.TWaveStress = Mathf.Lerp(-1f, 3f, val);
            });

            _tStressControl.value = Mathf.InverseLerp(-1, 3, 1f);

            _jWaveControl.onValueChanged.AddListener((val) =>
            {
                _heart.JWaveStress = val;
            });

            _jWaveControl.value = _heart.JWaveStress;

            _uWaveControl.onValueChanged.AddListener(val =>
            {
                _heart.UWaveStress = val;
            });

            _uWaveControl.value = _heart.UWaveStress;

            _hrvControl.onValueChanged.AddListener((val) =>
            {
                _heart.HRVGain = val;
            });

            _hrvControl.value = _heart.HRVGain;

            FitToCamera();
        }

        private void Update()
        {
            UpdateHeartRate();
            UpdateBloodPressure();
            UpdatePPT();
        }

        private void UpdateHeartRate()
        {
            float hr = _heart.HeartRate;
            _heartRate.text = $"Heart Rate: {Mathf.RoundToInt(hr)}";
        }

        private void UpdateBloodPressure()
        {
            float sbp = _heart.SystolicBloodPressure;
            float dbp = _heart.DiastolicBloodPressure;

            _bloodPressure.text = $"Blood Pressure: {Mathf.RoundToInt(sbp)}/{Mathf.RoundToInt(dbp)}";
        }

        private void UpdatePPT()
        {
            float ppt = _heart.PulseTransitTime;

            _ptt.text = $"Pulse Transit Time: {ppt.ToString("F2")}";
        }

        private void FitToCamera()
        {
            if (!_ecgPannel || !_targetCamera) return;

            _ecgPannel.transform.localScale = Vector3.one;

            float distance = Vector3.Distance(_ecgPannel.transform.position, _targetCamera.transform.position);

            float frustumHeight = 2f * distance * Mathf.Tan(_targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * _targetCamera.aspect;

            Vector3 meshSize = _ecgPannel.GetComponent<MeshFilter>().sharedMesh.bounds.size;

            float scaleX = frustumWidth / meshSize.x;
            float scaleY = frustumHeight / meshSize.z;

            _ecgPannel.transform.localScale = new Vector3(scaleX, 1f, scaleY);
            _ecgPannel.transform.position = _targetCamera.transform.position + _targetCamera.transform.forward * distance;
        }
    }
}
