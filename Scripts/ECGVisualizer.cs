using UnityEngine;

using System.Collections.Generic;

using ColbyO.CardioSim.Rendering;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGVisualizer.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim
{
    /// <summary>
    /// Handles Visualization of an ECG from the Heart Class. 
    /// </summary>
    public class ECGVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Heart _heart;

        [SerializeField, HideInInspector] private ECGDisplayMode _displayMode = ECGDisplayMode.Material;
        [SerializeField, HideInInspector] private int _maxSampleBuffer = 2048;
        [SerializeField, HideInInspector] private float _secondsToDisplay = 2.0f;
        [SerializeField, HideInInspector] private float _yScale = 1f;
        [SerializeField, HideInInspector] private float _totalWidth = 2.0f;
        [SerializeField, HideInInspector] private int _leadLength = 5;
        [SerializeField, HideInInspector] private float _persistence = 0.95f;
        [SerializeField, HideInInspector] private Color _traceColor = Color.green;
        [SerializeField, HideInInspector] private Color _flashColor = Color.white * 10f;
        [SerializeField, HideInInspector] private float _lineWidth = 0.01f;

        [SerializeField, HideInInspector] private Color _backgroundColor;
        [SerializeField, HideInInspector] private bool _enableGrid = false;
        [SerializeField, HideInInspector] private Color _gridColor;
        [SerializeField, HideInInspector] private float _gridSize = 25f;
        [SerializeField, HideInInspector] private float _gridLineThickness = 0.1f;
        [SerializeField, HideInInspector] private bool _enableGridFadeOut = true;

        [SerializeField, HideInInspector] private MeshRenderer _meshRenderer;
        [SerializeField, HideInInspector] private Material _ecgMaterial;
        [SerializeField, HideInInspector] private MonoBehaviour _rendererComponent;

        private ECGMeshCreator _meshCreator = null;
        private ECGTextureCreator _textureCreator = null;
        private IECGRenderer CustomRenderer => _rendererComponent as IECGRenderer;

        public Heart Heart
        {
            get => _heart;
            set => _heart = value;
        }

        public float SecondsToDisplay
        {
            get => _secondsToDisplay;
            set => _secondsToDisplay = value;
        }

        public float YScale
        {
            get => _yScale;
            set => _yScale = value;
        }

        public float TotalWidth
        {
            get => _totalWidth;
            set => _totalWidth = value;
        }

        public int LeadLength
        {
            get => _leadLength;
            set => _leadLength = value;
        }

        public float Persistence
        {
            get => _persistence;
            set => _persistence = value;
        }

        public Color TraceColor
        {
            get => _traceColor;
            set => _traceColor = value;
        }

        public Color FlashColor
        {
            get => _flashColor;
            set => _flashColor = value;
        }

        public float LineWidth
        {
            get => _lineWidth;
            set => _lineWidth = value;
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        public bool EnableGrid
        {
            get => _enableGrid;
            set => _enableGrid = value;
        }

        public Color GridColor
        {
            get => _gridColor;
            set => _gridColor = value;
        }

        public float GridSize
        {
            get => _gridSize;
            set => _gridSize = value;
        }

        public float GridLineThickness
        {
            get => _gridLineThickness;
            set => _gridLineThickness = value;
        }

        public bool EnableGridFadeOut
        {
            get => _enableGridFadeOut;
            set => _enableGridFadeOut = value;
        }

        private void FixedUpdate()
        {
            if (!_heart) return;
            Render(_heart.SampleBuffer);
        }

        public float GetSecondsToDisplay()
        {
            return Mathf.Max(_secondsToDisplay, 1);
        }

        private void Render(Queue<float> buffer)
        {
            if (_displayMode == ECGDisplayMode.Material && _ecgMaterial != null)
            {
                _textureCreator ??= new ECGTextureCreator(_maxSampleBuffer, _ecgMaterial);
                _textureCreator.SetSettings(
                    _secondsToDisplay,
                    _yScale,
                    _lineWidth,
                    _leadLength,
                    _persistence,
                    _traceColor,
                    _flashColor,
                    _backgroundColor,
                    _enableGrid,
                    _gridColor,
                    _gridSize,
                    _gridLineThickness,
                    _enableGridFadeOut
                );
                _textureCreator.SetEngineSettings(_heart.EngineDt, _heart.SampleRate);
                _textureCreator.Render(buffer);
            }
            else if (_displayMode == ECGDisplayMode.Mesh && _meshRenderer != null)
            {
                _meshCreator ??= new ECGMeshCreator(_meshRenderer, _maxSampleBuffer, _lineWidth, _traceColor);
                _meshCreator.SetSettings(
                    _yScale,
                    _lineWidth,
                    _totalWidth,
                    _secondsToDisplay,
                    _leadLength,
                    _persistence,
                    _traceColor,
                    _flashColor
                );
                _meshCreator.SetEngineSettings(_heart.EngineDt, _heart.SampleRate);
                _meshCreator.Render(buffer);
            }
            else if (_displayMode == ECGDisplayMode.Custom && CustomRenderer != null)
            {
                CustomRenderer.SetEngineSettings(_heart.EngineDt, _heart.SampleRate);
                CustomRenderer.Render(buffer);
            }
            else
            {
                buffer.Clear();
            }
        }
    }
}