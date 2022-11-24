using EA;
using static System.StringComparison;

namespace Arkitektum.Kartverket.SOSI.Model
{
    internal static class ConnectorExtensions
    {
        public static bool IsTopoType(this Connector connector)
        {
            return connector.Stereotype.Equals("topo", InvariantCultureIgnoreCase) ||
                   connector.MetaType.Equals("topo", InvariantCultureIgnoreCase);
        }

        public static bool ErNavigerbar(this Connector connector, bool isSupplierEnd)
        {
            switch (isSupplierEnd)
            {
                case true when connector.ClientEnd.IsNavigable:
                case false when connector.SupplierEnd.IsNavigable:
                    return true;
                default:
                    return false;
            }
        }
    }
}
