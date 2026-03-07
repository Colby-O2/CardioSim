using System.Collections.Generic;

using UnityEngine;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGTextureCreator.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Rendering
{
    /// <summary>
    /// A material based ECG waveform renderer that converts the signal data to a 1D texture.
    /// The voltage data is then passed to a shader for visualization.
    /// 
    /// </summary>
    public class ECGTextureCreator : IECGRenderer
    {
        /// <summary>The shader material used to draw the ECG.</summary>
        public Material Material { get; private set; }

        /// <summary>The maximum horizontal resolution of the data texture.</summary>
        public int Resolution { get; private set; }

        private float _pointsPerSecond;
        private float _secondsToDisplay;

        private readonly Texture2D _dataTexture;
        private readonly float[] _samples;
        private int _head;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="resolution">The maximum texture size allowed.</param>
        /// <param name="material">The ECG material.</param>
        public ECGTextureCreator(int resolution, Material material)
        {
            Material = material;
            Resolution = resolution;

            _samples = new float[Resolution];

            _dataTexture = new Texture2D(Resolution, 1, TextureFormat.RFloat, false);
            _dataTexture.filterMode = FilterMode.Bilinear;
            _dataTexture.wrapMode = TextureWrapMode.Repeat;

            Material.SetTexture("_DataTex", _dataTexture);
            Material.SetFloat("_Resolution", Resolution);
        }

        /// <summary>
        /// Configures visual styling and shader parameters for the ECG trace.
        /// </summary>
        /// <param name="secondsToDisplay">How many seconds of the ECG are visible on screen at once.</param>
        /// <param name="yScale">Vertical amplitude multiplier for the signal.</param>
        /// <param name="lineWidth">The thickness of the rendered line.</param>
        /// <param name="leadLength">Controls the visual "tail" length of the trace leading edge.</param>
        /// <param name="presistence">How long the signal remains visible before fading.</param>
        /// <param name="traceColor">The primary color of the ECG line.</param>
        /// <param name="flashColor">The color of the highlight at the leading edge of the scan.</param>
        public void SetSettings(
            float secondsToDisplay,
            float yScale,
            float lineWidth,
            float leadLength,
            float presistence,
            Color traceColor,
            Color flashColor,
            Color backgroundColor,
            bool enableGrid,
            Color gridColor,
            float gridSize,
            float gridLineThickness,
            bool enableGridFadeOut
        )
        {
            _secondsToDisplay = secondsToDisplay;

            Material.SetFloat("_YScale", yScale);
            Material.SetFloat("_LineWidth", lineWidth);
            Material.SetFloat("_LeadLength", Mathf.InverseLerp(1f, 20f, leadLength) * 0.1f);
            Material.SetFloat("_Presistence", (1.0f - Mathf.InverseLerp(0.8f, 1f,  presistence)) * 20f);
            Material.SetColor("_TraceColor", traceColor);
            Material.SetColor("_FlashColor", flashColor);

            Material.SetColor("_BackgroundColor", backgroundColor);
            Material.SetFloat("_ShowGrid", enableGrid ? 1.0f : 0.0f);
            Material.SetColor("_GridColor", gridColor);
            Material.SetFloat("_GridSize", gridSize);
            Material.SetFloat("_GridLineThickness", gridLineThickness);
            Material.SetFloat("_EnableGridFadeOut", enableGridFadeOut ? 1.0f : 0.0f);
        }

        public void SetEngineSettings(float engineDt, float sampleRate)
        {
            float simulatorHz = 1f / engineDt;
            _pointsPerSecond = simulatorHz / sampleRate;
        }

        public void Render(Queue<float> sampleBuffer)
        {
            int activeSampleCount = Mathf.RoundToInt(_pointsPerSecond * _secondsToDisplay);
            activeSampleCount = Mathf.Clamp(activeSampleCount, 1, Resolution);

            int clearLookahead = 3;

            while (sampleBuffer.Count > 0)
            {
                _samples[_head] = sampleBuffer.Dequeue();

                for (int i = 1; i <= clearLookahead; i++)
                {
                    int nextIndex = (_head + i) % activeSampleCount;
                    _samples[nextIndex] = 0;
                }

                _head = (_head + 1) % activeSampleCount;
            }

            _dataTexture.SetPixelData(_samples, 0);
            _dataTexture.Apply();

            float uvScale = (float)activeSampleCount / Resolution;

            Material.SetFloat("_UVScale", uvScale);
            Material.SetFloat("_HeadPosition", (float)_head / activeSampleCount);
        }
    }
}