//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGState.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Engine
{
    /// <summary>
    /// Represents the state vector for the synthetic ECG dynamical model based on McSharry et al. [1].
    /// This structure stores the angular trajectory (x, y) and the individual morphological 
    /// components (P, C, T, J, U) that constitute the characteristic heartbeat waveform.
    /// 
    /// State Components:
    ///     x, y : Coordinates defining the circular limit cycle in the 2D plane.
    ///     P    : Atrial depolarization component (P-wave).
    ///     C    : Ventricular depolarization component (QRS complex).
    ///     T    : Ventricular repolarization component (T-wave).
    ///     J    : Optional ST-segment / J-point deviation component.
    ///     U    : Optional late repolarization component (U-wave).
    ///
    /// References:
    ///  [1] McSharry, P. E., Clifford, G. D., Tarassenko, L., & Smith, L. A. (2003). 
    ///      A dynamical model for generating synthetic electrocardiogram signals. 
    ///      IEEE Transactions on Biomedical Engineering, 50(3), 289–294. 
    ///      https://doi.org/10.1109/tbme.2003.808805      
    /// </summary>
    internal struct ECGState
    {
        public float x, y, P, C, T, J, U;

        public ECGState(float x = 1.0f, float y = 0.0f, float P = 0.0f, float C = 0.0f, float T = 0.0f, float J = 0.0f, float U = 0.0f)
        {
            this.x = x; this.y = y;
            this.P = P; this.C = C; this.T = T;
            this.J = J; this.U = U;
        }

        public readonly float GetVoltage(float gain = 1.0f, float pGain = 1.0f, float cGain = 1.0f, float tGain = 1.0f, float jGain = 0.0f, float uGain = 1.0f)
        {
            return gain * (P * pGain + C * cGain + J * jGain + T * tGain + U * uGain);
        }

        public static ECGState operator +(ECGState a, ECGState b) => new ECGState { x = a.x + b.x, y = a.y + b.y, P = a.P + b.P, C = a.C + b.C, T = a.T + b.T, J = a.J + b.J, U = a.U + b.U };
        public static ECGState operator *(ECGState a, float b) => new ECGState { x = a.x * b, y = a.y * b, P = a.P * b, C = a.C * b, T = a.T * b, J = a.J * b, U = a.U * b };
    }
}
