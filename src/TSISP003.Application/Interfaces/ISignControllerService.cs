using TSISP003.Application.DTOs;
using TSISP003.Domain.Entities;
using TSISP003.Domain.Enums;
using TSISP003.Domain.Interfaces;

namespace TSISP003.Application.Interfaces;

public interface ISignControllerService
{
    Task<SignStatusReply?> GetStatus();
    Task<SignController?> GetControllerConfigurationAsync();
    Task StartSession();
    Task Password(string passwordSeed);
    Task HeartbeatPoll();
    Task EndSession();
    Task<AckReply> SystemReset(byte groupId, byte resetLevel);
    Task<AckReply> UpdateTime(DateTime? dateTime = null);
    Task<SignStatusReply> SignSetTextFrame(SignSetTextFrame request);
    Task<SignStatusReply> SignSetGraphicsFrame(SignSetGraphicsFrame request);
    Task<SignStatusReply> SignSetHighResolutionGraphicsFrame(SignSetHighResolutionGraphicsFrame request);
    Task SignConfigurationRequest();
    Task<AckReply> SignDisplayAtomicFrames(SignDisplayAtomicFrame request);
    Task<SignStatusReply> SignSetMessage(SignSetMessage request);
    Task<SignStatusReply> SignSetPlan(SignSetPlan request);
    Task<AckReply> SignDisplayFrame(SignDisplayFrame request);
    Task<AckReply> SignDisplayMessage(SignDisplayMessage request);
    Task<AckReply> EnablePlan(byte groupId, byte planId);
    Task<AckReply> DisablePlan(byte groupId, byte planId);
    Task<ReportEnabledPlans> RequestEnabledPlans();
    Task<AckReply> SignSetDimmingLevel(List<(byte groupId, byte dimmingMode, byte luminanceLevel)> entries);
    Task<AckReply> PowerOnOff(byte groupId, bool powerOn);
    Task<AckReply> DisableEnableDevice(List<(byte groupId, bool enabled)> entries);
    Task<ISignResponse> SignRequestStoredFrameMessagePlan(RequestType requestType, byte requestID);
    Task<SignExtendedStatusReply> SignExtendedStatusRequest();
    Task<List<FaultLogEntry>> RetrieveFaultLog();
    Task<AckReply> ResetFaultLog();
    Task<HARStatusReply> HARSetStrategy(HARSetStrategy request);
    Task<AckReply> HARActivateStrategy(ushort strategyId);
    Task<HARStatusReply> HARSetPlan(HARSetPlan request);
    Task<ISignResponse> HARRequestStoredVoiceStrategyPlan(byte requestType, ushort requestId, byte sequenceNumber);
    Task<bool> ExtendedRequestMessage(ExtendedRequestMessageDto request);
}
