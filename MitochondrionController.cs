using UnityEngine;
using CellTwin.Core;

namespace CellTwin.Organelles
{
    // -------------------------------------------------------------------------
    //  MitochondrionController
    //
    //  Responds to OXPHOS flux and ROS level:
    //    • Scale pulses with OXPHOS activity (cristae expansion/contraction)
    //    • Emission color shifts green → amber → red with rising ROS
    //    • Rotation speed reflects membrane potential magnitude
    //
    //  Setup:  Attach to a capsule (or imported mesh) tagged "Mitochondrion".
    //          Assign a URP Lit material with _EmissionColor and _BaseColor.
    //          Enable emission in the material.
    // -------------------------------------------------------------------------
    public class MitochondrionController : OrganelleBase
    {
        [Header("Colors")]
        public Color healthyEmission  = new Color(0.11f, 0.62f, 0.46f);  // teal-green
        public Color stressEmission   = new Color(0.94f, 0.62f, 0.15f);  // amber
        public Color criticalEmission = new Color(0.89f, 0.36f, 0.20f);  // coral-red

        [Header("Scale")]
        public float baseScale    = 1.0f;
        public float maxScaleDelta = 0.25f;   // swell at high OXPHOS

        [Header("Rotation")]
        public float baseRotationSpeed = 4f;  // deg/s when OXPHOS = 100 %

        // Internal
        Vector3 _targetScale;
        Color   _targetEmission;
        float   _rotSpeed;

        protected override void Awake()
        {
            base.Awake();
            organelleName = "Mitochondrion";
            _targetScale  = transform.localScale;
        }

        void Update()
        {
            // Smooth scale
            transform.localScale = Vector3.Lerp(
                transform.localScale, _targetScale, Time.deltaTime * LerpSpeed);

            // Lazy rotation (simulates diffusion)
            transform.Rotate(Vector3.up, _rotSpeed * Time.deltaTime, Space.World);
        }

        protected override void OnStateChanged(CellState s)
        {
            float oxFrac  = s.oxphosFlux / 100f;
            float rosFrac = Mathf.Clamp01(s.ros / 15f);

            // Scale: swell when OXPHOS is high (fusing mitochondria)
            float scaleMult = baseScale + maxScaleDelta * oxFrac;
            _targetScale = Vector3.one * scaleMult;

            // Emission: health gradient green → amber → red
            _targetEmission = rosFrac < 0.5f
                ? Color.Lerp(healthyEmission, stressEmission,  rosFrac * 2f)
                : Color.Lerp(stressEmission,  criticalEmission, (rosFrac - 0.5f) * 2f);

            SetMaterialColor("_EmissionColor", _targetEmission * (0.6f + oxFrac * 0.8f));
            SetMaterialColor("_BaseColor",     Color.Lerp(
                new Color(0.06f, 0.17f, 0.12f),
                new Color(0.24f, 0.06f, 0.03f), rosFrac));

            // Rotation speed ~ membrane potential magnitude
            _rotSpeed = baseRotationSpeed * Mathf.Abs(s.membranePotential) / 90f;
        }
    }
}
