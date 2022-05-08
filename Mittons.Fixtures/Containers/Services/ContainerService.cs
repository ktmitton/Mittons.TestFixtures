using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Containers.Attributes;
using Mittons.Fixtures.Containers.Gateways;
using Mittons.Fixtures.Containers.Resources;
using Mittons.Fixtures.Core.Attributes;
using Mittons.Fixtures.Core.Resources;

namespace Mittons.Fixtures.Containers.Services
{
    public class ContainerService : IContainerService
    {
        public IEnumerable<IResource> Resources { get; private set; }

        public IEnumerable<IResourceAdapter> ResourceAdapters { get; private set; }

        public string ServiceId { get; private set; }

        private readonly IContainerGateway _containerGateway;

        private bool _teardownOnDispose;

        public ContainerService(IContainerGateway containerGateway)
        {
            _containerGateway = containerGateway;
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(_teardownOnDispose ? _containerGateway.RemoveContainerAsync(ServiceId, CancellationToken.None) : Task.CompletedTask);
        }

        public async Task InitializeAsync(IEnumerable<Attribute> attributes, CancellationToken cancellationToken)
        {
            var run = attributes.OfType<RunAttribute>().Single();

            _teardownOnDispose = run.TeardownOnComplete;

            var image = attributes.OfType<ImageAttribute>().Single();

            var command = attributes.OfType<CommandAttribute>().SingleOrDefault();

            var healthCheckDescription = attributes.OfType<IHealthCheckDescription>().SingleOrDefault();

            ServiceId = await _containerGateway.CreateContainerAsync(
                    image.Name,
                    image.PullOption,
                    new Dictionary<string, string>
                    {
                        { "mittons.fixtures.run.id", run.Id }
                    },
                    command?.Value,
                    healthCheckDescription,
                    cancellationToken
                ).ConfigureAwait(false);

            await _containerGateway.EnsureContainerIsHealthyAsync(ServiceId, cancellationToken).ConfigureAwait(false);

            Resources = await _containerGateway.GetAvailableResourcesAsync(ServiceId, cancellationToken).ConfigureAwait(false);

            IEnumerable<IResourceAdapter> fileAdapters = Resources.Where(x => x.GuestUri.AbsolutePath.Last() != '/').Select(x => new FileResourceAdapter(x, _containerGateway)).ToList();
            IEnumerable<IResourceAdapter> directoryAdapters = Resources.Where(x => x.GuestUri.AbsolutePath.Last() == '/').Select(x => new DirectoryResourceAdapter(x, _containerGateway)).ToList();

            ResourceAdapters = fileAdapters.Concat(directoryAdapters);

            var networkAliases = attributes.OfType<NetworkAliasAttribute>();

            foreach (var alias in networkAliases)
            {
                await alias.NetworkService.ConnectAsync(alias, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
