using UnityEngine;
using CellTwin.Core;

namespace CellTwin.Organelles
{
    // -------------------------------------------------------------------------
    //  MetaboliteParticleController
    //
    //  Drives three ParticleSystems (ATP, ROS, Glucose) using simulation state.
    //  Assign the three systems in the Inspector.
    //
    //  Recommended ParticleSystem settings for each:
    //    • Start Speed: 0.05 – 0.2
    //    • Start Size:  0.01 – 0.03
    //    • Shape: Sphere, radius matching cell radius
    //    • Simulation Space: World
    //    • Renderer: Unlit material with appropriate color
    // -------------------------------------------------------------------------
    public class MetaboliteParticleController : MonoBehaviour
    {
        [Header("Particle Systems")]
        public ParticleSystem atpParticles;
        public ParticleSystem rosParticles;
        public ParticleSystem glucoseParticles;

        [Header("Emission rate scaling")]
        public float atpMaxRate     = 120f;
        public float rosMaxRate     = 80f;
        public float glucoseMaxRate = 60f;

        SimulationManager _sim;

        void Start()
        {
            _sim = SimulationManager.Instance;
            if (_sim) _sim.OnStateUpdated.AddListener(OnStateChanged);
        }

        void OnDestroy()
        {
            if (_sim) _sim.OnStateUpdated.RemoveListener(OnStateChanged);
        }

        void OnStateChanged(CellState s)
        {
            SetEmission(atpParticles,     (s.atp / 30f)           * atpMaxRate);
            SetEmission(rosParticles,     Mathf.Clamp01(s.ros / 15f) * rosMaxRate);
            SetEmission(glucoseParticles, (s.glucose / 100f)      * glucoseMaxRate);

            // ROS particles glow brighter (emission intensity) when critical
            if (rosParticles)
            {
                var main = rosParticles.main;
                float rosIntensity = Mathf.Clamp01(s.ros / 15f);
                // Shift color from amber to red as ROS rises
                main.startColor = Color.Lerp(
                    new Color(0.94f, 0.62f, 0.15f),
                    new Color(0.89f, 0.20f, 0.10f),
                    rosIntensity);
            }
        }

        static void SetEmission(ParticleSystem ps, float rate)
        {
            if (!ps) return;
            var em = ps.emission;
            em.rateOverTime = Mathf.Max(0f, rate);
        }
    }
}
