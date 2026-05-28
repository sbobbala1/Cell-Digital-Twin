using UnityEngine;
using CellTwin.Core;

namespace CellTwin.Organelles
{
    // =========================================================================
    //  NucleusController
    //  • Nuclear pore objects open/close based on membrane potential
    //  • Nucleolus pulses with gene expression proxy (1 – proteinFolding stress)
    //  • Outer envelope color shifts with viability
    // =========================================================================
    public class NucleusController : OrganelleBase
    {
        [Header("Nuclear pores (assign child GameObjects)")]
        public Transform[] nuclearPores;

        [Header("Nucleolus")]
        public Transform nucleolus;
        public float nucleolusBaseScale = 0.3f;
        public float nucleolusMaxScale  = 0.45f;

        [Header("Colors")]
        public Color healthyColor  = new Color(0.20f, 0.18f, 0.55f);
        public Color damagedColor  = new Color(0.55f, 0.18f, 0.18f);

        float _poreOpenFrac;
        float _nucleolusTarget;

        protected override void Awake()
        {
            base.Awake();
            organelleName = "Nucleus";
        }

        void Update()
        {
            if (nucleolus)
            {
                float s = Mathf.Lerp(nucleolus.localScale.x,
                    Mathf.Lerp(nucleolusBaseScale, nucleolusMaxScale, _nucleolusTarget),
                    Time.deltaTime * LerpSpeed);
                nucleolus.localScale = Vector3.one * s;
            }

            // Animate nuclear pore scale
            foreach (var pore in nuclearPores)
            {
                if (!pore) continue;
                float target = Mathf.Lerp(0.4f, 1.0f, _poreOpenFrac);
                float cur = Mathf.Lerp(pore.localScale.x, target, Time.deltaTime * LerpSpeed);
                pore.localScale = Vector3.one * cur;
            }
        }

        protected override void OnStateChanged(CellState s)
        {
            // Pores open proportionally to membrane potential magnitude
            _poreOpenFrac = Mathf.Clamp01(Mathf.Abs(s.membranePotential) / 90f);

            // Nucleolus expands when protein folding is stressed (rRNA demand ↑)
            _nucleolusTarget = 1f - (s.proteinFolding / 100f);

            // Envelope color
            float viability = s.viability / 100f;
            SetMaterialColor("_BaseColor", Color.Lerp(damagedColor, healthyColor, viability));
            SetMaterialColor("_EmissionColor",
                Color.Lerp(damagedColor, healthyColor, viability) * 0.3f);
        }
    }

    // =========================================================================
    //  EndoplasmicReticulumController
    //  • Tubule thickness scales with protein folding load (UPR response)
    //  • Emissive color reflects ER stress
    // =========================================================================
    public class EndoplasmicReticulumController : OrganelleBase
    {
        [Header("Colors")]
        public Color normalColor = new Color(0.13f, 0.35f, 0.65f);
        public Color stressColor = new Color(0.65f, 0.28f, 0.10f);

        [Header("Scale")]
        public float normalScale = 1.0f;
        public float stressScale = 1.35f;   // swollen ER under UPR

        Vector3 _targetScale;

        protected override void Awake()
        {
            base.Awake();
            organelleName = "Endoplasmic Reticulum";
            _targetScale = transform.localScale;
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale, _targetScale, Time.deltaTime * LerpSpeed);
        }

        protected override void OnStateChanged(CellState s)
        {
            float stressFrac = 1f - (s.proteinFolding / 100f);

            _targetScale = Vector3.one * Mathf.Lerp(normalScale, stressScale, stressFrac);

            Color col = Color.Lerp(normalColor, stressColor, stressFrac);
            SetMaterialColor("_BaseColor",     col);
            SetMaterialColor("_EmissionColor", col * stressFrac * 0.6f);
        }
    }

    // =========================================================================
    //  CellMembraneController
    //  • Rim / Fresnel intensity driven by membrane potential
    //  • Transparency reflects viability (requires URP alpha clipping or fade)
    //  • Surface dissolve at very low viability
    // =========================================================================
    public class CellMembraneController : OrganelleBase
    {
        [Header("Shader property names")]
        public string rimColorProp      = "_RimColor";
        public string rimPowerProp      = "_RimPower";
        public string dissolveAmountProp = "_DissolveAmount";
        public string opacityProp        = "_Opacity";

        [Header("Rim colors")]
        public Color healthyRim  = new Color(0.22f, 0.75f, 0.60f);
        public Color damagedRim  = new Color(0.89f, 0.36f, 0.20f);

        protected override void Awake()
        {
            base.Awake();
            organelleName = "Cell Membrane";
        }

        protected override void OnStateChanged(CellState s)
        {
            float memFrac      = Mathf.Clamp01(Mathf.Abs(s.membranePotential) / 90f);
            float viabilityFrac = s.viability / 100f;

            Color rim = Color.Lerp(damagedRim, healthyRim, viabilityFrac);
            SetMaterialColor(rimColorProp, rim);
            SetMaterialFloat(rimPowerProp, Mathf.Lerp(1.5f, 4f, memFrac));

            // Dissolve begins below 40 % viability
            float dissolve = viabilityFrac > 0.4f ? 0f
                : Remap(viabilityFrac, 0.4f, 0f, 0f, 0.9f);
            SetMaterialFloat(dissolveAmountProp, dissolve);
            SetMaterialFloat(opacityProp, Mathf.Lerp(0.3f, 0.85f, viabilityFrac));
        }
    }
}
