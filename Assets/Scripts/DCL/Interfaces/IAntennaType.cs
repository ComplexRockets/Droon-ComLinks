namespace Assets.Scripts.DroonComLinks.Interfaces {
    public interface IAntennaType {
        float sensitivityConstant { get; }
        float txPowerRatio { get; }
        float minFrenquency { get; }
        float maxFrenquency { get; }
        float minPower { get; }
        float maxPower { get; }
        float azAngle { get; }
        float eqAngle { get; }
        float defaultFrequency { get; }
        float defaultSize { get; }
        float defaultEfficency { get; }
        float defaultMaxPowerConsumption { get; }
    }
}