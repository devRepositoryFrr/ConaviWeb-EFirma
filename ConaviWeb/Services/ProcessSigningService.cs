using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using ConaviWeb.Tools;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Hosting;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace ConaviWeb.Services
{
    public class ProcessSigningService : IProcessSigningService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IProcessSignRepository _processSignRepository;
        private readonly ISecurityRepository _securityRepository;
        private readonly ISourceFileRepository _sourceFileRepository;
        public ProcessSigningService(IWebHostEnvironment environment, IProcessSignRepository processSignRepository, ISecurityRepository securityRepository, ISourceFileRepository sourceFileRepository)
        {
            _environment = environment;
            _processSignRepository = processSignRepository;
            _securityRepository = securityRepository;
            _sourceFileRepository = sourceFileRepository;
        }

        public async Task<bool> ProcessFileSatAsync(User user, DataSignRequest dataSignRequest, IEnumerable<FileResponse> files)
        {
            var fileKey = Array.Empty<byte>();
            var fileCert = Array.Empty<byte>();
            bool certificadoValido, llavePrivadaValida, keyCerMatched, vigencia;
            bool success = false;
            DatosCertificado.datosgeneralescertificado certificadoleido = new DatosCertificado.datosgeneralescertificado();
            DatosCadenaOriginal datosfea = new DatosCadenaOriginal();

            
            if (dataSignRequest.KeySat.Length > 0)
            {
                using var ms = new MemoryStream();
                string ext = System.IO.Path.GetExtension(dataSignRequest.KeySat.FileName);
                    
                dataSignRequest.KeySat.CopyTo(ms);
                    fileKey = ms.ToArray();
                    //string s = Convert.ToBase64String(fileKey);
            }
            if (dataSignRequest.CerSat.Length > 0)
            {
                using var ms = new MemoryStream();
                string ext = System.IO.Path.GetExtension(dataSignRequest.CerSat.FileName);

                dataSignRequest.CerSat.CopyTo(ms);
                fileCert = ms.ToArray();
                //string s = Convert.ToBase64String(fileKey);
            }


            var archivoKey = OBC_Utilities.MatchKeyPwd(dataSignRequest.PassFirmante.ToCharArray(), fileKey);
            llavePrivadaValida = archivoKey != null;
            if (llavePrivadaValida)
            {

                X509Certificate archivoCer = OBC_Utilities.LoadCertificate(fileCert);
                keyCerMatched = OBC_Utilities.MatchKeyCer(archivoKey, archivoCer);

                certificadoleido = ObtenCertificado(archivoCer, user.Id, user.RFC);//vigencia

                certificadoValido = certificadoleido.Valido;

                vigencia = certificadoleido.Vigente;

                if (keyCerMatched && certificadoValido && vigencia)
                {
                    foreach (var file in files)
                    {
                        var pathFile = System.IO.Path.Combine(_environment.WebRootPath, file.FilePath, file.FileName);
                        byte[] fileDoc = System.IO.File.ReadAllBytes(pathFile);
                        if (file.IdPadre != 0)
                        {
                            file.Id = file.IdPadre;
                        }
                        success = await FirmaDocumentoAsync(pathFile, fileDoc, fileCert, archivoKey, file.Id, user, file.IdPartition, file.NuFirma);
                    }


                }
            }
            return success;
        }
        private async Task<bool> FirmaDocumentoAsync(string pathDoc, byte[] fileDoc, byte[] fileCert, AsymmetricKeyParameter archivoKey, int idArchivoPadre, User user, int idPartition, int nuFirma)
        {


            DateTime inicio = DateTime.Now;
            bool success = true;
            try
            {

                if (archivoKey != null)  //&& (superiorseleccionado)  
                {
                    var partition = await _securityRepository.GetPartition(idPartition);
                    string shorthPathXML = "";
                    string currentXML = "";
                    string XMLName = "";
                    DatosCadenaOriginal datosfea = new();
                    string cadenaOriginal = "";
                    bool estatus;
                    cadenaOriginal = "|" + XMLOriginalv2(pathDoc,fileDoc, fileCert, user, partition, out datosfea, out estatus, out shorthPathXML, out currentXML, out XMLName) + "||";
                    datosfea.CadenaOriginal = cadenaOriginal;
                    string pathXML = System.IO.Path.Combine(currentXML, XMLName);
                    // Firma de sello y actualización de XML
                    if (datosfea.CadenaOriginal != "" && estatus == true)
                    {
                        datosfea.Sello = GeneraSello(datosfea.CadenaOriginal, archivoKey, fileCert);
                        ActualizaXMLv2(datosfea.Sello, pathXML);

                        //Genera QR sello
                        QRCodeGenerator qrGenerator = new();
                        QRCodeData qrCodeData = qrGenerator.CreateQrCode(datosfea.CadenaOriginal, QRCodeGenerator.ECCLevel.Q);
                        QRCode qrcode = new QRCode(qrCodeData);
                        Bitmap qrCodeImage = qrcode.GetGraphic(5, System.Drawing.Color.Black, System.Drawing.Color.White, null, 15, 6, false);
                        string QrName = System.IO.Path.GetFileNameWithoutExtension(pathXML) + ".jpg";
                        string routeQR = System.IO.Path.Combine(_environment.WebRootPath, "doc","EFirma", QrName);
                        qrCodeImage.Save(routeQR);//write your path where you want to store the qr-code image.
                        //Genera reporte pdf con sello
                        success = await EditPDFAsync(pathDoc, routeQR, idArchivoPadre, datosfea, user, shorthPathXML, XMLName, partition, nuFirma);

                    }


                    datosfea = null;


                    //mensaje = "Se realizó la firma de documentación con éxito.\nDocumentos firmados: " + docsfirmados + "\nTiempo transcurrido: " + tiempo + mensaje;
                    //ProgresoFirma(false, true, mensaje, sello);
                    //}
                    //else
                    //{
                    //    //ProgresoFirma(false, false, "Ocurrió un error y no se pudo firmar el documento, intente nuevamente", "");
                    //}

                    // mostrarAlerta("(" + lst_beneficiarios.Count + ") Beneficiarios firmados correctamente. ", AlertaVO.type_success);
                }
                else
                {
                    // mostrarAlerta("Seleccione algun estatus válido.", AlertaVO.type_warning);
                }
            }
            catch (Exception ex)
            {
                success = false;
            }
            return success;
        }

        private string XMLOriginalv2(string pathDoc,byte[] fileDoc, byte[] fileCert, User user, Partition partition, out DatosCadenaOriginal datosfea, out bool estatus, out string shorthPathXML, out string currentXML, out string XMLName)
        {
            //.NET Core no permite resolver URI externos para XML
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            try
            {

                //string datosPersona = string.Join(";", string.Join(" ", persona.persona.nombre, persona.persona.aPaterno, persona.persona.aMaterno).Trim(), persona.persona.RFC, persona.persona.numEmpleado, persona.persona.area, persona.persona.nombrePuesto);

                DatosCertificado.datosgeneralescertificado datoscertificado = new DatosCertificado.datosgeneralescertificado();
                Org.BouncyCastle.X509.X509Certificate archivoCer = OBC_Utilities.LoadCertificate(fileCert);
                datoscertificado = ObtenCertificado(archivoCer, user.Id, user.RFC);
                DatosCadenaOriginal datosCadenaOriginal = new DatosCadenaOriginal();
                DateTime dateTime = DateTime.Now;
                try
                {

                    datosCadenaOriginal.Tema = "Firma Electrónica";
                    //_datosco.Correcto = datosco.Correcto;
                    datosCadenaOriginal.Folio = dateTime.ToString("ddMMyyHHmmssffff");
                    datosCadenaOriginal.Movimiento = "Firma";//"Cancelacion";
                    datosCadenaOriginal.HashArchivo = ProccessFileTools.GetHashDocument(fileDoc);
                    datosCadenaOriginal.NombreFirmante = user.Name + " " + user.LName + " " + user.SLName;
                    //_datosco.idFirmante = benefVO.id;
                    //_datosco.usuarioFirmante = benefVO.id;
                    datosCadenaOriginal.NumEmpleadoFirmante = user.EmployeeNumber;
                    datosCadenaOriginal.PuestoFirmante = user.Position;
                    datosCadenaOriginal.AreaFirmante = user.Area.ToString();
                    datosCadenaOriginal.RfcFirmante = datoscertificado.SujetoRFC;
                    //_datosco.sello = datosco.sello;
                    //_datosco.sistema = datosco.sistema;
                    datosCadenaOriginal.TimeStampOCSP = datoscertificado.TsValidacion;
                    datosCadenaOriginal.TimeStampSign = datoscertificado.TsValidacion;
                    datosCadenaOriginal.AlgoritmoFirma = datoscertificado.AlgoritmoFirma;
                    datosCadenaOriginal.CertificateNumber = datoscertificado.NumeroSerie;
                    //_datosco.id_archivo = benefVO.id_archivo;
                    //Logica Recursos Humanos
                    string shortPath;
                    if (user.IdSystem == 4)
                    {
                        shortPath = System.IO.Path.Combine("doc", "EFirma", "XML", partition.Text);
                    }
                    else
                    {
                        shortPath = System.IO.Path.Combine("doc", "EFirma", "XML", dateTime.Year.ToString(), dateTime.Month.ToString(), partition.Text);
                    }
                    string currentPath = System.IO.Path.Combine(_environment.WebRootPath, shortPath);
                    //string currentPath = pathRoot + dateTime.Year + "\\" + dateTime.Month;
                    if (!Directory.Exists(currentPath))
                        ProccessFileTools.CreateDirectory(currentPath);
                    string pathXslt = System.IO.Path.Combine(_environment.WebRootPath, "doc", "EFirma", "Xslt", "fea_convenio.xslt");
                    string xmlName = System.IO.Path.GetFileNameWithoutExtension(pathDoc) + "_" + dateTime.ToString("ddMMyyHHmmss") + ".xml";
                    string pathXml = System.IO.Path.Combine(currentPath, xmlName);
                    XmlSerializerNamespaces xmlNameSpace = new XmlSerializerNamespaces();
                    XmlTextWriter xmlTextWriter = new XmlTextWriter(pathXml, System.Text.Encoding.UTF8); //Definir metodología de nombramiento de archivo
                    xmlTextWriter.Formatting = Formatting.Indented;
                    XmlSerializer xs = new XmlSerializer(typeof(DatosCadenaOriginal));
                    xs.Serialize(xmlTextWriter, datosCadenaOriginal, xmlNameSpace);

                    xmlTextWriter.Close();

                    //Cargar el XML generado
                    StreamReader leerXML = new StreamReader(pathXml);
                    XPathDocument XMLgenerado = new XPathDocument(leerXML);


                    StreamReader leerXSLT = new StreamReader(pathXslt);

                    XPathDocument xslt = new XPathDocument(leerXSLT);
                    XslCompiledTransform transformacionXslt = new XslCompiledTransform();
                    transformacionXslt.Load(xslt);

                    StringWriter str = new StringWriter();
                    XmlTextWriter myWriter = new XmlTextWriter(str);

                    //Aplicando transformacion
                    transformacionXslt.Transform(XMLgenerado, null, myWriter);

                    //Resultado
                    datosCadenaOriginal.IdArchivo = 5;
                    datosfea = datosCadenaOriginal;
                    estatus = true;
                    currentXML = currentPath;
                    shorthPathXML = shortPath;
                    XMLName = xmlName;
                    return str.ToString();



                }
                catch (Exception errf)
                {
                    //MessageBox.Show("Ha ocurrido un error y no es posible mostrar la información.\nPor favor contacte al Departamento de Análisis y Programación.\n\n" + errf.Message);
                    Console.WriteLine(errf.Message);
                    DatosCadenaOriginal feaerror = new DatosCadenaOriginal();
                    //feaerror.Correcto = false;
                    //feaerror.Mensaje = errf.Message;// feaerror.solicitante;
                    datosfea = feaerror;
                    estatus = false;
                    shorthPathXML = "";
                    currentXML = "";
                    XMLName = "";
                    return "";
                }
            }
            catch (Exception exc)
            {
                DatosCadenaOriginal feaerror = new DatosCadenaOriginal();
                datosfea = feaerror;
                estatus = false;
                shorthPathXML = "";
                currentXML = "";
                XMLName = "";
                return "";
            }
        }




        public async Task<bool> EditPDFAsync(string pathDoc, string routeQR, int idArchivoPadre, DatosCadenaOriginal datosfea, User user, string shorthPathXML, string XMLName, Partition partition, int nuFirma)
        {
            DateTime dateTime = DateTime.Now;
            //Logica Recursos Humanos
            string shortPath;
            string partitionPath;
            if (user.IdSystem == 4 || user.IdSystem == 5)
            {
                shortPath = System.IO.Path.Combine("doc", "EFirma", "Firmado", partition.Text);
                partitionPath = System.IO.Path.Combine("doc", "EFirma", "Firmado");
            }
            else
            {
                shortPath = System.IO.Path.Combine("doc", "EFirma", "Firmado", dateTime.Year.ToString(), dateTime.Month.ToString(), partition.Text);
                partitionPath = System.IO.Path.Combine("doc", "EFirma", "Firmado", dateTime.Year.ToString(), dateTime.Month.ToString());
            }

            SigningFile signingFile = new();
            signingFile.SignatureDate = datosfea.TimeStampSign;
            signingFile.Folio = datosfea.Folio;
            signingFile.OriginalString = datosfea.CadenaOriginal;
            signingFile.SignatureStamp = datosfea.Sello;
            signingFile.CertSeries = datosfea.CertificateNumber;
            signingFile.Algorithm = datosfea.AlgoritmoFirma;
            signingFile.Hash = datosfea.HashArchivo;
            signingFile.FilePath = shortPath;
            var filePath = System.IO.Path.Combine(_environment.WebRootPath, shortPath);
            if (!Directory.Exists(filePath))
                ProccessFileTools.CreateDirectory(filePath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(pathDoc);
            signingFile.FileName = fileName + "_" + dateTime.ToString("ddMMyyHHmmss") + ".pdf";
            string pdfresult = System.IO.Path.Combine(filePath, signingFile.FileName);

            bool success = false;
            //int nuFirma = 3;
            var numeroFirma = nuFirma + 1;
            if (user.IdSystem == 5)
            {
                
                PdfReader reader = new PdfReader(System.IO.File.OpenRead(pathDoc));
                PdfDocument pdfDoc = new PdfDocument(reader, new PdfWriter(pdfresult));
                Document document = new Document(pdfDoc);
                var totalPages = pdfDoc.GetNumberOfPages();
                PdfFont font_title = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
                PdfFont font_content = PdfFontFactory.CreateFont(StandardFonts.COURIER);
                int pBottom = 0, nPage = totalPages - 3;
                switch (user.PFirma)
                {
                    case 1:
                        pBottom = 590;
                        break;
                    case 2:
                        pBottom = 440;
                        break;
                    case 3:
                        pBottom = 270;
                        break;
                    case 4:
                        pBottom = 100;
                        break;
                    case 5:
                        nPage += 1;
                        pBottom = 610;
                        break;
                    case 6:
                        nPage += 1;
                        pBottom = 440;
                        break;
                    case 7:
                        nPage += 1;
                        pBottom = 290;
                        break;
                    case 8:
                        nPage += 1;
                        pBottom = 140;
                        break;
                    case 9:
                        nPage += 2;
                        pBottom = 610;
                        break;
                    case 10:
                        nPage += 2;
                        pBottom = 460;
                        break;
                    case 11:
                        nPage += 2;
                        pBottom = 300;
                        break;
                    case 12:
                        nPage += 2;
                        pBottom = 150;
                        break;
                    case 13:
                        nPage += 3;
                        pBottom = 590;
                        break;
                    case 14:
                        nPage += 3;
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
                    .Add(new Paragraph(signingFile.OriginalString))//cadena original
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
                    .Add(new Paragraph(signingFile.SignatureStamp))//Sello
                    .SetFont(font_content)
                    .SetFontSize(6)
                    .SetTextAlignment(TextAlignment.JUSTIFIED)
                    .SetHeight(40);
                // Upload image
                ImageData imageData = ImageDataFactory.Create(routeQR);
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
            }
            else
            {
                if (numeroFirma == 1)
                {
                    PdfReader reader = new PdfReader(System.IO.File.OpenRead(pathDoc));
                    PdfDocument pdfDoc = new PdfDocument(reader, new PdfWriter(pdfresult));
                    Document document = new Document(pdfDoc);
                    var iHeader = System.IO.Path.Combine(_environment.WebRootPath, "img", "headerConavi.png");
                    var iFooter = System.IO.Path.Combine(_environment.WebRootPath, "img", "footerConavi.png");
                    pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE, new TextFooterEventHandler(document, iHeader, iFooter));
                    //pdfDoc.AddNewPage();
                    iText.Kernel.Geom.Rectangle mediabox = pdfDoc.GetPage(1).GetMediaBox();
                    Console.WriteLine(mediabox);
                    var a = new PageSize(mediabox);
                    document.Add(new AreaBreak(AreaBreakType.LAST_PAGE));
                    document.Add(new AreaBreak(a));
                    //MARGEN DEL DOCUMENTO
                    document.SetMargins(70, 50, 70, 50);
                    GenPDF(document, signingFile, routeQR, user);
                    document.Close();
                }
                else
                {
                    PdfReader reader = new PdfReader(System.IO.File.OpenRead(pathDoc));
                    PdfDocument pdfDoc = new PdfDocument(reader, new PdfWriter(pdfresult));
                    Document document = new Document(pdfDoc);
                    document.Add(new AreaBreak(AreaBreakType.LAST_PAGE));
                    //MARGEN DEL DOCUMENTO
                    document.SetMargins(70, 50, 70, 50);
                    GenPDF(document, numeroFirma, signingFile, routeQR, user);
                    document.Close();
                }
            }
            //Delete QR
            if (File.Exists(routeQR))
            {
                File.Delete(routeQR);
            }
            success = await _processSignRepository.InsertSigningFile(signingFile, user, idArchivoPadre, shorthPathXML, XMLName, partition);
            if (partition.PathPartition == null)
            {
                partition.PathPartition = partitionPath;
                await _sourceFileRepository.UpdateParition(partition);
            }
            return success;
        }
        public Document GenPDF(Document document, SigningFile signingFile, string routeQR, User user)
        {


            PdfFont font_title = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
            PdfFont font_content = PdfFontFactory.CreateFont(StandardFonts.COURIER);
            PdfFont fonte = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
            PdfFont fonts = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);


            Table firma = new Table(1, true);
            firma.SetBorder(Border.NO_BORDER);
            firma.SetRelativePosition(0, 40, 0, 200);
            Cell Firmante = new Cell(1, 1)
                 .SetTextAlignment(TextAlignment.LEFT)
                 .SetFont(font_title)
                 .SetFontSize(7)
                 .SetHeight(10)
                 .SetFontColor(new DeviceRgb(130, 27, 63))
                 .SetWidth(100)
                 .SetBorder(Border.NO_BORDER)
                 .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                 .SetBorderTop(new SolidBorder(ColorConstants.BLACK, 0.1f))
                 .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                 .SetVerticalAlignment((VerticalAlignment.MIDDLE))
                 .Add(new Paragraph("Firmante: " + user.Name + " " + user.LName + " " + user.SLName));

            Cell hCadenaOriginal = new Cell(1, 1)
                  .SetTextAlignment(TextAlignment.LEFT)
                  .SetFont(font_title)
                  .SetFontSize(7)
                  .SetHeight(10)
                  .SetWidth(100)
                  .SetBorder(Border.NO_BORDER)
                  .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetBorderTop(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetVerticalAlignment((VerticalAlignment.MIDDLE))
                  .Add(new Paragraph("Cadena Original"));
            Cell cadenaOriginal = new Cell(1, 1)
                .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph(signingFile.OriginalString))//cadena original
                .SetFont(font_content)
                .SetFontSize(6)
                .SetWidth(10)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetHeight(40);
            Cell hFirmaEConavi = new Cell(1, 1)
             .SetTextAlignment(TextAlignment.LEFT)
             .SetFont(font_title)
             .SetFontSize(7)
             .SetBorder(Border.NO_BORDER)
             .SetWidth(10)
             .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
             .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
             .SetHeight(10)
             .SetVerticalAlignment((VerticalAlignment.MIDDLE))
             .Add(new Paragraph("Firma electrónica "));
            Cell firmaEConavi = new Cell(1, 1).SetBorder(Border.NO_BORDER)
                   .Add(new Paragraph(signingFile.SignatureStamp))//sello
                   .SetFont(font_content)
                   .SetFontSize(6)
                   .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                   .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                   .SetBorder(Border.NO_BORDER)
                   .SetWidth(10)
                   .SetTextAlignment(TextAlignment.JUSTIFIED)
                   .SetHeight(40);
            // Upload image
            ImageData imageData = ImageDataFactory.Create(routeQR);
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(imageData).ScaleAbsolute(60, 60);
            Cell imagenqr = new Cell(1, 1)
           .SetBorder(Border.NO_BORDER)
           .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetBorderBottom(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetHeight(60)
           .Add(image);
            Cell ultima = new Cell(1, 1).SetBorder(Border.NO_BORDER);

            firma.AddCell(Firmante);
            firma.AddCell(hCadenaOriginal);
            firma.AddCell(cadenaOriginal);
            firma.AddCell(hFirmaEConavi);
            firma.AddCell(firmaEConavi);
            firma.AddCell(imagenqr);
            firma.AddCell(ultima);
            document.Add(firma);
            document.Close();
            return document;
        }

        public Document GenPDF(Document document, int nuFirma, SigningFile signingFile, string routeQR, User user)
        {


            PdfFont font_title = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);
            PdfFont font_content = PdfFontFactory.CreateFont(StandardFonts.COURIER);
            PdfFont fonte = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);
            PdfFont fonts = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD);


            Table firma = new Table(1, true);
            firma.SetBorder(Border.NO_BORDER);
            var position = nuFirma == 2 ? 240 : 440;
            firma.SetRelativePosition(0, position, 0, 200);

            Cell Firmante = new Cell(1, 1)
                 .SetTextAlignment(TextAlignment.LEFT)
                 .SetFont(font_title)
                 .SetFontSize(7)
                 .SetHeight(10)
                 .SetFontColor(new DeviceRgb(130, 27, 63))
                 .SetWidth(100)
                 .SetBorder(Border.NO_BORDER)
                 .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                 .SetBorderTop(new SolidBorder(ColorConstants.BLACK, 0.1f))
                 .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                 .SetVerticalAlignment((VerticalAlignment.MIDDLE))
                 .Add(new Paragraph("Firmante: " + user.Name + " " + user.LName + " " + user.SLName));

            Cell hCadenaOriginal = new Cell(1, 1)
                  .SetTextAlignment(TextAlignment.LEFT)
                  .SetFont(font_title)
                  .SetFontSize(7)
                  .SetHeight(10)
                  .SetWidth(100)
                  .SetBorder(Border.NO_BORDER)
                  .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetBorderTop(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                  .SetVerticalAlignment((VerticalAlignment.MIDDLE))
                  .Add(new Paragraph("Cadena Original"));
            Cell cadenaOriginal = new Cell(1, 1)
                .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                .SetBorder(Border.NO_BORDER)
                .Add(new Paragraph(signingFile.OriginalString))//cadena original
                .SetFont(font_content)
                .SetFontSize(6)
                .SetWidth(10)
                .SetTextAlignment(TextAlignment.JUSTIFIED)
                .SetHeight(40);
            Cell hFirmaEConavi = new Cell(1, 1)
             .SetTextAlignment(TextAlignment.LEFT)
             .SetFont(font_title)
             .SetFontSize(7)
             .SetBorder(Border.NO_BORDER)
             .SetWidth(10)
             .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
             .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
             .SetHeight(10)
             .SetVerticalAlignment((VerticalAlignment.MIDDLE))
             .Add(new Paragraph("Firma electrónica "));
            Cell firmaEConavi = new Cell(1, 1).SetBorder(Border.NO_BORDER)
                   .Add(new Paragraph(signingFile.SignatureStamp))//sello
                   .SetFont(font_content)
                   .SetFontSize(6)
                   .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
                   .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
                   .SetBorder(Border.NO_BORDER)
                   .SetWidth(10)
                   .SetTextAlignment(TextAlignment.JUSTIFIED)
                   .SetHeight(40);
            // Upload image
            ImageData imageData = ImageDataFactory.Create(routeQR);
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(imageData).ScaleAbsolute(60, 60);
            Cell imagenqr = new Cell(1, 1)
           .SetBorder(Border.NO_BORDER)
           .SetBorderLeft(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetBorderRight(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetBorderBottom(new SolidBorder(ColorConstants.BLACK, 0.1f))
           .SetHeight(60)
           .Add(image);
            Cell ultima = new Cell(1, 1).SetBorder(Border.NO_BORDER);

            firma.AddCell(Firmante);
            firma.AddCell(hCadenaOriginal);
            firma.AddCell(cadenaOriginal);
            firma.AddCell(hFirmaEConavi);
            firma.AddCell(firmaEConavi);
            firma.AddCell(imagenqr);
            firma.AddCell(ultima);
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
            

        public DatosCertificado.datosgeneralescertificado ObtenCertificado(X509Certificate archivoCer, int idUsuario, string rfcRequest)
        {
            try
            {
                DatosCertificado.datosgeneralescertificado datos_cer = new DatosCertificado.datosgeneralescertificado();
                datos_cer.LeeCertificado(archivoCer, out datos_cer);
                datos_cer.Vigente = datos_cer.vigencia(rfcRequest, datos_cer.SujetoRFC, datos_cer.FechaExpira, datos_cer.FechaInicio);

                if (datos_cer.Vigente)
                {

                    OBC_Ocsp.resultadoocsp consultaocsp = datos_cer.AutenticidadCertificado(archivoCer.SerialNumber, archivoCer.NotBefore, idUsuario, datos_cer.NumeroSerie);
                    datos_cer.Autentico = consultaocsp.status.ToString() == "Vigente";
                    datos_cer.TsValidacion = consultaocsp.TimeStampQuery;
                }
                datos_cer.Valido = datos_cer.Vigente && datos_cer.Autentico;

                return datos_cer;
            }
            catch (Exception er)
            {
                DatosCertificado.datosgeneralescertificado datos_cer = new DatosCertificado.datosgeneralescertificado();
                datos_cer.Vigente = false;
                datos_cer.Valido = false;
                datos_cer.Observaciones = er.Message;
                return datos_cer;
            }
        }

        private string GeneraSello(string CadenaOriginal, AsymmetricKeyParameter archivoKey, byte[] fileCert)
        {
            #region Firma de documentos
            byte[] CO = Encoding.UTF8.GetBytes(CadenaOriginal);

            X509Certificate archivoCer = OBC_Utilities.LoadCertificate(fileCert);

            X509CertificateEntry certEntry = new X509CertificateEntry(archivoCer);

            //Pendiente! Rutina para obtener el algoritmo firma, si no se encuentra se deberá registrar previamente

            ISigner sig = SignerUtilities.GetSigner(archivoCer.SigAlgName);
            sig.Init(true, archivoKey);
            sig.BlockUpdate(CO, 0, CO.Length); //CO es la cadena original a firmar
            byte[] signature = sig.GenerateSignature();
            string sello = Convert.ToBase64String(signature);

            #region Validación de sello
            //byte[] selloBytes = Convert.FromBase64String(sello);
            //sig.Reset();
            //sig = SignerUtilities.GetSigner(archivoCer.SigAlgName);
            //sig.Init(false, archivoCer.GetPublicKey());
            //sig.BlockUpdate(CO, 0, CO.Length);
            //bool valido = true;
            //valido = sig.VerifySignature(selloBytes);
            #endregion
            return sello;
            #endregion
        }

        private void ActualizaXMLv2(string sello, string pathXML)
        {

            string pathXslt = System.IO.Path.Combine(_environment.WebRootPath, "doc", "EFirma", "Xslt", "fea_convenio.xslt");
            XmlDocument doc = new XmlDocument();

            StreamReader leerXML = new StreamReader(pathXML);
            doc.LoadXml(leerXML.ReadToEnd());
            leerXML.Close();
            XmlNode root = doc.DocumentElement;

            //Generación del nodo Sello
            XmlElement elem = doc.CreateElement("sello");
            elem.InnerText = sello;

            //Se agrega el nodo al documento XML
            root.AppendChild(elem);
            //doc.Save(fbdRepositorio.SelectedPath + "\\ASI_A05F103_" + id + "-" + consultatipo_ + ".xml");
            doc.Save(pathXML);


            //Cargar el XML generado
            StreamReader leerXMLV2 = new StreamReader(pathXML);
            XPathDocument XMLgenerado = new XPathDocument(leerXMLV2);

            StreamReader leerXSLT = new StreamReader(pathXslt);

            XPathDocument xslt = new XPathDocument(leerXSLT);
            XslCompiledTransform transformacionXslt = new XslCompiledTransform();
            transformacionXslt.Load(xslt);

            StringWriter str = new StringWriter();
            XmlTextWriter myWriter = new XmlTextWriter(str);

            //Aplicando transformacion
            transformacionXslt.Transform(XMLgenerado, null, myWriter);



        }

    }
}
