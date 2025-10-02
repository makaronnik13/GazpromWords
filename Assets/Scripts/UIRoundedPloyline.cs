using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/RoundedPolyline")]
public class UIRoundedPolyline : MaskableGraphic
{
    [SerializeField] Vector2[] points;
    [SerializeField, Min(0.1f)] float thickness = 12f;
    [SerializeField, Range(2,64)] int joinSegments = 8;
    [SerializeField, Range(0,64)] int capSegments = 8;
    [SerializeField] bool closed;

    public Vector2[] Points { get => points; set { points = value; SetVerticesDirty(); } }
    public float Thickness { get => thickness; set { thickness = Mathf.Max(0.1f,value); SetVerticesDirty(); } }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Length < 2) return;

        var verts = new List<UIVertex>(1024);
        var inds  = new List<int>(2048);
        float r = thickness * 0.5f;

        var pts = new List<Vector2>(points.Length + (closed ? 1 : 0));
        pts.AddRange(points);
        if (closed) pts.Add(points[0]);

        Vector2 PrevDir(int i)
        {
            if (i == 0) return (pts[1] - pts[0]).normalized;
            return (pts[i] - pts[i-1]).normalized;
        }
        Vector2 NextDir(int i)
        {
            if (i == pts.Count-1) return (pts[i] - pts[i-1]).normalized;
            return (pts[i+1] - pts[i]).normalized;
        }

        void AddVertex(Vector2 p, Vector2 uv, out int idx)
        {
            var v = UIVertex.simpleVert;
            v.color = color;
            v.position = p;
            v.uv0 = uv;
            verts.Add(v);
            idx = verts.Count - 1;
        }

        void AddQuad(int a,int b,int c,int d)
        {
            inds.Add(a); inds.Add(b); inds.Add(c);
            inds.Add(a); inds.Add(c); inds.Add(d);
        }

        void AddFan(Vector2 center, Vector2 from, Vector2 to, bool cw)
        {
            float ang0 = Mathf.Atan2(from.y, from.x);
            float ang1 = Mathf.Atan2(to.y, to.x);
            float d = Mathf.DeltaAngle(ang0*Mathf.Rad2Deg, ang1*Mathf.Rad2Deg)*Mathf.Deg2Rad;
            if (cw && d>0) d -= Mathf.PI*2f;
            if (!cw && d<0) d += Mathf.PI*2f;
            int steps = Mathf.Max(1, joinSegments);
            int ci; AddVertex(center, Vector2.zero, out ci);
            for (int s=0; s<=steps; s++)
            {
                float t = s/(float)steps;
                float a = ang0 + d*t;
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a))*r;
                int vi; AddVertex(p, new Vector2(t,0), out vi);
                if (s>0) { inds.Add(ci); inds.Add(vi-1); inds.Add(vi); }
            }
        }

        int prevLeft=-1, prevRight=-1;

        for (int i=0; i<pts.Count-1; i++)
        {
            Vector2 p0 = pts[i];
            Vector2 p1 = pts[i+1];
            if ((p1-p0).sqrMagnitude < 1e-6f) continue;

            Vector2 d0 = PrevDir(i);
            Vector2 d1 = NextDir(i);
            Vector2 n0 = new Vector2(-d0.y, d0.x);
            Vector2 n1 = new Vector2(-d1.y, d1.x);

            if (i==0 && !closed)
            {
                if (capSegments>0) AddFan(p0, -d0, -d0, false);
                else
                {
                    int a,b;
                    AddVertex(p0 - n0*r, Vector2.zero, out a);
                    AddVertex(p0 + n0*r, Vector2.zero, out b);
                    prevLeft = a; prevRight = b;
                }
            }

            Vector2 miterDir = (n0 + n1).normalized;
            float cos = Vector2.Dot(n0, miterDir);
            float miterLen = r / Mathf.Max(0.1f, cos);

            Vector2 left0  = p0 + n0*r;
            Vector2 right0 = p0 - n0*r;
            Vector2 left1  = p1 + n1*r;
            Vector2 right1 = p1 - n1*r;

            bool turnRight = Vector3.Cross(new Vector3(d0.x,d0.y,0), new Vector3(d1.x,d1.y,0)).z < 0;

            if (i>0 || closed)
            {
                if (joinSegments>0)
                {
                    if (turnRight) AddFan(p0, n0, n1, true);
                    else AddFan(p0, -n0, -n1, false);

                    int a = prevLeft, b = prevRight;
                    int c,d; AddVertex(left0, Vector2.zero, out c); AddVertex(right0, Vector2.zero, out d);
                    AddQuad(a,b,d,c);
                    prevLeft = c; prevRight = d;
                }
                else
                {
                    Vector2 mL = p0 + miterDir*miterLen;
                    Vector2 mR = p0 - miterDir*miterLen;
                    int a = prevLeft, b = prevRight;
                    int c,d; AddVertex(mL, Vector2.zero, out c); AddVertex(mR, Vector2.zero, out d);
                    AddQuad(a,b,d,c);
                    prevLeft = c; prevRight = d;
                }
            }
            else
            {
                int c,d; AddVertex(left0, Vector2.zero, out c); AddVertex(right0, Vector2.zero, out d);
                prevLeft = c; prevRight = d;
            }

            int e,f; AddVertex(left1, Vector2.zero, out e); AddVertex(right1, Vector2.zero, out f);
            AddQuad(prevLeft, prevRight, f, e);
            prevLeft = e; prevRight = f;

            if (i==pts.Count-2 && !closed)
            {
                if (capSegments>0) AddFan(p1, d1, d1, false);
            }
        }

        vh.AddUIVertexStream(verts, inds);
    }
}
