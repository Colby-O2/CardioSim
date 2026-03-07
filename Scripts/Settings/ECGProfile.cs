using UnityEngine;

using System.Linq;
using System.Reflection;

using ColbyO.CardioSim.SO;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGProfile.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Settings
{
    [CreateAssetMenu(fileName = "DefaultECGProfile", menuName = "ECG/Profile")]
    public sealed class ECGProfile : BaseSO
    {
        [Header("Profile Settings")]
        public string profileName;

        [Header("P-Wave (Atrial)")]
        [Min(0f)] public float PGain = 0.03f;
        [Space(5)]
        public float P_Minus_Amp = 0.7f;
        [Min(0.01f)] public float P_Minus_Width = 0.2f;
        [Range(-1f, 1f)] public float P_Minus_Phase = -0.1875f;
        [Space(5)]
        public float P_Plus_Amp = 0.8f;
        [Min(0.01f)] public float P_Plus_Width = 0.1f;
        [Range(-1f, 1f)] public float P_Plus_Phase = -0.1666667f;

        [Header("QRS Complex (Ventricular Depolarization)")]
        [Min(0f)] public float CGain = 0.03f;
        [Space(5)]
        public float Q_Amp = -1.0f;
        [Min(0.01f)] public float Q_Width = 0.1f;
        [Range(-1f, 1f)] public float Q_Phase = -0.03846154f;
        [Space(5)]
        public float R_Amp = 20.0f;
        [Min(0.01f)] public float R_Width = 0.1f;
        [Range(-1f, 1f)] public float R_Phase = 0.0f;
        [Space(5)]
        public float S_Amp = -9.5f;
        [Min(0.01f)] public float S_Width = 0.1f;
        [Range(-1f, 1f)] public float S_Phase = 0.03333334f;

        [Header("J-Wave (Osborn Notch)")]
        [Min(0f)] public float JGain = 0f;
        [Space(5)]
        public float J_Amp = 1.5f;
        [Min(0.01f)] public float J_Width = 0.08f;
        [Range(-1f, 1f)] public float J_Phase = 0.055f;

        [Header("T-Wave (Repolarization)")]
        [Min(0f)] public float TGain = 0.15f;
        [Space(5)]
        public float T_Minus_Amp = 0.27f;
        [Min(0.01f)] public float T_Minus_Width = 0.4f;
        [Range(-1f, 1f)] public float T_Minus_Phase = 0.2f;
        [Space(5)]
        public float T_Plus_Amp = 0.15f;
        [Min(0.01f)] public float T_Plus_Width = 0.55f;
        [Range(-1f, 1f)] public float T_Plus_Phase = 0.2857143f;

        [Header("U-Wave (Late Repolarization)")]
        [Min(0f)] public float UGain = 0.5f;
        [Space(5)]
        public float U_Amp = 0.05f;
        [Min(0.01f)] public float U_Width = 0.3f;
        [Range(-1f, 1f)] public float U_Phase = 0.55f;

        [HideInInspector] public float[] PAmplitudes, PWidths, PPhases;
        [HideInInspector] public float[] CAmplitudes, CWidths, CPhases;
        [HideInInspector] public float[] TAmplitudes, TWidths, TPhases;
        [HideInInspector] public float[] JAmplitudes, JWidths, JPhases;
        [HideInInspector] public float[] UAmplitudes, UWidths, UPhases;

        private static FieldInfo[] _parameterFields;

        private void OnEnable() => SyncArrays();
        private void OnValidate() => SyncArrays();

        public void SyncArrays()
        {
            PAmplitudes = new[] { P_Minus_Amp, P_Plus_Amp };
            PWidths = new[] { P_Minus_Width, P_Plus_Width };
            PPhases = new[] { P_Minus_Phase, P_Plus_Phase };

            CAmplitudes = new[] { Q_Amp, R_Amp, S_Amp };
            CWidths = new[] { Q_Width, R_Width, S_Width };
            CPhases = new[] { Q_Phase, R_Phase, S_Phase };

            TAmplitudes = new[] { T_Minus_Amp, T_Plus_Amp };
            TWidths = new[] { T_Minus_Width, T_Plus_Width };
            TPhases = new[] { T_Minus_Phase, T_Plus_Phase };

            JAmplitudes = new[] { J_Amp };
            JWidths = new[] { J_Width };
            JPhases = new[] { J_Phase };

            UAmplitudes = new[] { U_Amp };
            UWidths = new[] { U_Width };
            UPhases = new[] { U_Phase };
        }

        public static void Lerp(ECGProfile a, ECGProfile b, ref ECGProfile result, float t)
        {
            if (a == null || b == null || result == null) return;

            _parameterFields ??= typeof(ECGProfile)
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => f.FieldType == typeof(float))
                    .ToArray();

            foreach (FieldInfo field in _parameterFields)
            {
                float valA = (float)field.GetValue(a);
                float valB = (float)field.GetValue(b);
                float interpolated = Mathf.Lerp(valA, valB, t);

                field.SetValue(result, interpolated);
            }

            result.SyncArrays();
        }
    }
}