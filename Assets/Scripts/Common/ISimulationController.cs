using UnityEngine;

namespace VISimulation
{
    public interface ISimulationController
    {
        void ApplyVariant(SimulationVariantData variant);
        void UpdateAll();
    }
}
