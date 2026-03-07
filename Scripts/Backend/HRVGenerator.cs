using UnityEngine;

using ColbyO.CardioSim.Math;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    HRVGenerator.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Engine
{
    /// <summary>
    /// Generates a Heart Rate Variability (HRV) time series based on the McSharry dynamical model [1].
    /// The purpose is to simulate the autonomic nervous system's influence on heart rhythm by creating a 
    /// power spectrum with Low Frequency (LF) and High Frequency (HF) Gaussian peaks, then use an 
    /// IFFT to produce a time domain buffer. 
    /// 
    /// Parameters:
    ///     _f1 (LF Band) = 0.10 Hz  : Associated with sympathetic activity.
    ///     _f2 (HF band) = 0.25 Hz : Associated with sympathetic and parasympathetic activity.
    ///     _c1           = 0.01    : Standard Deviation (width) of the LF peak
    ///     _c2           = 0.01    : Standard Deviation (width) of the HF peak
    ///     
    /// These parameters are set to the recommended values based on McSharry's paper [1].
    /// 
    /// References:
    ///  [1] McSharry, P. E., Clifford, G. D., Tarassenko, L., & Smith, L. A. (2003). 
    ///      A dynamical model for generating synthetic electrocardiogram signals. 
    ///      IEEE Transactions on Biomedical Engineering, 50(3), 289–294. 
    ///      https://doi.org/10.1109/tbme.2003.808805      
    /// </summary>
    internal sealed class HRVGenerator
    {
        private readonly float _f1 = 0.10f;
        private readonly float _f2 = 0.25f;
        private readonly float _c1 = 0.01f;
        private readonly float _c2 = 0.01f;

        /// <summary>
        /// Generates a time domain HRV buffer using spectral synthesis. 
        /// It constructs a frequency spectrum based on two Gaussian distributions (LF and HF), 
        /// applies random phases to simulate stochastic biological signals, 
        /// and transforms the result into a time series.
        /// </summary>
        /// <param name="N">The number of samples to generate (must be a power of 2).</param>
        /// <param name="sampleRate">The sampling frequency in Hz.</param>
        /// <param name="lf">The power scaling factor for the LF band.</param>
        /// <param name="hf">The power scaling factor for the HF band.</param>
        /// <returns>The RR-interval (time between two R waves) variations over time.</returns>
        public float[] GenerateHRVBuffer(int N, float sampleRate, float lf, float hf)
        {
            Complex[] spectrum = new Complex[N];
            for (int i = 0; i < N / 2; i++)
            {
                float f = i * sampleRate / N;
                float term1 = (lf / Mathf.Sqrt(2 * Mathf.PI * _c1 * _c1)) * Mathf.Exp(-Mathf.Pow(f - _f1, 2) / (2 * _c1 * _c1));
                float term2 = (hf / Mathf.Sqrt(2 * Mathf.PI * _c2 * _c2)) * Mathf.Exp(-Mathf.Pow(f - _f2, 2) / (2 * _c2 * _c2));
                float mag = Mathf.Sqrt(term1 + term2);
                float phase = Random.Range(0f, 2f * Mathf.PI);
                spectrum[i] = new Complex(mag * Mathf.Cos(phase), mag * Mathf.Sin(phase));
                if (i > 0) spectrum[N - i] = new Complex(spectrum[i].real, -spectrum[i].imag);
            }
            FFT.IFFT(spectrum);
            float[] timeSeries = new float[N];
            for (int i = 0; i < N; i++) timeSeries[i] = spectrum[i].real / N;
            return timeSeries;
        }
    }
}
