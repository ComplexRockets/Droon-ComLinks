using Assets.Scripts.DroonComLinks.Interfaces;

namespace Assets.Scripts.DroonComLinks
{
    public static class AntennaTypes {
        public static ParametricalParabolic parametricalParabolic = new ParametricalParabolic ();
        public static Whip whip = new Whip ();
        public static CubeSatSBand cubeSatSBand = new CubeSatSBand ();

        public class ParametricalParabolic : IAntennaType {
            public float minFrenquency => 2f;
            public float maxFrenquency => 50f;
            public float minPower => 5;
            public float maxPower => 80;
            public float azAngle => -1; // TODO
            public float eqAngle => azAngle;
            public float txPowerRatio => 1.5f;
            public float sensitivityConstant => 500f;
            public float defaultFrequency => 10f;
            public float defaultSize => 1f;
            public float defaultEfficency => 0.6f;
            public float defaultMaxPowerConsumption => 50f;

        }

        public class Whip : IAntennaType {
            public float sensitivityConstant => 5f;
            public float txPowerRatio => 3;
            public float minFrenquency => 4;
            public float maxFrenquency => 35;
            public float minPower => 1;
            public float maxPower => 20;
            public float azAngle => 360;
            public float eqAngle => 80;
            public float defaultFrequency => 10f;
            public float defaultSize => 1f;
            public float defaultEfficency => 0.6f;
            public float defaultMaxPowerConsumption => 10f;
        }

        public class CubeSatSBand : IAntennaType {
            public float minFrenquency => 2f;
            public float maxFrenquency => 4f;
            public float minPower => 1;
            public float maxPower => 9;
            public float txPowerRatio => 4.5f;
            public float azAngle => 180;
            public float eqAngle => 180;
            public float sensitivityConstant => 1f;
            public float defaultFrequency => 3f;
            public float defaultSize => 1f;
            public float defaultEfficency => 0.6f;
            public float defaultMaxPowerConsumption => 5f;
        }

        public class Defaults {
            public static float parabolicDiameter => 3f;
            public static float parabolicDepth => 0.5f;
        }
    }
}