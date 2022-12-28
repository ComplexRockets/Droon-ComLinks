namespace Assets.Scripts.DroonComLinks.Controls
{
    public class DCLInput
    {
        public string Type
        {
            get;
            private set;
        }

        public float Value
        {
            get;
            private set;
        }

        public double Time
        {
            get;
            private set;
        }

        public DCLInput(string type, float value, double time)
        {
            Type = type;
            Value = value;
            Time = time;
        }
    }
}