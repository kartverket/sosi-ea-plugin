using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.XWPF.UserModel;

namespace Arkitektum.Kartverket.SOSI.EA.Plugin.Test
{
    [TestClass]
    public class DocxGeneratorTest
    {
        [TestMethod]
        public void TestDocXGenerering()
        {
            var pakkenavn = "Testpakke_280224";
            var isFag = false;

            var fullFilePath = "Produktspesifikasjon_test.docx";

            var doc = new XWPFDocument();

            var p0 = doc.CreateParagraph();
            p0.Alignment = ParagraphAlignment.LEFT;
            p0.SpacingAfter = 0;
            var r0 = p0.CreateRun();
            r0.FontSize = 13;
            r0.FontFamily = "Calibri Light";
            r0.SetColor("2F5496");

            if (isFag) r0.SetText("Fagområde: " + pakkenavn);
            else r0.SetText("Produktspesifikasjon: " + pakkenavn);

            var p1 = doc.CreateParagraph();
            p1.Alignment = ParagraphAlignment.LEFT;
            p1.SpacingAfter = 0;
            var r1 = p1.CreateRun();
            r1.FontSize = 11;
            r1.FontFamily = "Calibri Light";
            r1.SetColor("1F3763");
            r1.SetText("Objekttyper");

            var p2 = doc.CreateParagraph();
            p2.Alignment = ParagraphAlignment.LEFT;
            p2.SpacingAfter = 0;
            var r2 = p2.CreateRun();
            r2.FontFamily = "Calibri Light";
            r2.SetColor("2F5496");
            r2.FontSize = 11;
            r2.IsItalic = true;
            r2.SetText("Dataavgrensning");

            var table = doc.CreateTable(1, isFag ? 6 : 5);

            var tblLayout = table.GetCTTbl().tblPr.AddNewTblLayout();
            tblLayout.type = ST_TblLayoutType.@fixed;
            table.SetCellMargins(0, 70, 0, 70);

            table.Width = 5600;
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
                table.SetColumnWidth(1, 1500);
                table.SetColumnWidth(2, 1468);
                table.SetColumnWidth(3, 549);
                table.SetColumnWidth(4, 548);
            }

            table.SetBottomBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetTopBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetLeftBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetRightBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");
            table.SetInsideHBorder(XWPFTable.XWPFBorderType.SINGLE, 1, 0, "000000");

            InsertHeaderCell(table.GetRow(0).GetCell(0), "UML Egenskapsnavn");
            InsertHeaderCell(table.GetRow(0).GetCell(1), "SOSI Egenskapsnavn");
            InsertHeaderCell(table.GetRow(0).GetCell(2), "Tillatte verdier");
            InsertHeaderCell(table.GetRow(0).GetCell(3), "Mult");
            InsertHeaderCell(table.GetRow(0).GetCell(4), "SOSI-", "type");
            if (isFag) InsertHeaderCell(table.GetRow(0).GetCell(5), "Standard");

            var row = table.CreateRow();
            InsertValueCell(row.GetCell(0), "Geometri");
            InsertValueCell(row.GetCell(1), "KURVE,BUEP,SIRKELP,BEZIER,KLOTOIDE");
            var row2 = table.CreateRow();
            InsertValueCell(row2.GetCell(1), "..OBJTYPE");
            InsertValueCell(row2.GetCell(2), "= (Dataavgrensning)");
            InsertValueCell(row2.GetCell(3), "[1..1]");
            InsertValueCell(row2.GetCell(4), "T32");

            var row3 = table.CreateRow();
            InsertHeaderCell(row3.GetCell(0), "Restriksjoner");
            row3.MergeCells(0, isFag ? 5 : 4);

            var row4 = table.CreateRow();
            InsertValueCell(row4.GetCell(0), "Avgrenser: Politisone_5.0");
            row4.MergeCells(0, isFag ? 5 : 4);

            var p3 = doc.CreateParagraph();
            p3.Alignment = ParagraphAlignment.LEFT;
            p3.SpacingAfter = 0;
            var r3 = p3.CreateRun();
            r3.FontFamily = "Calibri Light";
            r3.SetColor("2F5496");
            r3.FontSize = 11;
            r3.IsItalic = true;
            r3.SetText("Hovedpolitisonegrense");

            using (var fs = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write))
            {
                doc.Write(fs);
            }
        }

        private void InsertHeaderCell(XWPFTableCell cell, string line1, string line2=null)
        {
            cell.SetColor("F3F3F3");
            var p = cell.Paragraphs[0];
            var r = p.CreateRun();
            r.FontFamily = "Calibri";
            r.IsBold = true;
            r.SetText(line1);
            
            if (line2 != null)
            {
                r.AddBreak(BreakType.TEXTWRAPPING);
                r.AppendText(line2);
                /*p.SpacingAfter = 0;
                var p2 = cell.AddParagraph();
                var r2 = p2.CreateRun();
                r2.FontFamily = "Calibri";
                r2.IsBold = true;
                r2.SetText(line2);*/
            }
        }

        private void InsertValueCell(XWPFTableCell cell, string value)
        {
            var p = cell.Paragraphs[0];
            var r = p.CreateRun();
            r.FontFamily = "Calibri";
            r.FontSize = 11;
            r.SetText(value);
        }
    }
}
