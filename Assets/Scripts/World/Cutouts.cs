using UnityEngine;

namespace ShadowLineProtocol
{
    // Placeholder "cardboard cutout" art built from Unity primitives. Swap for real models later.
    public static class Cutouts
    {
        public static readonly Color Cardboard = new Color(0.78f, 0.62f, 0.42f);
        public static readonly Color DarkCardboard = new Color(0.55f, 0.42f, 0.28f);
        public static readonly Color RegolithColor = new Color(0.56f, 0.52f, 0.46f);
        public static readonly Color IceColor = new Color(0.72f, 0.9f, 1f);

        public static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 scale,
            Color color, bool keepCollider = false, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static void Flag(Transform parent, Color color)
        {
            Part(parent, PrimitiveType.Cube, new Vector3(1f, 0.7f, 0f), new Vector3(0.05f, 1.4f, 0.05f), Cardboard);
            Part(parent, PrimitiveType.Cube, new Vector3(1.26f, 1.24f, 0f), new Vector3(0.5f, 0.32f, 0.03f), color);
        }

        public static Color CarryColor(ResourceType type) => type == ResourceType.Ice ? IceColor : RegolithColor;

        public static Transform BuildingVisual(BuildingDef def, Transform root, bool colliders)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root, false);
            bool c = colliders;

            switch (def.Type)
            {
                case BuildingType.Lander:
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f), new Vector3(3f, 1.5f, 3f), Cardboard, c);
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 3.2f, 0f), new Vector3(3.05f, 0.15f, 3.05f), new Color(0.95f, 0.5f, 0.1f));
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 4.2f, 0f), new Vector3(1.6f, 0.25f, 1.6f), DarkCardboard);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward;
                        Part(visual, PrimitiveType.Cube, dir * 1.8f + Vector3.up * 0.6f, new Vector3(0.15f, 1.2f, 0.15f), DarkCardboard);
                        Part(visual, PrimitiveType.Cylinder, dir * 1.8f + Vector3.up * 0.03f, new Vector3(0.6f, 0.03f, 0.6f), DarkCardboard);
                    }
                    break;

                case BuildingType.Habitat:
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 2f, 2.4f), Cardboard, c, new Vector3(0f, 0f, 90f));
                    Part(visual, PrimitiveType.Cylinder, new Vector3(1.2f, 1.2f, 0f), new Vector3(2.45f, 0.08f, 2.45f), new Color(0.3f, 0.75f, 0.4f), false, new Vector3(0f, 0f, 90f));
                    Part(visual, PrimitiveType.Cylinder, new Vector3(-1.2f, 1.2f, 0f), new Vector3(2.45f, 0.08f, 2.45f), new Color(0.3f, 0.75f, 0.4f), false, new Vector3(0f, 0f, 90f));
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.75f, -1.15f), new Vector3(0.8f, 1.2f, 0.1f), DarkCardboard);
                    break;

                case BuildingType.SolarArray:
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(0.2f, 1.6f, 0.2f), DarkCardboard, c);
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 1.7f, 0.04f), new Vector3(3.1f, 2.1f, 0.05f), Cardboard, false, new Vector3(15f, 0f, 0f));
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 1.7f, 0f), new Vector3(2.9f, 1.9f, 0.06f), new Color(0.12f, 0.18f, 0.45f), c, new Vector3(15f, 0f, 0f));
                    break;

                case BuildingType.CommsTower:
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.3f, 1.2f), DarkCardboard, c);
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 3f, 0f), new Vector3(0.25f, 2.8f, 0.25f), Cardboard, c);
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0f, 5.1f, -0.35f), new Vector3(1.4f, 0.04f, 1.4f), Cardboard, false, new Vector3(-60f, 0f, 0f));
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 5.9f, 0f), new Vector3(0.25f, 0.25f, 0.25f), new Color(0.9f, 0.2f, 0.2f));
                    break;

                case BuildingType.OxygenPlant:
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(3f, 0.6f, 3f), Cardboard, c);
                    Part(visual, PrimitiveType.Cylinder, new Vector3(-0.7f, 1.4f, 0f), new Vector3(1f, 0.8f, 1f), new Color(0.75f, 0.9f, 0.95f), c);
                    Part(visual, PrimitiveType.Cylinder, new Vector3(0.7f, 1.4f, 0f), new Vector3(1f, 0.8f, 1f), new Color(0.75f, 0.9f, 0.95f), c);
                    Part(visual, PrimitiveType.Cube, new Vector3(0f, 1.9f, 0f), new Vector3(1.6f, 0.12f, 0.12f), DarkCardboard);
                    break;
            }
            return visual;
        }
    }
}
