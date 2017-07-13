using System.Xml.Serialization;

namespace Synapse.Handlers.DNS
{
    public class DnsHandlerConfig
    {
        [XmlElement]
        public string DnsServer { get; set; }
    }
}
