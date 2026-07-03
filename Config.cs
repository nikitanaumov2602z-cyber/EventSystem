using Exiled.API.Interfaces;

namespace EventSystem
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public string ProjectName { get; set; } = "Delta Project";
        public string DefaultEventName { get; set; } = "Обычный День (ОД)";
        public string DefaultRPLevel { get; set; } = "Hard-RP";
        public string HintInstruction { get; set; } = "📢 Следуйте указаниям проводящего.";
        public string BroadcastStart { get; set; } = "Ивент начался! Следуйте указаниям проводящего.";
        public string BroadcastEnd { get; set; } = "Ивент завершён. Спасибо за участие!";
        public string PermissionHost { get; set; } = "ev.host";
        public string PermissionHelper { get; set; } = "ev.helper";
        public float UpdateInterval { get; set; } = 1.0f;

        // Color HUD
        public string HudColorTitle { get; set; } = "#FAFF86";
        public string HudColorAccent { get; set; } = "#FFD700";
        public string HudColorMain { get; set; } = "#FFFFFF";
        public string HudColorSecondary { get; set; } = "#A0A0A0";
        public string HudColorHighlight { get; set; } = "#00FF00";
        public string HudColorInfo { get; set; } = "#87CEEB";
        public string HudColorRole { get; set; } = "#FF6B6B";

        // AudioPlayer
        public bool AudioEnabled { get; set; } = true;
        public string AudioFolder { get; set; } = "audio";
        public string SoundEventStart { get; set; } = "event_start.ogg";
        public string SoundEventEnd { get; set; } = "event_end.ogg";
        public string SoundRoleGive { get; set; } = "role_give.ogg";
        public float AudioVolume { get; set; } = 0.5f;
        public int AudioBotId { get; set; } = 1;
        public string AudioBotChannel { get; set; } = "RoundSummary";
    }
}
