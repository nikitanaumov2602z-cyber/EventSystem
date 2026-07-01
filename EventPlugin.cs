using System;
using Exiled.API.Features;
using Exiled.API.Interfaces;

namespace EventSystem
{
    public class EventPlugin : Plugin<Config>
    {
        public static EventPlugin Instance;
        public override string Name => "DeltaProject";
        public override string Author => "Delta Project";
        public override Version Version => new Version(1, 0, 1);
        public override Version RequiredExiledVersion => new Version(8, 0, 0);

        private EventHandler eventHandler;

        public override void OnEnabled()
        {
            Instance = this;
            eventHandler = new EventHandler(this);
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            eventHandler = null;
            Instance = null;
            base.OnDisabled();
        }
    }
}
