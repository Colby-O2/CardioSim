using System.Collections.Generic;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    IECGRenderer.cs
//-----------------------------------------------------------------------
namespace ColbyO.CardioSim.Rendering
{
    /// <summary>
    /// An interface for components responsible for rendering ECGs.
    /// 
    /// </summary>
    /// <author>Colby-O</author>
    public interface IECGRenderer
    {
        /// <summary>
        /// Configures the renderer to account for the sampling rate and engine time step.
        /// </summary>
        /// <param name="engineDt">The ECG engine's timestep.</param>
        /// <param name="sampleRate">The frequency the ECG data is collected.</param>
        public void SetEngineSettings(float engineDt, float sampleRate);

        /// <summary>
        /// Processes and draws the current batch of ECG data points.
        /// </summary>
        /// <param name="sampleBuffer">A queue containing the raw voltage samples to be visualized.</param>
        /// <remarks>
        /// Implementations should dequeue from the buffer. 
        /// Otherwise the buffer would need to be cleared before returning to
        /// prevent a memory leak.
        /// </remarks>
        public void Render(Queue<float> sampleBuffer);
    }
}