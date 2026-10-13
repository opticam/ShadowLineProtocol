using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    public class MapLabel : MonoBehaviour
    {
        public static readonly List<MapLabel> All = new List<MapLabel>();

        public string Text;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
