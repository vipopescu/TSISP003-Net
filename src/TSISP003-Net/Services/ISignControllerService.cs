using TSISP003.DTOs;
using TSISP003.Domain.Entities;
using static TSISP003.Utilities.Enums;

namespace TSISP003.Services;

/// <summary>
/// Defines operations for communicating with a sign controller device.
/// </summary>
public interface ISignControllerService : IHostedService
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
    Task HARSetVoiceDataIncomplete();
    Task HARSetVoiceDataComplete();
    Task HARSetStrategy();
    Task HARActivateStrategy();
    Task HARSetPlan();
    Task HARRequestStoredVoiceStrategyPlan();
    Task RequestEnvironmentalWeatherValues();
    Task EnvironmentalWeatherValues();
    Task EnvironmentalWeatherThresholdDefinition();
    Task RequestThresholdDefinition();
    Task RequestEnvironmentalWeatherEventLog();
    Task ResetEnvironmentalWeatherEventLog();
    Task ProcessPasswordSeed(string applicationData);
    Task ProcessAcknowledge(string applicationData);
    Task ProcessSignStatusReply(string applicationData);
    Task ProcessHARStatusReply(string applicationData);
    Task ProcessEnvironmentalWeatherStatusReply(string applicationData);
    Task ProcessSignConfigurationReply(string applicationData);
    Task ProcessReportEnabledPlans(string applicationData);
    Task ProcessSignExtendedStatusReply(string applicationData);
    Task ProcessFaultLogReply(string applicationData);
    Task ProcessHARVoiceDataAck(string applicationData);
    Task ProcessHARVoiceDataNak(string applicationData);
    Task ProcessEnvironmentalWeatherValuesReply(string applicationData);
    Task ProcessEnvironmentalWeatherThresholdDefinitionReply(string applicationData);
    Task ProcessEnvironmentalWeatherEventLogReply(string applicationData);
    Task ProcessSignSetTextFrame(string applicationData);
    Task ProcessSignSetGraphicsFrame(string applicationData);
    Task ProcessSignSetHighResolutionGraphicsFrame(string applicationData);
    Task ProcessSignSetMessage(string applicationData);
    Task ProcessSignSetPlan(string applicationData);
    Task ProcessRejectMessage(string applicationData);
    Task ProcessAckMessage(string applicationData);
    Task<bool> ExtendedRequestMessage(ExtendedRequestMessageDto request);
}
