using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[AddComponentMenu("UI/Rounded Line From Rects (Smooth)")]
public class UIRoundedLineFromRects : MaskableGraphic
{
    public List<RectTransform> test;

    [ContextMenu("Test")]
    public void Test() { SetPositions(test.ToArray()); }

    public RectTransform[] targets;
    [Min(0.1f)] public float thickness = 16f;
    [Min(0f)] public float cornerRadius = 16f;
    [Range(2,64)] public int cornerSegments = 8;
    [Range(0,64)] public int capSegments = 12;
    public bool closed;

    RectTransform rt;
    Vector3[] prevWorld;

    protected override void Awake()
    {
        base.Awake();
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (targets == null || targets.Length < 2) return;
        if (prevWorld == null || prevWorld.Length != targets.Length) { prevWorld = new Vector3[targets.Length]; SetVerticesDirty(); return; }
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) return;
            Vector3 w = targets[i].TransformPoint(Vector3.zero);
            if ((w - prevWorld[i]).sqrMagnitude > 0.0001f) { SetVerticesDirty(); return; }
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (targets == null || targets.Length < 2) return;
        if (rt == null) rt = GetComponent<RectTransform>();

        var raw = new List<Vector2>(targets.Length);
        if (prevWorld == null || prevWorld.Length != targets.Length) prevWorld = new Vector3[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) return;
            Vector3 w = targets[i].TransformPoint(Vector3.zero);
            prevWorld[i] = w;
            Vector3 l = rt.InverseTransformPoint(w);
            raw.Add(new Vector2(l.x, l.y));
        }
        if (closed) raw.Add(raw[0]);

        var center = BuildSmoothed(raw);
        if (center.Count < 2) return;

        float r = thickness * 0.5f;
        var verts = new List<UIVertex>(center.Count * 4 + 128);
        var inds  = new List<int>(center.Count * 6 + 256);

        var tangents = new List<Vector2>(center.Count);
        for (int i = 0; i < center.Count; i++)
        {
            Vector2 t;
            if (i == center.Count - 1) t = (center[i] - center[i - 1]).normalized;
            else if (i == 0) t = (center[1] - center[0]).normalized;
            else t = (center[i + 1] - center[i - 1]).normalized;
            tangents.Add(t.sqrMagnitude < 1e-8f ? Vector2.right : t.normalized);
        }

        var leftIdx = new List<int>(center.Count);
        var rightIdx = new List<int>(center.Count);

        for (int i = 0; i < center.Count; i++)
        {
            Vector2 n = new Vector2(-tangents[i].y, tangents[i].x);
            int li = AddVertex(verts, center[i] + n * r, color);
            int ri = AddVertex(verts, center[i] - n * r, color);
            leftIdx.Add(li);
            rightIdx.Add(ri);
        }

        for (int i = 0; i < center.Count - 1; i++)
        {
            AddQuad(inds, leftIdx[i], rightIdx[i], rightIdx[i + 1], leftIdx[i + 1]);
        }

        if (!closed && capSegments > 0)
        {
            Vector2 t0 = (center[1] - center[0]).normalized;
            Vector2 t1 = (center[^1] - center[^2]).normalized;
            AddHalfCircle(verts, inds, center[0],  -t0, r, capSegments, true);   // старт: CCW
            AddHalfCircle(verts, inds, center[^1],  t1,  r, capSegments, false);  // конец: CW
        }

        vh.AddUIVertexStream(verts, inds);
    }

    List<Vector2> BuildSmoothed(List<Vector2> pts)
    {
        var result = new List<Vector2>(pts.Count * (cornerSegments + 1));
        if (pts.Count < 2) { result.AddRange(pts); return result; }

        if (!closed) result.Add(pts[0]);

        int last = pts.Count - 1;
        int start = closed ? 0 : 1;
        int end = closed ? last : last - 1;

        for (int i = start; i <= end; i++)
        {
            int i0 = (i - 1 + pts.Count) % pts.Count;
            int i1 = i % pts.Count;
            int i2 = (i + 1) % pts.Count;

            Vector2 p0 = pts[i0];
            Vector2 p1 = pts[i1];
            Vector2 p2 = pts[i2];

            Vector2 d0 = (p1 - p0).normalized;
            Vector2 d1 = (p2 - p1).normalized;
            if (d0.sqrMagnitude < 1e-8f || d1.sqrMagnitude < 1e-8f || Vector2.Dot(d0, d1) > 0.999f)
            {
                if (!closed || i != end) result.Add(p1);
                continue;
            }

            float maxFillet = Mathf.Min((p1 - p0).magnitude, (p2 - p1).magnitude) * 0.5f;
            float fillet = Mathf.Clamp(cornerRadius, 0f, maxFillet);

            Vector2 a = p1 - d0 * fillet;
            Vector2 b = p1 + d1 * fillet;

            if (result.Count == 0 || (result[^1] - a).sqrMagnitude > 1e-6f) result.Add(a);

            for (int s = 1; s < cornerSegments; s++)
            {
                float t = s / (float)cornerSegments;
                Vector2 q = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * p1 + t * t * b;
                result.Add(q);
            }

            result.Add(b);
        }

        if (!closed) result.Add(pts[^1]);
        if (closed) result.Add(result[0]);

        return result;
    }

    static int AddVertex(List<UIVertex> v, Vector2 p, Color32 c)
    {
        var uiv = UIVertex.simpleVert;
        uiv.position = p;
        uiv.color = c;
        v.Add(uiv);
        return v.Count - 1;
    }

    static void AddQuad(List<int> ind, int a, int b, int c, int d)
    {
        ind.Add(a); ind.Add(c); ind.Add(b);
        ind.Add(a); ind.Add(d); ind.Add(c);
    }

    void AddHalfCircle(List<UIVertex> v, List<int> ind, Vector2 center, Vector2 tangent, float radius, int segments, bool ccw)
    {
        int ci = AddVertex(v, center, this.color);
        Vector2 n = new Vector2(-tangent.y, tangent.x);
        float baseAng = Mathf.Atan2(n.y, n.x);
        float step = (ccw ? 1f : -1f) * Mathf.PI / segments;
        float a = baseAng;
        int prev = -1;
        for (int s = 0; s <= segments; s++)
        {
            Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            int vi = AddVertex(v, p, this.color);
            if (s > 0)
            {
                ind.Add(ci); ind.Add(vi); ind.Add(prev);
            }
            prev = vi;
            a += step;
        }
    }

    public void SetPositions(RectTransform[] targets) { this.targets = targets; }
    public void SetColor(Color color) { this.color = color; }
}
