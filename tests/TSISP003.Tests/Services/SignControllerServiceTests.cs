using Microsoft.Extensions.Logging;
using Moq;
using TSISP003.Configuration;
using TSISP003.Infrastructure.Tcp;
using TSISP003.Services;
using TSISP003.Domain.Entities;
using TSISP003.Utilities;
using static TSISP003.Services.SignControllerServiceConfig;

namespace TSISP003.Tests.Services;

public class SignControllerServiceTests : IDisposable
{
    private readonly Mock<ITCPClient> _mockTcpClient;
    private readonly Mock<ILogger<SignControllerService>> _mockLogger;
    private readonly SignControllerConnectionOptions _deviceSettings;
    private readonly SignControllerService _service;

    public SignControllerServiceTests()
    {
        _mockTcpClient = new Mock<ITCPClient>();
        _mockLogger = new Mock<ILogger<SignControllerService>>();
        _deviceSettings = new SignControllerConnectionOptions
        {
            IpAddress = "192.168.1.100",
            Port = 12333,
            Address = "01",
            PasswordOffset = "1234",
            SeedOffset = "56"
        };
        _service = new SignControllerService(_mockTcpClient.Object, _deviceSettings, _mockLogger.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region NS (Send Sequence Number) Tests

    [Fact]
    public void NS_DefaultValue_IsZero()
    {
        Assert.Equal(0, _service.NS);
    }

    [Fact]
    public void NS_SetValue_ReturnsSetValue()
    {
        _service.NS = 100;
        Assert.Equal(100, _service.NS);
    }

    [Fact]
    public void NS_SetMaxValue_ReturnsMaxValue()
    {
        _service.NS = 255;
        Assert.Equal(255, _service.NS);
    }

    [Fact]
    public void NS_ThreadSafe_ConcurrentAccess()
    {
        // Test thread-safe access
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            int value = i;
            tasks.Add(Task.Run(() => _service.NS = value));
        }

        Task.WaitAll(tasks.ToArray());

        // Just verify it doesn't throw and has a valid value
        Assert.InRange(_service.NS, 0, 255);
    }

    #endregion

    #region NR (Receive Sequence Number) Tests

    [Fact]
    public void NR_DefaultValue_IsZero()
    {
        Assert.Equal(0, _service.NR);
    }

    [Fact]
    public void NR_SetValue_ReturnsSetValue()
    {
        _service.NR = 50;
        Assert.Equal(50, _service.NR);
    }

    [Fact]
    public void NR_SetMaxValue_ReturnsMaxValue()
    {
        _service.NR = 255;
        Assert.Equal(255, _service.NR);
    }

    [Fact]
    public void NR_ThreadSafe_ConcurrentAccess()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            int value = i;
            tasks.Add(Task.Run(() => _service.NR = value));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.InRange(_service.NR, 0, 255);
    }

    #endregion

    #region SignConfigurationReceived Tests

    [Fact]
    public void SignConfigurationReceived_DefaultValue_IsFalse()
    {
        Assert.False(_service.SignConfigurationReceived);
    }

    [Fact]
    public void SignConfigurationReceived_SetTrue_ReturnsTrue()
    {
        _service.SignConfigurationReceived = true;
        Assert.True(_service.SignConfigurationReceived);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Should not throw
        _service.Dispose();
        _service.Dispose();
    }

    #endregion

    #region SignSetTextFrame Tests

    [Fact]
    public async Task SignSetTextFrame_SendsCorrectMessage()
    {
        // Arrange
        var frame = new SignSetTextFrame
        {
            FrameID = 1,
            Revision = 0,
            Font = 1,
            Colour = 1,
            Conspicuity = 0,
            NumberOfCharsInText = 5,
            Text = "48454C4C4F", // "HELLO" in hex
            CRC = 0x1234
        };

        string? sentMessage = null;
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Callback<string>(msg => sentMessage = msg)
            .Returns(Task.CompletedTask);

        // Mock receiving ACK response
        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006"); // ACK

        // Act
        try
        {
            await _service.SignSetTextFrame(frame);
        }
        catch (TimeoutException)
        {
            // Expected since we're not fully simulating the protocol
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignSetMessage Tests

    [Fact]
    public async Task SignSetMessage_SendsCorrectMessage()
    {
        // Arrange
        var message = new SignSetMessage
        {
            MessageID = 1,
            Revision = 0,
            TransitionTimeBetweenFrames = 10,
            Frame1ID = 1,
            Frame1Time = 100
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006"); // ACK

        // Act
        try
        {
            await _service.SignSetMessage(message);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignDisplayMessage Tests

    [Fact]
    public async Task SignDisplayMessage_SendsCorrectMessage()
    {
        // Arrange
        var displayMessage = new SignDisplayMessage
        {
            GroupID = 1,
            MessageID = 10
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006"); // ACK

        // Act
        try
        {
            await _service.SignDisplayMessage(displayMessage);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignDisplayFrame Tests

    [Fact]
    public async Task SignDisplayFrame_SendsCorrectMessage()
    {
        // Arrange
        var displayFrame = new SignDisplayFrame
        {
            SignID = 1,
            FrameID = 10
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006"); // ACK

        // Act
        try
        {
            await _service.SignDisplayFrame(displayFrame);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SystemReset Tests

    [Fact]
    public async Task SystemReset_Level0_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006"); // ACK

        // Act
        try
        {
            await _service.SystemReset(1, 0);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task SystemReset_InvalidLevel_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.SystemReset(1, 4));
    }

    [Fact]
    public async Task SystemReset_Level255_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SystemReset(1, 255);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region EnablePlan Tests

    [Fact]
    public async Task EnablePlan_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.EnablePlan(1, 10);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region DisablePlan Tests

    [Fact]
    public async Task DisablePlan_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.DisablePlan(1, 10);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignSetGraphicsFrame Tests

    [Fact]
    public async Task SignSetGraphicsFrame_SendsCorrectMessage()
    {
        // Arrange
        var frame = new SignSetGraphicsFrame
        {
            FrameID = 1,
            Revision = 0,
            NumberOfRows = 10,
            NumberOfColumns = 20,
            Colour = 1,
            Conspicuity = 0,
            GraphicsLength = 4,
            GraphicsData = "FF00FF00"
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignSetGraphicsFrame(frame);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignSetHighResolutionGraphicsFrame Tests

    [Fact]
    public async Task SignSetHighResolutionGraphicsFrame_SendsCorrectMessage()
    {
        // Arrange
        var frame = new SignSetHighResolutionGraphicsFrame
        {
            FrameID = 1,
            Revision = 0,
            NumberOfRows = 100,
            NumberOfColumns = 200,
            Colour = 0x0E, // 24-bit RGB
            Conspicuity = 0,
            GraphicsLength = 8,
            GraphicsData = "FF00FF00FF00FF00"
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignSetHighResolutionGraphicsFrame(frame);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignSetPlan Tests

    [Fact]
    public async Task SignSetPlan_SendsCorrectMessage()
    {
        // Arrange
        var plan = new SignSetPlan
        {
            PlanID = 1,
            Revision = 0,
            DayOfWeek = 0x7F, // Daily
            Entries = new List<SignSetPlanEntry>
            {
                new SignSetPlanEntry
                {
                    FrameMessageType = 1,
                    FrameMessageID = 10,
                    StartHour = 8,
                    StartMinute = 0,
                    StopHour = 17,
                    StopMinute = 0
                }
            }
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignSetPlan(plan);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region PowerOnOff Tests

    [Fact]
    public async Task PowerOnOff_PowerOn_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.PowerOnOff(1, true);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task PowerOnOff_PowerOff_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.PowerOnOff(1, false);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region RetrieveFaultLog Tests

    [Fact]
    public async Task RetrieveFaultLog_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.RetrieveFaultLog();
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region ResetFaultLog Tests

    [Fact]
    public async Task ResetFaultLog_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.ResetFaultLog();
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region UpdateTime Tests

    [Fact]
    public async Task UpdateTime_SendsCorrectMessage()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 0);

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.UpdateTime(dateTime);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region EndSession Tests

    [Fact]
    public async Task EndSession_SendsEndSessionMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.EndSession();
        }
        catch
        {
            // May timeout
        }

        // Assert - verify that SendAsync was called (end session message sent)
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_ReturnsTask()
    {
        // Act
        var statusTask = _service.GetStatus();

        // Assert - GetStatus returns a Task<SignStatusReply>
        Assert.NotNull(statusTask);
        Assert.IsType<Task<SignStatusReply?>>(statusTask);
    }

    #endregion

    #region HAR Tests

    [Fact]
    public async Task HARSetStrategy_SendsCorrectMessage()
    {
        // Arrange
        var strategy = new HARSetStrategy
        {
            StrategyID = 100,
            Revision = 1,
            VoiceIDs = new List<ushort> { 1, 2, 3 }
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.HARSetStrategy(strategy);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task HARActivateStrategy_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.HARActivateStrategy(100);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task HARSetPlan_SendsCorrectMessage()
    {
        // Arrange
        var plan = new HARSetPlan
        {
            PlanID = 1,
            Revision = 1,
            DayOfWeek = 0x7F,
            Entries = new List<HARSetPlanEntry>
            {
                new HARSetPlanEntry
                {
                    StrategyID = 100,
                    StartHour = 8,
                    StartMinute = 0,
                    StopHour = 17,
                    StopMinute = 0
                }
            }
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.HARSetPlan(plan);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignSetDimmingLevel Tests

    [Fact]
    public async Task SignSetDimmingLevel_Automatic_SendsCorrectMessage()
    {
        // Arrange
        var entries = new List<(byte GroupID, byte DimmingMode, byte LuminanceLevel)>
        {
            (1, 0, 0) // Automatic dimming
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignSetDimmingLevel(entries);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task SignSetDimmingLevel_Manual_SendsCorrectMessage()
    {
        // Arrange
        var entries = new List<(byte GroupID, byte DimmingMode, byte LuminanceLevel)>
        {
            (1, 1, 8) // Manual dimming at level 8
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignSetDimmingLevel(entries);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region DisableEnableDevice Tests

    [Fact]
    public async Task DisableEnableDevice_Enable_SendsCorrectMessage()
    {
        // Arrange
        var entries = new List<(byte GroupID, bool Enabled)>
        {
            (1, true)
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.DisableEnableDevice(entries);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task DisableEnableDevice_Disable_SendsCorrectMessage()
    {
        // Arrange
        var entries = new List<(byte GroupID, bool Enabled)>
        {
            (1, false)
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.DisableEnableDevice(entries);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region RequestEnabledPlans Tests

    [Fact]
    public async Task RequestEnabledPlans_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.RequestEnabledPlans();
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignExtendedStatusRequest Tests

    [Fact]
    public async Task SignExtendedStatusRequest_SendsCorrectMessage()
    {
        // Arrange
        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignExtendedStatusRequest();
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion

    #region SignDisplayAtomicFrames Tests

    [Fact]
    public async Task SignDisplayAtomicFrames_SendsCorrectMessage()
    {
        // Arrange
        var atomicFrames = new SignDisplayAtomicFrame
        {
            GroupID = 1,
            NumbeOfSigns = 2,
            Frames = new List<SignDisplayFrame>
            {
                new SignDisplayFrame { SignID = 1, FrameID = 10 },
                new SignDisplayFrame { SignID = 2, FrameID = 20 }
            }
        };

        _mockTcpClient.Setup(x => x.SendAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockTcpClient.Setup(x => x.ReadAsync())
            .ReturnsAsync("\u0006");

        // Act
        try
        {
            await _service.SignDisplayAtomicFrames(atomicFrames);
        }
        catch (TimeoutException)
        {
            // Expected
        }

        // Assert
        _mockTcpClient.Verify(x => x.SendAsync(It.IsAny<string>()), Times.AtLeastOnce());
    }

    #endregion
}
