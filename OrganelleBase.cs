using UnityEngine;
using CellTwin.Core;

namespace CellTwin.Organelles
{
    // -------------------------------------------------------------------------
    //  OrganelleBase — attach to every organelle GameObject.
    //  Subclass and override OnStateChanged() for organelle-specific behaviour.
    // -------------------------------------------------------------------------
    public abstract class OrganelleBase : MonoBehaviour
    {
        [Header("Organelle")]
        public string organelleName = "Organelle";

        [Header("Interaction")]
        public bool isInteractable = true;

        // Cached refs
        protected Renderer[]     Renderers;
        protected Collider[]      Colliders;
        protected SimulationManager Sim;

        protected CellState State  => Sim ? Sim.CurrentState : default;

        // Smooth lerp speed for visual transitions
        protected const float LerpSpeed = 3f;

        // ── Lifecycle ──────────────────────────────────────────────────────
        protected virtual void Awake()
        {
            Renderers = GetComponentsInChildren<Renderer>(true);
            Colliders  = GetComponentsInChildren<Collider>(true);
        }

        protected virtual void Start()
        {
            Sim = SimulationManager.Instance;
            if (Sim) Sim.OnStateUpdated.AddListener(OnStateChanged);
        }

        protected virtual void OnDestroy()
        {
            if (Sim) Sim.OnStateUpdated.RemoveListener(OnStateChanged);
        }

        // ── Override in subclasses ─────────────────────────────────────────
        protected abstract void OnStateChanged(CellState state);

        // ── Helpers ────────────────────────────────────────────────────────

        /// Smoothly sets a named float on all child materials.
        protected void SetMaterialFloat(string property, float value)
        {
            foreach (var r in Renderers)
                foreach (var m in r.materials)
                    if (m.HasProperty(property))
                        m.SetFloat(property, value);
        }

        /// Smoothly sets a named color on all child materials.
        protected void SetMaterialColor(string property, Color color)
        {
            foreach (var r in Renderers)
                foreach (var m in r.materials)
                    if (m.HasProperty(property))
                        m.SetColor(property, color);
        }

        /// Linearly remap value from [inMin,inMax] → [outMin,outMax].
        protected static float Remap(float v, float inMin, float inMax,
                                              float outMin, float outMax)
            => Mathf.Lerp(outMin, outMax, Mathf.InverseLerp(inMin, inMax, v));
    }
}
