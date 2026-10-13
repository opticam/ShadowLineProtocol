using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowLineProtocol
{
    public enum SelectableKind
    {
        Colonist,
        Deposit,
        Building,
        CargoPod,
    }

    // Gives an object one click box covering its whole cutout, plus a hover/selection outline.
    public class Selectable : MonoBehaviour
    {
        const float OutlineWidth = 0.07f;
        static readonly Color HoverColor = new Color(1f, 0.92f, 0.4f);
        static readonly Color SelectedColor = new Color(0.3f, 1f, 0.4f);
        static Material hoverMaterial;
        static Material selectedMaterial;

        public static readonly List<Selectable> All = new List<Selectable>();

        public SelectableKind Kind { get; private set; }

        readonly List<Renderer> hulls = new List<Renderer>();
        bool hovered;
        bool selected;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Init(SelectableKind kind, Vector3 colliderCenter, Vector3 colliderSize)
        {
            Kind = kind;
            foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = colliderCenter;
            box.size = colliderSize;
            BuildHulls();
            Refresh();
        }

        public void InitFitted(SelectableKind kind, float padding)
        {
            var bounds = LocalRendererBounds();
            Init(kind, bounds.center, bounds.size + Vector3.one * (padding * 2f));
        }

        public void SetHovered(bool value)
        {
            hovered = value;
            Refresh();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            Refresh();
        }

        void Refresh()
        {
            bool show = hovered || selected;
            var material = GetMaterial(hovered);
            foreach (var hull in hulls)
            {
                if (hull == null) continue;
                hull.enabled = show && material != null;
                if (hull.enabled) hull.sharedMaterial = material;
            }
        }

        Bounds LocalRendererBounds()
        {
            var bounds = new Bounds();
            bool first = true;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                Bounds wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? wb.min.x : wb.max.x,
                        (i & 2) == 0 ? wb.min.y : wb.max.y,
                        (i & 4) == 0 ? wb.min.z : wb.max.z);
                    Vector3 local = transform.InverseTransformPoint(corner);
                    if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                    else bounds.Encapsulate(local);
                }
            }
            return bounds;
        }

        void BuildHulls()
        {
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null) continue;

                var hull = new GameObject("Outline");
                hull.transform.SetParent(filter.transform, false);
                Vector3 size = Vector3.Scale(filter.sharedMesh.bounds.size, filter.transform.lossyScale);
                hull.transform.localScale = new Vector3(Grow(size.x), Grow(size.y), Grow(size.z));

                hull.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = hull.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = false;
                hulls.Add(renderer);
            }
        }

        static float Grow(float worldSize)
        {
            worldSize = Mathf.Abs(worldSize);
            return worldSize < 0.001f ? 1f : (worldSize + OutlineWidth * 2f) / worldSize;
        }

        static Material GetMaterial(bool hover)
        {
            if (hoverMaterial == null)
            {
                var shader = Resources.Load<Shader>("ShadowLineOutline");
                if (shader == null) return null;
                hoverMaterial = new Material(shader) { color = HoverColor };
                selectedMaterial = new Material(shader) { color = SelectedColor };
            }
            return hover ? hoverMaterial : selectedMaterial;
        }
    }
}
