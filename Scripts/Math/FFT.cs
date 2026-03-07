using UnityEngine;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    FFT.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Math
{
    /// <summary>
    /// A Fast Fourier Transform (FFT) implementation using Cooley-Tukey Algorithum.
    /// </summary>
    internal static class FFT
    {
        /// <summary>
        /// Performs an Inverse FFT. 
        /// Note: The input buffer must be a power of 2.
        /// </summary>
        /// <param name="buffer">An array of Complex numbers to be transformed.</param>
        public static void IFFT(Complex[] buffer)
        {
            int n = buffer.Length;
            if (n <= 1) return;
            Complex[] even = new Complex[n / 2];
            Complex[] odd = new Complex[n / 2];
            for (int i = 0; i < n / 2; i++) { even[i] = buffer[2 * i]; odd[i] = buffer[2 * i + 1]; }
            IFFT(even); IFFT(odd);
            for (int k = 0; k < n / 2; k++)
            {
                float angle = 2f * Mathf.PI * k / n;
                Complex w = new Complex(Mathf.Cos(angle), Mathf.Sin(angle));
                Complex t = w * odd[k];
                buffer[k] = even[k] + t;
                buffer[k + n / 2] = even[k] - t;
            }
        }
    }
}
