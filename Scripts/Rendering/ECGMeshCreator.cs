using UnityEngine;

using System.Collections.Generic;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGMeshCreator.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Rendering
{
    /// <summary>
    /// Renders a ECG waveform by dynamically generate a mesh using a quad strip approach
    /// where each segment is aligned to the signal slope.
    /// </summary>
    public class ECGMeshCreator: IECGRenderer
    {
        private readonly int _maxSampleBuffer = 2048;

        private float _yScale;
        private float _lineWidth;
        private float _totalWidth;
        private float _secondsToDisplay;
        private int _leadLength;
        private float _persistence;
        private Color _traceColor;
        private Color _flashColor;

        private float _fade;

        private float _xSpacing;
        private int _sampleCount;

        private readonly Mesh _mesh;
        private readonly Vector3[] _vertices;
        private readonly int[] _triangles;
        private readonly Color[] _colors;

        private int _head = 0;
        private float _lastSample = 0f;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="mr">The MeshRenderer component where the ECG will be drawn.</param>
        /// <param name="maxSampleBuffer">Maximum vertices allowed.</param>
        /// <param name="lineWidth">Thickness of the ECG line.</param>
        /// <param name="traceColor">Color of the signal line.</param>
        public ECGMeshCreator(MeshRenderer mr, int maxSampleBuffer, float lineWidth, Color traceColor)
        {
            _maxSampleBuffer = maxSampleBuffer;
            _lineWidth = lineWidth;
            _traceColor = traceColor;

            MeshFilter mf = mr.GetComponent<MeshFilter>();

            _sampleCount = _maxSampleBuffer;

            _mesh = new Mesh();
            _mesh.MarkDynamic();
            mf.mesh = _mesh;

            _vertices = new Vector3[_sampleCount * 4];
            _triangles = new int[_sampleCount * 6];
            _colors = new Color[_sampleCount * 4];

            BuildIndexBuffer();
            InitializeVertices();

            _mesh.vertices = _vertices;
            _mesh.triangles = _triangles;
            _mesh.colors = _colors;

            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// Configures visual styling for the ECG trace.
        /// </summary>
        /// 
        /// <param name="yScale">Vertical amplitude multiplier for the signal.</param>
        /// <param name="lineWidth">The thickness of the rendered line.</param>
        /// <param name="totalWidth">The total horizontal span of the renderer in the scene.</param>
        /// <param name="secondsToDisplay">How many seconds of the ECG are visible on screen at once.</param>
        /// <param name="leadLength">Controls the visual "tail" length of the trace leading edge.</param>
        /// <param name="persistence">How long the signal remains visible before fading.</param>
        /// <param name="traceColor">The primary color of the ECG line.</param>
        /// <param name="flashColor">The color of the highlight at the leading edge of the scan.</param>
        public void SetSettings(
            float yScale,
            float lineWidth,
            float totalWidth,
            float secondsToDisplay,
            int leadLength,
            float persistence,
            Color traceColor,
            Color flashColor
        )
        {
            if (_secondsToDisplay != secondsToDisplay)
            {
                _head = 0;
                _lastSample = 0f;
            }

            _yScale = yScale;
            _lineWidth = lineWidth;
            _totalWidth = totalWidth;
            _secondsToDisplay = secondsToDisplay;
            _leadLength = leadLength;
            _persistence = persistence;
            _traceColor = traceColor;
            _flashColor = flashColor;
        }

        public void SetEngineSettings(float engineDt, float sampleRate)
        {
            AdjustSampleSettings(engineDt, sampleRate, _totalWidth, _secondsToDisplay);
        }

        public void Render(
            Queue<float> sampleBuffer
        )
        {
            _fade = (sampleBuffer.Count > 0.1f) ? _persistence / sampleBuffer.Count : 0.0f;

            while (sampleBuffer.Count > 0)
            {
                float voltage = sampleBuffer.Dequeue();
                float current = voltage * _yScale;
                AddSample(current);
                _lastSample = current;
            }

            for (int i = _sampleCount; i < _maxSampleBuffer; i++)
            {
                SetQuad(i, 0, 0, 0, 0);
            }

            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
        }

        /// <summary>
        /// Synchronizes the mesh resolution and spacing with the current engine timing and display requirements.
        /// </summary>
        /// <param name="dt">timestep of the ECG engine.</param>
        /// <param name="sampleRate">Frequency of incoming data.</param>
        /// <param name="totalWidth">The physical width of the mesh.</param>
        /// <param name="secondsToDisplay">The time window of data to fit within the totalWidth.</param>
        private void AdjustSampleSettings(float dt, float sampleRate, float totalWidth, float secondsToDisplay)
        {
            float simulatorHz = 1f / dt;
            float pointsPerSecond = simulatorHz / sampleRate;

            int newSampleCount = Mathf.RoundToInt(pointsPerSecond * secondsToDisplay);

            _sampleCount = Mathf.Min(newSampleCount, _maxSampleBuffer);

            if (_sampleCount > 0)
            {
                _xSpacing = totalWidth / _sampleCount;
            }

            if (_head >= _sampleCount)
            {
                _head = 0;
            }
        }

        /// <summary>
        /// Pre-calculates the triangle indices for the quad strip. 
        /// Since the topology of the line doesn't change, 
        /// this is called only once at initialization.
        /// </summary>
        private void BuildIndexBuffer()
        {
            for (int i = 0; i < _sampleCount; i++)
            {
                int v = i * 4;
                int t = i * 6;

                _triangles[t + 0] = v + 0;
                _triangles[t + 1] = v + 1;
                _triangles[t + 2] = v + 2;

                _triangles[t + 3] = v + 2;
                _triangles[t + 4] = v + 1;
                _triangles[t + 5] = v + 3;
            }
        }

        /// <summary>
        /// Sets initial vertex positions and clears the mesh data to a baseline state.
        /// </summary>
        private void InitializeVertices()
        {
            for (int i = 0; i < _sampleCount; i++)
            {
                float x = i * _xSpacing;
                SetQuad(i, x, 0f, 0f, 0f);
            }
        }

        /// <summary>
        /// Internal logic to push a new sample into the circular mesh buffer.
        /// Updates the mesh at the 'head' and calculates fading/flash colors.
        /// </summary>
        /// <param name="current">The vertical amplitude of the current sample.</param>
        private void AddSample(float current)
        {
            int i = _head;

            float x0 = i * _xSpacing;
            float x1 = (i + 1) * _xSpacing;

            SetQuad(i, x0, _lastSample, x1, current);

            int fadeLength = Mathf.Max(1, _sampleCount / 10);
            for (int f = 1; f <= fadeLength; f++)
            {
                int idx = (i - f + _sampleCount) % _sampleCount;

                int vBase = idx * 4;
                for (int v = 0; v < 4; v++)
                    _colors[vBase + v] = new Color(_traceColor.r, _traceColor.g, _traceColor.b, 1f);
            }

            for (int v = 0; v < _colors.Length; v++)
            {
                _colors[v].a = Mathf.Lerp(_colors[v].a, 0f, 1.0f - _persistence);
            }

            for (int l = 0; l < _leadLength; l++)
            {
                int targetIdx = (_head - l + _sampleCount) % _sampleCount;

                int vBase = targetIdx * 4;
                for (int v = 0; v < 4; v++)
                {
                    _colors[vBase + v] = _flashColor;
                }
            }

            _head = (_head + 1) % _sampleCount;
        }

        /// <summary>
        /// Calculates the quad orientation (normal to the line direction) to maintain consistent thickness.
        /// </summary>
        private void SetQuad(int i, float x0, float y0, float x1, float y1)
        {
            Vector2 dir = new Vector2(y1 - y0, -(x1 - x0)).normalized * (_lineWidth * 0.5f);

            int v = i * 4;

            _vertices[v + 0] = new Vector3(x0 + dir.x, y0 + dir.y, 0);
            _vertices[v + 1] = new Vector3(x0 - dir.x, y0 - dir.y, 0);
            _vertices[v + 2] = new Vector3(x1 + dir.x, y1 + dir.y, 0);
            _vertices[v + 3] = new Vector3(x1 - dir.x, y1 - dir.y, 0);

            for (int k = 0; k < 4; k++)
                _colors[v + k] = _traceColor;
        }
    }
}