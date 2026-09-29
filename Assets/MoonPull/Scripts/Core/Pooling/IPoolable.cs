namespace MoonPull.Core.Pooling
{
    /// <summary>Implemented by components that must reset state when reused from a pool.</summary>
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }
}
