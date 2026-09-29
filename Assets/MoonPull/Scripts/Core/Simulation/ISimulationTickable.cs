namespace MoonPull.Core.Simulation
{
    /// <summary>A gameplay system advanced by <see cref="SimulationLoop"/> in a fixed, explicit order.</summary>
    public interface ISimulationTickable
    {
        void SimulationTick(float deltaTime, float levelTime);
    }
}
