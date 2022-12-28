using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using UI.Xml;
using UnityEngine;
using static Assets.Scripts.DroonComLinks.Antennas.AntennaTypes;

namespace Assets.Scripts.DroonComLinks {
    public class DataManager {
        private Mod _mod => Mod.Instance;
        private XmlSerializer _xmlSerializer;
        private FileStream _fileStream;
        public string customTypesPath;

        public void Initialise () {
            System.IO.Directory.CreateDirectory (_mod.saveFilePath);
            customTypesPath = _mod.saveFilePath + "CustomAntennaTypes.xml";
            _xmlSerializer = new XmlSerializer (typeof (AntennaTypesDataBase));
        }

        public void SaveAntennaTypes (List<CustomType> _antennaTypes) {
            AntennaTypesDataBase antennaDB = new AntennaTypesDataBase () {
                antennaTypes = _antennaTypes
            };

            _fileStream = new FileStream (customTypesPath, FileMode.Create);
            _xmlSerializer.Serialize (_fileStream, antennaDB);
            _fileStream.Close ();
        }
    }

    public class AntennaTypesDataBase {
        [XmlArrayAttribute ("CustomAntennaTypes")]
        public List<CustomType> antennaTypes;
    }
}