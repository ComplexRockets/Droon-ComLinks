using System.Collections.Generic;

namespace Assets.Scripts.DroonComLinks.Objects {
    public class DCLInputs {
        public List<string> Types {
            get;
            private set;
        }

        public List<float> Values {
            get;
            private set;
        }

        public double Time {
            get;
            private set;
        }

        public DCLInputs (double time) {
            Types = new List<string>();
            Values = new List<float>();
            Time = time;
        }

        public void AddInput (string type, float value) {
            Types.Add(type);
            Values.Add(value);
        }
    }
}