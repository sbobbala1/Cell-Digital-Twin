# Cell Digital Twin — Unity AR/VR (Option A: C# only)

## Package dependencies
Install via Window → Package Manager:

| Package | Version tested |
|---|---|
| Universal Render Pipeline | 14+ |
| XR Interaction Toolkit | 2.5+ |
| AR Foundation | 5.0+ |
| ARKit XR Plugin (iOS) or ARCore XR Plugin (Android) | 5.0+ |
| TextMeshPro | (included with Unity) |

---

## File layout

```
CellTwin/
  Scripts/
    Core/
      SimulationManager.cs          ← metabolic tick, all state
    Organelles/
      OrganelleBase.cs              ← abstract base, attach to each organelle GO
      MitochondrionController.cs    ← OXPHOS, ROS, rotation
      OtherOrganelles.cs            ← Nucleus, ER, CellMembrane controllers
      MetaboliteParticleController.cs ← ATP / ROS / Glucose particle systems
    XR/
      CellTwinXRController.cs       ← grab, hover highlight, pinch-to-scale
      XRSimSlider.cs                ← physical world-space slider
    UI/
      CellDataPanel.cs              ← world-space metrics + alert log
  Shaders/
    CellMembrane.shader             ← Fresnel rim + dissolve (URP)
```

---

## Scene wiring (step by step)

### 1. SimulationManager
- Create an empty GameObject named **SimulationManager**.
- Attach `SimulationManager.cs`.
- Leave default values or tune Initial Glucose / O2 / Temperature.

### 2. Cell root
- Create an empty GameObject named **Cell**.
- Attach `CellTwinXRController.cs` and `XRGrabInteractable`.
- Add a sphere collider (radius ~0.15) on Cell so it is grabbable.

### 3. Organelles (children of Cell)
Create child GameObjects for each organelle:

**Mitochondria** (create 2–3 copies):
- Mesh: capsule (scale 0.06 x 0.03 x 0.03), rotate ~30° on Z.
- Attach `MitochondrionController.cs`.
- Material: URP Lit, enable emission.

**Nucleus**:
- Mesh: sphere (scale 0.09).
- Attach `NucleusController.cs`.
- Add 8 small sphere children as nuclear pores, assign to `nuclearPores[]`.
- Add a smaller sphere child (scale 0.03) as nucleolus, assign to `nucleolus`.

**ER**:
- Mesh: flattened/stretched capsule or spline ribbon.
- Attach `EndoplasmicReticulumController.cs`.

**Cell Membrane**:
- Mesh: sphere (scale 0.30), apply `CellMembrane.shader` material.
- Attach `CellMembraneController.cs`.
- Set Cull Back → Front in the shader if you want interior visibility,
  or duplicate with Cull Front for the outer shell.

### 4. Particles
- Create an empty GameObject named **Particles** (child of Cell).
- Add three `ParticleSystem` children: ATP (green), ROS (orange), Glucose (blue).
- Attach `MetaboliteParticleController.cs` to the parent, assign the three PS.
- Recommended: Simulation Space = World, Shape = Sphere radius 0.14.

### 5. World-space UI
- Create a **Canvas** (Render Mode: World Space) near the cell.
- Add TMP text children for ATP, Vmem, ROS, Viability.
- Attach `CellDataPanel.cs` and assign the text references.
- For flux bars: add three RectTransforms set to anchor-based width, assign to glycBar / oxphBar / foldBar.

### 6. XR sliders (optional)
- For each parameter (Glucose, O2, Temperature):
  - Create two empty GameObjects as trackStart / trackEnd.
  - Create a sphere handle between them with `XRGrabInteractable`.
  - Attach `XRSimSlider.cs`, assign track transforms and parameter enum.

### 7. AR Foundation (for phone AR)
- Add **AR Session** and **AR Session Origin** (or **XR Origin**) to the scene.
- Parent the Cell under **AR Session Origin → Camera Offset**.
- Use `ARPlaneManager` to detect surfaces; on plane tap, call:
  ```csharp
  cell.transform.position = hitPose.position;
  ```

### 8. Drug / Heat shock buttons
- Any UI Button or XR button can call:
  ```csharp
  SimulationManager.Instance.SetDrug(true);
  SimulationManager.Instance.SetHeatShock(true);
  ```

---

## Runtime controls

| Method | Effect |
|---|---|
| `SetGlucose(float 0–100)` | Changes glucose availability |
| `SetO2(float 0–100)` | Changes oxygen; triggers HIF-1α log at < 20 |
| `SetTemperature(float 30–45)` | Shifts temp factor; > 40 causes protein misfolding |
| `SetDrug(bool)` | Toggles mitochondrial inhibitor (–60 % OXPHOS, –60 % glycolysis) |
| `SetHeatShock(bool)` | Heat shock stress (–45 % OXPHOS, +3 μM ROS, –25 % folding) |

---

## Recommended URP settings
- Color Space: Linear
- HDR: enabled (for emission bloom)
- Post Processing: Bloom (Intensity 0.3, Threshold 0.8) brings organelle emissions to life
