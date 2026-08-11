using UnityEngine;

namespace VISimulation
{
    public class SimulationVariantManager : MonoBehaviour
    {
        private static SimulationVariantManager _instance;
        public static SimulationVariantManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SimulationVariantManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SimulationVariantManager (Auto)");
                        _instance = go.AddComponent<SimulationVariantManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public SimulationVariantData ActiveVariant { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetActiveVariant(SimulationVariantData variant)
        {
            ActiveVariant = variant;
            Debug.Log($"[VariantManager] Active variant set to: {(variant != null ? variant.variantName : "None")}");
        }
    }
}
