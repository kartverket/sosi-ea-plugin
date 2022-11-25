using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using EA;
using Attribute = EA.Attribute;

namespace Arkitektum.Kartverket.SOSI.Model
{
    internal class CodelistHelper
    {
        private readonly Repository _repository;
        private readonly RepositoryHelper _repositoryHelper;

        public CodelistHelper(Repository repository)
        {
            _repository = repository;
            _repositoryHelper = new RepositoryHelper(repository);
        }

        public IEnumerable<string> HentTillatteVerdierFraBeskrankninger(Element element, Attribute att,
            List<Beskrankning> oclListe, Basiselement eg)
        {
            foreach (Beskrankning be in oclListe)
            {
                if (be.OCL != null)
                {
                    if (be.OCL.Contains(att.Name) && be.OCL.ToLower().Contains("inv:") &&
                        be.OCL.ToLower().Contains("notempty") == false && be.OCL.ToLower().Contains("implies") == false &&
                        be.OCL.ToLower().Contains("and") == false)
                    {
                        if (be.OCL.Contains("="))
                        {
                            eg.Operator = "=";
                        }
                        else if (be.OCL.Contains("&lt;&gt;"))
                        {
                            eg.Operator = "!=";
                        }
                        else
                        {
                            _repositoryHelper.Log($"ADVARSEL: Fant beskrankning for {att.Name} i {element.Name}," +
                                                  " men klarte ikke å løse operator-uttrykket.");
                        }

                        //finne verdier neste token etter operator, TODO må forbedres mye!!!
                        string ocl = be.OCL.Substring(be.OCL.ToLower().IndexOf("inv:") + 4);
                        string[] separators = { "'", "=", "&lt;&gt;", "or" };
                        string[] tokens = ocl.Trim().Split(separators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string verdi in tokens)
                        {
                            if ((verdi.Contains(att.Name)) == false && verdi.Trim().Length > 0)
                            {
                                yield return verdi;
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<string> HentEksterneKodelisteverdier(Element element)
        {
            var codelistUrl = RepositoryHelper.GetTaggedValue(element.TaggedValues, "codeList");

            var externalCodeList = new ExternalCodelistFetcher(_repositoryHelper).Fetch(codelistUrl);

            if (string.IsNullOrEmpty(externalCodeList))
                yield break;

            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(externalCodeList);

            var dictionaryEntries = xmlDocument["gml:Dictionary"]?.GetElementsByTagName("gml:dictionaryEntry");

            if (dictionaryEntries == null)
                yield break;

            foreach (XmlElement dictionaryEntry in dictionaryEntries)
            {
               yield return dictionaryEntry["gml:Definition"]["gml:identifier"]?.InnerText;
            }
        }

        public IEnumerable<string> HentLokaleKodelisteverdier(Element element)
        {
            foreach (Attribute attributt in element.Attributes)
            {
                if (attributt.Default.Trim().Length > 0)
                {
                    yield return attributt.Default.Trim();
                }
                else
                {
                    var sosiVerdi = RepositoryHelper.GetTaggedValue(attributt.TaggedValues, RepositoryHelper.SosiVerdi);
                    yield return !string.IsNullOrEmpty(sosiVerdi) ? sosiVerdi.Trim() : attributt.Name;
                }
            }
        }

        public IEnumerable<string> HentArvedeKodeverdier(Element element)
        {
            foreach (var connector in element.Connectors.Cast<Connector>().Where(c => c.MetaType == "Generalization"))
            {
                var connectorSupplierElement = _repository.GetElementByID(connector.SupplierID);

                if (element.Name == connectorSupplierElement.Name)
                    continue;

                foreach (Attribute attributt in connectorSupplierElement.Attributes)
                {
                    if (attributt.Default.Trim().Length > 0)
                    {
                        yield return attributt.Default.Trim();
                    }
                    else
                    {
                        var sosiVerdi = RepositoryHelper.GetTaggedValue(attributt.TaggedValues, RepositoryHelper.SosiVerdi);
                        if (sosiVerdi != null && sosiVerdi.Trim().Length > 0)
                        {
                            yield return sosiVerdi.Trim();
                        }
                        else
                        {
                            yield return attributt.Name;
                        }
                    }
                }
            }
        }
    }
}
