using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Line Connector (Rounded)")]
    [ExecuteInEditMode]
    public class UILineConnector : MonoBehaviour
    {
        public RectTransform[] targets;

        private RectTransform _rt;
        private UIRoundedPolyline _poly;
        private UILineRenderer _legacy;
        private Vector3[] _prevWorld;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _poly = GetComponent<UIRoundedPolyline>();
            _legacy = GetComponent<UILineRenderer>();
        }

        private void OnEnable()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_poly == null) _poly = GetComponent<UIRoundedPolyline>();
            if (_legacy == null) _legacy = GetComponent<UILineRenderer>();
        }

        private void Update()
        {
            if (targets == null || targets.Length < 2) return;

            bool need = _prevWorld == null || _prevWorld.Length != targets.Length;
            if (!need)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null) { need = true; break; }
                    Vector3 w = targets[i].TransformPoint(Vector3.zero);
                    if ((w - _prevWorld[i]).sqrMagnitude > 0.0001f) { need = true; break; }
                }
            }
            if (!need) return;

            var pts = new Vector2[targets.Length];
            if (_prevWorld == null || _prevWorld.Length != targets.Length) _prevWorld = new Vector3[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) return;
                Vector3 w = targets[i].TransformPoint(Vector3.zero);
                _prevWorld[i] = w;
                Vector3 l = _rt.InverseTransformPoint(w);
                pts[i] = new Vector2(l.x, l.y);
            }

            if (_poly != null) _poly.Points = pts;
            else if (_legacy != null) _legacy.Points = pts;
        }
    }
}
