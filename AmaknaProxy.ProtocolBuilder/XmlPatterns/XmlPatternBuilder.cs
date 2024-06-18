using System.Xml;
using AmaknaProxy.ProtocolBuilder.Parsing;

namespace AmaknaProxy.ProtocolBuilder.XmlPatterns
{
    public abstract class XmlPatternBuilder
    {
        protected Parser Parser;

        protected XmlPatternBuilder(Parser parser)
        {
            Parser = parser;
        }

        public abstract void WriteToXml(XmlWriter writer);
    }
}