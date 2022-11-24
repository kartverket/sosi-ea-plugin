using System;
using EA;
using Attribute = EA.Attribute;

namespace Arkitektum.Kartverket.SOSI.Model
{
    public class RepositoryHelper
    {
        private readonly Repository _repository;

        public const string SosiNavn = "sosi_navn";
        public const string SosiDatatype = "sosi_datatype";
        public const string SosiLengde = "sosi_lengde";
        public const string SosiVerdi = "sosi_verdi";
        public const string SosiKortnavn = "sosi_kortnavn";
        public const string SosiVersjon = "sosi_versjon";

        public RepositoryHelper(Repository repository)
        {
            _repository = repository;
        }



        /// <summary>
        /// Returns sosi name from tagged values. If not defined it looks for sosi name in classifier element.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public string GetSosiNameForAttribute(Attribute attribute)
        {
            string sosiName = GetTaggedValue(attribute.TaggedValues, SosiNavn);

            if (string.IsNullOrEmpty(sosiName) && attribute.ClassifierID != 0)
            {
                Element elm = _repository.GetElementByID(attribute.ClassifierID);
                sosiName = GetTaggedValue(elm.TaggedValues, SosiNavn);
            }

            

            return sosiName;
        }

        public static string GetTaggedValue(Collection taggedValues, string searchString)
        {
            foreach (dynamic tag in taggedValues)
            {
                if (tag.Name.Equals(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    return tag.Value.Trim();
                }
            }
            return null;
        }

        public static string GetRoleTaggedValue(Collection taggedValues, string searchString)
        {
            foreach (dynamic tag in taggedValues)
            {
                if (tag.Tag.Equals(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    return tag.Value.Trim();
                }
            }
            return null;
        }

        public static string GetConnectorTaggedValue(Collection taggedValues, string searchString)
        {
            foreach (dynamic tag in taggedValues)
            {
                if (tag.Name.Equals(searchString, StringComparison.OrdinalIgnoreCase))
                {
                    return tag.Value.Trim();
                }
            }
            return null;
        }

        public void Log(string message)
        {
            _repository.WriteOutput("System", message, 0);
        }
    }
}
