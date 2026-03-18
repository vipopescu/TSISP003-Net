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
        Assert.Equal(0, message.FrameID1);
        Assert.Equal(0, message.FrameTime1);
        Assert.Equal(0, message.FrameID2);
        Assert.Equal(0, message.FrameTime2);
        Assert.Equal(0, message.FrameID3);
        Assert.Equal(0, message.FrameTime3);
        Assert.Equal(0, message.FrameID4);
        Assert.Equal(0, message.FrameTime4);
        Assert.Equal(0, message.FrameID5);
        Assert.Equal(0, message.FrameTime5);
        Assert.Equal(0, message.FrameID16);
        Assert.Equal(0, message.FrameTime6);
    }

    [Fact]
    public void Message_SetAllProperties()
    {
        // Arrange & Act
        var message = new Message
        {
            FrameID1 = 11,
            FrameTime1 = 100,
            FrameID2 = 12,
            FrameTime2 = 110,
            FrameID3 = 13,
            FrameTime3 = 120,
            FrameID4 = 14,
            FrameTime4 = 130,
            FrameID5 = 15,
            FrameTime5 = 140,
            FrameID16 = 16,
            FrameTime6 = 150
        };

        // Assert
        Assert.Equal(11, message.FrameID1);
        Assert.Equal(100, message.FrameTime1);
        Assert.Equal(12, message.FrameID2);
        Assert.Equal(110, message.FrameTime2);
        Assert.Equal(13, message.FrameID3);
        Assert.Equal(120, message.FrameTime3);
        Assert.Equal(14, message.FrameID4);
        Assert.Equal(130, message.FrameTime4);
        Assert.Equal(15, message.FrameID5);
        Assert.Equal(140, message.FrameTime5);
        Assert.Equal(16, message.FrameID16);
        Assert.Equal(150, message.FrameTime6);
    }

    [Fact]
    public void Message_FrameTimings_MaxValue()
    {
        // Arrange & Act
        var message = new Message
        {
            FrameTime1 = byte.MaxValue,
            FrameTime2 = byte.MaxValue,
            FrameTime3 = byte.MaxValue,
            FrameTime4 = byte.MaxValue,
            FrameTime5 = byte.MaxValue,
            FrameTime6 = byte.MaxValue
        };

        // Assert
        Assert.Equal(255, message.FrameTime1);
        Assert.Equal(255, message.FrameTime2);
        Assert.Equal(255, message.FrameTime3);
        Assert.Equal(255, message.FrameTime4);
        Assert.Equal(255, message.FrameTime5);
        Assert.Equal(255, message.FrameTime6);
    }

    [Fact]
    public void Message_SingleFrameSequence()
    {
        // Arrange & Act - A message with only one frame
        var message = new Message
        {
            FrameID1 = 10,
            FrameTime1 = 50
        };

        // Assert
        Assert.Equal(10, message.FrameID1);
        Assert.Equal(50, message.FrameTime1);
        Assert.Equal(0, message.FrameID2); // Remaining frames should be 0
    }

    [Fact]
    public void Message_MultiFrameSequence()
    {
        // Arrange & Act - A message with multiple frames
        var message = new Message
        {
            FrameID1 = 10,
            FrameTime1 = 30,
            FrameID2 = 20,
            FrameTime2 = 30,
            FrameID3 = 30,
            FrameTime3 = 30
        };

        // Assert
        Assert.Equal(10, message.FrameID1);
        Assert.Equal(20, message.FrameID2);
        Assert.Equal(30, message.FrameID3);
    }
}
