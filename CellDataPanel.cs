using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CellTwin.Core;

namespace CellTwin.XR
{
    // -------------------------------------------------------------------------
    //  CellDataPanel
    //
    //  A world-space canvas that shows live metrics as text.
    //  Assign in the Inspector:
    //    • atpText, memText, rosText, viaText  → TMP labels for live values
    //    • logContainer                        → parent for log line prefabs
    //    • logLinePrefab                       → prefab with a TMP component
    //
    //  Attach this to a world-space Canvas parented near the cell.
    // -------------------------------------------------------------------------
    public class CellDataPanel : MonoBehaviour
    {
        [Header("Metric labels")]
        public TextMeshProUGUI atpText;
        public TextMeshProUGUI memText;
        public TextMeshProUGUI rosText;
        public TextMeshProUGUI viaText;

        [Header("Flux bars (optional RectTransforms — set anchorMax.x)")]
        public RectTransform glycBar;
        public RectTransform oxphBar;
        public RectTransform foldBar;

        [Header("Alert log")]
        public Transform          logContainer;
        public TextMeshProUGUI    logLinePrefab;
        public int                maxLogLines = 6;
        public Color              alertColor   = new Color(0.9f, 0.3f, 0.2f);
        public Color              warnColor    = new Color(0.9f, 0.65f, 0.1f);
        public Color              okColor      = new Color(0.2f, 0.8f, 0.5f);

        // ── Camera billboard ──────────────────────────────────────────────
        [Header("Billboard")]
        public bool  billboardToCamera = true;
        Transform    _cam;

        SimulationManager _sim;
        Queue<TextMeshProUGUI> _logLines = new();

        void Start()
        {
            _sim = SimulationManager.Instance;
            if (_sim)
            {
                _sim.OnStateUpdated.AddListener(OnStateUpdated);
                _sim.OnLog.AddListener(msg => AddLog(msg, okColor));
                _sim.OnAlert.AddListener(msg => AddLog(msg, alertColor));
            }
            _cam = Camera.main?.transform;
        }

        void LateUpdate()
        {
            if (billboardToCamera && _cam)
                transform.rotation = Quaternion.LookRotation(
                    transform.position - _cam.position, Vector3.up);
        }

        void OnDestroy()
        {
            if (_sim)
            {
                _sim.OnStateUpdated.RemoveListener(OnStateUpdated);
            }
        }

        // ── Metric update ─────────────────────────────────────────────────
        void OnStateUpdated(CellState s)
        {
            if (atpText) atpText.text = $"ATP  {s.atp:F1} mol/s";
            if (memText) memText.text = $"Vmem {s.membranePotential:F0} mV";
            if (rosText) rosText.text = $"ROS  {s.ros:F1} μM";
            if (viaText)
            {
                viaText.text  = $"Viability  {s.viability:F0}%";
                viaText.color = s.viability > 70f ? okColor
                              : s.viability > 40f ? warnColor : alertColor;
            }

            SetBar(glycBar, s.glycolysisFlux / 100f);
            SetBar(oxphBar, s.oxphosFlux     / 100f);
            SetBar(foldBar, s.proteinFolding  / 100f);
        }

        static void SetBar(RectTransform rt, float frac)
        {
            if (!rt) return;
            var a = rt.anchorMax; a.x = Mathf.Clamp01(frac); rt.anchorMax = a;
        }

        // ── Log ───────────────────────────────────────────────────────────
        void AddLog(string msg, Color color)
        {
            if (!logContainer || !logLinePrefab) return;

            var line = Instantiate(logLinePrefab, logContainer);
            line.text  = msg;
            line.color = color;
            _logLines.Enqueue(line);

            while (_logLines.Count > maxLogLines)
            {
                var old = _logLines.Dequeue();
                if (old) Destroy(old.gameObject);
            }
        }
    }

    // -------------------------------------------------------------------------
    //  CellInspectPanel
    //
    //  Shows per-organelle detail when the user selects an organelle in XR.
    //  Singleton for easy reference from CellTwinXRController.
    // -------------------------------------------------------------------------
    public class CellInspectPanel : MonoBehaviour
    {
        public static CellInspectPanel Instance { get; private set; }

        [Header("Inspect panel")]
        public GameObject       panelRoot;
        public TextMeshProUGUI  titleText;
        public TextMeshProUGUI  detailText;

        void Awake()
        {
            Instance = this;
            if (panelRoot) panelRoot.SetActive(false);
        }

        public void ShowOrganelle(string name, CellState s)
        {
            if (!panelRoot) return;
            panelRoot.SetActive(true);
            if (titleText) titleText.text = name;
            if (detailText) detailText.text = name switch
            {
                "Mitochondrion" => $"OXPHOS flux: {s.oxphosFlux:F0}%\n" +
                                   $"Membrane potential: {s.membranePotential:F0} mV\n" +
                                   $"ROS production: {s.ros:F1} μM",

                "Nucleus"       => $"Viability: {s.viability:F0}%\n" +
                                   $"Protein folding: {s.proteinFolding:F0}%\n" +
                                   "Nuclear pore status: " + (Mathf.Abs(s.membranePotential) > 60 ? "open" : "restricted"),

                "Endoplasmic Reticulum" =>
                                   $"Protein folding: {s.proteinFolding:F0}%\n" +
                                   $"UPR stress: {(s.proteinFolding < 60 ? "active" : "nominal")}\n" +
                                   $"ER stress level: {(100f - s.proteinFolding):F0}%",

                "Cell Membrane" => $"Membrane potential: {s.membranePotential:F0} mV\n" +
                                   $"Viability: {s.viability:F0}%\n" +
                                   $"Integrity: {(s.viability > 70 ? "intact" : s.viability > 40 ? "compromised" : "critical")}",

                _               => $"State: active\nViability: {s.viability:F0}%"
            };
        }

        public void Hide() => panelRoot?.SetActive(false);
    }
}
