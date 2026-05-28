using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using CellTwin.Core;
using CellTwin.Organelles;

namespace CellTwin.XR
{
    // -------------------------------------------------------------------------
    //  CellTwinXRController
    //
    //  Requires: Unity XR Interaction Toolkit (com.unity.xr.interaction.toolkit)
    //
    //  Place on the root "Cell" GameObject. Handles:
    //    • Grab (pick up and move the whole cell)
    //    • Organelle hover highlight
    //    • Pinch-to-scale the cell
    //    • Dispatch inspect events when user selects an organelle
    // -------------------------------------------------------------------------
    [RequireComponent(typeof(XRGrabInteractable))]
    public class CellTwinXRController : MonoBehaviour
    {
        [Header("Scale")]
        public float minScale = 0.05f;
        public float maxScale = 0.5f;
        public float defaultScale = 0.15f;

        [Header("Highlight")]
        public Color hoverTint = new Color(1f, 1f, 0.6f, 1f);

        XRGrabInteractable _grab;
        SimulationManager  _sim;
        float              _currentScale;

        // Organelle currently under gaze / pointer
        OrganelleBase _hoveredOrganelle;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _currentScale = defaultScale;
            transform.localScale = Vector3.one * _currentScale;
        }

        void Start()
        {
            _sim = SimulationManager.Instance;

            // Wire grab events
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
            _grab.hoverEntered.AddListener(OnHoverEnter);
            _grab.hoverExited.AddListener(OnHoverExit);
        }

        // ── Grab callbacks ─────────────────────────────────────────────────
        void OnGrabbed(SelectEnterEventArgs args)
        {
            // Allow the cell to follow the controller
        }

        void OnReleased(SelectExitEventArgs args)
        {
            // Snap back to world anchor if desired — comment out to free-float
            // transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        // ── Hover highlight ────────────────────────────────────────────────
        void OnHoverEnter(HoverEnterEventArgs args)
        {
            // Try to find an OrganelleBase on the collider's hierarchy
            if (args.interactableObject is XRBaseInteractable xr)
            {
                _hoveredOrganelle = xr.GetComponentInChildren<OrganelleBase>();
                if (_hoveredOrganelle)
                    HighlightOrganelle(_hoveredOrganelle, true);
            }
        }

        void OnHoverExit(HoverExitEventArgs args)
        {
            if (_hoveredOrganelle)
            {
                HighlightOrganelle(_hoveredOrganelle, false);
                _hoveredOrganelle = null;
            }
        }

        void HighlightOrganelle(OrganelleBase org, bool on)
        {
            foreach (var r in org.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m.HasProperty("_EmissionColor"))
                        m.SetColor("_EmissionColor",
                            on ? hoverTint * 0.5f : Color.black);
        }

        // ── Pinch-to-scale (call from a hand-tracking gesture recogniser) ──
        public void OnPinchScale(float pinchDelta)
        {
            _currentScale = Mathf.Clamp(_currentScale + pinchDelta, minScale, maxScale);
            transform.localScale = Vector3.one * _currentScale;
        }

        // ── Organelle inspection (call from UI or gaze dwell timer) ────────
        public void InspectHovered()
        {
            if (_hoveredOrganelle == null) return;
            CellInspectPanel.Instance?.ShowOrganelle(
                _hoveredOrganelle.organelleName, _sim.CurrentState);
        }
    }
}
