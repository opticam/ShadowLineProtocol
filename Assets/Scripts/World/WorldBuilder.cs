using UnityEngine;

namespace ShadowLineProtocol
{
    // Lays out the Era 1 landing site: ground, Shackleton crater, deposits, lander, crew and the stray cargo pod.
    public static class WorldBuilder
    {
        public const float MapHalfSize = 100f;
        public static readonly Vector3 CraterCenter = new Vector3(50f, 0f, 35f);
        const float CraterRadius = 20f;
        static readonly Vector3 CargoPodPosition = new Vector3(-28f, 0f, 22f);

        // Returns the cargo pod for the Landfall objective.
        public static GameObject Build(Transform root)
        {
            BuildTerrain(root);

            CreateBuilding(BuildingDef.Lander, Vector3.zero, Quaternion.identity, true, root);

            foreach (var p in new[] { new Vector3(12f, 0f, 6f), new Vector3(-10f, 0f, 10f), new Vector3(8f, 0f, -12f), new Vector3(-14f, 0f, -6f), new Vector3(17f, 0f, -3f) })
                CreateDeposit(root, ResourceType.Regolith, p, 120);

            foreach (var offset in new[] { new Vector3(-5f, 0f, -5f), new Vector3(2f, 0f, 3f), new Vector3(6f, 0f, -5f), new Vector3(-2f, 0f, 7f) })
                CreateDeposit(root, ResourceType.Ice, CraterCenter + offset, 80);

            CreateColonist(root, "Mateo Ferreira", "Mateo", "Regolith feedstock", CrewRole.Hauler, new Color(0.95f, 0.55f, 0.15f), new Vector3(-3f, 0f, -5f));
            CreateColonist(root, "Hana Novak", "Hana", "Habitat construction", CrewRole.Engineer, new Color(0.35f, 0.8f, 0.4f), new Vector3(0f, 0f, -5.5f));
            CreateColonist(root, "Dr. Amara Okonkwo-Reyes", "Amara", "Ice prospecting", CrewRole.Prospector, new Color(0.3f, 0.75f, 0.95f), new Vector3(3f, 0f, -5f));

            return CreateCargoPod(root);
        }

        public static Building CreateBuilding(BuildingDef def, Vector3 position, Quaternion rotation, bool complete, Transform parent)
        {
            var go = new GameObject(def.Name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var visual = Cutouts.BuildingVisual(def, go.transform, true);
            // Set up before Init squashes the visual into a foundation.
            var size = def.Size + new Vector3(0.4f, 0.4f, 0.4f);
            go.AddComponent<Selectable>().Init(SelectableKind.Building, new Vector3(0f, size.y * 0.5f, 0f), size);
            var building = go.AddComponent<Building>();
            building.Init(def, visual, complete);
            return building;
        }

        static void BuildTerrain(Transform root)
        {
            var ground = Cutouts.Part(root, PrimitiveType.Plane, Vector3.zero, new Vector3(MapHalfSize * 0.2f, 1f, MapHalfSize * 0.2f), new Color(0.45f, 0.44f, 0.42f), true);
            ground.name = "Ground";

            float d = CraterRadius * 2f;
            Cutouts.Part(root, PrimitiveType.Cylinder, CraterCenter + Vector3.up * 0.005f, new Vector3(d + 4f, 0.005f, d + 4f), new Color(0.33f, 0.32f, 0.31f)).name = "Crater rim";
            Cutouts.Part(root, PrimitiveType.Cylinder, CraterCenter + Vector3.up * 0.012f, new Vector3(d, 0.005f, d), new Color(0.11f, 0.11f, 0.13f)).name = "Crater floor";
            AddLabel(root, CraterCenter + Vector3.up * 0.5f + Vector3.forward * (CraterRadius - 3f), "Shackleton crater - permanent shadow, water ice");

            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3((float)(rng.NextDouble() * 2 - 1) * 90f, 0f, (float)(rng.NextDouble() * 2 - 1) * 90f);
                if (p.magnitude < 22f || (p - CraterCenter).magnitude < CraterRadius + 4f || (p - CargoPodPosition).magnitude < 5f) continue;
                float s = 0.5f + (float)rng.NextDouble() * 2f;
                float grey = 0.35f + (float)rng.NextDouble() * 0.15f;
                Cutouts.Part(root, PrimitiveType.Sphere, p, new Vector3(s, s * 0.6f, s * 0.8f), new Color(grey, grey, grey * 0.97f), false, new Vector3(0f, (float)rng.NextDouble() * 360f, 0f)).name = "Boulder";
            }
        }

        static void CreateDeposit(Transform root, ResourceType type, Vector3 position, int amount)
        {
            var go = new GameObject(type + " deposit");
            go.transform.SetParent(root, false);
            go.transform.position = position;

            if (type == ResourceType.Ice)
            {
                Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 1.4f), Cutouts.IceColor, true, new Vector3(0f, 25f, 0f));
                Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0.7f, 0.3f, 0.6f), new Vector3(0.8f, 0.6f, 0.8f), Cutouts.IceColor, false, new Vector3(10f, 60f, 5f));
                Cutouts.Flag(go.transform, new Color(0.3f, 0.6f, 1f));
            }
            else
            {
                Cutouts.Part(go.transform, PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(2.4f, 0.9f, 2.4f), Cutouts.RegolithColor, true);
                Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0.6f, 0.35f, 0.4f), Vector3.one * 0.45f, Cutouts.RegolithColor * 0.9f, false, new Vector3(20f, 35f, 10f));
                Cutouts.Flag(go.transform, new Color(0.85f, 0.55f, 0.25f));
            }

            go.AddComponent<Selectable>().InitFitted(SelectableKind.Deposit, 0.3f);
            go.AddComponent<ResourceNode>().Init(type, amount, type == ResourceType.Ice ? 0.9f : 1.2f);
        }

        static void CreateColonist(Transform root, string fullName, string shortName, string specialty, CrewRole role, Color accent, Vector3 position)
        {
            var go = new GameObject(fullName);
            go.transform.SetParent(root, false);
            go.transform.position = position;

            Cutouts.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.7f, 0.03f, 0.7f), Cutouts.DarkCardboard);
            var ring = Cutouts.Part(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.01f, 1.4f), new Color(0.3f, 1f, 0.4f));

            // The board turns to face the camera, like a cardboard standee.
            var board = new GameObject("Board").transform;
            board.SetParent(go.transform, false);
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.7f, 1.5f, 0.05f), Cutouts.Cardboard, true);
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 1.85f, 0f), new Vector3(0.55f, 0.5f, 0.05f), accent);
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 1.85f, -0.04f), new Vector3(0.38f, 0.22f, 0.02f), new Color(0.1f, 0.1f, 0.15f));
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.04f), new Vector3(0.5f, 0.12f, 0.02f), accent);
            var carry = Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 0.7f, -0.2f), Vector3.one * 0.4f, Color.white);

            // Square footprint so the click box covers the board whichever way it faces.
            go.AddComponent<Selectable>().Init(SelectableKind.Colonist, new Vector3(0f, 1.15f, 0f), new Vector3(1.1f, 2.4f, 1.1f));
            go.AddComponent<Colonist>().Init(fullName, shortName, specialty, role, accent, board, ring, carry);
        }

        static GameObject CreateCargoPod(Transform root)
        {
            var go = new GameObject("Cargo Pod");
            go.transform.SetParent(root, false);
            go.transform.position = CargoPodPosition;
            Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.4f, 1.2f, 1.4f), new Color(1f, 0.5f, 0.1f), true, new Vector3(12f, 30f, -18f));
            Cutouts.Part(go.transform, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(1.45f, 0.2f, 1.45f), Color.white, false, new Vector3(12f, 30f, -18f));
            go.AddComponent<MapLabel>().Text = "Stray cargo pod";
            go.AddComponent<Selectable>().InitFitted(SelectableKind.CargoPod, 0.3f);
            return go;
        }

        static void AddLabel(Transform root, Vector3 position, string text)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(root, false);
            go.transform.position = position;
            go.AddComponent<MapLabel>().Text = text;
        }
    }
}
