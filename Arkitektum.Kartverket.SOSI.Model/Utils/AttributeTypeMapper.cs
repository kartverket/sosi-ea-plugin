using System.Collections.Generic;

namespace Arkitektum.Kartverket.SOSI.Model
{
    internal static class AttributeTypeMapper
    {
        private static readonly Dictionary<string, string> Types = new Dictionary<string, string>
        {
            { "gm_point", "punkt" },
            { "gm_multipoint", "sverm" },
            { "gm_curve", "kurve" },
            { "gm_compositecurve", "kurve" },
            { "gm_surface", "flate" },
            { "gm_compositesurface", "flate" },
        };

        public static string GetSosiType(string gmType)
        {
            return Types[gmType];
        }

        public static bool IsGeometry(string gmType)
        {
            return Types.ContainsKey(gmType.ToLower());
        }

        public static bool IsBasicGeometry(string gmType)
        {
            return !gmType.ToLower().Equals("gm_point") && Types.ContainsKey(gmType.ToLower());
        }
    }
}
