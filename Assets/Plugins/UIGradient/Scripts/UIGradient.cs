using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author: Josh H.
/// Support: assetstore.joshh@gmail.com
/// </summary>

namespace UIGradient.Scripts
{
    [AddComponentMenu(menuName: "UI/Effects/UI Gradient")]
    [RequireComponent(requiredComponent: typeof(RectTransform))]
    public class UIGradient : BaseMeshEffect
    {
        [Tooltip(tooltip: "How the gradient color will be blended with the graphics color.")]
        [SerializeField] private UIGradientBlendMode blendMode;

        [SerializeField] [Range(min: 0, max: 1)] private float intensity = 1f;

        [SerializeField] private UIGradientType gradientType;

        //Linear Colors
        [SerializeField] private Color linearColor1 = Color.yellow;
        [SerializeField] private Color linearColor2 = Color.red;

        //Corner Colors
        [SerializeField] private Color cornerColorUpperLeft = Color.red;
        [SerializeField] private Color cornerColorUpperRight = Color.yellow;
        [SerializeField] private Color cornerColorLowerRight = Color.green;
        [SerializeField] private Color cornerColorLowerLeft = Color.blue;

        //Complex Linear
        [SerializeField] private Gradient linearGradient;

        [SerializeField] [Range(min: 0, max: 360)] private float angle;

        private RectTransform _rectTransform;

        protected RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = transform as RectTransform;
                }
                return _rectTransform;
            }
        }

        public UIGradientBlendMode BlendMode
        {
            get
            {
                return blendMode;
            }

            set
            {
                blendMode = value;
                ForceUpdateGraphic();
            }
        }

        public float Intensity
        {
            get
            {
                return intensity;
            }

            set
            {
                intensity = Mathf.Clamp01(value: value);
                ForceUpdateGraphic();
            }
        }

        public UIGradientType GradientType
        {
            get
            {
                return gradientType;
            }

            set
            {
                gradientType = value;
                ForceUpdateGraphic();
            }
        }

        public Color LinearColor1
        {
            get
            {
                return linearColor1;
            }

            set
            {
                linearColor1 = value;
                ForceUpdateGraphic();
            }
        }

        public Color LinearColor2
        {
            get
            {
                return linearColor2;
            }

            set
            {
                linearColor2 = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorUpperLeft
        {
            get
            {
                return cornerColorUpperLeft;
            }

            set
            {
                cornerColorUpperLeft = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorUpperRight
        {
            get
            {
                return cornerColorUpperRight;
            }

            set
            {
                cornerColorUpperRight = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorLowerRight
        {
            get
            {
                return cornerColorLowerRight;
            }

            set
            {
                cornerColorLowerRight = value;
                ForceUpdateGraphic();
            }
        }

        public Color CornerColorLowerLeft
        {
            get
            {
                return cornerColorLowerLeft;
            }

            set
            {
                cornerColorLowerLeft = value;
                ForceUpdateGraphic();
            }
        }

        public float Angle
        {
            get
            {
                return angle;
            }

            set
            {
                if (value < 0)
                {
                    angle = (value % 360) + 360;
                }
                else
                {
                    angle = value % 360;
                }
                ForceUpdateGraphic();
            }
        }

        public Gradient LinearGradient
        {
            get
            {
                return linearGradient;
            }

            set
            {
                linearGradient = value;
                ForceUpdateGraphic();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (enabled)
            {
                UIVertex vert = new UIVertex();
                if (gradientType == UIGradientType.ComplexLinear)
                {
                    CutMesh(vh: vh);
                }

                for (int i = 0; i < vh.currentVertCount; i++)
                {
                    vh.PopulateUIVertex(vertex: ref vert, i: i);

#if UNITY_2018_1_OR_NEWER
                    Vector2 normalizedPosition = ((Vector2)vert.position - rectTransform.rect.min) / (rectTransform.rect.max - rectTransform.rect.min);
#else
                    Vector2 size = rectTransform.rect.max - rectTransform.rect.min;
                    Vector2 normalizedPosition = Vector2.Scale((Vector2)vert.position - rectTransform.rect.min, new Vector2(1f / size.x, 1f / size.y));
#endif

                    normalizedPosition = RotateNormalizedPosition(normalizedPosition: normalizedPosition, angle: angle);

                    //get color with selected gradient type
                    Color gradientColor = Color.black;
                    if (gradientType == UIGradientType.Linear)
                    {
                        gradientColor = GetColorInGradient(ul: linearColor1, ur: linearColor1, lr: linearColor2, ll: linearColor2, normalizedPosition: normalizedPosition);
                    }
                    else if (gradientType == UIGradientType.Corner)
                    {
                        gradientColor = GetColorInGradient(ul: cornerColorUpperLeft, ur: cornerColorUpperRight, lr: cornerColorLowerRight, ll: cornerColorLowerLeft, normalizedPosition: normalizedPosition);
                    }
                    else if (gradientType == UIGradientType.ComplexLinear)
                    {
                        gradientColor = linearGradient.Evaluate(time: normalizedPosition.y);
                    }
                    vert.color = BlendColor(c1: vert.color, c2: gradientColor, mode: blendMode, intensity: intensity);
                    vh.SetUIVertex(vertex: vert, i: i);
                }
            }
        }

        protected void CutMesh(VertexHelper vh)
        {
            var tris = new List<UIVertex>();

            vh.GetUIVertexStream(stream: tris);

            vh.Clear();

            var list = new List<UIVertex>();

            var d = GetCutDirection();

            IEnumerable<float> cuts = linearGradient.alphaKeys.Select(selector: x => { return x.time; });
            cuts = cuts.Union(second: linearGradient.colorKeys.Select(selector: x => { return x.time; }));

            foreach (var item in cuts)
            {
                list.Clear();
                var point = GetCutOrigin(f: item);
                if (item < 0.001 || item > 0.999)
                {
                    continue;
                }
                else
                {
                    for (int j = 0; j < tris.Count; j += 3)
                    {
                        CutTriangle(tris: tris, idx: j, list: list, cutDirection: d, point: point);
                    }
                }
                tris.Clear();
                tris.AddRange(collection: list);
            }
            vh.AddUIVertexTriangleStream(verts: tris);
        }

        UIVertex UIVertexLerp(UIVertex v1, UIVertex v2, float f)
        {
            UIVertex vert = new UIVertex();

            vert.position = Vector3.Lerp(a: v1.position, b: v2.position, t: f);
            vert.color = Color.Lerp(a: v1.color, b: v2.color, t: f);
            vert.uv0 = Vector2.Lerp(a: v1.uv0, b: v2.uv0, t: f);
            vert.uv1 = Vector2.Lerp(a: v1.uv1, b: v2.uv1, t: f);
            vert.uv2 = Vector2.Lerp(a: v1.uv2, b: v2.uv2, t: f);
            vert.uv3 = Vector2.Lerp(a: v1.uv3, b: v2.uv3, t: f);

            return vert;
        }

        Vector2 GetCutDirection()
        {
            var v = Vector2.up.Rotate(degrees: -angle);
            v = new Vector2(x: v.x / rectTransform.rect.size.x,y: v.y / rectTransform.rect.size.y);
            return v.Rotate(degrees: 90);
        }

        void CutTriangle(List<UIVertex> tris, int idx, List<UIVertex> list, Vector2 cutDirection, Vector2 point)
        {
            var a = tris[index: idx];
            var b = tris[index: idx + 1];
            var c = tris[index: idx + 2];

            float bc = OnLine(p1: b.position, p2: c.position, o: point, dir: cutDirection);
            float ab = OnLine(p1: a.position, p2: b.position, o: point, dir: cutDirection);
            float ca = OnLine(p1: c.position, p2: a.position, o: point, dir: cutDirection);

            if (IsOnLine(f: ab))
            {
                if (IsOnLine(f: bc))
                {
                    var pab = UIVertexLerp(v1: a, v2: b, f: ab);
                    var pbc = UIVertexLerp(v1: b, v2: c, f: bc);
                    list.AddRange(collection: new List<UIVertex>() { a, pab, c, pab, pbc, c, pab, b, pbc });
                }
                else
                {
                    var pab = UIVertexLerp(v1: a, v2: b, f: ab);
                    var pca = UIVertexLerp(v1: c, v2: a, f: ca);
                    list.AddRange(collection: new List<UIVertex>() { c, pca, b, pca, pab, b, pca, a, pab });
                }
            }
            else if (IsOnLine(f: bc))
            {
                var pbc = UIVertexLerp(v1: b, v2: c, f: bc);
                var pca = UIVertexLerp(v1: c, v2: a, f: ca);
                list.AddRange(collection: new List<UIVertex>() { b, pbc, a, pbc, pca, a, pbc, c, pca });
            }
            else
            {
                list.AddRange(collection: tris.GetRange(index: idx, count: 3));
            }
        }

        bool IsOnLine(float f)
        {
            return f <= 1 && f > 0;
        }

        /// <summary>
        /// Calculates intersection of two lines.
        /// </summary>
        /// <param name="p1">Point on line 1</param>
        /// <param name="p2">Point on line 1</param>
        /// <param name="o">Point on line 2</param>
        /// <param name="dir">Direction of line 2</param>
        /// <returns>f: lerp(p1,p2,f) is the point of intersection</returns>
        float OnLine(Vector2 p1, Vector2 p2, Vector2 o, Vector2 dir)
        {
            float tmp = (p2.x - p1.x) * dir.y - (p2.y - p1.y) * dir.x;
            if (tmp == 0)
            {
                return -1;
            }
            float mu = ((o.x - p1.x) * dir.y - (o.y - p1.y) * dir.x) / tmp;
            return mu;
        }

        float ProjectedDistance(Vector2 p1, Vector2 p2, Vector2 normal)
        {
            return Vector2.Distance(a: Vector3.Project(vector: p1, onNormal: normal), b: Vector3.Project(vector: p2, onNormal: normal));
        }

        Vector2 GetCutOrigin(float f)
        {
            var v = Vector2.up.Rotate(degrees: -angle);

            v = new Vector2(x: v.x / rectTransform.rect.size.x,y: v.y / rectTransform.rect.size.y);

            Vector3 p1, p2;

            if (angle % 180 < 90)
            {
                p1 = Vector3.Project(vector: Vector2.Scale(a: rectTransform.rect.size, b: (Vector2.down + Vector2.left)) * 0.5f, onNormal: v);
                p2 = Vector3.Project(vector: Vector2.Scale(a: rectTransform.rect.size,b: (Vector2.up + Vector2.right)) * 0.5f, onNormal: v);
            }
            else
            {
                p1 = Vector3.Project(vector: Vector2.Scale(a: rectTransform.rect.size,b: (Vector2.up + Vector2.left)) * 0.5f, onNormal: v);
                p2 = Vector3.Project(vector: Vector2.Scale(a: rectTransform.rect.size,b: (Vector2.down + Vector2.right)) * 0.5f, onNormal: v);
            }
            if (angle % 360 >= 180)
            {
                return Vector2.Lerp(a: p2, b: p1, t: f) + rectTransform.rect.center;
            }
            else
            {
                return Vector2.Lerp(a: p1, b: p2, t: f) + rectTransform.rect.center;
            }
        }

        private Color BlendColor(Color c1, Color c2, UIGradientBlendMode mode, float intensity)
        {
            if (mode == UIGradientBlendMode.Override)
            {
                return Color.Lerp(a: c1, b: c2, t: intensity);
            }
            else if (mode == UIGradientBlendMode.Multiply)
            {
                return Color.Lerp(a: c1, b: c1 * c2, t: intensity);
            }
            else
            {
                Debug.LogErrorFormat(format: "Mode is not supported: {0}", mode);
                return c1;
            }
        }

        /// <summary>
        /// Rotates a position in with coordinates in [0,1]
        /// </summary>
        /// <param name="normalizedPosition">Point to rotate</param>
        /// <param name="angle">Angle to rotate in degrees</param>
        /// <returns>Rotated point</returns>
        private Vector2 RotateNormalizedPosition(Vector2 normalizedPosition, float angle)
        {
            float a = Mathf.Deg2Rad * (angle < 0 ? (angle % 90 + 90) : (angle % 90));
            float scale = Mathf.Sin(f: a) + Mathf.Cos(f: a);

            return (normalizedPosition - Vector2.one * 0.5f).Rotate(degrees: angle) / scale + Vector2.one * 0.5f;
        }

        /// <summary>
        /// Sets vertices of the referenced Graphic dirty. This triggers a new mesh generation and modification.
        /// </summary>
        public void ForceUpdateGraphic()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }

        /// <summary>
        /// Calculates color interpolated between 4 corners.
        /// </summary>
        /// <param name="ul">upper left (0,1)</param>
        /// <param name="ur">upper right (1,1)</param>
        /// <param name="lr">lower right (1,0)</param>
        /// <param name="ll">lower left (0,0)</param>
        /// <param name="normalizedPosition">position (x,y) in [0,1]</param>
        /// <returns>interpolated color</returns>
        private Color GetColorInGradient(Color ul, Color ur, Color lr, Color ll, Vector2 normalizedPosition)
        {
            return Color.Lerp(a: Color.Lerp(a: ll, b: lr, t: normalizedPosition.x), b: Color.Lerp(a: ul, b: ur, t: normalizedPosition.x), t: normalizedPosition.y); ;
        }

        public enum UIGradientBlendMode
        {
            Override,
            Multiply
        }

        public enum UIGradientType
        {
            Linear,
            Corner,
            ComplexLinear
        }

#if UNITY_EDITOR
        /// <summary>
        /// Reset is called when the user hits the Reset button in the Inspector's
        /// context menu or when adding the component the first time.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            linearGradient = new Gradient();

            // Populate the color keys
            var colorKey = new GradientColorKey[3];
            colorKey[0].color = new Color(r: 0.5137255f, g: 0.2274510f, b: 0.7058824f);
            colorKey[0].time = 0.0f;
            colorKey[1].color = new Color(r: 0.9921569f, g: 0.1137255f, b: 0.1137255f);
            colorKey[1].time = 0.5f;
            colorKey[2].color = new Color(r: 0.9882353f, g: 0.6901961f, b: 0.2705882f);
            colorKey[2].time = 1.0f;

            // Populate the alpha keys
            var alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 1.0f;

            linearGradient.SetKeys(colorKeys: colorKey, alphaKeys: alphaKey);

            ForceUpdateGraphic();
        }
#endif
    }
}