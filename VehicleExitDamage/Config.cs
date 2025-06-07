using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace VehicleExitDamage
{
    public class Config : IRocketPluginConfiguration
    {
        public bool Enabled { get; set; }
        public bool IgnoreAdmins { get; set; }

        [XmlArray("DamageTiers")]
        [XmlArrayItem("Tier")]
        public List<DamageTier> DamageTiers { get; set; }

        public void LoadDefaults()
        {
            Enabled = true;
            IgnoreAdmins = true;
            DamageTiers = new List<DamageTier>
            {
                new DamageTier { MinSpeed = 20, Damage = 15 },
                new DamageTier { MinSpeed = 50, Damage = 40 },
                new DamageTier { MinSpeed = 90, Damage = 80 },
                new DamageTier { MinSpeed = 120, Damage = 101 }
            };
        }
    }

    public class DamageTier
    {
        [XmlAttribute]
        public float MinSpeed { get; set; }

        [XmlAttribute]
        public float Damage { get; set; }
    }
}