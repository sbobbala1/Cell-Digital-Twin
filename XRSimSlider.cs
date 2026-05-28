using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using CellTwin.Core;
using System;

namespace CellTwin.XR
{
    // -------------------------------------------------------------------------
    //  XRSimSlider
    //
    //  A physical world-space slider the user can grab in AR/VR to tune
    //  simulation parameters (glucose, O2, temperature).
    //
    //  Setup:
    //    1. Create a thin Track object (cube, scale ~0.2 x 0.01 x 0.01).
    //    2. Create a Handle child (sphere, ~0.02 radius) with an XRGrabInteractable.
    //    3. Attach this script to the Handle.
    //    4. Set trackStart/trackEnd to the world positions of the track endpoints.
    //    5. Choose parameter from the enum and assign min/max values.
    // -------------------------------------------------------------------------
    public class XRSimSlider : MonoBehaviour
    {
        public enum SimParameter { Glucose, O2, Temperature }

        [Header("Slider setup")]
        public Transform trackStart;   // left/bottom endpoint
        public Transform trackEnd;     // right/top endpoint
        public SimParameter parameter = SimParameter.Glucose;
        public float minValue = 0f;
        public float maxValue = 100f;

        [Header("Label")]
        public TMPro.TextMeshPro label;

        XRGrabInteractable _grab;
        SimulationManager  _sim;
        bool               _grabbed;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }

        void Start()
        {
            _sim = SimulationManager.Instance;
            _grab.selectEntered.AddListener(_ => _grabbed = true);
            _grab.selectExited.AddListener(_ =>  _grabbed = false);

            // Disable default throw physics — we want constrained movement only
            _grab.trackRotation = false;
        }

        void Update()
        {
            if (!_grabbed || trackStart == null || trackEnd == null) return;

            // Project handle position onto track axis
            Vector3 trackDir = (trackEnd.position - trackStart.position);
            float   trackLen = trackDir.magnitude;
            Vector3 trackUnit = trackDir / trackLen;

            float t = Mathf.Clamp01(
                Vector3.Dot(transform.position - trackStart.position, trackUnit) / trackLen);

            // Constrain handle to track
            transform.position = Vector3.Lerp(trackStart.position, trackEnd.position, t);

            // Map to parameter range
            float value = Mathf.Lerp(minValue, maxValue, t);
            ApplyValue(value);

            if (label) label.text = $"{parameter}: {value:F0}";
        }

        void ApplyValue(float v)
        {
            if (!_sim) return;
            switch (parameter)
            {
                case SimParameter.Glucose:     _sim.SetGlucose(v);     break;
                case SimParameter.O2:          _sim.SetO2(v);          break;
                case SimParameter.Temperature: _sim.SetTemperature(v); break;
            }
        }
    }
}
