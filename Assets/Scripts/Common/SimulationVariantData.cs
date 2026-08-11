using UnityEngine;
using System.Collections.Generic;

namespace VISimulation
{
    [CreateAssetMenu(fileName = "NewVariant", menuName = "VI Simulation/Variant Data")]
    public class SimulationVariantData : ScriptableObject
    {
        public string variantName;
        public string sceneName;

        [Header("Parameter Settings")]
        public List<ParameterSetting> parameters = new List<ParameterSetting>();

        [System.Serializable]
        public class ParameterSetting
        {
            public string parameterId; // e.g., "Height", "FontSize", "TextLRV"
            public float value;
            public bool isAdjustable = true;
            public bool isEnabled = true;
        }

        public ParameterSetting GetParameter(string id)
        {
            return parameters.Find(p => p.parameterId == id);
        }
    }
}
