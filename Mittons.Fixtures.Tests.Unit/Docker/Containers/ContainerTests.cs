using Xunit;
using Moq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Containers;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public class ContainerTests
    {
        [Theory]
        [InlineData("myimage")]
        [InlineData("otherimage")]
        public void Ctor_WhenInitializedWithAnImageName_ExpectTheImageNameToBePassedToTheDockerRunCommand(string imageName)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new Container(gatewayMock.Object, imageName, string.Empty);

            // Assert
            gatewayMock.Verify(x => x.Run(imageName, string.Empty), Times.Once);
        }

        [Theory]
        [InlineData("mycommand")]
        [InlineData("othercommand")]
        public void Ctor_WhenInitializedWithACommand_ExpectTheCommandToBePassedToTheDockerRunCommand(string command)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new Container(gatewayMock.Object, string.Empty, command);

            // Assert
            gatewayMock.Verify(x => x.Run(string.Empty, command), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalled_ExpectADockerRemoveCommandToBeExecuted()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            using var container = new Container(gatewayMock.Object, string.Empty, string.Empty);

            // Act
            container.Dispose();

            // Assert
            gatewayMock.Verify(x => x.Remove(container.Id), Times.Once);
        }

        [Fact]
        public void Dispose_WhenCalledWhileAnotherContainerIsRunning_ExpectOnlyTheCalledContainerToBeRemoved()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();
            gatewayMock.Setup(x => x.Run("runningimage", string.Empty)).Returns("runningid");
            gatewayMock.Setup(x => x.Run("disposingimage", string.Empty)).Returns("disposingid");

            using var runningContainer = new Container(gatewayMock.Object, "runningimage", string.Empty);
            using var disposingContainer = new Container(gatewayMock.Object, "disposingimage", string.Empty);

            // Act
            disposingContainer.Dispose();

            // Assert
            gatewayMock.Verify(x => x.Remove(disposingContainer.Id), Times.Once);
            gatewayMock.Verify(x => x.Remove(runningContainer.Id), Times.Never);
        }
    }
}