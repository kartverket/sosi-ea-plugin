using System;
using System.Collections.Generic;
using System.Linq;
using EA;
using Attribute = EA.Attribute;

namespace Arkitektum.Kartverket.SOSI.Model
{
    public class Sosimodell
    {
        private readonly Repository _repository;

        private List<Objekttype> _objekttyper;
        public Sosimodell(Repository repository)
        {
            this._repository = repository;
        }

        public List<Objekttype> ByggObjektstruktur()
        {
            _objekttyper = new List<Objekttype>();

            Package valgtPakke = _repository.GetTreeSelectedPackage();

            _objekttyper = LagObjekttyperForElementerIPakke(valgtPakke);

            if (SkalLeggeTilFlateavgrensning(_objekttyper, out var navnPaFlaterSomManglerFlateavgrensning))
            {
                Logg("Legger til Flateavgrensning");
                _objekttyper.Add(OpprettFlateavgrensning(HentApplicationSchemaPakkeNavn(valgtPakke.Element), navnPaFlaterSomManglerFlateavgrensning));
            }

            if (SkalLeggeTilKantUtsnitt(_objekttyper))
            {
                Logg("Ingen objekter med navn KantUtsnitt funnet. Legger til KantUtsnitt");
                _objekttyper.Add(OpprettKantUtsnitt(HentApplicationSchemaPakkeNavn(valgtPakke.Element)));
            }


            return _objekttyper;
        }

        private bool SkalLeggeTilKantUtsnitt(List<Objekttype> objekttyper)
        {
            return objekttyper.Any(o => o.HarGeometri("flate"))
                && !objekttyper.Any(o => string.Equals(o.UML_Navn, "KantUtsnitt", StringComparison.InvariantCultureIgnoreCase));
        }

        private Objekttype OpprettKantUtsnitt(string standard)
        {
            return new Objekttype()
            {
                UML_Navn = "KantUtsnitt",
                ErOpprettetMaskinelt = true,

                // Maskinelt opprettet KantUtsnitt skrives som
                // "..INKLUDER KantUtsnitt" i SOSIKontrollGenerator

                Geometrityper = new List<string>() { "KURVE" },
                Standard = standard,
                Egenskaper = new List<AbstraktEgenskap>()
                {
                    new Basiselement()
                    {
                        SOSI_Navn = "..OBJTYPE",
                        Operator =  "=",
                        TillatteVerdier = new List<string> {"KantUtsnitt"},
                        Multiplisitet = "[1..1]",
                        Datatype = "T12"
                    }
                },
                OCLconstraints = new List<Beskrankning>
                {
                    new Beskrankning()
                    {
                        Navn = "KantUtsnitt",
                        Notat = "Objekttypen kan forekomme som et resultat av klipping av datasettet."
                    }
                }
            };
        }

        private bool SkalLeggeTilFlateavgrensning(List<Objekttype> objekttyper, out List<string> navnPaFlaterSomManglerFlateavgrensning)
        {
            var flaterSomManglerFlateavgrensning = objekttyper.Where(o => o.HarGeometri("flate") && o.AvgrensesAv.Count == 0).ToList();
            navnPaFlaterSomManglerFlateavgrensning = new List<string>();

            foreach (Objekttype objekttype in objekttyper)
            {
                if (objekttype.HarGeometri("flate"))
                {
                    foreach (string avgrensesAv in objekttype.AvgrensesAv)
                    {
                        Objekttype avgrensesAvObjekttype = FinnObjekttypeMedNavn(objekttyper, avgrensesAv);
                        if (avgrensesAvObjekttype == null)
                        {
                            LoggDebug("Finner ikke objektet [{avgrensesAv}] som avgrenser objekttypen [{objekttype.UML_Navn}]");
                            continue;
                        }

                        if (avgrensesAvObjekttype.ErEnFlateavgrensning())
                        {
                            Logg($"Flaten i {objekttype.UML_Navn} blir avgrenset av kurven i {avgrensesAvObjekttype.UML_Navn}. Legger ikke til egen flateavgrensning.");
                            break;
                        }
                    }
                }
            }

            foreach (var objekttype in flaterSomManglerFlateavgrensning)
            {
                Logg($"Flaten i {objekttype.UML_Navn} mangler avgrensninger, legger til egen flateavgrensning");
                objekttype.AvgrensesAv.Add("Flateavgrensning");
                navnPaFlaterSomManglerFlateavgrensning.Add(objekttype.UML_Navn);
            }

            return flaterSomManglerFlateavgrensning.Any();
        }

        private void LoggDebug(string melding)
        {
#if DEBUG
            _repository.WriteOutput("System", $"DEBUG: {melding}", 0);
#endif
        }

        private void Logg(string melding)
        {
            _repository.WriteOutput("System", melding, 0);
        }

        private Objekttype FinnObjekttypeMedNavn(List<Objekttype> objekttyper, string navn)
        {
            return objekttyper.FirstOrDefault(o => string.Equals(o.UML_Navn, navn, StringComparison.InvariantCultureIgnoreCase));
        }

        private static Objekttype OpprettFlateavgrensning(string standard, List<string> navnPaFlaterSomManglerFlateavgrensning)
        {
            return new Objekttype()
            {
                UML_Navn = "Flateavgrensning",
                Geometrityper = new List<string>() { "KURVE" },
                Standard = standard,
                Egenskaper = new List<AbstraktEgenskap>()
                {
                    new Basiselement()
                    {
                        SOSI_Navn = "..OBJTYPE",
                        Operator =  "=",
                        TillatteVerdier = new List<string> {"Flateavgrensning"},
                        Multiplisitet = "[1..1]",
                        Datatype = "T18"
                    }
                },
                OCLconstraints = new List<Beskrankning>
                {
                    new Beskrankning()
                    {
                        Navn = "Flateavgrensning",
                        Notat = "Objekttypen er lagt til for å avgrense flaten for å tilfredsstille geometrimodellen i SOSI-formatet."
                    }
                },
                Avgrenser = navnPaFlaterSomManglerFlateavgrensning,
            };
        }

        private List<Objekttype> LagObjekttyperForElementerIPakke(Package pakke)
        {
            var objekttyper = new List<Objekttype>();

            foreach (Element element in pakke.Elements)
            {
                if (SkalLageObjekttypeAvElement(element))
                {
                    objekttyper.Add(LagObjekttype(element, "..", true, null));
                }
            }
            foreach (Package underpakke in pakke.Packages)
            {
                objekttyper.AddRange(LagObjekttyperForElementerIPakke(underpakke));
            }
            return objekttyper;
        }

        private static bool SkalLageObjekttypeAvElement(Element el)
        {
            return el.Type == "Class" && (el.Stereotype.ToLower() == "featuretype" || el.Stereotype.ToLower() == "type") && el.Abstract == "0";
        }


        public Objekttype LagObjekttype(Element element, string prikknivå, bool lagObjekttype, List<Beskrankning> oclFraSubObjekt)
        {
            var standard = HentApplicationSchemaPakkeNavn(element);

            var objekttype = new Objekttype
            {
                Egenskaper = new List<AbstraktEgenskap>(),
                Geometrityper = new List<string>(),
                OCLconstraints = new List<Beskrankning>(),
                Avgrenser = new List<string>(),
                AvgrensesAv = new List<string>(),
                UML_Navn = element.Name,
                Notat = element.Notes,
                SOSI_Navn = prikknivå,
                Standard = standard
            };

            if (lagObjekttype)
            {
                var basiselement = new Basiselement
                {
                    Standard = standard,
                    SOSI_Navn = prikknivå + "OBJTYPE",
                    UML_Navn = "",
                    Operator = "=",
                    TillatteVerdier = new List<string> { element.Name },
                    Multiplisitet = "[1..1]",
                    Datatype = "T32"
                };

                objekttype.Egenskaper.Add(basiselement);

                if (element.Name.TrimStart('.').Length > 32)
                    _repository.WriteOutput("System", "FEIL: Objektnavn er lengre enn 32 tegn - " + element.Name, 0);
            }

            if (oclFraSubObjekt != null)
            {
                foreach (Beskrankning beskrankning in oclFraSubObjekt)
                {
                    objekttype.LeggTilBeskrankning(beskrankning);
                }
            }

            objekttype.LeggTilBeskrankninger(LagBeskrankninger(element));

            //Sjekk for modell 5 regler på topo/avgrensninger
            var avgrensninger = LagTopoAvgrensninger(element);
            if (avgrensninger.Any())
                objekttype.AvgrensesAv.AddRange(avgrensninger);

            foreach (Attribute att in element.Attributes)
            {
                try
                {
                    var type = GetAttributeType(att.Type);

                    if (type == "flate" || type == "punkt" || type == "sverm")
                    {
                        objekttype.Geometrityper.Add(type.ToUpper());
                    }
                    else if (type == "kurve")
                    {
                        objekttype.Geometrityper.Add("KURVE");
                        objekttype.Geometrityper.Add("BUEP");
                        objekttype.Geometrityper.Add("SIRKELP");
                        objekttype.Geometrityper.Add("BEZIER");
                        objekttype.Geometrityper.Add("KLOTOIDE");
                    }
                    else
                    {
                        var basiselementBuilder = new BasiselementBuilder(_repository, prikknivå, standard).ForAttributt(att);

                        if (att.ClassifierID != 0)
                        {
                            var attributtElement = _repository.GetElementByID(att.ClassifierID);

                            if (BasiselementBuilder.ErBasistype(att.Type))
                            {
                                objekttype.LeggTilEgenskap(basiselementBuilder.MedMappingAvBasistyper().Opprett());
                            }
                            else if (attributtElement.IsCodeList())
                            {
                                objekttype.LeggTilEgenskap(LagKodelisteEgenskap(prikknivå, attributtElement, att,
                                    objekttype.OCLconstraints));
                            }
                            else if (attributtElement.IsUnion())
                            {
                                LagUnionEgenskaper(prikknivå, attributtElement, objekttype, standard);
                            }
                            else
                            {
                                objekttype.LeggTilEgenskap(LagGruppeelement(attributtElement, att, prikknivå, objekttype));
                            }
                        }
                        else
                        {
                            objekttype.LeggTilEgenskap(basiselementBuilder.MedMappingAvBasistyper().Opprett());
                        }
                    }
                }
                catch (Exception ex)
                {
                    _repository.WriteOutput("System", "FEIL: Finner ikke datatype for " + att.Name + " på " + element.Name + " :" + ex.Message, 0);
                }
            }

            foreach (Connector connector in element.Connectors)
            {
                if (connector.MetaType == "Association" || connector.MetaType == "Aggregation" || connector.MetaType == "topo")
                {
                    Element source = _repository.GetElementByID(connector.SupplierID);
                    Element destination = _repository.GetElementByID(connector.ClientID);

                    var isSource = source.Name == element.Name;

                    if (connector.IsTopoType())
                    {
                        AddTopo(element, objekttype, connector, source, destination);
                    }
                    else if (connector.SupplierEnd.Aggregation == 2 && connector.ErNavigerbar(isSource)) //Composite
                    {
                        objekttype.Egenskaper.Add(LagGruppeelementKomposisjon(connector, prikknivå, objekttype));
                    }
                    else if (connector.ErNavigerbar(isSource))
                    {
                        objekttype.Egenskaper.AddRange(LagConnectorEgenskaper(prikknivå, connector, element, standard, objekttype));
                    }
                }
                else if (connector.MetaType == "Generalization")
                {
                    var connectorSupplierElement = _repository.GetElementByID(connector.SupplierID);
                    if (element.Name != connectorSupplierElement.Name)
                    {
                        var tmp2 = LagObjekttype(connectorSupplierElement, prikknivå, false, null);
                        objekttype.Inkluder = tmp2;
                        foreach (var geometritype in tmp2.Geometrityper.Where(g => !objekttype.Geometrityper.Contains(g)))
                        {
                            objekttype.Geometrityper.Add(geometritype);
                        }
                        foreach (var avgrensetObjekt in tmp2.Avgrenser.Where(o => !objekttype.Avgrenser.Contains(o)))
                        {
                            objekttype.Avgrenser.Add(avgrensetObjekt);
                        }
                        foreach (var avgrensendeObjekt in tmp2.AvgrensesAv.Where(obj => !objekttype.AvgrensesAv.Contains(obj)))
                        {
                            objekttype.AvgrensesAv.Add(avgrensendeObjekt);
                        }

                        foreach (Beskrankning b in tmp2.OCLconstraints)
                        {
                            objekttype.LeggTilBeskrankning(b, tmp2.UML_Navn);
                        }
                    }
                }
            }
            return objekttype;
        }

        private static IEnumerable<string> LagTopoAvgrensninger(Element element)
        {
            foreach (Constraint constraint in element.Constraints.Cast<Constraint>()
                         .Where(c => c.Name.StartsWith("KanAvgrensesAv")))
            {
                var objectTypeNames = constraint.Name.Remove(0, 15);
                var objectTypes = objectTypeNames.Split(',');
                foreach (var objectType in objectTypes)
                {
                    yield return objectType.Trim();
                }
            }
        }

        private static IEnumerable<Beskrankning> LagBeskrankninger(Element element)
        {
            return from Constraint constraint in element.Constraints select LagBeskrankning(constraint);
        }

        private static bool ErTomDatatype(Element element)
        {
            return element.Attributes.Count == 0;
        }

        private Basiselement LagEgenskapForKjentType(string prikknivå, Attribute attributt, string standard)
        {
            var builder = new BasiselementBuilder(_repository, prikknivå, standard);
            var basiselement = builder.ForAttributt(attributt).Opprett();
            basiselement.Datatype = KjentTypeMapper.GetKjentType(attributt.Type).Datatype;

            return basiselement;
        }

        private void AddTopo(Element e, Objekttype ot, Connector connector, Element source, Element destination)
        {
            //TODO Skal ikke avgrense på abstrakte objekter kun på realiserbare
            if (connector.Direction == "Bi-Directional")
            {
                _repository.WriteOutput("System", "FEIL: Topo assosiasjonen kan ikke ha 'Bi-Directional' mellom " + source.Name + " og " + destination.Name, 0);
            }
            else if (connector.Direction == "Source -> Destination")
            {
                if (source.Name != e.Name)
                {
                    if (source.Abstract == "0") 
                        ot.AvgrensesAv.Add(source.Name);
                    ot.AvgrensesAv.AddRange(HentRealiserbareObjektarvListe(source));
                }
                else
                {
                    if (destination.Abstract == "0") 
                        ot.Avgrenser.Add(destination.Name);
                    ot.Avgrenser.AddRange(HentRealiserbareObjektarvListe(destination));
                }
            }
            else if (connector.Direction == "Destination -> Source")
            {
                if (destination.Name != e.Name)
                {
                    if (destination.Abstract == "0")
                        ot.AvgrensesAv.Add(destination.Name);
                    ot.AvgrensesAv.AddRange(HentRealiserbareObjektarvListe(destination));
                }
                else
                {
                    if (source.Abstract == "0")
                        ot.Avgrenser.Add(source.Name);
                    ot.Avgrenser.AddRange(HentRealiserbareObjektarvListe(source));

                }
            }
            else if (connector.Direction == "Unspecified")
            {
                _repository.WriteOutput("System", "ADVARSEL: Topo assosiasjonen mangler angivelse av 'Direction' mellom " + source.Name + " og " + destination.Name, 0);
            }
        }

        private IEnumerable<string> HentRealiserbareObjektarvListe(Element source)
        {
            var objektnavn = new List<string>();

            foreach (Connector connector in source.Connectors)
            {
                if (connector.MetaType == "Generalization")
                {
                    Element elm = _repository.GetElementByID(connector.ClientID);
                    if (source.Name != elm.Name)
                    {
                        if (elm.Abstract == "0")
                            objektnavn.Add(elm.Name);
                        else
                        {
                            IEnumerable<string> arvnavn = HentRealiserbareObjektarvListe(elm);
                            if (arvnavn.Count() > 0)
                                objektnavn.AddRange(arvnavn);
                        }
                    }
                }
            }

            return objektnavn;
        }

        private void LagUnionEgenskaper(string prikknivå, Element element, Objekttype ot, string standard)
        {
            var attributeName = new List<string>();

            foreach (Attribute attributt in element.Attributes)
            {
                attributeName.Add(attributt.Name);

                if (attributt.ClassifierID != 0)
                {
                    var attributtElement = _repository.GetElementByID(attributt.ClassifierID);

                    if (attributtElement.IsCodeList())
                    {
                        ot.Egenskaper.Add(LagKodelisteEgenskap(prikknivå + ".", attributtElement, attributt, ot.OCLconstraints));
                    }
                    else if (attributtElement.IsUnion())
                    {
                        LagUnionEgenskaper(prikknivå, attributtElement, ot, standard);
                    }
                    else if (attributt.IsBasicTypeOrBasicGeometryType())
                    {
                        ot.Egenskaper.Add(LagEgenskap(prikknivå + ".", attributt, standard));
                    }
                    else
                    {
                        ot.Egenskaper.Add(LagGruppeelement(attributtElement, attributt, prikknivå, ot));
                    }
                }
                else
                {
                    ot.Egenskaper.Add(LagEgenskap(prikknivå + ".", attributt, standard));
                }
            }

            ot.OCLconstraints.Add(new Beskrankning
            {
                Navn = "Union " + element.Name,
                Notat = "et av elementene " + string.Join(",", attributeName.ToArray(), 0, attributeName.Count) + " er påkrevet"
            });
        }

        private string GetAttributeType(string type)
        {
            type = type.ToLower();
            if (type == "gm_point")
                type = "punkt";
            else if (type == "gm_multipoint")
                type = "sverm";
            else if (type == "gm_curve" || type == "gm_compositecurve")
                type = "kurve";
            else if (type == "gm_surface" || type == "gm_compositesurface")
                type = "flate";

            return type;
        }

        private List<AbstraktEgenskap> LagConnectorEgenskaper(string prikknivå, Connector connector, Element e, string standard, Objekttype ot)
        {
            List<AbstraktEgenskap> retur = new List<AbstraktEgenskap>();

            var typeAssosiasjon = RepositoryHelper.GetConnectorTaggedValue(connector.TaggedValues, "sosi_assosiasjon");
            
            Element source = _repository.GetElementByID(connector.SupplierID);
            Element destination = _repository.GetElementByID(connector.ClientID);

            if (typeAssosiasjon == null)
            {
                _repository.WriteOutput("System", "ADVARSEL: Det er ikke definert type SOSI assosiasjon mellom " + source.Name + " og " + destination.Name + " assosiasjonen. Behandles som REF.", 0);
                typeAssosiasjon = "ref";
            }

            typeAssosiasjon = typeAssosiasjon.ToLower();

            var isSource = source.Name == e.Name;

            if (isSource)
            {
                if (typeAssosiasjon == "primærnøkler")
                {
                    var sosiNavnRef = RepositoryHelper.GetRoleTaggedValue(connector.ClientEnd.TaggedValues, RepositoryHelper.SosiNavn);
                    
                    var assosiasjon = new Gruppeelement
                    {
                        SOSI_Navn = sosiNavnRef == null ? "" : prikknivå + sosiNavnRef,
                        Multiplisitet = "[" + connector.ClientEnd.Cardinality + "]",
                        UML_Navn = connector.ClientEnd.Role + "(rolle)",
                    };
                    assosiasjon.FiksMultiplisitet();

                    if (sosiNavnRef == null)
                    {
                        _repository.WriteOutput("System", "FEIL: Finner ikke tagged value SOSI_navn for " + connector.ClientEnd.Role + " på " + destination.Name, 0);
                    }

                    var sjekkedeObjekter = new List<string>();
                    SosiEgenskapRetur refSosinavn = FinnPrimærnøkkel(destination, prikknivå + ".", standard, ot, sjekkedeObjekter);

                    if (refSosinavn != null)
                    {
                        foreach (AbstraktEgenskap item in refSosinavn.Egenskaper)
                        {
                            item.FiksMultiplisitet();
                        }
                        assosiasjon.Egenskaper = refSosinavn.Egenskaper;
                        retur.Add(assosiasjon);
                    }
                    else _repository.WriteOutput("System", "FEIL: Finner ikke primærnøkkel for " + connector.ClientEnd.Role + " på " + destination.Name, 0);
                }
                else if (typeAssosiasjon == "fremmednøkler")
                {
                    SosiEgenskapRetur pnSosinavn = FinnFremmednøkkel(destination, source, connector, connector.ClientEnd);
                    if (pnSosinavn != null)
                    {
                        var eg = new Basiselement
                        {
                            Datatype = pnSosinavn.SOSI_Datatype + pnSosinavn.SOSI_Lengde,
                            SOSI_Navn = prikknivå + pnSosinavn.SOSI_Navn,
                            UML_Navn = connector.ClientEnd.Role + "(rolle)",
                            Multiplisitet = "[" + connector.ClientEnd.Cardinality + "]",
                            Standard = HentApplicationSchemaPakkeNavn(destination),
                            TillatteVerdier = new List<string>()
                        };
                        eg.FiksMultiplisitet();
                        retur.Add(eg);
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(connector.ClientEnd.Role) && (connector.ClientEnd.Navigable == "Navigable" || connector.Direction == "Unspecified")) //pluss navigerbart
                    {
                        AddClientEnd(prikknivå, connector, retur, source, destination);
                    }
                    if (connector.SupplierID == connector.ClientID && !String.IsNullOrEmpty(connector.SupplierEnd.Role) && (connector.SupplierEnd.Navigable == "Navigable" || connector.Direction == "Unspecified")) //pluss navigerbart
                    {
                        AddSupplierEnd(prikknivå, connector, retur, source, destination);
                    }
                }
            }
            else
            {
                if (typeAssosiasjon == "primærnøkler")
                {
                    var sosiNavnRef = RepositoryHelper.GetRoleTaggedValue(connector.SupplierEnd.TaggedValues, RepositoryHelper.SosiNavn) ?? "";

                    var assosiasjon = new Gruppeelement
                    {
                        SOSI_Navn = prikknivå + sosiNavnRef,
                        Multiplisitet = "[" + connector.SupplierEnd.Cardinality + "]",
                        UML_Navn = connector.SupplierEnd.Role + "(rolle)",
                    };

                    if (sosiNavnRef.Length == 0)
                    {
                        _repository.WriteOutput("System", "FEIL: Finner ikke tagged value SOSI_navn for " + connector.SupplierEnd.Role + " på " + source.Name, 0);
                    }

                    assosiasjon.FiksMultiplisitet();

                    var sjekkedeObjekter = new List<string>();
                    SosiEgenskapRetur refSosinavn = FinnPrimærnøkkel(source, prikknivå + ".", standard, ot, sjekkedeObjekter);
                    if (refSosinavn != null)
                    {
                        foreach (AbstraktEgenskap item in refSosinavn.Egenskaper)
                        {
                            //item.Multiplisitet = "[" + connector.SupplierEnd.Cardinality + "]";
                            item.FiksMultiplisitet();
                        }

                        assosiasjon.Egenskaper = refSosinavn.Egenskaper;
                        retur.Add(assosiasjon);
                    }
                    else _repository.WriteOutput("System", "FEIL: Finner ikke primærnøkkel for " + connector.SupplierEnd.Role + " på " + source.Name, 0);
                }
                else if (typeAssosiasjon == "fremmednøkler")
                {
                    SosiEgenskapRetur pnSosinavn = FinnFremmednøkkel(source, destination, connector, connector.SupplierEnd);
                    if (pnSosinavn != null)
                    {
                        Basiselement eg = new Basiselement
                        {
                            UML_Navn = connector.SupplierEnd.Role + "(rolle)",
                            Multiplisitet = "[" + connector.SupplierEnd.Cardinality + "]",
                            Datatype = pnSosinavn.SOSI_Datatype + pnSosinavn.SOSI_Lengde,
                            SOSI_Navn = prikknivå + pnSosinavn.SOSI_Navn,
                            Standard = HentApplicationSchemaPakkeNavn(source),
                            TillatteVerdier = new List<string>()
                        };
                        eg.FiksMultiplisitet();
                        retur.Add(eg);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(connector.SupplierEnd.Role) &&
                        (connector.SupplierEnd.Navigable == "Navigable" || connector.Direction == "Unspecified")) //pluss navigerbart
                    {
                        AddSupplierEnd(prikknivå, connector, retur, source, destination);
                    }

                    if (connector.SupplierID == connector.ClientID && !string.IsNullOrEmpty(connector.ClientEnd.Role) &&
                        (connector.ClientEnd.Navigable == "Navigable" || connector.Direction == "Unspecified")) //pluss navigerbart
                    {
                        AddClientEnd(prikknivå, connector, retur, source, destination);
                    }
                }
            }
            return retur;
        }

        private void AddClientEnd(string prikknivå, Connector connector, List<AbstraktEgenskap> retur, Element source, Element destination)
        {
            var sosiNavn = RepositoryHelper.GetRoleTaggedValue(connector.ClientEnd.TaggedValues, RepositoryHelper.SosiNavn);

            var eg = new Basiselement
            {
                Datatype = "REF",
                UML_Navn = connector.ClientEnd.Role + "(rolle)",
                Multiplisitet = "[" + connector.ClientEnd.Cardinality + "]",
                Standard = HentApplicationSchemaPakkeNavn(destination),
                SOSI_Navn = prikknivå + (sosiNavn ?? "FIX"),
                TillatteVerdier = new List<string>()
            };
            eg.FiksMultiplisitet();

            retur.Add(eg);
            if (sosiNavn == null)
                _repository.WriteOutput("System", "FEIL: Mangler angivelse av tagged value sosi_navn på assosiasjonsende " + connector.ClientEnd.Role + " mellom " + source.Name + " og " + destination.Name, 0);
        }

        private void AddSupplierEnd(string prikknivå, Connector connector, List<AbstraktEgenskap> retur, Element source, Element destination)
        {
            var sosiNavn = RepositoryHelper.GetRoleTaggedValue(connector.SupplierEnd.TaggedValues, RepositoryHelper.SosiNavn);

            var eg = new Basiselement
            {
                UML_Navn = connector.SupplierEnd.Role + "(rolle)",
                Multiplisitet = "[" + connector.SupplierEnd.Cardinality + "]",
                Datatype = "REF",
                SOSI_Navn = prikknivå + (sosiNavn ?? "FIX"),
                Standard = HentApplicationSchemaPakkeNavn(source),
                TillatteVerdier = new List<string>()
            };
            eg.FiksMultiplisitet();

            retur.Add(eg);

            if (sosiNavn == null)
                _repository.WriteOutput("System", "FEIL: Mangler angivelse av tagged value sosi_navn på assosiasjonsende " + connector.SupplierEnd.Role + " mellom " + source.Name + " og " + destination.Name, 0);
        }

        private SosiEgenskapRetur FinnFremmednøkkel(Element destination, Element source, Connector connector, ConnectorEnd end)
        {
            SosiEgenskapRetur retur = null;
            var attributtnavn = RepositoryHelper.GetRoleTaggedValue(end.TaggedValues, "sosi_fremmednøkkel");

            if (attributtnavn?.Length > 0)
            {
                var sosiNavn = "";
                var sosiLengde = "";
                var sosiDatatype = "";
                foreach (Attribute att in source.Attributes)
                {
                    if (attributtnavn.Equals(att.Name.ToLower(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        foreach (TaggedValue tag in att.TaggedValues)
                        {
                            switch (tag.Name.ToLower())
                            {
                                case "sosi_navn":
                                    sosiNavn = tag.Value;
                                    break;
                                case "sosi_lengde":
                                    sosiLengde = tag.Value;
                                    break;
                                case "sosi_datatype":
                                    sosiDatatype = tag.Value;
                                    break;
                            }
                        }
                        retur = new SosiEgenskapRetur
                        {
                            SOSI_Navn = sosiNavn,
                            SOSI_Lengde = sosiLengde,
                            SOSI_Datatype = sosiDatatype
                        };
                        break;
                    }
                }
            }
            else
            {
                foreach (Attribute att in source.Attributes)
                {
                    var sosiFremmednøkkel = RepositoryHelper.GetTaggedValue(att.TaggedValues, "sosi_fremmednøkkel");

                    if (sosiFremmednøkkel?.Equals(destination.Name, StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        //Finnes alt så den skal ikke lages..
                        break;
                    }

                    _repository.WriteOutput("System", "FEIL: Ufullstendig assosiasjon: " + end.Role, 0);
                }
            }

            return retur;
        }

        private SosiEgenskapRetur FinnPrimærnøkkel(Element source, string prikknivå, string standard, Objekttype ot, List<string> arvedeobjekterSjekket)
        {
            var retur = new SosiEgenskapRetur
            {
                Egenskaper = new List<AbstraktEgenskap>()
            };

            foreach (Attribute att in source.Attributes)
            {
                var erPrimærnøkkel = false;

                var primærnøkkel = RepositoryHelper.GetTaggedValue(att.TaggedValues, "sosi_primærnøkkel");
                if (primærnøkkel?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    erPrimærnøkkel = true;
                }

                if (erPrimærnøkkel)
                {

                    if (att.IsBasicType())
                    {
                        retur.Egenskaper.Add(LagEgenskap(prikknivå, att, standard));
                    }
                    else //Kompleks type(datatype) som primærnøkkel
                    {
                        if (att.ClassifierID != 0)
                        {
                            Element attributeElement = _repository.GetElementByID(att.ClassifierID);
                            if (attributeElement.IsCodeList())
                            {
                                retur.Egenskaper.Add(LagKodelisteEgenskap(prikknivå, attributeElement, att, ot.OCLconstraints));
                            }
                            else
                            {
                                retur.Egenskaper.Add(LagGruppeelement(attributeElement, att, prikknivå, ot));
                            }
                        }
                        else
                        {
                            _repository.WriteOutput("System", "FEIL: Primærnøkkel er feil definert på : " + source.Name, 0);
                        }
                    }
                }

                foreach (Connector connector in source.Connectors)
                {
                    if (connector.MetaType == "Generalization")
                    {
                        Element connectorSourceElement = _repository.GetElementByID(connector.SupplierID);
                        if (source.Name != connectorSourceElement.Name)
                        {
                            if (arvedeobjekterSjekket.Contains(connectorSourceElement.Name) == false)
                            {
                                var ret = FinnPrimærnøkkel(connectorSourceElement, prikknivå, standard, ot, arvedeobjekterSjekket);
                                arvedeobjekterSjekket.Add(connectorSourceElement.Name);
                                retur.Egenskaper.AddRange(ret.Egenskaper);
                            }
                        }
                    }
                }
            }
            return retur;
        }

        private Gruppeelement LagGruppeelement(Element umlElement, Attribute att2, string prikknivå, Objekttype parentObjekttype)
        {
            var standard = HentApplicationSchemaPakkeNavn(umlElement);
            var gruppeelement = new Gruppeelement
            {
                Egenskaper = new List<AbstraktEgenskap>(),
                OCLconstraints = new List<Beskrankning>(),
                Notat = umlElement.Notes,
                SOSI_Navn = prikknivå,
                Standard = standard
            };
            var harSosiNavn = false;
            if (att2 == null)
            {
                gruppeelement.Multiplisitet = "[1..1]";
                gruppeelement.UML_Navn = umlElement.Name;
            }
            else
            {
                gruppeelement.UML_Navn = att2.Name;
                gruppeelement.Multiplisitet = "[" + att2.LowerBound + ".." + att2.UpperBound + "]";

                var sosiNavn = RepositoryHelper.GetTaggedValue(att2.TaggedValues, RepositoryHelper.SosiNavn);
                if (!string.IsNullOrEmpty(sosiNavn))
                {
                    gruppeelement.SOSI_Navn = prikknivå + sosiNavn;
                    harSosiNavn = gruppeelement.SOSI_Navn.Length > 0;
                }
            }
            if (!harSosiNavn)
            {
                var sosiNavn = RepositoryHelper.GetTaggedValue(umlElement.TaggedValues, RepositoryHelper.SosiNavn);
                if (!string.IsNullOrEmpty(sosiNavn))
                {
                    gruppeelement.SOSI_Navn = prikknivå + sosiNavn;
                    harSosiNavn = gruppeelement.SOSI_Navn.Length > 0;
                }
            }

            if (harSosiNavn == false) _repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_navn på gruppeelement: " + umlElement.Name, 0);

            foreach (Constraint constraint in umlElement.Constraints)
            {
                parentObjekttype.LeggTilBeskrankning(LagBeskrankning(constraint));
            }

            foreach (Attribute att in umlElement.Attributes)
            {
                var nestePrikknivå = prikknivå + ".";

                var basiselementBuilder = new BasiselementBuilder(_repository, nestePrikknivå, standard).ForAttributt(att);

                if (att.ClassifierID != 0)
                {
                    var attributtElement = _repository.GetElementByID(att.ClassifierID);

                    if (KjentTypeMapper.ErKjentType(att.Type))
                    {
                        gruppeelement.Egenskaper.Add(LagEgenskapForKjentType(nestePrikknivå, att, standard));
                    }
                    else if (attributtElement.IsCodeList())
                    {
                        gruppeelement.Egenskaper.Add(LagKodelisteEgenskap(nestePrikknivå, attributtElement, att, parentObjekttype.OCLconstraints));
                    }
                    else if (attributtElement.IsUnion())
                    {
                        LagUnionEgenskaperForGruppeelement(nestePrikknivå, attributtElement, parentObjekttype, standard, gruppeelement);
                    }
                    else if (BasiselementBuilder.ErBasistype(att.Type))
                    {
                        gruppeelement.LeggTilEgenskap(basiselementBuilder.MedMappingAvBasistyper().Opprett());
                    }
                    else if (ErTomDatatype(attributtElement))
                    {
                        gruppeelement.LeggTilEgenskap(basiselementBuilder.MedMappingAvTomDatatype().Opprett());
                    }
                    else
                    {
                        gruppeelement.Egenskaper.Add(LagGruppeelement(attributtElement, att, nestePrikknivå, parentObjekttype));
                    }
                }
                else
                {
                    gruppeelement.LeggTilEgenskap(basiselementBuilder.MedMappingAvBasistyper().Opprett());
                }
            }

            foreach (Connector connector in umlElement.Connectors)
            {
                if (connector.MetaType == "Association" || connector.MetaType == "Aggregation")
                {
                    Element source = _repository.GetElementByID(connector.SupplierID);
                    Element destination = _repository.GetElementByID(connector.ClientID);

                    var isSource = source.Name == umlElement.Name;

                    if (connector.SupplierEnd.Aggregation == 2 && connector.ErNavigerbar(isSource)) //Composite
                    {
                        gruppeelement.Egenskaper.Add(LagGruppeelementKomposisjon(connector, prikknivå, parentObjekttype));
                    }
                    else if (connector.ErNavigerbar(isSource))
                    {
                        gruppeelement.Egenskaper.AddRange(LagConnectorEgenskaper(prikknivå, connector, umlElement, standard, parentObjekttype));
                    }
                }
                else if (connector.MetaType == "Generalization")
                {
                    Element elmg = _repository.GetElementByID(connector.SupplierID);

                    if (umlElement.Name != elmg.Name)
                    {
                        Gruppeelement tmp2 = LagGruppeelement(elmg, null, prikknivå, parentObjekttype);
                        gruppeelement.Inkluder = tmp2;
                    }
                }
            }
            return gruppeelement;
        }

        private Gruppeelement LagGruppeelementKomposisjon(Connector conn, string prikknivå, Objekttype pot)
        {
            Element connectorClientElement = _repository.GetElementByID(conn.ClientID);
            var standard = HentApplicationSchemaPakkeNavn(connectorClientElement);

            var ot = new Gruppeelement
            {
                Egenskaper = new List<AbstraktEgenskap>(),
                OCLconstraints = new List<Beskrankning>(),
                UML_Navn = conn.ClientEnd.Role + "(rolle)",
                Notat = connectorClientElement.Notes,
                SOSI_Navn = prikknivå,
                Standard = standard,
                Multiplisitet = "[" + conn.ClientEnd.Cardinality + "]"
            };

            var sosiNavn = RepositoryHelper.GetRoleTaggedValue(conn.ClientEnd.TaggedValues, RepositoryHelper.SosiNavn);
            var harSosiNavn = sosiNavn != null;
            ot.SOSI_Navn = prikknivå + sosiNavn;

            if (harSosiNavn == false) 
                _repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_navn på komposisjon: " + conn.ClientEnd.Role, 0);

            foreach (Constraint constraint in connectorClientElement.Constraints)
            {
                pot.OCLconstraints.Add(LagBeskrankning(constraint));
            }

            foreach (Attribute att in connectorClientElement.Attributes)
            {
                if (att.ClassifierID != 0)
                {
                    var attributeElement = _repository.GetElementByID(att.ClassifierID);

                    if (KjentTypeMapper.ErKjentType(att.Type))
                    {
                        ot.Egenskaper.Add(LagEgenskapForKjentType(prikknivå, att, standard));
                    }
                    else if (attributeElement.IsCodeList())
                    {
                        ot.Egenskaper.Add(LagKodelisteEgenskap(prikknivå + ".", attributeElement, att, pot.OCLconstraints));
                    }
                    else if (attributeElement.IsUnion())
                    {
                        LagUnionEgenskaperForGruppeelement(prikknivå, attributeElement, pot, standard, ot);
                    }
                    else if (att.IsBasicTypeOrBasicGeometryType())
                    {
                        ot.Egenskaper.Add(LagEgenskap(prikknivå + ".", att, standard));
                    }
                    else
                    {
                        ot.Egenskaper.Add(LagGruppeelement(attributeElement, att, prikknivå + ".", pot));
                    }
                }
                else
                {
                    ot.Egenskaper.Add(LagEgenskap(prikknivå + ".", att, standard));
                }
            }

            foreach (Connector connector in connectorClientElement.Connectors)
            {
                if (connector.MetaType == "Association" || connector.MetaType == "Aggregation")
                {
                    Element source = _repository.GetElementByID(connector.SupplierID);
                    Element destination = _repository.GetElementByID(connector.ClientID);

                    var isSource = source.Name == connectorClientElement.Name;

                    if (connector.SupplierEnd.Aggregation == 2 && connector.ErNavigerbar(isSource)) //Composite
                    {
                        ot.Egenskaper.Add(LagGruppeelementKomposisjon(connector, prikknivå, pot));
                    }
                    else if (connector.ErNavigerbar(isSource))
                    {
                        ot.Egenskaper.AddRange(LagConnectorEgenskaper(prikknivå, connector, connectorClientElement, standard, pot));
                    }

                }
                if (connector.MetaType == "Generalization")
                {
                    Element connectorSupplierElement = _repository.GetElementByID(connector.SupplierID);
                    if (connectorClientElement.Name != connectorSupplierElement.Name)
                    {
                        ot.Inkluder = LagGruppeelement(connectorSupplierElement, null, prikknivå, pot);
                    }
                }
            }
            return ot;
        }

        private void LagUnionEgenskaperForGruppeelement(string prikknivå, Element element, Objekttype pot, string standard, Gruppeelement ot)
        {
            var attributeName = new List<string>();

            foreach (Constraint constraint in element.Constraints)
            {
                pot.OCLconstraints.Add(LagBeskrankning(constraint));
            }

            foreach (Attribute attributt in element.Attributes)
            {
                attributeName.Add(attributt.Name);

                if (attributt.ClassifierID != 0)
                {
                    var attributeElement = _repository.GetElementByID(attributt.ClassifierID);
                    
                    if (attributeElement.IsCodeList())
                    {
                        ot.Egenskaper.Add(LagKodelisteEgenskap(prikknivå + ".", attributeElement, attributt, pot.OCLconstraints));
                    }
                    else if (attributeElement.IsUnion())
                    {
                        LagUnionEgenskaperForGruppeelement(prikknivå, element, pot, standard, ot);
                    }
                    else if (attributt.IsBasicTypeOrBasicGeometryType())
                    {
                        ot.Egenskaper.Add(LagEgenskap(prikknivå + ".", attributt, standard));
                    }
                    else
                    {
                        ot.Egenskaper.Add(LagGruppeelement(attributeElement, attributt, prikknivå + ".", pot));
                    }
                }
                else
                {
                    ot.Egenskaper.Add(LagEgenskap(prikknivå + ".", attributt, standard));
                }
            }

            pot.OCLconstraints.Add(new Beskrankning
            {
                Navn = "Union " + element.Name,
                Notat = "Ett av elementene " + string.Join(",", attributeName.ToArray(), 0, attributeName.Count) + " er påkrevet"
            });
        }

        private static Beskrankning LagBeskrankning(Constraint constraint)
        {
            var oclDescription = "";
            if (constraint.Notes.Contains("/*") && constraint.Notes.Contains("*/"))
            {
                oclDescription = constraint.Notes.Substring(constraint.Notes.ToLower().IndexOf("/*") + 2,
                    constraint.Notes.ToLower().IndexOf("*/") - 2 - constraint.Notes.ToLower().IndexOf("/*"));
            }

            return new Beskrankning
            {
                Navn = constraint.Name,
                Notat = oclDescription,
                OCL = constraint.Notes
            };
        }

        private string HentApplicationSchemaPakkeNavn(Element elm)
        {
            while (true)
            {
                var pakkenavn = "FIX";

                if (elm.PackageID == 0) 
                    return pakkenavn;

                var pakke = _repository.GetPackageByID(elm.PackageID);
                if (pakke.Element == null) 
                    return pakkenavn;

                if (!pakke.Element.IsApplicationSchema() && !pakke.Element.IsUnderArbeid())
                {
                    elm = pakke.Element;
                    continue;
                }

                var status = "";
                if (pakke.Element.IsUnderArbeid())
                    status = " (under arbeid)";

                pakkenavn = pakke.Element.Name + status;
                string kortnavn = "";
                string versjon = "";

                foreach (TaggedValue tag in pakke.Element.TaggedValues)
                {
                    switch (tag.Name.ToLower())
                    {
                        case "sosi_kortnavn":
                            kortnavn = tag.Value;
                            break;
                        case "sosi_versjon":
                            versjon = tag.Value;
                            break;
                    }
                }

                return kortnavn.Length > 0 
                    ? $"{kortnavn} {versjon}{status}" 
                    : pakkenavn;
            }
        }

        private Basiselement LagKodelisteEgenskap(string prikknivå, Element element, Attribute att, List<Beskrankning> oclListe)
        {
            try
            {
                var builder = new BasiselementBuilder(_repository, prikknivå, HentApplicationSchemaPakkeNavn(element));
                var eg = builder.ForAttributt(att).MedDatatype("T").MedOperator("=").Opprett();

                var harSosiNavn = false;
                var harSosiLengde = false;

                var codelistHelper = new CodelistHelper(_repository);

                var verdierFraBeskrankninger = codelistHelper.HentTillatteVerdierFraBeskrankninger(element, att, oclListe, eg);
                if (verdierFraBeskrankninger.Any())
                {
                    eg.TillatteVerdier.AddRange(verdierFraBeskrankninger);
                }
                else
                {
                    eg.TillatteVerdier.AddRange(codelistHelper.HentLokaleKodelisteverdier(element));
                }

                if (string.IsNullOrEmpty(eg.SOSI_Navn))
                {
                    var navn = RepositoryHelper.GetTaggedValue(element.TaggedValues, RepositoryHelper.SosiNavn);
                    eg.SOSI_Navn = prikknivå + navn;
                    if (!string.IsNullOrEmpty(navn))
                        harSosiNavn = true;
                }

                var datatype = eg.Datatype;
                foreach (TaggedValue tag in element.TaggedValues)
                {
                    switch (tag.Name.ToLower())
                    {
                        case "length":
                            eg.Datatype = datatype + tag.Value;
                            harSosiLengde = true;
                            break;
                        case "sosi_lengde":
                            eg.Datatype = datatype + tag.Value;
                            harSosiLengde = true;
                            break;
                        case "sosi_datatype":
                            eg.Datatype = datatype.Replace("T", tag.Value);
                            break;
                    }
                }
                
                if (harSosiNavn == false) _repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_navn på kodeliste " + element.Name + ", attributt: " + att.Name, 0);
                if (harSosiLengde == false) _repository.WriteOutput("System", "FEIL: Mangler tagged value sosi_lengde på kodeliste " + element.Name + ", attributt: " + att.Name, 0);

                eg.TillatteVerdier.AddRange(codelistHelper.HentArvedeKodeverdier(element));
                return eg;
            }
            catch (Exception e)
            {
                _repository.WriteOutput("System", "FEIL: " + e.Message + " " + e.Source, 0);
                return null;
            }
        }

        private Basiselement LagEgenskap(string prikknivå, Attribute att, string standard)
        {
            var builder = new BasiselementBuilder(_repository, prikknivå, standard);

            var basiselement = builder.ForAttributt(att).MedMappingAvBasistyper().Opprett();

            if (basiselement.SOSI_Navn.TrimStart('.').Length > 32)
                _repository.WriteOutput("System", "FEIL: tagged value SOSI_navn er lengre enn 32 tegn på attributt: " + att.Name, 0);

            return basiselement;
        }

        public static IEnumerable<SosiKodeliste> ByggSosiKodelister(Package valgtPakke)
        {
            var kodelister = valgtPakke.Elements.Cast<Element>().Where(e => e.IsCodeList())
                .Select(LagSosiKodeliste).Where(kodeliste => kodeliste != null).ToList();

            foreach (Package subPakke in valgtPakke.Packages)
                kodelister.AddRange(ByggSosiKodelister(subPakke));

            return kodelister;
        }

        private static SosiKodeliste LagSosiKodeliste(IDualElement element)
        {
            var erSosiKodeliste = false;

            var kodeliste = new SosiKodeliste
            {
                Verdier = new List<SosiKode>(),
                Navn = element.Name
            };

            foreach (Attribute attributt in element.Attributes)
            {
                var sosiKode = new SosiKode
                {
                    Navn = attributt.Name,
                    Beskrivelse = attributt.Notes.Trim()
                };

                if (attributt.Default.Trim().Length > 0)
                {
                    sosiKode.SosiVerdi = attributt.Default.Trim();
                }
                else
                {
                    var sosiVerdi = RepositoryHelper.GetTaggedValue(attributt.TaggedValues, RepositoryHelper.SosiVerdi);
                    if (sosiVerdi != null)
                    {
                        sosiVerdi = sosiVerdi.Trim();
                        if (sosiVerdi.Length > 0)
                        {
                            sosiKode.SosiVerdi = sosiVerdi;
                            erSosiKodeliste = true;
                        }
                    }
                    else
                    {
                        sosiKode.SosiVerdi = attributt.Name;
                    }
                }
                kodeliste.Verdier.Add(sosiKode);
            }

            return erSosiKodeliste ? kodeliste : null;
        }
    }
}
