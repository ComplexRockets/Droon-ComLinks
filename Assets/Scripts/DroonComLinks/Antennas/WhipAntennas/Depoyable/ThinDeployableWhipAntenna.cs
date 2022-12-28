namespace Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Deployable
{
    public class ThinDeployableWhipAntenna : DeployableWhipAntenna
    {
        public override float[] segmentsOffset { get; set; }
        public override int segmentCount { get; set; }
        public override string stylePrefabName { get; set; }
        public override float mass { get; set; }

        public ThinDeployableWhipAntenna()
        {
            segmentsOffset = new float[] { 0, -1.33f, -1.33f, -1.3f };
            segmentCount = 4;
            stylePrefabName = "DeployableWhipAntenna3";
            mass = 0.05f;
        }
    }
}