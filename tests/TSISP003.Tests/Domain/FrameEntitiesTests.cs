using TSISP003.Domain.Entities;

namespace TSISP003.Tests.Domain;

public class FrameEntitiesTests
{
    #region Frame (Base Class) Tests

    [Fact]
    public void Frame_DefaultValues()
    {
        // Act
        var frame = new Frame();

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.FrameRevision);
        Assert.Equal(0, frame.Color);
        Assert.Equal(0, frame.Conspicuity);
    }

    [Fact]
    public void Frame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new Frame
        {
            FrameID = 1,
            FrameRevision = 2,
            Color = 3,
            Conspicuity = 4
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(2, frame.FrameRevision);
        Assert.Equal(3, frame.Color);
        Assert.Equal(4, frame.Conspicuity);
    }

    [Fact]
    public void Frame_FrameID_MaxValue()
    {
        // Arrange & Act
        var frame = new Frame { FrameID = byte.MaxValue };

        // Assert
        Assert.Equal(255, frame.FrameID);
    }

    [Fact]
    public void Frame_FrameRevision_MaxValue()
    {
        // Arrange & Act
        var frame = new Frame { FrameRevision = byte.MaxValue };

        // Assert
        Assert.Equal(255, frame.FrameRevision);
    }

    #endregion

    #region TextFrame Tests

    [Fact]
    public void TextFrame_DefaultValues()
    {
        // Act
        var frame = new TextFrame();

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.FrameRevision);
        Assert.Equal(0, frame.Color);
        Assert.Equal(0, frame.Conspicuity);
        Assert.Equal(0, frame.Font);
        Assert.Equal(string.Empty, frame.Text);
    }

    [Fact]
    public void TextFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new TextFrame
        {
            FrameID = 1,
            FrameRevision = 2,
            Color = 3,
            Conspicuity = 4,
            Font = 5,
            Text = "Hello World"
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(2, frame.FrameRevision);
        Assert.Equal(3, frame.Color);
        Assert.Equal(4, frame.Conspicuity);
        Assert.Equal(5, frame.Font);
        Assert.Equal("Hello World", frame.Text);
    }

    [Fact]
    public void TextFrame_InheritsFromFrame()
    {
        // Arrange
        var frame = new TextFrame();

        // Assert
        Assert.IsAssignableFrom<Frame>(frame);
    }

    [Fact]
    public void TextFrame_EmptyText()
    {
        // Arrange & Act
        var frame = new TextFrame { Text = string.Empty };

        // Assert
        Assert.Equal(string.Empty, frame.Text);
    }

    [Fact]
    public void TextFrame_LongText()
    {
        // Arrange
        var longText = new string('A', 1000);

        // Act
        var frame = new TextFrame { Text = longText };

        // Assert
        Assert.Equal(longText, frame.Text);
        Assert.Equal(1000, frame.Text.Length);
    }

    [Fact]
    public void TextFrame_Font_MaxValue()
    {
        // Arrange & Act
        var frame = new TextFrame { Font = byte.MaxValue };

        // Assert
        Assert.Equal(255, frame.Font);
    }

    #endregion

    #region GraphicFrame Tests

    [Fact]
    public void GraphicFrame_DefaultValues()
    {
        // Act
        var frame = new GraphicFrame();

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.FrameRevision);
        Assert.Equal(0, frame.Color);
        Assert.Equal(0, frame.Conspicuity);
        Assert.Equal((ushort)0, frame.NumberOfRows);
        Assert.Equal((ushort)0, frame.NumberOfColumns);
        Assert.Equal(string.Empty, frame.Graphic);
    }

    [Fact]
    public void GraphicFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new GraphicFrame
        {
            FrameID = 1,
            FrameRevision = 2,
            Color = 3,
            Conspicuity = 4,
            NumberOfRows = 100,
            NumberOfColumns = 200,
            Graphic = "FF00FF00"
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(2, frame.FrameRevision);
        Assert.Equal(3, frame.Color);
        Assert.Equal(4, frame.Conspicuity);
        Assert.Equal((ushort)100, frame.NumberOfRows);
        Assert.Equal((ushort)200, frame.NumberOfColumns);
        Assert.Equal("FF00FF00", frame.Graphic);
    }

    [Fact]
    public void GraphicFrame_InheritsFromFrame()
    {
        // Arrange
        var frame = new GraphicFrame();

        // Assert
        Assert.IsAssignableFrom<Frame>(frame);
    }

    [Fact]
    public void GraphicFrame_MaxDimensions()
    {
        // Arrange & Act
        var frame = new GraphicFrame
        {
            NumberOfRows = ushort.MaxValue,
            NumberOfColumns = ushort.MaxValue
        };

        // Assert
        Assert.Equal(ushort.MaxValue, frame.NumberOfRows);
        Assert.Equal(ushort.MaxValue, frame.NumberOfColumns);
    }

    [Fact]
    public void GraphicFrame_LargeGraphicData()
    {
        // Arrange
        var largeGraphic = new string('F', 10000);

        // Act
        var frame = new GraphicFrame { Graphic = largeGraphic };

        // Assert
        Assert.Equal(largeGraphic, frame.Graphic);
    }

    #endregion

    #region HighResolutionGraphicFrame Tests

    [Fact]
    public void HighResolutionGraphicFrame_DefaultValues()
    {
        // Act
        var frame = new HighResolutionGraphicFrame();

        // Assert
        Assert.Equal(0, frame.FrameID);
        Assert.Equal(0, frame.FrameRevision);
        Assert.Equal(0, frame.Color);
        Assert.Equal(0, frame.Conspicuity);
        Assert.Equal(0, frame.NumberOfRows);
        Assert.Equal(0, frame.NumberOfColumns);
        Assert.Equal(string.Empty, frame.Graphic);
    }

    [Fact]
    public void HighResolutionGraphicFrame_SetAllProperties()
    {
        // Arrange & Act
        var frame = new HighResolutionGraphicFrame
        {
            FrameID = 1,
            FrameRevision = 2,
            Color = 3,
            Conspicuity = 4,
            NumberOfRows = 50,
            NumberOfColumns = 100,
            Graphic = "AABBCCDD"
        };

        // Assert
        Assert.Equal(1, frame.FrameID);
        Assert.Equal(2, frame.FrameRevision);
        Assert.Equal(3, frame.Color);
        Assert.Equal(4, frame.Conspicuity);
        Assert.Equal(50, frame.NumberOfRows);
        Assert.Equal(100, frame.NumberOfColumns);
        Assert.Equal("AABBCCDD", frame.Graphic);
    }

    [Fact]
    public void HighResolutionGraphicFrame_InheritsFromFrame()
    {
        // Arrange
        var frame = new HighResolutionGraphicFrame();

        // Assert
        Assert.IsAssignableFrom<Frame>(frame);
    }

    [Fact]
    public void HighResolutionGraphicFrame_MaxDimensions()
    {
        // Arrange & Act - Note: HighResolutionGraphicFrame uses byte for dimensions
        var frame = new HighResolutionGraphicFrame
        {
            NumberOfRows = byte.MaxValue,
            NumberOfColumns = byte.MaxValue
        };

        // Assert
        Assert.Equal(byte.MaxValue, frame.NumberOfRows);
        Assert.Equal(byte.MaxValue, frame.NumberOfColumns);
    }

    [Fact]
    public void HighResolutionGraphicFrame_EmptyGraphic()
    {
        // Arrange & Act
        var frame = new HighResolutionGraphicFrame { Graphic = string.Empty };

        // Assert
        Assert.Equal(string.Empty, frame.Graphic);
    }

    #endregion
}
