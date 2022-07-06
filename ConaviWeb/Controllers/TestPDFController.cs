using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace ConaviWeb.Controllers
{
    public class TestPDFController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        public TestPDFController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }
        public IActionResult Index()
        {
            //GenComitePDF(2);
            return Ok();
        }
        [Route("PDF/{data?}")]

        public Document GenComitePDF(int data)
        {
            var pdfPath = System.IO.Path.Combine(_environment.WebRootPath,"doc","FIRMAS.pdf");
            var pdfResult = System.IO.Path.Combine(_environment.WebRootPath, "doc", "FIRMAS2.pdf");
            PdfReader reader = new PdfReader(System.IO.File.OpenRead(pdfPath));
            PdfDocument pdfDoc = new PdfDocument(reader, new PdfWriter(pdfResult));
            Document document = new Document(pdfDoc);
            var totalPages = pdfDoc.GetNumberOfPages();
            //var iHeader = System.IO.Path.Combine(_environment.WebRootPath, "img", "headerConavi.png");
            //var iFooter = System.IO.Path.Combine(_environment.WebRootPath, "img", "footerConavi.png");
            //pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TextFooterEventHandler(document, iHeader, iFooter));
            PdfFont font_title = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
            PdfFont font_content = PdfFontFactory.CreateFont(StandardFonts.COURIER);
            int pBottom = 0,nPage = 0;
            switch (data)
            {
                case 1:
                    nPage = 1;
                    pBottom = 590;
                    break;
                case 2:
                    nPage = 1;
                    pBottom = 440;
                    break;
                case 3:
                    nPage = 1;
                    pBottom = 270;
                    break;
                case 4:
                    nPage = 1;
                    pBottom = 100;
                    break;
                case 5:
                    nPage = 2;
                    pBottom = 610;
                    break;
                case 6:
                    nPage = 2;
                    pBottom = 440;
                    break;
                case 7:
                    nPage = 2;
                    pBottom = 290;
                    break;
                case 8:
                    nPage = 2;
                    pBottom = 140;
                    break;
                case 9:
                    nPage = 3;
                    pBottom = 610;
                    break;
                case 10:
                    nPage = 3;
                    pBottom = 460;
                    break;
                case 11:
                    nPage = 3;
                    pBottom = 300;
                    break;
                case 12:
                    nPage = 3;
                    pBottom = 150;
                    break;
                case 13:
                    nPage = 4;
                    pBottom = 590;
                    break;
                case 14:
                    nPage = 4;
                    pBottom = 420;
                    break;
            }

            Table firma = new Table(4, true);
            firma.SetBorder(Border.NO_BORDER);
            firma.SetMaxWidth(480);
            firma.SetFixedPosition(nPage, 55, pBottom, 440);

            Cell hCadenaOriginal = new Cell(1, 4)
                  .SetTextAlignment(TextAlignment.LEFT)
                  .SetFont(font_title)
                  .SetFontSize(7)
                  .SetHeight(10)
                  .SetWidth(10)
                  .SetBorder(Border.NO_BORDER)
                  //.SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  //.SetBorderTop(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  //.SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetVerticalAlignment((VerticalAlignment.MIDDLE))
                  .Add(new Paragraph("Cadena Original"));
            Cell cadenaOriginal = new Cell(1, 3)
                //.SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                //.SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("||Firma|Firma Electrónica|1205221336457012|David Avellaneda Agüero|| AVAD8910042A5 |0|CONAVI|2022-05-12T13:37:31-05:00|71-3F-A5-59-54-D6-EBF1-B2-D7-E9-BC-85-51-42-98-11-DF-23-5A-84-92-FE-2F-D6-8E-E6-D6-FB-6B-5A-CB-62-FD-47-C1-B5-A8-E3-95-2B-90-02-0E-99-81-3E-48-E8-BF-63-42-BB-CF-AC26-59-99-40-5A-4A-47-93-1D|00001000000500619765||"))//cadena original
                .SetFont(font_content)
                .SetFontSize(6)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetHeight(40)
                .SetWidth(20);

            Cell hFirmaEConavi = new Cell(1, 4)
             .SetTextAlignment(TextAlignment.LEFT)
             .SetFont(font_title)
             .SetFontSize(7)
             .SetBorder(Border.NO_BORDER)
             //.SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
             //.SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
             .SetHeight(10)
             .SetWidth(10)
             .SetVerticalAlignment((VerticalAlignment.MIDDLE))
             .Add(new Paragraph("Firma electrónica "));
            Cell firmaEConavi = new Cell(1, 3)
                //.SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                //.SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("WMKgRvKRAAMfjoFc5ozIAy+YlndgUoV8ilEdbUk59WR1tlD88mwgA/tpYAJxMRz60FIIKJCO+4tls2EnbBNeY8PmLAxyNoPvmN62J5MGCdYxPOTSCcvMFoqbvo1l+zpsqcTr2b5S5UXbrjHoQLiXVLmuYIcFT92zMOUof1IWKUr2aQ361BZIpNJ47a+a1rHQoNv4+FaJTInwZHz4vGUOUvqFPvpGGWG39xQKVuvEF6OpUIRm8H2yATpZrgbK2KpIS0EHxPEW/OxEYTzXGGIbNKC9QuumABNiUn8B519s+u7Mi/1DYCL4B2jp47RUBXcp9J/Sx5vqGXrOgzdbzeNJiA=="))//Sello
                .SetFont(font_content)
                .SetFontSize(6)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetHeight(40);
            // Upload image
            var imagePath = System.IO.Path.Combine(_environment.WebRootPath,"doc","EFirma", "AECD610914HTCNRL05_280322145457.jpg");
            ImageData imageData = ImageDataFactory.Create(imagePath);
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(imageData).ScaleAbsolute(40, 40);
            Cell imagenqr = new Cell(1, 1)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
           .SetBorder(Border.NO_BORDER)
           .SetMarginLeft(40)
           //.SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
           //.SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
           //.SetBorderBottom(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetHeight(40)
           .Add(image);

            firma.AddCell(hCadenaOriginal);
            firma.AddCell(cadenaOriginal);
            firma.AddCell(new Cell(1, 4).SetBorder(Border.NO_BORDER));
            firma.AddCell(hFirmaEConavi);
            firma.AddCell(firmaEConavi);
            firma.AddCell(imagenqr);
            document.Add(firma);
            
            document.Close();
            return document;
        }

        private class TextFooterEventHandler : IEventHandler
        {
            protected Document doc;
            protected string _header;
            protected string _footer;

            public TextFooterEventHandler(Document doc, string iHeader, string iFooter)
            {
                this.doc = doc;
                _header = iHeader;
                _footer = iFooter;
            }

            public void HandleEvent(Event currentEvent)
            {
                PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
                //Rectangle pageSize = docEvent.GetPage().GetPageSize();
                PdfDocument pdfDoc = docEvent.GetDocument();
                PdfPage page = docEvent.GetPage();
                iText.Kernel.Geom.Rectangle pageSize = page.GetPageSize();
                int pageNumber = pdfDoc.GetPageNumber(page);
                int pagesNumber = pdfDoc.GetNumberOfPages();//pdfDoc.GetPageNumber(page);
                if (pagesNumber != pageNumber)
                {
                    return;
                }
                PdfFont font = null;
                try
                {
                    font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
                }
                catch (IOException e)
                {
                    Console.Error.WriteLine(e.Message);
                }

                Canvas canvas = new Canvas(docEvent.GetPage(), pageSize);
                canvas
                    .Close();
                iText.Layout.Element.Image img = new iText.Layout.Element.Image(ImageDataFactory
                .Create(_header))
                .SetTextAlignment(TextAlignment.CENTER);
                canvas.Add(img);
                iText.Layout.Element.Image footer = new iText.Layout.Element.Image(ImageDataFactory
                  .Create(_footer))
                  .SetFixedPosition(10, 0)
                  .ScaleAbsolute(580, 70)
                  .SetTextAlignment(TextAlignment.CENTER);
                canvas.Add(footer);
            }
        }
    }
}
