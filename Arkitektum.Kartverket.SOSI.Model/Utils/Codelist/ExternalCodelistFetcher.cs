using System;
using System.Net;
using System.Text;

namespace Arkitektum.Kartverket.SOSI.Model
{
    public class ExternalCodelistFetcher
    {
        private static RepositoryHelper _repositoryHelper;

        public ExternalCodelistFetcher(RepositoryHelper repositoryHelper)
        {
            _repositoryHelper = repositoryHelper;
        }

        public string Fetch(string url)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (var client = new WebClient())
                {
                    client.Headers.Add(HttpRequestHeader.Accept, "application/xml"); // TODO: Use "application/gml+xml" when prepared for in production environment
                    client.Encoding = Encoding.UTF8;

                    return client.DownloadString(url);
                }
            }
            catch (WebException e)
            {
                _repositoryHelper.Log($"FEIL: Kodeliste kunne ikke hentes fra url {url}\n Status: {e.Status}");

                return null;
            }
            catch (Exception)
            {
                _repositoryHelper.Log($"FEIL: Kodeliste kunne ikke hentes fra url {url}");

                return null;
            }
        }
    }
}
