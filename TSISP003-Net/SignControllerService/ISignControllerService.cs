
namespace TSISP003.SignControllerService
{
    public interface ISignControllerService : IHostedService
    {
        Task StartSession();
        Task Password(string passwordSeed);
        Task HeartbeatPoll();
        Task EndSession();
        Task SystemReset();
        Task UpdateTime();
        Task SignSetTextFrame();
        Task SignSetGraphicsFrame();
        Task SignSetHighResolutionGraphicsFrame();
        Task SignConfigurationRequest();
        Task SignDisplayAtomicFrames();
        Task SignSetMessage();
        Task SignSetPlan();
        Task SignDisplayFrame();
        Task SignDisplayMessage();
        Task EnablePlan();
        Task DisablePlan();
        Task RequestEnabledPlans();
        Task SignSetDimmingLevel();
        Task PowerOnOff();
        Task DisableEnableDevice();
        Task SignRequestStoredFrameMessagePlan();
        Task SignExtendedStatusRequest();
        Task RetrieveFaultLog();
        Task ResetFaultLog();
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
    }
}