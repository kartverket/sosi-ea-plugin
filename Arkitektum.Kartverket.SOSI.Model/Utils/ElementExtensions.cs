using EA;
using static System.StringComparison;

namespace Arkitektum.Kartverket.SOSI.Model
{
    internal static class ElementExtensions
    {
        public static bool IsCodeList(this Element element)
        {
            return element.Stereotype.Equals("codelist", InvariantCultureIgnoreCase)  ||
                   element.Stereotype.Equals("enumeration", InvariantCultureIgnoreCase) ||
                   element.Type.Equals("enumeration", InvariantCultureIgnoreCase);
        }

        public static bool IsApplicationSchema(this Element element)
        {
            return element.Stereotype.Equals("applicationschema", InvariantCultureIgnoreCase);
        }

        public static bool IsUnderArbeid(this Element element)
        {
            return element.Stereotype.Equals("underarbeid", InvariantCultureIgnoreCase);
        }

        public static bool IsUnion(this Element element)
        {
            return element.Stereotype.Equals("union", InvariantCultureIgnoreCase);
        }

        public static bool IsTaggedAsExternalCodelist(this Element element)
        {
            return RepositoryHelper.GetTaggedValue(element.TaggedValues, "asDictionary") == "true";
        }
    }
}
