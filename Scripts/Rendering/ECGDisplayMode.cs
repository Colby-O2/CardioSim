//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    ECGDisplayMode.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Rendering
{
    /// <summary>
    /// Rendering methods for ECG visualization.
    /// 
    /// </summary>
    /// <author>Colby-O</author>
    public enum ECGDisplayMode
    {
        /// <summary>
        /// No ECG visualization.
        /// </summary>
        None,
        /// <summary>
        /// Renders the ECG using geometry generated at runtime on the CPU.
        /// </summary>
        Mesh,
        /// <summary>
        /// Passes the ECG voltages to a Shader and renders on the GPU.
        /// </summary>
        Material,
        /// <summary>
        /// Uses a user-defined rendering implementation.
        /// </summary>
        Custom
    }
}