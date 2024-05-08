using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Arkitektum.Kartverket.SOSI.Model;
using EA;
using NPOI.XWPF.UserModel;
using NPOI.OpenXmlFormats.Wordprocessing;

namespace Arkitektum.Kartverket.SOSI.EA.Plugin.Services
{
    public class WordSOSIRealiseringGenerator
    {
        private readonly string _eaModelDirectory;
        private int _noOfColumns;

        public WordSOSIRealiseringGenerator(Repository repository)
        {
            _eaModelDirectory = Path.GetDirectoryName(repository.ConnectionString);
        }

        public void LagWordRapportSosiSyntaks(Sosimodell sosimodell, bool isFag, Package pakke)
        {
            var otList = sosimodell.ByggObjektstruktur();
            var kodelister = Sosimodell.ByggSosiKodelister(pakke);
            var pakkenavn = pakke.Name;

            if (sosimodell.HarFlateavgrensning())
                otList.Add(sosimodell.LagFlateavgrensning());
            
            if (sosimodell.HarKantutsnitt())
                otList.Add(sosimodell.LagKantUtsnitt());

            _noOfColumns = isFag ? 6 : 5;

            try
            {
                var directoryPath = Path.Combine(_eaModelDirectory, "Produktspesifikasjoner");
                Directory.CreateDirectory(directoryPath);

                var fullFilePath = Path.Combine(directoryPath, "Produktspesifikasjon_" + pakkenavn + ".docx");
                using (var fs = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
                {
                    var doc = new XWPFDocument();

                    CreateTitle(doc, isFag, pakkenavn);

                    if (otList.Count > 0)
                        CreateHeading3(doc, "Objekttyper");

                    foreach (var objekttype in otList)
                        CreateObjekttypeTable(doc, objekttype, isFag);

                    if (kodelister.Any())
                        CreateHeading3(doc, "Kodelister");

                    foreach (var kodeliste in kodelister)
                        CreateKodelisteTable(doc, kodeliste);

                    doc.Write(fs);
                }

                Process.Start(directoryPath);
            }
            catch (Exception e)
            {
                MessageBox.Show($"{e.Message} {e.Source}");
            }
        }

        // Objekttype
        private void CreateObjekttypeTable(XWPFDocument doc, Objekttype objekttype, bool isFag)
        {
            CreateTableHeading(doc, objekttype.UML_Navn);

            var table = doc.CreateTable(1, _noOfColumns);

            SetObjekttypeTableLayout(table, isFag);

            CreateObjekttypeTableHeader(table, isFag);

            if (objekttype.Geometrityper.Count > 0)
                PrintWordTableGeometri(table, objekttype.Geometrityper);

            foreach (var property in objekttype.Egenskaper)
                PrintWordTableEgenskap(table, property, isFag);

            if (objekttype.Inkluder != null)
                PrintWordTableArvetObjekt(table, objekttype, isFag);

            if (objekttype.OCLconstraints.Count > 0 || objekttype.Avgrenser.Count > 0 || objekttype.AvgrensesAv.Count > 0)
            {
                var constraintsHeaderRow = table.CreateRow();
                InsertHeaderCell(constraintsHeaderRow.GetCell(0), "Restriksjoner");
                constraintsHeaderRow.MergeCells(0, _noOfColumns - 1);

                if (objekttype.Avgrenser.Count > 0)
                {
                    var row = table.CreateRow();
                    InsertValueCell(row.GetCell(0), "Avgrenser: " + string.Join(", ", objekttype.Avgrenser));
                    row.MergeCells(0, _noOfColumns - 1);
                }
                if (objekttype.AvgrensesAv.Count > 0)
                {
                    var row = table.CreateRow();
                    InsertValueCell(row.GetCell(0), "Avgrenses av: " + string.Join(", ", objekttype.AvgrensesAv));
                    row.MergeCells(0, _noOfColumns - 1);
                }

                foreach (var beskrankning in objekttype.OCLconstraints)
                {
                    var row = table.CreateRow();
                    
                    var builder = new StringBuilder();
                    if (beskrankning.ErArvet())
                    {
                        builder.Append("Fra supertype ").Append(beskrankning.OpprinneligFraElementNavn).AppendLine(":");
                    }
                    builder.Append(beskrankning.Navn).Append(": ").Append(beskrankning.Notat);

                    InsertValueCell(row.GetCell(0), builder.ToString());
                    row.MergeCells(0, _noOfColumns - 1);
                }
            }
        }

        private static void SetObjekttypeTableLayout(XWPFTable table, bool isFag)
        {
            SetTableLayout(table);

            if (isFag)
            {
                table.SetColumnWidth(0, 1220);
                table.SetColumnWidth(1, 1220);
                table.SetColumnWidth(2, 1220);
                table.SetColumnWidth(3, 420);
                table.SetColumnWidth(4, 420);
                table.SetColumnWidth(5, 1100);
            }
            else
            {
                table.SetColumnWidth(0, 1540);
                table.SetColumnWidth(1, 1540);
                table.SetColumnWidth(2, 1426);
                table.SetColumnWidth(3, 547);
                table.SetColumnWidth(4, 547);
            }
        }

        private static void CreateObjekttypeTableHeader(XWPFTable table, bool isFag)
        {
            InsertHeaderCell(table.GetRow(0).GetCell(0), "UML Egenskapsnavn");
            InsertHeaderCell(table.GetRow(0).GetCell(1), "SOSI Egenskapsnavn");
            InsertHeaderCell(table.GetRow(0).GetCell(2), "Tillatte verdier");
            InsertHeaderCell(table.GetRow(0).GetCell(3), "Mult");
            InsertHeaderCell(table.GetRow(0).GetCell(4), "SOSI-", "type");
            if (isFag) InsertHeaderCell(table.GetRow(0).GetCell(5), "Standard");
        }

        private void PrintWordTableEgenskap(XWPFTable table, AbstraktEgenskap element, bool isFag)
        {
            if (element.ErAssosiasjonSomBeskriverAvgrensning())
                return;

            if (element is Basiselement basiselement)
            {
                PrintWordTableBasiselement(table, basiselement, isFag);
            }
            else
            {
                var gruppeelement = (Gruppeelement)element;

                PrintWordTableGruppeelement(table, gruppeelement, isFag);

                foreach (var property in gruppeelement.Egenskaper)
                {
                    PrintWordTableEgenskap(table, property, isFag);
                }
                if (gruppeelement.Inkluder != null)
                {
                    PrintWordTableArvetGruppeelement(table, gruppeelement, isFag);
                }
            }
        }

        private void PrintWordTableArvetGruppeelement(XWPFTable table, Gruppeelement gruppeelement, bool isFag)
        {
            if (gruppeelement.Inkluder != null)
            {

                foreach (var property in gruppeelement.Inkluder.Egenskaper)
                {
                    PrintWordTableEgenskap(table, property, isFag);
                }

                if (gruppeelement.Inkluder.Inkluder != null)
                    PrintWordTableArvetGruppeelement(table, gruppeelement.Inkluder, isFag);
            }
        }

        private void PrintWordTableArvetObjekt(XWPFTable table, Objekttype objekttype, bool isFag)
        {
            if (objekttype.Inkluder != null)
            {

                foreach (var property in objekttype.Inkluder.Egenskaper)
                {
                    PrintWordTableEgenskap(table, property, isFag);
                }

                if (objekttype.Inkluder.Inkluder != null) 
                    PrintWordTableArvetObjekt(table, objekttype.Inkluder, isFag);
            }
        }
        
        private static void PrintWordTableGruppeelement(XWPFTable table, Gruppeelement g, bool isFag)
        {
            var row = table.CreateRow();
            
            InsertValueCell(row.GetCell(0), g.UML_Navn);
            InsertValueCell(row.GetCell(1), g.SOSI_Navn);
            InsertValueCell(row.GetCell(2), "*");
            InsertValueCell(row.GetCell(3), g.Multiplisitet);
            InsertValueCell(row.GetCell(4), "*");
            
            if (isFag)
                InsertValueCell(row.GetCell(5), g.Standard);
        }

        private static void PrintWordTableGeometri(XWPFTable table, IEnumerable<string> geometries)
        {
            var row = table.CreateRow();

            InsertValueCell(row.GetCell(0), "Geometri");
            InsertValueCell(row.GetCell(1), string.Join(",", geometries));
        }

        private static void PrintWordTableBasiselement(XWPFTable table, Basiselement basiselement, bool isFag)
        {
            var row = table.CreateRow();
            
            InsertValueCell(row.GetCell(0), basiselement.UML_Navn);
            InsertValueCell(row.GetCell(1), basiselement.SOSI_Navn);
            InsertValueCell(row.GetCell(2), GetAnyAllowedValuesOrDefaultValue());
            InsertValueCell(row.GetCell(3), basiselement.Multiplisitet);
            InsertValueCell(row.GetCell(4), basiselement.Datatype);
            
            if (isFag)
                InsertValueCell(row.GetCell(5), basiselement.Standard);
            return;

            string GetAnyAllowedValuesOrDefaultValue()
            {
                var operatorAndParenthesis = basiselement.Operator + " ({0})";

                if (basiselement.TillatteVerdier.Any())
                {
                    return string.Format(operatorAndParenthesis, basiselement.TillatteVerdier.Count < 10
                        ? string.Join(",", basiselement.TillatteVerdier)
                        : "Kodeliste");
                }

                return basiselement.HarStandardVerdi()
                    ? string.Format(operatorAndParenthesis, basiselement.StandardVerdi)
                    : string.Empty;
            }
        }

        // Kodeliste
        private static void CreateKodelisteTable(XWPFDocument doc, SosiKodeliste kodeliste)
        {
            CreateTableHeading(doc, kodeliste.Navn);

            var table = doc.CreateTable(1, 3);

            SetKodelisteTableLayout(table);

            CreateKodelisteTableHeader(table);

            foreach (var sosiKode in kodeliste.Verdier)
            {
                var row = table.CreateRow();
                InsertValueCell(row.GetCell(0), sosiKode.SosiVerdi);
                InsertValueCell(row.GetCell(1), sosiKode.Navn);
                InsertValueCell(row.GetCell(2), sosiKode.Beskrivelse);
            }
        }

        private static void SetKodelisteTableLayout(XWPFTable table)
        {
            SetTableLayout(table);

            table.SetColumnWidth(0, 1100);
            table.SetColumnWidth(1, 1205);
            table.SetColumnWidth(2, 3295);
        }

        private static void CreateKodelisteTableHeader(XWPFTable table)
        {
            InsertHeaderCell(table.GetRow(0).GetCell(0), "Kode");
            InsertHeaderCell(table.GetRow(0).GetCell(1), "Navn");
            InsertHeaderCell(table.GetRow(0).GetCell(2), "Beskrivelse");
        }

        // Delte metoder
        private static void SetTableLayout(XWPFTable table)
        {
            var tblLayout = table.GetCTTbl().tblPr.AddNewTblLayout();
            tblLayout.type = ST_TblLayoutType.@fixed;

            table.SetCellMargins(0, 70, 0, 70);

            table.Width = 5600;

            table.SetBottomBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetTopBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetLeftBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetRightBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetInsideHBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
        }

        private static void CreateTableHeading(XWPFDocument doc, string text)
        {
            var p = doc.CreateParagraph();
            p.Alignment = ParagraphAlignment.LEFT;
            p.SpacingAfter = 0;
            var r = p.CreateRun();
            r.FontFamily = "Calibri Light";
            r.SetColor("2F5496");
            r.FontSize = 11;
            r.IsItalic = true;

            r.SetText(text);
        }

        private static void InsertHeaderCell(XWPFTableCell cell, string line1, string line2 = null)
        {
            cell.SetColor("F3F3F3");
            var p = cell.Paragraphs[0];
            var r = p.CreateRun();
            r.FontFamily = "Calibri";
            r.FontSize = 11;
            r.IsBold = true;
            r.SetText(line1);
            if (line2 != null)
            {
                r.AddBreak(BreakType.TEXTWRAPPING);
                r.AppendText(line2);
            }
        }

        private static void InsertValueCell(XWPFTableCell cell, string value)
        {
            var p = cell.Paragraphs[0];
            var r = p.CreateRun();
            r.FontFamily = "Calibri";
            r.FontSize = 11;
            r.SetText(value);
        }

        private static void CreateTitle(XWPFDocument doc, bool isFag, string pakkenavn)
        {
            var p = doc.CreateParagraph();
            p.Alignment = ParagraphAlignment.LEFT;
            p.SpacingAfter = 0;
            var r = p.CreateRun();
            r.FontFamily = "Calibri Light";
            r.SetColor("2F5496");
            r.FontSize = 13;

            if (isFag) r.SetText("Fagområde: " + pakkenavn);
            else r.SetText("Produktspesifikasjon: " + pakkenavn);
        }

        private static void CreateHeading3(XWPFDocument doc, string text)
        {
            var p = doc.CreateParagraph();
            p.Alignment = ParagraphAlignment.LEFT;
            p.SpacingAfter = 0;
            var r = p.CreateRun();
            r.FontFamily = "Calibri Light";
            r.SetColor("1F3763");
            r.FontSize = 12;

            r.SetText(text);
        }
    }
}
