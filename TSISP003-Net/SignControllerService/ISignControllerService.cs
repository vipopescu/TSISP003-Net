
namespace TSISP003.SignControllerService
{
    public interface ISignControllerService : IHostedService
    {
        Task StartSession();
        Task Password();
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
        Task ProcessPasswordSeed();
        Task ProcessAcknowledge();
        Task ProcessSignStatusReply();
        Task ProcessHARStatusReply();
        Task ProcessEnvironmentalWeatherStatusReply();
        Task ProcessSignConfigurationReply();
        Task ProcessReportEnabledPlans();
        Task ProcessSignExtendedStatusReply();
        Task ProcessFaultLogReply();
        Task ProcessHARVoiceDataAck();
        Task ProcessHARVoiceDataNak();
        Task ProcessEnvironmentalWeatherValuesReply();
        Task ProcessEnvironmentalWeatherThresholdDefinitionReply();
        Task ProcessEnvironmentalWeatherEventLogReply();
    }
}