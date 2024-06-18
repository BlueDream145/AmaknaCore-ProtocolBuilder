using System.Xml.Serialization;

namespace AmaknaProxy.ProtocolBuilder.XmlPatterns
{
    public class XmlField
    {
        [XmlAttribute]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute]
        public string Type
        {
            get;
            set;
        }

        [XmlAttribute]
        public string Limit
        {
            get;
            set;
        }

        [XmlAttribute]
        public string Condition
        {
            get;
            set;
        }
    }
}