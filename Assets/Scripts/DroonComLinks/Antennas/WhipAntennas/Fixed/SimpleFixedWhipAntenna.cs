namespace Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Fixed
{
    public class SimpleFixedWhipAntenna : FixedWhipAntenna
    {
        public override string stylePrefabName { get; set; }
        public override float mass { get; set; }

        public SimpleFixedWhipAntenna()
        {
            stylePrefabName = "FixedMonopoleAntenna1";
            mass = 0.01f;
        }
    }
}