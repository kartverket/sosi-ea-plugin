﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EA;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using arkitektum.gistools.generators.Validator;
using Arkitektum.Kartverket.SOSI.Model;
using Arkitektum.Kartverket.SOSI.EA.Plugin.Services;

namespace Arkitektum.Kartverket.SOSI.EA.Plugin
{
    public class Main{
        private bool m_ShowFullMenus = false;
        private SosiNavigator eaSosiControl;
        private Repository _repository;
        List<ValidationResult> _validationResults = new List<ValidationResult>();

        public String EA_Connect(Repository repository)
        {
            _repository = repository;
            this.eaSosiControl = repository.AddWindow("Sosi", "Arkitektum.Kartverket.SOSI.EA.Plugin.SosiNavigator") as SosiNavigator;
            return "a string";
        }

        public Boolean EA_OnPostNewAttribute(Repository Repository, EventProperties Info)
        {
            Boolean oppdatert = false;
            if (Info != null)
            {
                global::EA.Attribute att = Repository.GetAttributeByID(Info.Get(0).Value);
                if (att.Stereotype.ToLower() == "attribute")
                {
                    att.Stereotype = "";
                    att.StereotypeEx = "";
                    att.Update();
                    SaveTaggedValueOnAttribute(att, "SOSI_navn", att.Name.ToUpper());
                    SaveTaggedValueOnAttribute(att, "SOSI_lengde", "");
                    SaveTaggedValueOnAttribute(att, "sequenceNumber", "");
                    SaveTaggedValueOnAttribute(att, "isMetadata", "false");
                }
                if (att.Stereotype.ToLower() == "kodelisteattribute")
                {
                    att.Stereotype = "";
                    att.StereotypeEx = "";
                    att.Update();
                    SaveTaggedValueOnAttribute(att, "SOSI_navn", att.Name.ToUpper());
                    SaveTaggedValueOnAttribute(att, "SOSI_lengde", "");
                    SaveTaggedValueOnAttribute(att, "sequenceNumber", "");
                    SaveTaggedValueOnAttribute(att, "isMetadata", "false");
                    SaveTaggedValueOnAttribute(att, "defaultCodeSpace", "");

                }
            }

            return oppdatert;
        }

        private static void SaveTaggedValueOnAttribute(global::EA.Attribute att, string taggedValueName, string taggedValue)
        {
                var tv1 = att.TaggedValues.AddNew(taggedValueName, "");
                tv1.Value = taggedValue;
                tv1.Update();
        }

        public Boolean EA_OnPostNewConnector(Repository Repository, EventProperties Info) {
            Boolean oppdatert = false;

            if (Info != null) 
            {
                Connector connector = Repository.GetConnectorByID(Info.Get(0).Value);
                if (connector.Stereotype.ToLower() == "topo")
                {
                    connector.ClientEnd.Role = "avgrensning";
                    RoleTag tv = connector.ClientEnd.TaggedValues.AddNew("xsdEncodingRule", "");
                    tv.Value = "notEncoded";
                    tv.Update();
                    connector.Update();
                    oppdatert = true;
                }
                else if (connector.Stereotype.ToLower() == "sosirefassosiasjon" || connector.Stereotype.ToLower() == "sosifremmednøklerassosiasjon" || connector.Stereotype.ToLower() == "sosiprimærnøklerassosiasjon")
                {
                    connector.SupplierEnd.Role = "angi navn";
                    RoleTag tv = connector.SupplierEnd.TaggedValues.AddNew("inlineOrByReference", "inlinebyrefEnum");
                    tv.Value = "inlineOrByReference";
                    tv.Update();
                    RoleTag tv2 = connector.SupplierEnd.TaggedValues.AddNew("SOSI_navn", "");
                    tv2.Update();
                    
                    connector.StyleEx="HideConnStereotype=1;";
                    connector.Update();
                    oppdatert = true;
                }
                
            }
            

            return oppdatert;
        }

        public void EA_OnContextItemChanged(Repository Repository, string GUID, ObjectType ot)
        {
            if (this.eaSosiControl == null)
                this.eaSosiControl = Repository.AddWindow("Sosi", "Arkitektum.Kartverket.SOSI.EA.Plugin.SosiNavigator") as SosiNavigator;
            if (this.eaSosiControl != null)
            {
                this.eaSosiControl.setSearch(Repository, GUID, ot);
            }
            

        }

        public object EA_GetMenuItems(Repository Repository, string Location, string MenuName)
        {

            switch (MenuName)
            {
                case "":
                    return "-&SOSI format realisering";
                case "-&SOSI format realisering":
                    string[] ar1 = { "&Lag SOSI format realisering", "Lag &definisjonsfiler for SOSI kontroll", "Om..." }; //, "Lag &Objektkatalog"
                    return ar1;
               

            }
            return "";
        }

        bool IsProjectOpen(Repository Repository)
        {
            try
            {
                Collection c = Repository.Models;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void EA_GetMenuState(Repository Repository, string Location, string MenuName, string ItemName, ref bool IsEnabled, ref bool IsChecked)
        {
            if (IsProjectOpen(Repository))
            {
                if (ItemName == "&Innstillinger...")
                    IsChecked = m_ShowFullMenus;

            }
            else
                IsEnabled = false;
        }

        public void EA_MenuClick(Repository Repository, string Location, string MenuName, string ItemName)
        {
            switch (ItemName)
            {
                case "&Lag SOSI format realisering":
                    GenererWordSyntaksSpesifikasjon(Repository);
                    break;
                case "Lag &definisjonsfiler for SOSI kontroll":
                    GenererDefinisjonsfiler(Repository);
                    break;
                case "Lag &Objektkatalog":
                    GenererObjektkatalog(Repository);
                    break;
                case "Om...":
                    frmAbout anAbout = new frmAbout();
                    anAbout.ShowDialog();
                    break;
            }
        }

        private void GenererObjektkatalog(Repository Repository)
        {
            if (!ModelIsValid() && (UserAbortsDueToInvalidModel() || UserAbortsDueToCircularReference())) return;

            try
            {

                Package valgtPakke = Repository.GetTreeSelectedPackage();
                if (valgtPakke.Element.Stereotype.ToLower() == "applicationschema" || valgtPakke.Element.Stereotype.ToLower() == "underarbeid")
                {

                    bool isFag = true;
                    string navn = valgtPakke.Name;
                    string beskrivelse = valgtPakke.Notes;
                    string organisasjon = "Kartverket";
                    string person = "ukjent";
                    string versjon = "";

                    foreach (TaggedValue theTags in valgtPakke.Element.TaggedValues)
                    {
                        switch (theTags.Name.ToLower())
                        {
                            case "sosi_spesifikasjonstype":
                                if (theTags.Value.ToLower() == "produktspesifikasjon") isFag = false;
                                break;
                            case "sosi_organisasjon":
                                organisasjon = theTags.Value;
                                break;
                            case "sosi_produsent":
                                person = theTags.Value;
                                break;
                            case "sosi_versjon":
                                versjon = theTags.Value;
                                break;
                        }

                    }

                    string eadirectory = Path.GetDirectoryName(Repository.ConnectionString);
                    string xmlfil = eadirectory + @"\objektkatalog\" + valgtPakke.Name + ".xml";


                    string katalog = Path.GetDirectoryName(xmlfil);

                    if (!Directory.Exists(katalog))
                    {
                        Directory.CreateDirectory(katalog);
                    }

                    Sosimodell modell = new Sosimodell(Repository);
                    List<Objekttype> otList = modell.ByggObjektstruktur();
                    List<SosiKodeliste> kodeList = modell.ByggSosiKodelister();
                   
                    XDocument doc = new ObjektKatalogGenerator().LagObjektKatalog(versjon, organisasjon, person, navn, beskrivelse, otList, isFag, kodeList);
                    doc.Save(xmlfil);

                    XDocument html = new ObjektKatalogGenerator().CreateHtmlCatalogForXmlDocument(doc);
                    string htmlfil = xmlfil.Replace(".xml", ".html");
                    html.Save(htmlfil);

                    System.Diagnostics.Process.Start(htmlfil);
                    
                    
                }
                else MessageBox.Show("Vennligst velg en pakke med stereotype applicationSchema.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void GenererWordSyntaksSpesifikasjon(Repository Repository)
        {
            if (!ModelIsValid() && (UserAbortsDueToInvalidModel() || UserAbortsDueToCircularReference())) return;

            try
            {
                Package valgtPakke = Repository.GetTreeSelectedPackage();
                if (valgtPakke.Element.Stereotype.ToLower() == "applicationschema" || valgtPakke.Element.Stereotype.ToLower() == "underarbeid")
                {
                    bool isFag = true;

                    foreach (TaggedValue theTags in valgtPakke.Element.TaggedValues)
                    {
                        switch (theTags.Name.ToLower())
                        {
                            case "sosi_spesifikasjonstype":
                                if (theTags.Value.ToLower() == "produktspesifikasjon") isFag=false;
                                break;
                        }

                    }

                    Sosimodell modell = new Sosimodell(Repository);
                    List<Objekttype> otList = modell.ByggObjektstruktur();
                    List<SosiKodeliste> kodeList = modell.ByggSosiKodelister();
                    string eadirectory = Path.GetDirectoryName(Repository.ConnectionString);
                    
                    var gen = new WordSOSIRealiseringGenerator();
                    gen.LagWordRapportSosiSyntaks(otList, isFag, kodeList, valgtPakke.Name);

                }
                else MessageBox.Show("Vennligst velg en pakke med stereotype applicationSchema.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void GenererDefinisjonsfiler(Repository Repository)
        {
            if (!ModelIsValid() && (UserAbortsDueToInvalidModel() || UserAbortsDueToCircularReference())) return;

            try
            {
                Package valgtPakke = Repository.GetTreeSelectedPackage();
                if (valgtPakke.Element.Stereotype.ToLower() == "applicationschema" || valgtPakke.Element.Stereotype.ToLower() == "underarbeid")
                {
                    Sosimodell modell = new Sosimodell(Repository);
                    List<Objekttype> otList = modell.ByggObjektstruktur();
                    var gen = new SosiKontrollGenerator();
                    gen.GenererDefFiler(otList, Repository);
                }
                else MessageBox.Show("Vennligst velg en pakke med stereotype applicationSchema.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            finally
            {
                
            }
        }

        public string WriteToOutputwindow(Repository repository, object args)
        {
            string[] currentpackage = (string[])args;
            string melding = currentpackage[0];
            repository.WriteOutput("System", melding, 0);
            return "";
        }

        public String EA_OnInitializeTechnologies(Repository r)
        {
            string technology = "";
            Assembly assem = this.GetType().Assembly;
            using (Stream stream = assem.GetManifestResourceStream("Arkitektum.Kartverket.SOSI.EA.Plugin.sosi_mdg.xml"))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        technology = reader.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error Initializing SOSI Technology");
                }

            }
            return technology;

        }

        public String EA_OnRetrieveModelTemplate(Repository r, string sLocation)
        {
            string sTemplate = "";
            Assembly assem = this.GetType().Assembly;
            using (Stream stream = assem.GetManifestResourceStream("Arkitektum.Kartverket.SOSI.EA.Plugin.xmi_prodspek.xml"))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        sTemplate = reader.ReadToEnd();
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error getting template");
                }

            }
            return sTemplate;

        }

        private bool ModelIsValid()
        {
            var umlValidator = new UMLValidator(_repository, new ValidatorSettings(true, true));
            _validationResults = umlValidator.RunValidation(_repository.GetTreeSelectedPackage());
            return umlValidator.IsValid(_validationResults);
        }

        private bool UserAbortsDueToCircularReference()
        {
            bool hasCircularReference = _validationResults.Any(v => v.ErrorMessage.StartsWith("Sirkelreferanse"));
            if (hasCircularReference)
            {
                string caption = "Operasjonen ble avbrutt";
                string message =
                    "Modellen inneholder sirkelreferanser og vi kan ikke sikre kjøring uten fare for at programmet henger. Alt arbeid bør være lagret før du evt. fortsetter. Fortsett kjøring på eget ansvar?";
                DialogResult generateAnyway = MessageBox.Show(message, caption, MessageBoxButtons.YesNo);
                bool userHasAborted = generateAnyway == DialogResult.No;
                if (userHasAborted)
                {
                    _repository.WriteOutput("System", "Operasjonen ble avbrutt av brukeren. Modellen inneholder sirkelreferanser.", 0);
                }

                return userHasAborted;
            }
            return false;
        }

        private bool UserAbortsDueToInvalidModel()
        {
            string caption = "Ugyldig SOSI-modell";
            string message = "Modellen er ikke gyldig ihht. SOSI-standard. Vil du fortsette?";

            DialogResult generateAnyway = MessageBox.Show(message, caption, MessageBoxButtons.YesNo);

            bool userHasAborted = generateAnyway == DialogResult.No;
            if (userHasAborted)
            {
                _repository.WriteOutput("System", "Operasjonen ble avbrutt av brukeren. Modellen er ikke en gyldig SOSI-modell.", 0);
            }
            return userHasAborted;
        }


    }
}
