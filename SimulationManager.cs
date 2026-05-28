using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CellTwin.Core
{
    // -------------------------------------------------------------------------
    //  CellState — all simulation variables in one serializable struct
    // -------------------------------------------------------------------------
    [Serializable]
    public struct CellState
    {
        public float glucose;       // 0–100 %
        public float o2;            // 0–100 %
        public float temperature;   // 30–45 °C

        public float atp;           // mol/s  (computed)
        public float membranePotential; // mV  (computed, typically –90 to –40)
        public float ros;           // μM     (computed)
        public float viability;     // 0–100 % (computed)

        public float glycolysisFlux;  // 0–100 %
        public float oxphosFlux;      // 0–100 %
        public float proteinFolding;  // 0–100 %

        public bool  drugActive;
        public bool  heatShockActive;
    }

    // -------------------------------------------------------------------------
    //  SimulationManager — singleton that owns the metabolic tick
    // -------------------------------------------------------------------------
    public class SimulationManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────
        public static SimulationManager Instance { get; private set; }

        // ── Inspector ──────────────────────────────────────────────────────
        [Header("Initial conditions")]
        [Range(0,100)] public float initialGlucose    = 60f;
        [Range(0,100)] public float initialO2         = 80f;
        [Range(30,45)] public float initialTemperature = 37f;

        [Header("Tick")]
        [Min(0.1f)] public float tickInterval = 0.5f;   // seconds between updates

        [Header("Noise")]
        [Range(0,2)] public float membranePotentialNoise = 1.5f;
        [Range(0,1)] public float rosNoise               = 0.3f;

        // ── Events (subscribe from OrganelleControllers, UI, etc.) ─────────
        [HideInInspector] public UnityEvent<CellState> OnStateUpdated  = new();
        [HideInInspector] public UnityEvent<string>    OnAlert         = new();
        [HideInInspector] public UnityEvent<string>    OnLog           = new();

        // ── State ──────────────────────────────────────────────────────────
        public CellState CurrentState  { get; private set; }
        public CellState PreviousState { get; private set; }

        // ── Lifecycle ──────────────────────────────────────────────────────
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            var s = new CellState
            {
                glucose     = initialGlucose,
                o2          = initialO2,
                temperature = initialTemperature,
                drugActive        = false,
                heatShockActive   = false
            };
            CurrentState = s;
            Compute(ref s);
            CurrentState = s;
        }

        void Start()
        {
            Log("Cell digital twin initialized — steady state");
            StartCoroutine(TickLoop());
        }

        // ── Public API (called by UI sliders / XR interactions) ────────────
        public void SetGlucose(float v)
        {
            var s = CurrentState; s.glucose = Mathf.Clamp(v, 0, 100); CurrentState = s;
        }
        public void SetO2(float v)
        {
            var s = CurrentState; s.o2 = Mathf.Clamp(v, 0, 100); CurrentState = s;
            if (v < 20) Log("Hypoxic response initiated — HIF-1α stabilizing");
        }
        public void SetTemperature(float v)
        {
            var s = CurrentState; s.temperature = Mathf.Clamp(v, 30, 45); CurrentState = s;
        }
        public void SetDrug(bool active)
        {
            var s = CurrentState; s.drugActive = active; CurrentState = s;
            Log(active ? "Drug applied — mitochondrial inhibitor active" : "Drug cleared");
        }
        public void SetHeatShock(bool active)
        {
            var s = CurrentState; s.heatShockActive = active; CurrentState = s;
            Log(active ? "Heat shock applied — chaperone response initiated" : "Heat shock removed");
        }

        // ── Tick ───────────────────────────────────────────────────────────
        IEnumerator TickLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(tickInterval);
                PreviousState = CurrentState;
                var s = CurrentState;
                Compute(ref s);
                CurrentState = s;
                CheckAlerts();
                OnStateUpdated.Invoke(CurrentState);
            }
        }

        // ── Metabolic model ────────────────────────────────────────────────
        void Compute(ref CellState s)
        {
            float g    = s.glucose / 100f;
            float o    = s.o2 / 100f;
            float t    = s.temperature;
            float drug = s.drugActive      ? 0.4f : 1f;
            float heat = s.heatShockActive ? 0.55f : 1f;

            // Temperature factor: optimal ~37–39 °C
            float tempFactor;
            if      (t < 37f) tempFactor = 0.6f + 0.4f * (t - 30f) / 7f;
            else if (t <= 39f) tempFactor = 1f;
            else              tempFactor = Mathf.Max(0f, 1f - (t - 39f) * 0.18f);

            s.glycolysisFlux = Mathf.Clamp01(g * 1.2f * tempFactor * drug) * 100f;
            s.oxphosFlux     = Mathf.Clamp01(o * g * 1.3f * tempFactor * drug * heat) * 100f;

            s.atp = (s.glycolysisFlux * 0.03f + s.oxphosFlux * 0.18f) * drug;

            float memNoise        = (UnityEngine.Random.value - 0.5f) * 2f * membranePotentialNoise;
            s.membranePotential   = -90f + s.oxphosFlux * 0.22f + memNoise;

            float rosBase = 0.5f
                + (1f - o) * 6f
                + (t > 39f ? (t - 39f) * 1.2f : 0f)
                + (s.drugActive      ? 2.5f : 0f)
                + (s.heatShockActive ? 3.0f : 0f)
                + (UnityEngine.Random.value - 0.5f) * 2f * rosNoise;
            s.ros = Mathf.Max(0f, rosBase);

            float foldPenalty = (t > 40f ? (t - 40f) * 18f : 0f)
                              + (s.heatShockActive ? 25f : 0f)
                              + (s.ros > 8f        ? 15f : 0f);
            s.proteinFolding = Mathf.Clamp(98f - foldPenalty, 10f, 100f);

            s.viability = Mathf.Clamp(
                100f - s.ros * 2.5f
                     - (100f - s.proteinFolding) * 0.4f
                     + s.atp * 0.8f
                     - Mathf.Max(0f, (t - 42f) * 8f),
                0f, 100f);
        }

        // ── Alert checks ───────────────────────────────────────────────────
        void CheckAlerts()
        {
            var c = CurrentState; var p = PreviousState;
            if (c.ros      >  8f  && p.ros      <= 8f)   Alert("ROS critically high — oxidative stress");
            if (c.viability < 60f && p.viability >= 60f)  Alert("Cell viability < 60 %");
            if (c.proteinFolding < 60f && p.proteinFolding >= 60f) Alert("Protein misfolding — HSP70 upregulated");
            if (c.atp < 5f && p.atp >= 5f) Alert("ATP depleted — metabolic arrest risk");
        }

        void Log(string msg)   => OnLog.Invoke(msg);
        void Alert(string msg) { OnAlert.Invoke(msg); OnLog.Invoke("⚠ " + msg); }
    }
}
