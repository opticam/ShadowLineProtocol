using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    public class ResourceNode : MonoBehaviour
    {
        public static readonly List<ResourceNode> All = new List<ResourceNode>();

        public ResourceType Type { get; private set; }
        public int Amount { get; private set; }
        public float Radius { get; private set; }

        int startingAmount;

        public void Init(ResourceType type, int amount, float radius)
        {
            Type = type;
            Amount = startingAmount = amount;
            Radius = radius;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public int Take(int requested)
        {
            int taken = Mathf.Min(requested, Amount);
            Amount -= taken;
            transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, (float)Amount / startingAmount);
            if (Amount <= 0) Destroy(gameObject);
            return taken;
        }

        public static ResourceNode FindNearest(ResourceType type, Vector3 from, float maxDistance)
        {
            ResourceNode best = null;
            float bestSqr = maxDistance * maxDistance;
            foreach (var node in All)
            {
                if (node.Type != type || node.Amount <= 0) continue;
                float sqr = (node.transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = node;
                }
            }
            return best;
        }
    }
}
