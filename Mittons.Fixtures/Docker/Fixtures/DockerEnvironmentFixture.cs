using System;
using System.Collections.Generic;
using System.Linq;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Networks;

namespace Mittons.Fixtures.Docker.Fixtures
{
    public abstract class DockerEnvironmentFixture : IDisposable
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        private List<Container> _containers;

        private DefaultNetwork[] _networks;

        public DockerEnvironmentFixture(IDockerGateway dockerGateway)
        {
            var networks = Attribute.GetCustomAttributes(this.GetType()).OfType<Network>();
            var duplicateNetworks = networks.GroupBy(x => x.Name).Where(x => x.Count() > 1);

            if (duplicateNetworks.Any())
            {
                throw new NotSupportedException($"Networks with the same name cannot be created for the same environment. The following networks were duplicated: [{string.Join(", ", duplicateNetworks.Select(x => x.Key))}]");
            }

            _networks = networks.Select(x => new DefaultNetwork(dockerGateway, $"{x.Name}-{InstanceId}")).ToArray();

            _containers = new List<Container>();

            foreach(var propertyInfo in this.GetType().GetProperties().Where(x => typeof(Container).IsAssignableFrom(x.PropertyType)))
            {
                var attributes = propertyInfo.GetCustomAttributes(false).OfType<Attribute>();

                var container = (Container)Activator.CreateInstance(propertyInfo.PropertyType, new object[] { dockerGateway, attributes});
                propertyInfo.SetValue(this, container);
                _containers.Add(container);

                foreach(var networkAlias in attributes.OfType<NetworkAlias>())
                {
                    dockerGateway.NetworkConnect($"{networkAlias.NetworkName}-{InstanceId}", container.Id, networkAlias.Alias);
                }
            }
        }

        public void Dispose()
        {
            foreach(var container in _containers)
            {
                container.Dispose();
            }

            foreach(var network in _networks)
            {
                network.Dispose();
            }
        }
    }
}