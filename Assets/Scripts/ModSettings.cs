namespace Assets.Scripts
{
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
    public enum DelayModes { None = 0, VeryEasy = 3, Easy = 7, Challenging = 15, Realistic = 100, Extreme = 200 };

    public class ModSettings : SettingsCategory<ModSettings>
    {
        private static ModSettings _instance;

        public ModSettings() : base("Droon ComLinks")
        {
        }

        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());

        public BoolSetting blockControls { get; private set; }
        public BoolSetting needPower { get; private set; }
        public EnumSetting<DelayModes> delay { get; private set; }
        public NumericSetting<int> delayOverride { get; private set; }
        public BoolSetting diffirenciateUpDownLink { get; private set; }
        public BoolSetting debugMode { get; private set; }

        protected override void InitializeSettings()
        {
            this.blockControls = this.CreateBool("Block Controls")
               .SetDescription("If enable, blocks the controls when the craft is out of range and no Drood is onboard (makes the game harder)")
               .SetDefault(true);

            this.needPower = this.CreateBool("Antenna Require Power")
               .SetDescription("If enable, blocks the connection between two antennas if not enough power. Also, sending inputs consumes power (makes the game harder)")
               .SetDefault(true);

            // this.delay = this.CreateEnum<DelayModes>("Antenna Delay Difficulty")
            //     .SetDescription("Changes the communications delay, None = 0%, VeryEasy = 3%, Easy = 7%, Normal = 15%, Realistic=100%, Extreme = 200%")
            //     .SetDefault(DelayModes.Easy);

            // this.delayOverride = this.CreateNumeric<int>("Antenna Delay Override", -1, 500, 500)
            //     .SetState(SettingState.Hidden)
            //     .SetDefault(value: -1);

            // this.diffirenciateUpDownLink = this.CreateBool("Differentiate Link Direction")
            //    .SetDescription("If enabled, a connection can be disconected, only uplink, only downlink or both, allowing different mechanics depending on the case (harder). if disabled, a connection is either connected or disconnected (easier)")
            //    .SetDefault(true);

            this.debugMode = this.CreateBool("Debug Mode")
               .SetDescription("")
               .SetDefault(false);
        }
    }
}