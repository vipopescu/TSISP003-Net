using TSISP003.Domain.Entities;

namespace TSISP003.Tests.Domain;

public class MessageTests
{
    [Fact]
    public void Message_DefaultValues()
    {
        // Act
        var message = new Message();

        // Assert
        Assert.Equal(0, message.MessageID);
        Assert.Equal(0, message.MessageRevision);
        Assert.Equal(0, message.TransitionTime);
        Assert.Equal(0, message.FrameID1);
        Assert.Equal(0, message.FrameID1Time);
        Assert.Equal(0, message.FrameID2);
        Assert.Equal(0, message.FrameID2Time);
        Assert.Equal(0, message.FrameID3);
        Assert.Equal(0, message.FrameID3Time);
        Assert.Equal(0, message.FrameID4);
        Assert.Equal(0, message.FrameID4Time);
        Assert.Equal(0, message.FrameID5);
        Assert.Equal(0, message.FrameID5Time);
        Assert.Equal(0, message.FrameID16);
        Assert.Equal(0, message.FrameID16Time);
    }

    [Fact]
    public void Message_SetAllProperties()
    {
        // Arrange & Act
        var message = new Message
        {
            MessageID = 1,
            MessageRevision = 2,
            TransitionTime = 10,
            FrameID1 = 11,
            FrameID1Time = 100,
            FrameID2 = 12,
            FrameID2Time = 110,
            FrameID3 = 13,
            FrameID3Time = 120,
            FrameID4 = 14,
            FrameID4Time = 130,
            FrameID5 = 15,
            FrameID5Time = 140,
            FrameID16 = 16,
            FrameID16Time = 150
        };

        // Assert
        Assert.Equal(1, message.MessageID);
        Assert.Equal(2, message.MessageRevision);
        Assert.Equal(10, message.TransitionTime);
        Assert.Equal(11, message.FrameID1);
        Assert.Equal(100, message.FrameID1Time);
        Assert.Equal(12, message.FrameID2);
        Assert.Equal(110, message.FrameID2Time);
        Assert.Equal(13, message.FrameID3);
        Assert.Equal(120, message.FrameID3Time);
        Assert.Equal(14, message.FrameID4);
        Assert.Equal(130, message.FrameID4Time);
        Assert.Equal(15, message.FrameID5);
        Assert.Equal(140, message.FrameID5Time);
        Assert.Equal(16, message.FrameID16);
        Assert.Equal(150, message.FrameID16Time);
    }

    [Fact]
    public void Message_MessageID_MaxValue()
    {
        // Arrange & Act
        var message = new Message { MessageID = byte.MaxValue };

        // Assert
        Assert.Equal(255, message.MessageID);
    }

    [Fact]
    public void Message_MessageRevision_MaxValue()
    {
        // Arrange & Act
        var message = new Message { MessageRevision = byte.MaxValue };

        // Assert
        Assert.Equal(255, message.MessageRevision);
    }

    [Fact]
    public void Message_TransitionTime_MaxValue()
    {
        // Arrange & Act
        var message = new Message { TransitionTime = byte.MaxValue };

        // Assert
        Assert.Equal(255, message.TransitionTime);
    }

    [Fact]
    public void Message_FrameTimings_MaxValue()
    {
        // Arrange & Act
        var message = new Message
        {
            FrameID1Time = byte.MaxValue,
            FrameID2Time = byte.MaxValue,
            FrameID3Time = byte.MaxValue,
            FrameID4Time = byte.MaxValue,
            FrameID5Time = byte.MaxValue,
            FrameID16Time = byte.MaxValue
        };

        // Assert
        Assert.Equal(255, message.FrameID1Time);
        Assert.Equal(255, message.FrameID2Time);
        Assert.Equal(255, message.FrameID3Time);
        Assert.Equal(255, message.FrameID4Time);
        Assert.Equal(255, message.FrameID5Time);
        Assert.Equal(255, message.FrameID16Time);
    }

    [Fact]
    public void Message_SingleFrameSequence()
    {
        // Arrange & Act - A message with only one frame
        var message = new Message
        {
            MessageID = 1,
            FrameID1 = 10,
            FrameID1Time = 50
        };

        // Assert
        Assert.Equal(10, message.FrameID1);
        Assert.Equal(50, message.FrameID1Time);
        Assert.Equal(0, message.FrameID2); // Remaining frames should be 0
    }

    [Fact]
    public void Message_MultiFrameSequence()
    {
        // Arrange & Act - A message with multiple frames
        var message = new Message
        {
            MessageID = 5,
            TransitionTime = 5,
            FrameID1 = 10,
            FrameID1Time = 30,
            FrameID2 = 20,
            FrameID2Time = 30,
            FrameID3 = 30,
            FrameID3Time = 30
        };

        // Assert
        Assert.Equal(5, message.MessageID);
        Assert.Equal(5, message.TransitionTime);
        Assert.Equal(10, message.FrameID1);
        Assert.Equal(20, message.FrameID2);
        Assert.Equal(30, message.FrameID3);
    }
}
