using EA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Arkitektum.Kartverket.SOSI.Model;

namespace Arkitektum.Kartverket.SOSI.EA.Plugin.Services
{
    public class SosiKontrollGenerator
    {
        private static readonly List<string> NgisDatasetShortnames = new List<string>
        {
            "FKB",
            "Havnedata",
            //"N5", spesialhåndtert i metoden som utfører sjekk
            "TUROGFRILUFTSRUTER",
            "MARKAGRENSEN",
            "SPR"
        };

        private bool _datasettErForvaltetMedNgis;

        public void GenererDefFiler(List<Objekttype> liste, Repository repository)
        {
            string produktgruppe = "";
            string kortnavn = "";
            string versjon = "";
            string sosiVersion = "";
            string versjonUtenP = "";
            bool fagområde = false;

            var modellHarFlateGeometri = liste.Any(o => o.HarGeometri("FLATE"));
            var modellAvgrensesAvKantUtsnitt = modellHarFlateGeometri; // Se variabel "harFlate" i metoden "LagSosiObjekt"
                                                                       // samt https://kartverket.atlassian.net/browse/GEOPORTAL-5528
                                                                       // Spesifikt "For alle flater skal det være minst ..INKLUDER KantUtsnitt"
            var modellAvgrensesAvFlateavgrensning = liste.Any(o => o.AvgrensesAv.Contains("Flateavgrensning"));

            Package valgtPakke = repository.GetTreeSelectedPackage();

            foreach (TaggedValue theTags in valgtPakke.Element.TaggedValues)
            {
                switch (theTags.Name.ToLower())
                {
                    case "sosi_spesifikasjonstype":
                        if (theTags.Value.ToLower() == "fagområde") fagområde = true;
                        break;
                    case "sosi_produktgruppe":
                        produktgruppe = theTags.Value.ToLower();
                        break;
                    case "sosi_kortnavn":
                        kortnavn = theTags.Value;
                        break;
                    case "version":
                        versjon = theTags.Value;
                        versjonUtenP = versjon.Replace(".", "");
                        break;
                    case "sosi_versjon":
                        sosiVersion = theTags.Value;
                        break;
                }

            }
            if (produktgruppe == "") repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_produktgruppe på applikasjonsskjemapakke " + valgtPakke.Element.Name, 0);
            if (kortnavn == "") repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_kortnavn på applikasjonsskjemapakke " + valgtPakke.Element.Name, 0);
            if (versjon == "") repository.WriteOutput("System", "FEIL: Mangler tagged value version på applikasjonsskjemapakke " + valgtPakke.Element.Name, 0);
            if (sosiVersion == "") repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_versjon på applikasjonsskjemapakke " + valgtPakke.Element.Name, 0);

            _datasettErForvaltetMedNgis = ErForvaltetMedNgis(kortnavn);

            //Lage kataloger
            string eadirectory = Path.GetDirectoryName(repository.ConnectionString);
            string baseDirectory = Path.Combine(eadirectory, "def");
            string fullfilRelativePath = Path.Combine(kortnavn, $"{kortnavn}_o.{versjonUtenP}");
            string fullfilFullPath = Path.Combine(baseDirectory, produktgruppe, fullfilRelativePath);
            string utvalgfilRelativePath = Path.Combine(kortnavn, $"{kortnavn}_u.{versjonUtenP}");
            string utvalgfilFullPath = Path.Combine(baseDirectory, produktgruppe, utvalgfilRelativePath);
            string deffilRelativePath = Path.Combine(kortnavn, $"{kortnavn}_d.{versjonUtenP}");
            string deffilFullPath = Path.Combine(baseDirectory, produktgruppe, deffilRelativePath);
            string defkatalogfilFullPath = Path.Combine(baseDirectory, produktgruppe, $"Def_{kortnavn}.{versjonUtenP}");

            string katalog = Path.GetDirectoryName(fullfilFullPath);

            if (!Directory.Exists(katalog))
            {
                Directory.CreateDirectory(katalog);
            }

            using (var file = new StreamWriter(defkatalogfilFullPath, false, Encoding.GetEncoding(1252)))
            {
                file.WriteLine("[SyntaksDefinisjoner]");
                file.WriteLine(deffilRelativePath);
                file.WriteLine($@"..\std\SOSISTD.{(sosiVersion == "4.5" ? "451" : "50")}");
                file.WriteLine("");
                file.WriteLine("[KodeForklaringer]");
                file.WriteLine($@"..\std\KODER.{(sosiVersion == "4.5" ? "45" : "50")}");
                file.WriteLine("");
                file.WriteLine("[UtvalgsRegler]");
                file.WriteLine(utvalgfilRelativePath);
                file.WriteLine("");
                file.WriteLine("[ObjektDefinisjoner]");
                file.WriteLine(fullfilRelativePath);
                if (modellHarFlateGeometri)
                {
                    file.WriteLine($@"..\std\Objektavgrensning.{(sosiVersion == "4.5" ? "45" : "50")}");
                }
            }

            using (var file = new StreamWriter(deffilFullPath, false, Encoding.GetEncoding(1252)))
            {
                List<Basiselement> listUnikeBasiselementer = new List<Basiselement>();
                List<Gruppeelement> listUnikeGruppeelementer = new List<Gruppeelement>();

                file.WriteLine("! ***** SOSI - Syntaksdefinisjoner **************!");
                foreach (Objekttype o in liste)
                {
                    LagSosiSyntaks(o, listUnikeBasiselementer, listUnikeGruppeelementer);
                }

                file.WriteLine(LagSosiSyntaksGrupper(listUnikeGruppeelementer));
                file.WriteLine(LagSosiSyntaksBasiselementer(listUnikeBasiselementer));

                /* Remove default types
                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..BEZIER S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..BUEP S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..DEF S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..FLATE S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..HODE S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..KLOTOIDE S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..KURVE S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..OBJDEF S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..OBJEKT S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..PUNKT S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..RASTER S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..SIRKELP S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..SLUTT S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..SVERM S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..SYMBOL S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..TEKST S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..TRASE S");

                file.WriteLine("");
                file.WriteLine(".DEF");
                file.WriteLine("..PERIODE *");
                file.WriteLine("...TIDSTART DATOTID");
                file.WriteLine("...TIDSLUTT DATOTID");
                */
            }

            using (var file = new StreamWriter(fullfilFullPath, false, Encoding.GetEncoding(1252)))
            {

                file.WriteLine("! ***************************************************************************!");
                file.WriteLine("! * SOSI-kontroll                                             " + kortnavn.ToUpper() + "-OBJEKTER *!");
                file.WriteLine("! * Objektdefinisjoner for " + kortnavn.ToUpper() + "          				                    *!");
                file.WriteLine("! *                            SOSI versjon  " + sosiVersion + "                            *!");
                file.WriteLine("! ***************************************************************************!");
                file.WriteLine("! *           Følger databeskrivelsene i Del 1, Praktisk bruk               *!");
                file.WriteLine("! *            og databeskrivelsene i Objektkatalogen,                      *!");
                file.WriteLine("! *                   Generert fra SOSI UML modell                          *!");
                file.WriteLine("! *                                                                         *!");
                file.WriteLine("! *               Statens kartverk, SOSI-sekretariatet           " + DateTime.Now.ToShortDateString() + " *!");
                file.WriteLine("! *                          nn                                             *!");
                file.WriteLine("! ***************************************************************************!");
                file.WriteLine("! ------ Antall objekttyper i denne katalogen: " + liste.Count);

                foreach (Objekttype o in liste)
                {
                    file.WriteLine("");
                    file.WriteLine(LagSosiObjekt(o, fagområde));

                }

            }

            using (StreamWriter file = new StreamWriter(utvalgfilFullPath, false, Encoding.GetEncoding(1252)))
            {

                file.WriteLine("! ***************************************************************************!");
                file.WriteLine("! * SOSI-kontroll                                             " + kortnavn.ToUpper() + "-UTVALG *!");
                file.WriteLine("! * Utvalgsregler for " + kortnavn.ToUpper() + "          				                    *!");
                file.WriteLine("! *                            SOSI versjon  " + sosiVersion + "                            *!");
                file.WriteLine("! ***************************************************************************!");
                file.WriteLine("! *           Følger databeskrivelsene i Del 1, Praktisk bruk               *!");
                file.WriteLine("! *            og databeskrivelsene i Objektkatalogen,                      *!");
                file.WriteLine("! *                   Generert fra SOSI UML modell                          *!");
                file.WriteLine("! *                                                                         *!");
                file.WriteLine("! *               Statens kartverk, SOSI-sekretariatet           " + DateTime.Now.ToShortDateString() + " *!");
                file.WriteLine("! *                          nn                                             *!");
                file.WriteLine("! ***************************************************************************!");
                file.WriteLine("! ------ Antall definerte objekttyper i denne katalogen: " + liste.Count);

                foreach (Objekttype o in liste)
                {
                    SkrivGruppeUtvalg(file, o.UML_Navn);
                }

                if (modellAvgrensesAvKantUtsnitt)
                    SkrivGruppeUtvalg(file, "KantUtsnitt");

                if (modellAvgrensesAvFlateavgrensning)
                    SkrivGruppeUtvalg(file, "Flateavgrensning");
            }

            Process.Start(baseDirectory);
        }

        private static void SkrivGruppeUtvalg(StreamWriter file, string objektNavn)
        {
            file.WriteLine("");
            file.WriteLine($".GRUPPE-UTVALG {objektNavn}");
            file.WriteLine($"..VELG  \"..OBJTYPE\" = {objektNavn}");
            file.WriteLine($"..BRUK-REGEL {objektNavn}");
        }


        internal string LagSosiSyntaks(Objekttype o, List<Basiselement> listeUnikeBasiselementer, List<Gruppeelement> listeUnikeGruppeelementer)
        {
            string tmp = Environment.NewLine;

            foreach (var b in o.Egenskaper)
            {
                tmp = LagSosiSyntaksEgenskap(tmp, b, listeUnikeBasiselementer, listeUnikeGruppeelementer);
            }

            tmp = LagSosiSyntaksArvetObjekt(tmp, o, listeUnikeBasiselementer, listeUnikeGruppeelementer);

            return tmp;
        }

        internal string LagSosiSyntaksGrupper(List<Gruppeelement> listGruppe)
        {
            string tmp = "";
            foreach (Gruppeelement b in listGruppe)
            {
                tmp = tmp + "" + Environment.NewLine;
                tmp = tmp + ".DEF" + Environment.NewLine;
                tmp = LagSosiSyntaksEgenskap(tmp, b, null, null);

            }
            return tmp;
        }

        internal string LagSosiSyntaksBasiselementer(List<Basiselement> listBasis)
        {
            string tmp = "";
            foreach (Basiselement b in listBasis)
            {
                tmp = tmp + "" + Environment.NewLine;
                tmp = tmp + ".DEF" + Environment.NewLine;
                tmp = LagSosiSyntaksEgenskap(tmp, b, null, null);
            }
            return tmp;

        }


        private static string LagSosiSyntaksEgenskap(string tmp, AbstraktEgenskap b1, List<Basiselement> listeUnikeBasiselementer, List<Gruppeelement> listeUnikeGruppeelementer)
        {
            if (b1 is Basiselement)
            {
                Basiselement b = (Basiselement)b1;
                tmp = tmp + b.SOSI_Navn + " " + b.Datatype + Environment.NewLine;
                if (listeUnikeBasiselementer != null) if (listeUnikeBasiselementer.Contains(b) == false) listeUnikeBasiselementer.Add(b);
            }
            else
            {
                Gruppeelement g = (Gruppeelement)b1;
                tmp = tmp + g.SOSI_Navn + " *" + Environment.NewLine;
                if (listeUnikeGruppeelementer != null) if (listeUnikeGruppeelementer.Contains(g) == false) listeUnikeGruppeelementer.Add(g);
                foreach (var b2 in g.Egenskaper)
                {
                    tmp = LagSosiSyntaksEgenskap(tmp, b2, listeUnikeBasiselementer, listeUnikeGruppeelementer);
                }
            }
            return tmp;
        }

        private static string LagSosiSyntaksArvetObjekt(string tmp, Objekttype o, List<Basiselement> listeUnikeBasiselementer, List<Gruppeelement> listeUnikeGruppeelementer)
        {
            if (o.Inkluder != null)
            {

                foreach (var b1 in o.Inkluder.Egenskaper)
                {
                    tmp = LagSosiSyntaksEgenskap(tmp, b1, listeUnikeBasiselementer, listeUnikeGruppeelementer);
                }

                if (o.Inkluder.Inkluder != null) tmp = LagSosiSyntaksArvetObjekt(tmp, o.Inkluder, listeUnikeBasiselementer, listeUnikeGruppeelementer);
            }
            return tmp;

        }

        public string LagSosiObjekt(Objekttype o, bool isFagområde)
        {
            StringBuilder builder = new StringBuilder(Environment.NewLine);
            builder.AppendLine(".OBJEKTTYPE");

            builder.Append("..TYPENAVN ").AppendLine(o.UML_Navn);
            if (o.Geometrityper.Count > 0)
                builder.Append("..GEOMETRITYPE ").AppendLine(string.Join(",", o.Geometrityper.ToArray(), 0, o.Geometrityper.Count));
            if (o.AvgrensesAv.Count > 0)
            {
                builder.Append("..AVGRENSES_AV ").Append(string.Join(",", o.AvgrensesAv.ToArray(), 0, o.AvgrensesAv.Count));
                if (o.Geometrityper.Any())
                {
                    var harFlate = o.HarGeometri("FLATE");
                    builder.AppendLine(harFlate ? ",KantUtsnitt" : string.Empty);
                    if (harFlate || _datasettErForvaltetMedNgis)
                    {
                        builder.Append("..INKLUDER ");
                        builder.Append(harFlate
                            ? o.AvgrensesAv.Contains("flateavgrensning")
                                ? "Flateavgrensning,KantUtsnitt"
                                : "KantUtsnitt"
                            : string.Empty);
                        builder.AppendLine(_datasettErForvaltetMedNgis ? ",Ngis" : string.Empty);
                    }
                }
            }
            if (o.Avgrenser.Count > 0)
                builder.Append("..AVGRENSER ").AppendLine(string.Join(",", o.Avgrenser.ToArray(), 0, o.Avgrenser.Count));

            builder.Append("..PRODUKTSPEK ").AppendLine(o.Standard.ToUpper());

            if (isFagområde)
                builder.AppendLine("..INKLUDER SOSI_Objekt");

            foreach (var b1 in o.Egenskaper)
            {
                builder.Append(LagSosiEgenskap(b1));
            }
            builder.Append(LagSosiArvetObjekt(o));
        
            return builder.ToString();
        }

        private bool ErForvaltetMedNgis(string kortnavn)
        {
            // N5 skal være med. N50, N500 ... skal ikke.
            return (kortnavn.StartsWith("N5") && !kortnavn.StartsWith("N50")) ||
                   NgisDatasetShortnames.Any(kortnavn.StartsWith);
        }

        private static string LagSosiArvetObjekt(Objekttype o)
        {
            StringBuilder builder = new StringBuilder();
            if (o.Inkluder != null)
            {
                foreach (var b1 in o.Inkluder.Egenskaper)
                {
                    builder.Append(LagSosiEgenskap(b1));
                }

                if (o.Inkluder.Inkluder != null)
                    builder.Append(LagSosiArvetObjekt(o.Inkluder));
            }
            return builder.ToString();
        }


        private static string LagSosiEgenskap(AbstraktEgenskap sosiEgenskap)
        {
            StringBuilder builder = new StringBuilder();
            var basiselement = sosiEgenskap as Basiselement;
            if (basiselement != null)
            {
                if (basiselement.TillatteVerdier.Count > 0)
                    SkrivEgenskapMedVerdier(builder, basiselement, basiselement.TillatteVerdier.ToArray());

                else if (basiselement.HarStandardVerdi())
                    SkrivEgenskapMedVerdier(builder, basiselement, basiselement.StandardVerdi);

                else if (basiselement.Datatype == "REF")

                    builder.Append("..EGENSKAP \"")
                        .Append(basiselement.UML_Navn)
                        .Append("\" * \"")
                        .Append(basiselement.SOSI_Navn)
                        .Append("\"    ")
                        .Append("*")
                        .Append("  ")
                        .Append(basiselement.Multiplisitet.Replace("[", "").Replace("]", "").Replace("*", "N").Replace(".", " "))
                        .AppendLine("  >< ()");
                else
                    builder.Append("..EGENSKAP \"")
                        .Append(basiselement.UML_Navn)
                        .Append("\" * \"")
                        .Append(basiselement.SOSI_Navn)
                        .Append("\"    ")
                        .Append(basiselement.Datatype)
                        .Append("  ")
                        .Append(basiselement.Multiplisitet.Replace("[", "").Replace("]", "").Replace("*", "N").Replace(".", " "))
                        .AppendLine("  >< ()");

            }
            else
            {
                Gruppeelement g = (Gruppeelement)sosiEgenskap;
                builder.Append("..EGENSKAP \"")
                    .Append(g.UML_Navn)
                    .Append("\" * \"")
                    .Append(g.SOSI_Navn)
                    .Append("\"    *  ")
                    .Append(g.Multiplisitet.Replace("[", "").Replace("]", "").Replace("*", "N").Replace(".", " "))
                    .AppendLine("  >< ()");

                foreach (var b2 in g.Egenskaper)
                {
                    builder.Append(LagSosiEgenskap(b2));
                }
                if (g.Inkluder != null)
                {
                    builder.Append(LagSosiEgenskap(g.Inkluder));
                }
            }
            return builder.ToString();
        }

        private static void SkrivEgenskapMedVerdier(StringBuilder builder, Basiselement basiselement, params string[] verdier)
        {
            builder.Append("..EGENSKAP \"")
                .Append(basiselement.UML_Navn)
                .Append("\" * \"")
                .Append(basiselement.SOSI_Navn)
                .Append("\"    ")
                .Append(basiselement.Datatype)
                .Append("  ")
                .Append(basiselement.Multiplisitet.Replace("[", "").Replace("]", "").Replace("*", "N").Replace(".", " "))
                .Append("  ").Append(basiselement.Operator)
                .Append(" (")
                .Append(string.Join(",", verdier))
                .AppendLine(")");
        }

    }
}
