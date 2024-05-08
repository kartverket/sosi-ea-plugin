using Arkitektum.Kartverket.SOSI.Model;
using Arkitektum.Kartverket.SOSI.EA.Plugin.Services;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arkitektum.Kartverket.SOSI.EA.Plugin.Test
{
    [TestClass]
    public class ObjektKatalogGeneratorTest
    {
        [TestMethod]
        public void TestGenereringAvObjektKatalog()
        {
            var egenskaper = new List<AbstraktEgenskap>
                {
                    new Basiselement {SOSI_Navn = "Dette er SOSI-Navnet"}
                };
            var objekttyper = new List<Objekttype>
                {
                    new Objekttype { UML_Navn = "Stedsnavn", Notat = "Dette er notatet", Egenskaper = egenskaper},
                    new Objekttype { UML_Navn = "StedsnavnType", Notat = "Enda et notat"}
                };
            
            var generator = new ObjektKatalogGenerator();
            
            var doc = generator.LagObjektKatalog("versjon", "org", "person", "navn", "beskrivelse", objekttyper, true, new List<SosiKodeliste>());
            doc.Save("objektkatalog.xml");
            
            var htmlDoc = generator.CreateHtmlCatalogForXmlDocument(doc);
            htmlDoc.Save("objektkatalog.html");
        }
    }
}
