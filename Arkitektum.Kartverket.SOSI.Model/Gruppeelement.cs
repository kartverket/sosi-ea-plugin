using System.Collections.Generic;

namespace Arkitektum.Kartverket.SOSI.Model
{
    public class Gruppeelement : AbstraktEgenskap
    {

        public Gruppeelement Inkluder { get; set; }
        public List<AbstraktEgenskap> Egenskaper { get; set; }
        public List<Beskrankning> OCLconstraints { get; set; }

        public void LeggTilEgenskap(AbstraktEgenskap egenskap)
        {
            Egenskaper.Add(egenskap);
        }

        public override bool Equals(object obj)
        {
            return this.SOSI_Navn == ((Gruppeelement)obj).SOSI_Navn;
        }
    }
}
