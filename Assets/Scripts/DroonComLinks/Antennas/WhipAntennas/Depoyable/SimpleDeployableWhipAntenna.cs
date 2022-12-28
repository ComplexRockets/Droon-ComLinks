namespace Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Deployable
{
    public class SimpleDeployableWhipAntenna : DeployableWhipAntenna
    {
        public override float[] segmentsOffset { get; set; }
        public override int segmentCount { get; set; }
        public override string stylePrefabName { get; set; }
        public override float mass { get; set; }

        public SimpleDeployableWhipAntenna()
        {
            segmentsOffset = new float[] { 0, -0.78f, -0.72f, -0.59f, -0.53f, -0.45f, -0.4f };
            segmentCount = 7;
            stylePrefabName = "DeployableWhipAntenna1";
            mass = 0.2f;
        }
    }
}