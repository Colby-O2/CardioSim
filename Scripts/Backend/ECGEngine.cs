using UnityEngine;

using ColbyO.CardioSim.Settings;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGEngine.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Engine
{
    /// <summary>
    /// A synthetic ECG singal generator based on the McSharry Dynamical Model [1] and the Wave-based Dynamical Model [2].
    ///
    /// This engine simulates cardiac electrical activity as a 3D trajectory where a non-linear limit cycle in the (x, y)
    /// plane generates a periodic phase (theta), driving a system of Gaussian "attractors" along the Z-axis. By adopting a wave-based paradigm,
    /// the model supports complex, biphasic morphologies for the P, QRS (C), T, J, and U wave components. The system utilizes a 4th Order Runge-Kutta
    /// solver to integrate the underlying ordinary differential equations. Additionally, this model intergates a Respiratory Sinus Arrhythmia
    /// model to simualte baseline wander.
    ///
    /// References:
    ///  [1] McSharry, P. E., Clifford, G. D., Tarassenko, L., & Smith, L. A. (2003). 
    ///      A dynamical model for generating synthetic electrocardiogram signals. 
    ///      IEEE Transactions on Biomedical Engineering, 50(3), 289–294. 
    ///      https://doi.org/10.1109/tbme.2003.808805 
    ///  [2] Sayadi, O., Shamsollahi, M. B., & Clifford, G. D. (2010). 
    ///      Synthetic ECG generation and Bayesian filtering using a gaussian wave-based dynamical model.
    ///      Physiological Measurement, 31(10), 1309–1329. 
    ///      https://doi.org/10.1088/0967-3334/31/10/002      
    /// </summary>
    internal sealed class ECGEngine
    {
        /// <summary>
        /// Advances the ECG simulation by one time step using the 4th Order Runge-Kutta numerical integrator.
        /// </summary>
        /// <param name="s">The current state of the ECG system.</param>
        /// <param name="dt">The simulation timestep.</param>
        /// <param name="t">Total elapsed simulation time.</param>
        /// <param name="omega">The angular velocity of the oscillator (2 * PI * HR / 60).</param>
        /// <param name="respirationRate">The respiration rate, used to calculate S0 offset.</param>
        /// <param name="respirationGain">A scaling factor to control the magnitude of the S0 offset.</param> 
        /// <param name="wave">The profile containing morphology parameters.</param>
        /// <param name="sharpness">An artist parameter to scale the QRS complex width.</param>
        /// <returns>The next ECGState.</returns>
        public ECGState Step(ECGState s, float dt, float t, float omega, float respirationRate, float respirationGain, ECGProfile wave, float sharpness)
        {
            // Performs RK4 to get the next ECGState
            ECGState k1 = GetDerivatives(s, t, omega, respirationRate, respirationGain, wave, sharpness);
            ECGState k2 = GetDerivatives(s + k1 * (dt * 0.5f), t + dt * 0.5f, omega, respirationRate, respirationGain, wave, sharpness);
            ECGState k3 = GetDerivatives(s + k2 * (dt * 0.5f), t + dt * 0.5f, omega, respirationRate, respirationGain, wave, sharpness);
            ECGState k4 = GetDerivatives(s + k3 * dt, t + dt, omega, respirationRate, respirationGain, wave, sharpness);

            ECGState next = s + (k1 + k2 * 2f + k3 * 2f + k4) * (dt / 6f);

            // Resets the ECG's oscillator if the next value happens to be NaN.
            if (float.IsNaN(next.x)) return new ECGState { x = 1f, y = 0f }; 

            return next;
        }

        /// <summary>
        /// Calculates the derivatives for the 3D state-space equations.
        /// </summary>
        /// <param name="s">The current state of the ECG system.</param>
        /// <param name="t">Total elapsed simulation time.</param>
        /// <param name="omega">The angular velocity of the heart rate oscillator (2 * PI * HR / 60).</param>
        /// <param name="respirationRate">The respiration rate, used to calculate S0 offset.</param>
        /// <param name="respirationGain">A scaling factor to control the magnitude of the S0 offset.</param> 
        /// <param name="wave">The profile containing morphology parameters.</param>
        /// <param name="sharpness">An artist parameter to scale the QRS complex width.</param>
        /// <returns>The derivative of s with respect to time.</returns>
        private ECGState GetDerivatives(ECGState s, float t, float omega, float respirationRate, float respirationGain, ECGProfile wave, float sharpness)
        {
            ECGState d = new ECGState();

            float r = Mathf.Sqrt(s.x * s.x + s.y * s.y);
            float alpha = 1f - r;
            d.x = alpha * s.x - omega * s.y;
            d.y = alpha * s.y + omega * s.x;

            float theta = Mathf.Atan2(s.y, s.x);
            if (theta < 0) theta += 2f * Mathf.PI;

            // S0 == P0 = T0 = C0 = J0 = U0
            float S0 = GetRespirationSignal(respirationRate, respirationGain, t);

            d.P = CalcSDot(s.P, theta, S0, omega, wave.PAmplitudes, wave.PWidths, wave.PPhases, 1.0f);
            d.C = CalcSDot(s.C, theta, S0, omega, wave.CAmplitudes, wave.CWidths, wave.CPhases, sharpness);
            d.T = CalcSDot(s.T, theta, S0, omega, wave.TAmplitudes, wave.TWidths, wave.TPhases, 1.0f);
            d.J = CalcSDot(s.J, theta, S0, omega, wave.JAmplitudes, wave.JWidths, wave.JPhases, sharpness);
            d.U = CalcSDot(s.U, theta, S0, omega, wave.UAmplitudes, wave.UWidths, wave.UPhases, 1.0f);

            return d;
        }

        /// <summary>
        /// Calculates the baseline singal due to respiration using:
        ///     S0 = A * Sin(2 * PI * respirationRate * t)
        /// where A = 0.15 mV as suggested in [1].
        /// </summary>
        /// <param name="respirationRate">The respiration rate, used to calculate S0 offset.</param>
        /// <param name="respirationGain">A scaling factor to control the magnitude of the S0 offset.</param> 
        /// <param name="t">Total elapsed simulation time.</param>
        /// <returns>Baseline voltage due to repiration in mV.</returns>
        public float GetRespirationSignal(float respirationRate, float respirationGain, float t)
        {
            // S0 == P0 = C0 = T0 = J0 = U0 = A * Sin(2 * PI * respirationRate * t)
            // where A = 0.15 mV as suggested in [1].
            float A = 0.15f;
            float S0 = respirationGain * A * Mathf.Sin(2.0f * Mathf.PI * respirationRate * t);
            return S0;
        }

        /// <summary>
        /// The Gaussian summation formula representing the derivative of the ECG signal (dS/dt).
        /// Calculates how much the voltage should move toward specific wave attractors.
        /// </summary>
        /// <param name="S">The current voltage of this specific wave component.</param>
        /// <param name="theta">The current phase of the cardiac cycle.</param>
        /// <param name="S0">The baseline wander offset.</param>
        /// <param name="omega">The angular velocity of the heart rate oscillator (2 * PI * HR / 60).</param>
        /// <param name="a">Array of amplitudes for the Gaussians.</param>
        /// <param name="b">Array of widths for the Gaussians.</param>
        /// <param name="p">Array of phase locations [-1,1] where the waves occur.</param>
        /// <param name="sharpness">An artist parameter to scale the QRS complex width.</param>
        /// <returns>The derivative of wave S with respect to time.</returns>
        private float CalcSDot(float S, float theta, float S0, float omega, float[] a, float[] b, float[] p, float sharpness)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float delta = Mathf.Atan2(Mathf.Sin(theta - p[i] * 2f * Mathf.PI), Mathf.Cos(theta - p[i] * 2f * Mathf.PI));
                float adjustedWidth = b[i] * sharpness;
                float b2 = adjustedWidth * adjustedWidth;
                sum += -a[i] * (delta / b2) * Mathf.Exp(-delta * delta / (2f * b2)) * omega;
            }
            return sum - (S - S0);
        }

        /// <summary>
        /// Generates a random value following a Gaussian distribution.
        /// 
        /// References(s):
        ///     [1] https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform
        /// </summary>
        /// <param name="mean">The center of the bell curve.</param>
        /// <param name="stdDev">The standard deviation (width) of the curve.</param>
        /// <returns>A randomized float from the normal distribution.</returns>
        public float NextGaussian(float mean = 0f, float stdDev = 1f)
        {
            float u1 = UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;

            // Box-Muller transform [1].
            float randNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

            return mean + stdDev * randNormal;
        }

        /// <summary>
        /// Combines the individual wave states into a single final voltage value.
        /// </summary>
        /// <param name="s">A ECGState.</param>
        /// <param name="wave">The profile containing morphology parameters.</param>
        /// <param name="gain">The overall gain of the measurement.</param>
        /// <param name="heartRate">The current target HR.</param>
        /// <param name="patientMovement">The strength of shifting of the ECG singal due to motion.</param>
        /// <param name="tStress">The stree factor for the T-Wave.</param>
        /// <param name="jStress">The gain for the J-Wave.</param>
        /// <param name="uStress">The gain for the U-Wave. Scales with HR internally.</param>
        /// <returns>The current voltage reading in mV.</returns>
        public float CalculateVoltage(ECGState s, ECGProfile wave, float gain, float heartRate, float patientMovement, float tStress, float jStress, float uStress)
        {
            // U-Wave is not visiable after ~90 BPM, this factor just scale the height of the U wave with respect to
            // the current target heart rate.
            float uScale = Mathf.Clamp01(1f - (heartRate - 60f) / (90f - 60f));

            float muscleNoise = gain * NextGaussian(0.0f, patientMovement * 0.01f);

            return s.GetVoltage(gain, wave.PGain, wave.CGain, wave.TGain * tStress, wave.JGain * jStress, wave.UGain * uStress * uScale) + muscleNoise;
        }
    }
}