namespace Mittons.Fixtures.Containers
{
    /// <inheritdoc/>
    public interface IContainerService : IService
    {
        string ContainerId { get; }
    }
}
