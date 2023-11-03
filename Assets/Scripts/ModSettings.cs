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

        public BoolSetting BlockControls { get; private set; }
        public BoolSetting NeedPower { get; private set; }
        public EnumSetting<DelayModes> Delay { get; private set; }
        public NumericSetting<int> DelayOverride { get; private set; }
        public BoolSetting DiffirenciateUpDownLink { get; private set; }
        public BoolSetting DebugMode { get; private set; }

        protected override void InitializeSettings()
        {
            this.BlockControls = this.CreateBool("Block Controls")
               .SetDescription("If enable, blocks the controls when the craft is out of range and no Drood is onboard (makes the game harder)")
               .SetDefault(true);

            this.NeedPower = this.CreateBool("Antenna Require Power")
               .SetDescription("If enable, blocks the connection between two antennas if not there isn't enough power. Also, sending inputs consumes power (makes the game harder)")
               .SetDefault(true);

            // this.delay = this.CreateEnum<DelayModes>("Antenna Delay Difficulty")
            //     .SetDescription("Changes the communications delay, None = 0%, VeryEasy = 3%, Easy = 7%, Normal = 15%, Realistic=100%, Extreme = 200%")
            //     .SetDefault(DelayModes.Easy);

            // this.delayOverride = this.CreateNumeric<int>("Antenna Delay Override", -1, 500, 500)
            //     .SetState(SettingState.Hidden)
            //     .SetDefault(value: -1);

            this.DebugMode = this.CreateBool("Debug Mode")
               .SetDescription("")
               .SetDefault(false);
        }
    }
}