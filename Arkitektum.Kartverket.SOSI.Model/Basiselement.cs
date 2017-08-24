using System.Collections.Generic;

namespace Arkitektum.Kartverket.SOSI.Model
{
    public class Basiselement : AbstraktEgenskap
    {
        public string Operator { get; set; }
        public List<string> TillatteVerdier { get; set; } //evt string array for kodelister
        public string StandardVerdi { get; set; }
        public string Datatype { get; set; }

        public override bool Equals(object obj)
        {
            return SOSI_Navn == ((Basiselement) obj).SOSI_Navn;
        }

        public void LeggTilTillatteVerdier(List<string> verdier)
        {
            TillatteVerdier.AddRange(verdier);
        }

        public bool HarStandardVerdi()
        {
            return !string.IsNullOrEmpty(StandardVerdi);
        }
    }
}