using System.Collections.Generic;

namespace Arkitektum.Kartverket.SOSI.Model
{
    internal static class KjentTypeMapper
    {
        private static readonly Dictionary<string, KjentType> KjenteTyper = new Dictionary<string, KjentType>
        {
            {"TM_Instant", new KjentType("TM_Instant", "DATOTID") },
            {"TM_Period", new KjentType("TM_Period", "PERIODE") }
        };

        public static bool ErKjentType(string type)
        {
            return KjenteTyper.ContainsKey(type);
        }

        public static KjentType GetKjentType(string type)
        {
            return KjenteTyper[type];
        }
    }
}
