using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mittons.Fixtures.Docker.Attributes;
using Mittons.Fixtures.Docker.Containers;
using Mittons.Fixtures.Docker.Gateways;
using Moq;
using Xunit;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers;

public class ContainerTests : BaseContainerTests
{
    public class InitializeTests
    {
        public class ImageTests : BaseContainerTests
        {
            [Theory]
            [InlineData("myimage")]
            [InlineData("otherimage")]
            public async Task InitializeAsync_WhenInitializedWithAnImageName_ExpectTheImageNameToBePassedToTheDockerRunCommand(string imageName)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(imageName), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(x => x.RunAsync(imageName, string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        public class CommandTests : BaseContainerTests
        {
            [Theory]
            [InlineData("mycommand")]
            [InlineData("othercommand")]
            public async Task InitializeAsync_WhenInitializedWithACommand_ExpectTheCommandToBePassedToTheDockerRunCommand(string command)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(command), new RunAttribute() });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(x => x.RunAsync(string.Empty, command, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        public class NetworkTests : BaseContainerTests
        {
            [Theory]
            [InlineData("192.168.0.0")]
            [InlineData("192.168.0.1")]
            [InlineData("127.0.0.1")]
            public async Task InitializeAsync_WhenCreated_ExpectTheDefaultIpAddressToBeSet(string ipAddress)
            {
                // Arrange
                var parsed = IPAddress.Parse(ipAddress);

                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetDefaultNetworkIpAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(parsed);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                Assert.Equal(parsed, container.IpAddress);
            }
        }

        public class LabelTests : BaseContainerTests
        {
            [Fact]
            public async Task InitializeAsync_WhenCreatedWithARun_ExpectLabelsToBePassedToTheGateway()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var run = new RunAttribute();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), run });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--label" && y.Value == $"mittons.fixtures.run.id={run.Id}")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }
        }

        public class HealthTests : BaseContainerTests
        {
            [Theory]
            [InlineData(HealthStatus.Healthy)]
            [InlineData(HealthStatus.Running)]
            public async Task InitializeAsync_WhenContainerHealthIsPassing_ExpectInitializationToComplete(HealthStatus status)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(status);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                // Assert
                await container.InitializeAsync(CancellationToken.None);
            }

            [Fact]
            public async Task InitializeAsync_WhenContainerBecomesHealthy_ExpectContainerIdToBeReturned()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.SetupSequence(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Unknown)
                    .ReturnsAsync(HealthStatus.Unknown)
                    .ReturnsAsync(HealthStatus.Healthy);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                var stopwatch = new Stopwatch();

                // Act
                stopwatch.Start();

                await container.InitializeAsync(CancellationToken.None);

                stopwatch.Stop();

                // Assert
                Assert.True(stopwatch.Elapsed > TimeSpan.FromMilliseconds(50));
            }

            [Theory]
            [InlineData(HealthStatus.Unknown)]
            [InlineData(HealthStatus.Unhealthy)]
            public async Task InitializeAsync_WhenContainerHealthNeverPassesBeforeProvidedTimeout_ExpectExceptionToBeThrown(HealthStatus status)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(status);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
                _containers.Add(container);

                // Act
                // Assert
                await Assert.ThrowsAsync<OperationCanceledException>(() => container.InitializeAsync(CancellationToken.None));
            }

            [Fact]
            public async Task InitializeAsync_WhenHealthChecksAreDisabled_ExpectTheDisabledFlagToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Disabled = true },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--no-healthcheck" && y.Value == string.Empty)),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Fact]
            public async Task InitializeAsync_WhenHealthChecksAreDisabledAndOtherFieldsAreSet_ExpectOnlyNoHealthCheckToBeApplied()
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute
                        {
                            Disabled = true,
                            Command = "test",
                            Interval = TimeSpan.FromSeconds(1),
                            Timeout = TimeSpan.FromSeconds(1),
                            StartPeriod = TimeSpan.FromSeconds(1),
                            Retries = 1
                        },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x =>
                                x.Any(y => y.Key == "--no-healthcheck") &&
                                !x.Any(y => y.Key == "--health-cmd") &&
                                !x.Any(y => y.Key == "--health-interval") &&
                                !x.Any(y => y.Key == "--health-timeout") &&
                                !x.Any(y => y.Key == "--health-start-period") &&
                                !x.Any(y => y.Key == "--health-retries")
                            ),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData("ps aux || exit 1")]
            [InlineData("echo hello")]
            public async Task InitializeAsync_WhenHealthCheckCommandIsSet_ExpectHealthCmdToBeApplied(string command)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Command = command },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--health-cmd" && y.Value == command)),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(250, 1)]
            [InlineData(500, 1)]
            [InlineData(750, 1)]
            [InlineData(1000, 1)]
            [InlineData(7500, 8)]
            public async Task InitializeAsync_WhenHealthCheckIntervalIsSet_ExpectHealthIntervalToBeApplied(int milliseconds, int seconds)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Interval = TimeSpan.FromMilliseconds(milliseconds) },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--health-interval" && y.Value == $"{seconds}s")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(250, 1)]
            [InlineData(500, 1)]
            [InlineData(750, 1)]
            [InlineData(1000, 1)]
            [InlineData(7500, 8)]
            public async Task InitializeAsync_WhenHealthCheckTimeoutIsSet_ExpectHealthTimeoutToBeApplied(int milliseconds, int seconds)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Timeout = TimeSpan.FromMilliseconds(milliseconds) },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--health-timeout" && y.Value == $"{seconds}s")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(250, 1)]
            [InlineData(500, 1)]
            [InlineData(750, 1)]
            [InlineData(1000, 1)]
            [InlineData(7500, 8)]
            public async Task InitializeAsync_WhenHealthCheckStartPeriodIsSet_ExpectHealthCheckStartPeriodToBeApplied(int milliseconds, int seconds)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { StartPeriod = TimeSpan.FromMilliseconds(milliseconds) },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--health-start-period" && y.Value == $"{seconds}s")),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }

            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            [InlineData(20)]
            public async Task InitializeAsync_WhenHealthRetriesIsSet_ExpectHealthRetriesToBeApplied(int retries)
            {
                // Arrange
                var containerGatewayMock = new Mock<IContainerGateway>();
                containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(HealthStatus.Running);

                var networkGatewayMock = new Mock<INetworkGateway>();

                var container = new Container(
                    containerGatewayMock.Object,
                    networkGatewayMock.Object,
                    Guid.Empty,
                    new Attribute[]
                    {
                        new ImageAttribute(string.Empty),
                        new CommandAttribute(string.Empty),
                        new HealthCheckAttribute { Retries = retries },
                        new RunAttribute()
                    });
                _containers.Add(container);

                // Act
                await container.InitializeAsync(CancellationToken.None);

                // Assert
                containerGatewayMock.Verify(
                        x =>
                        x.RunAsync(
                            string.Empty,
                            string.Empty,
                            It.Is<IEnumerable<KeyValuePair<string, string>>>(x => x.Any(y => y.Key == "--health-retries" && y.Value == retries.ToString())),
                            It.IsAny<CancellationToken>()
                        )
                    );
            }
        }
    }

    public class DisposeTests : BaseContainerTests
    {
        [Fact]
        public async Task DisposeAsync_WhenCalled_ExpectADockerRemoveCommandToBeExecuted()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            // Act
            await container.DisposeAsync();

            // Assert
            containerGatewayMock.Verify(x => x.RemoveAsync(container.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DisposeAsync_WhenCalledWhileAnotherContainerIsRunning_ExpectOnlyTheCalledContainerToBeRemoved()
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.RunAsync("runningimage", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("runningid");
            containerGatewayMock.Setup(x => x.RunAsync("disposingimage", string.Empty, It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("disposingid");
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var runningContainer = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute("runningimage"), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(runningContainer);
            await runningContainer.InitializeAsync(CancellationToken.None);

            var disposingContainer = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute("disposingimage"), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(disposingContainer);
            await disposingContainer.InitializeAsync(CancellationToken.None);

            // Act
            await disposingContainer.DisposeAsync();

            // Assert
            containerGatewayMock.Verify(x => x.RemoveAsync(disposingContainer.Id, It.IsAny<CancellationToken>()), Times.Once);
            containerGatewayMock.Verify(x => x.RemoveAsync(runningContainer.Id, It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    public class FileTests : BaseContainerTests
    {
        [Theory]
        [InlineData("file/one", "destination/one", "testowner", "testpermissions")]
        [InlineData("two", "two", "owner", "permissions")]
        public async Task AddFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string hostFilename, string containerFilename, string owner, string permissions)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            var cancellationToken = new CancellationToken();

            // Act
            await container.AddFileAsync(hostFilename, containerFilename, owner, permissions, cancellationToken);

            // Assert
            containerGatewayMock.Verify(x => x.AddFileAsync(container.Id, hostFilename, containerFilename, owner, permissions, cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData("destination/one")]
        [InlineData("two")]
        public async Task RemoveFile_WhenCalled_ExpectDetailsToBeForwardedToTheGateway(string containerFilename)
        {
            // Arrange
            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            // Act
            await container.RemoveFileAsync(containerFilename, CancellationToken.None);

            // Assert
            containerGatewayMock.Verify(x => x.RemoveFileAsync(container.Id, containerFilename, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("destination/one", "testowner", "testpermissions")]
        [InlineData("two", "owner", "permissions")]
        public async Task CreateFile_WhenCalledWithAString_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            var actualFilename = default(string);
            var actualContents = default(string);

            containerGatewayMock.Setup(x => x.AddFileAsync(container.Id, It.IsAny<string>(), containerFilename, owner, permissions, It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, string, CancellationToken>((_, hostFilename, _, _, _, _) =>
                {
                    actualFilename = hostFilename;
                    actualContents = File.ReadAllText(hostFilename);
                });

            // Act
            await container.CreateFileAsync(fileContents, containerFilename, owner, permissions, CancellationToken.None);

            // Assert
            Assert.Equal(Path.GetDirectoryName(Path.GetTempPath()), Path.GetDirectoryName(actualFilename));
            Assert.False(File.Exists(actualFilename));
            Assert.Equal(fileContents, actualContents);
        }

        [Theory]
        [InlineData("destination/one", "testowner", "testpermissions")]
        [InlineData("two", "owner", "permissions")]
        public async Task CreateFile_WhenCalledWithAStream_ExpectGatewayToBeCalledWithATemporaryFile(string containerFilename, string owner, string permissions)
        {
            // Arrange
            var fileContents = Guid.NewGuid().ToString();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));

            var containerGatewayMock = new Mock<IContainerGateway>();
            containerGatewayMock.Setup(x => x.GetHealthStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthStatus.Healthy);

            var networkGatewayMock = new Mock<INetworkGateway>();

            var container = new Container(containerGatewayMock.Object, networkGatewayMock.Object, Guid.Empty, new Attribute[] { new ImageAttribute(string.Empty), new CommandAttribute(string.Empty), new RunAttribute() });
            _containers.Add(container);

            var actualFilename = default(string);
            var actualContents = default(string);

            containerGatewayMock.Setup(x => x.AddFileAsync(container.Id, It.IsAny<string>(), containerFilename, owner, permissions, It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, string, CancellationToken>((_, hostFilename, _, _, _, _) =>
                {
                    actualFilename = hostFilename;
                    actualContents = File.ReadAllText(hostFilename);
                });

            // Act
            await container.CreateFileAsync(stream, containerFilename, owner, permissions, CancellationToken.None);

            // Assert
            Assert.Equal(Path.GetDirectoryName(Path.GetTempPath()), Path.GetDirectoryName(actualFilename));
            Assert.False(File.Exists(actualFilename));
            Assert.Equal(fileContents, actualContents);
        }
    }
}
