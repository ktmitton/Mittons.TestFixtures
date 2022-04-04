namespace Mittons.Fixtures.Docker.Gateways
{
    public interface IDockerGateway
    {
        string Run(string imageName, string command);

        void Remove(string containerName);
    }
}