# CardioSim
## Real-Time ECG Simulation for Unity 6

CardioSim is a real-time electrocardiogram (ECG) simulator for Unity based on the  
**[McSharry ECG model](https://doi.org/10.1109/tbme.2003.808805)**.

It generates physiologically realistic ECG waveforms that can be modified in real-time.

---

## Links

- [Demo](https://colby-o.itch.io/cardiosim)
- [Discord](https://discord.gg/8XhAPdCuUU)
- **Email:** cokeefe919@gmail.com

---

## Installation

1. Download the appropriate **`.unitypackage`** file from [HERE](https://github.com/Colby-O2/CardioSim/tree/downloads).
2. Drag the package into the **Assets** folder of your Unity project.
3. Add the **Heart** and **ECGVisualizer** components to a GameObject.
4. Select a **rendering mode**.
5. Configure the renderer or material depending on the visualization backend you want to use.

---

## ECG Simulation

- **Real-time ECG waveform simulation** based on the McSharry ECG model  
- **Controllable Heart-Rate Variability engine**  
- **Adjustable heart rate and waveform parameters**  
- **Switch between different pathological conditions**  

---

## Rendering Modes

### Material Mode (GPU)

GPU waveform rendering using **signed distance fields**.

- Apply the material to any surface
- Automatically corrects waveform aspect ratio
- Customizable grid and background colors

### Mesh Mode (CPU)

- Generates a **procedural mesh at runtime** to display the waveform

### Custom Mode

- Implement your own ECG rendering backend by creating a renderer that implements the  
  `IECGRenderer` interface.

---

## Compatibility

- **Unity 6+ (6000.x and newer)**
- **URP supported**
- **Desktop and WebGL fully supported**

---

## Example Uses

- **[Code Black](https://colby-o.itch.io/code-black)**

---

## License

CardioSim is released under **The Unlicense**. You may freely use, modify, and redistribute this software, including in commercial projects. Credit is not required, but **greatly appreciated**.

---
