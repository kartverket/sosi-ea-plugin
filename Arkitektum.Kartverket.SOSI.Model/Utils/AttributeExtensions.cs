using EA;
using static System.StringComparison;

namespace Arkitektum.Kartverket.SOSI.Model
{
    public static class AttributeExtensions
    {
        public static bool IsBasicTypeOrBasicGeometryType(this Attribute attribute)
        {
            return IsBasicGeometryType(attribute) ||
                   attribute.Type.Equals("integer", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("characterstring", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("real", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("date", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("datetime", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("boolean", InvariantCultureIgnoreCase) || 
                   attribute.Type.Equals("punkt", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("kurve", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("flate", InvariantCultureIgnoreCase);
        }

        public static bool IsBasicType(this Attribute attribute)
        {
            return attribute.Type.Equals("integer", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("characterstring", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("real", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("date", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("datetime", InvariantCultureIgnoreCase) ||
                   attribute.Type.Equals("boolean", InvariantCultureIgnoreCase);
        }

        private static bool IsBasicGeometryType(this Attribute attribute)
        {
            return AttributeTypeMapper.IsBasicGeometry(attribute.Type);
        }
    }
}
