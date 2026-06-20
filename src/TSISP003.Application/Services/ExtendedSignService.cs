using System.Collections.Concurrent;
using System.Text;
using TSISP003.Application.DTOs;
using TSISP003.Application.Interfaces;
using TSISP003.Application.Mapping;
using TSISP003.Domain.Entities;
using TSISP003.Domain.Enums;

namespace TSISP003.Application.Services;

/// <summary>
/// Orchestrates "extended" (non-protocol) management operations over the protocol
/// primitives exposed by <see cref="ISignControllerService"/>. Registered as a singleton
/// because it owns rolling per-device frame/message IDs that persist across requests.
/// </summary>
public class ExtendedSignService(ISignControllerServiceFactory factory) : IExtendedSignService
{
    // Rolling frame/message IDs per device, cycling 120..254 and shared across requests.
    private readonly ConcurrentDictionary<string, int> _deviceIds = new();
    private const byte MinId = 120;
    private const byte MaxId = 254;

    private byte GetNextId(string device)
        => (byte)_deviceIds.AddOrUpdate(device, MinId, (_, current) => current >= MaxId ? MinId : current + 1);

    public async Task BuildAndDisplayMessageAsync(string device, ExtendedRequestMessageDto request)
    {
        var controller = factory.GetSignControllerService(device);

        var message = new SignSetMessageDto { MessageID = GetNextId(device) };

        async Task<byte> StoreFrameAsync(ExtendedTextFrameDto frame)
        {
            var textFrame = new SignSetTextFrameDto
            {
                FrameID = GetNextId(device),
                Revision = 0,
                Font = frame.Font,
                Colour = frame.Colour,
                Conspicuity = frame.Conspicuity,
                Text = frame.Text
            };
            await controller.SignSetTextFrame(textFrame.AsEntity());
            return textFrame.FrameID;
        }

        if (request.Frame1 is not null) { message.Frame1ID = await StoreFrameAsync(request.Frame1); message.Frame1Time = (byte)(request.Frame1Time * 10); }
        if (request.Frame2 is not null) { message.Frame2ID = await StoreFrameAsync(request.Frame2); message.Frame2Time = (byte)(request.Frame2Time * 10); }
        if (request.Frame3 is not null) { message.Frame3ID = await StoreFrameAsync(request.Frame3); message.Frame3Time = (byte)(request.Frame3Time * 10); }
        if (request.Frame4 is not null) { message.Frame4ID = await StoreFrameAsync(request.Frame4); message.Frame4Time = (byte)(request.Frame4Time * 10); }
        if (request.Frame5 is not null) { message.Frame5ID = await StoreFrameAsync(request.Frame5); message.Frame5Time = (byte)(request.Frame5Time * 10); }
        if (request.Frame6 is not null) { message.Frame6ID = await StoreFrameAsync(request.Frame6); message.Frame6Time = (byte)(request.Frame6Time * 10); }

        await controller.SignSetMessage(message.AsEntity());

        await controller.SignDisplayMessage(new SignDisplayMessage
        {
            GroupID = 1,
            MessageID = message.MessageID
        });
    }

    public async Task<ExtendedStatusReplyDto?> GetExtendedStatusAsync(string device)
    {
        var controller = factory.GetSignControllerService(device);

        var status = await controller.GetStatus();
        if (status is null)
            return null;

        var baseDto = status.AsDto();

        // Resolve each distinct message/frame only once per request.
        var messageCache = new Dictionary<byte, ExtendedMessageContentDto?>();
        var frameCache = new Dictionary<byte, ExtendedFrameContentDto?>();

        var result = new ExtendedStatusReplyDto
        {
            OnlineStatus = baseDto.OnlineStatus,
            ApplicationErrorCode = baseDto.ApplicationErrorCode,
            DateTime = baseDto.dateTime,
            ControllerChecksum = baseDto.ControllerChecksum,
            ControllerErrorCode = baseDto.ControllerErrorCode,
            ControllerError = baseDto.ControllerError,
            NumberOfSigns = baseDto.NumberOfSigns
        };

        foreach (var (signId, sign) in baseDto.Signs)
        {
            var enriched = new ExtendedSignStatusContentDto
            {
                SignID = sign.SignID,
                SignErrorCode = sign.SignErrorCode,
                SignError = sign.SignError,
                SignEnabled = sign.SignEnabled,
                FrameID = sign.FrameID,
                FrameRevision = sign.FrameRevision,
                MessageID = sign.MessageID,
                MessageRevision = sign.MessageRevision,
                PlanID = sign.PlanID,
                PlanRevision = sign.PlanRevision
            };

            if (sign.MessageID != 0)
                enriched.ActiveMessage = await ResolveMessageAsync(controller, sign.MessageID, messageCache, frameCache);

            if (sign.FrameID != 0)
                enriched.ActiveFrame = await ResolveFrameAsync(controller, sign.FrameID, frameCache);

            result.Signs[signId] = enriched;
        }

        return result;
    }

    private async Task<ExtendedMessageContentDto?> ResolveMessageAsync(
        ISignControllerService controller, byte messageId,
        Dictionary<byte, ExtendedMessageContentDto?> messageCache,
        Dictionary<byte, ExtendedFrameContentDto?> frameCache)
    {
        if (messageCache.TryGetValue(messageId, out var cached))
            return cached;

        ExtendedMessageContentDto? content = null;
        try
        {
            if (await controller.SignRequestStoredFrameMessagePlan(RequestType.Message, messageId) is SignSetMessage msg)
            {
                content = new ExtendedMessageContentDto
                {
                    MessageID = msg.MessageID,
                    Revision = msg.Revision,
                    TransitionTimeBetweenFrames = msg.TransitionTimeBetweenFrames
                };

                foreach (var (frameId, time) in EnumerateFrames(msg))
                {
                    if (frameId == 0)
                        continue;

                    var frame = await ResolveFrameAsync(controller, frameId, frameCache);
                    content.Frames.Add(new ExtendedMessageFrameDto
                    {
                        FrameID = frameId,
                        Time = time,
                        Font = frame?.Font ?? 0,
                        Colour = frame?.Colour ?? 0,
                        Conspicuity = frame?.Conspicuity ?? 0,
                        Text = frame?.Text
                    });
                }
            }
        }
        catch
        {
            // Not stored / timed out / rejected — leave content null.
        }

        messageCache[messageId] = content;
        return content;
    }

    private async Task<ExtendedFrameContentDto?> ResolveFrameAsync(
        ISignControllerService controller, byte frameId,
        Dictionary<byte, ExtendedFrameContentDto?> frameCache)
    {
        if (frameCache.TryGetValue(frameId, out var cached))
            return cached;

        ExtendedFrameContentDto? content = null;
        try
        {
            // Only text frames carry readable text; graphics/hi-res frames resolve to null text.
            if (await controller.SignRequestStoredFrameMessagePlan(RequestType.Frame, frameId) is SignSetTextFrame textFrame)
            {
                content = new ExtendedFrameContentDto
                {
                    FrameID = textFrame.FrameID,
                    Revision = textFrame.Revision,
                    Font = textFrame.Font,
                    Colour = textFrame.Colour,
                    Conspicuity = textFrame.Conspicuity,
                    Text = DecodeHexText(textFrame.Text)
                };
            }
        }
        catch
        {
            // Not stored / timed out / rejected — leave content null.
        }

        frameCache[frameId] = content;
        return content;
    }

    private static IEnumerable<(byte FrameId, byte Time)> EnumerateFrames(SignSetMessage m)
    {
        yield return (m.Frame1ID, m.Frame1Time);
        yield return (m.Frame2ID, m.Frame2Time);
        yield return (m.Frame3ID, m.Frame3Time);
        yield return (m.Frame4ID, m.Frame4Time);
        yield return (m.Frame5ID, m.Frame5Time);
        yield return (m.Frame6ID, m.Frame6Time);
    }

    /// <summary>Decodes a hex-encoded ASCII string (e.g. "48454C4C4F" → "HELLO"); returns the input unchanged if it is not valid hex.</summary>
    private static string? DecodeHexText(string? hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
            return hex;

        try
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return Encoding.ASCII.GetString(bytes);
        }
        catch
        {
            return hex;
        }
    }
}
