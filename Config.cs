using Exiled.API.Interfaces;

namespace EventSystem
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public string ProjectName { get; set; } = "Delta Project";
        public string DefaultEventName { get; set; } = "Обычный День (ОД)";
        public string DefaultRPLevel { get; set; } = "Hard";
        public string HintInstruction { get; set; } = "📢 Следуйте указаниям проводящего.";
        public string BroadcastStart { get; set; } = "Ивент начался! Следуйте указаниям проводящего.";
        public string BroadcastEnd { get; set; } = "Ивент завершён. Спасибо за участие!";
        public string PermissionHost { get; set; } = "ev.host";
        public string PermissionHelper { get; set; } = "ev.helper";
        public float UpdateInterval { get; set; } = 1.0f;
    }
}
