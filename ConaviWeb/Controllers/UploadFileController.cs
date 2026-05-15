using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using ConaviWeb.Commons;
using ConaviWeb.Services;
using ConaviWeb.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static ConaviWeb.Models.AlertsViewModel;

namespace ConaviWeb.Controllers
{
    [Authorize]
    [Route("UploadFile")]
    public class UploadFileController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ISourceFileRepository _sourceFileRepository;
        private readonly ISecurityTools _securityTools;
        private readonly ISecurityRepository _securityRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProcessSignRepository _processSignRepository;
        private readonly IMailService _mailService;
        private readonly ILogger<UploadFileController> _logger;
        public UploadFileController(IWebHostEnvironment environment, ISourceFileRepository sourceFileRepository, ISecurityTools securityTools, ISecurityRepository securityRepository, IUserRepository userRepository, IProcessSignRepository processSignRepository, IMailService mailService, ILogger<UploadFileController> logger)
        {
            _environment = environment;
            _logger = logger;
            _sourceFileRepository = sourceFileRepository;
            _securityTools = securityTools;
            _securityRepository = securityRepository;
            _userRepository = userRepository;
            _processSignRepository = processSignRepository;
            _mailService = mailService;
        }

        public async Task<IActionResult> Index()
        {
            //User user = await _userRepository.GetUserDetails(Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
            var user = HttpContext.Session.GetObject<UserResponse>("ComplexObject");
            IEnumerable<Partition> partitions = await _securityRepository.GetPartitions(user.IdSistema);
            IEnumerable<User> users = await _securityRepository.GetUsers(user.IdSistema);
            
            ViewData["Partitions"] = partitions;
            ViewData["Users"] = users;
            ViewBag.Alert = TempData["Alert"];
            ViewBag.Sistema = user.IdSistema;
            return View("../EFirma/UploadFile");
        }

        
        [HttpPost]
        [DisableRequestSizeLimit,
        RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue,
        ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> LoadFile([FromForm] FileRequest formFiles)
        {
            try
            {
                var partition = await _securityRepository.GetPartition(formFiles.Partition);

                SourceFile sourceFile = new SourceFile();
            DateTime dateTime = DateTime.Now;
            sourceFile.IdUser = Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            bool success = true;
            Response respuesta = new Response();
            var sessionData = HttpContext.Session.GetObject<UserResponse>("ComplexObject");
            //Logica Recursos Humanos
            string shortPath = "";
            if (sessionData.IdSistema == 4 || sessionData.IdSistema == 5 || sessionData.IdSistema == 6)
            {
                shortPath = Path.Combine("doc", "EFirma", "Original", partition.Text);
            }
            else
            {
                shortPath = Path.Combine("doc", "EFirma", "Original", dateTime.Year.ToString(), dateTime.Month.ToString(), partition.Text);
            }
            
            var currentPath = Path.Combine(_environment.WebRootPath, shortPath);
            int count = formFiles.FileCollection.Count;
            foreach (var file in formFiles.FileCollection)
            {
                if (file.Length > 0)
                {
                    sourceFile.Hash = FileTools.GetHashDocument(file);
                    if (!Directory.Exists(currentPath))
                        FileTools.CreateDirectory(currentPath);
                    sourceFile.FilePath = shortPath;
                    sourceFile.FileName = file.FileName;
                        sourceFile.IdPartition = formFiles.Partition;
                    var filePath = Path.Combine(currentPath, file.FileName);
                    await FileTools.SaveFileAsync(file, filePath);
                    success = await _sourceFileRepository.InsertSourceFile(sourceFile);
                }
            }
            if (!success)
            {
                respuesta.Success = 0;
                respuesta.Message = "Ocurrio un error al cargas los archivos";
                    TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error al cargas los archivos");
                return RedirectToAction("Index","UploadFile");
            }

            respuesta.Success = 1;
            respuesta.Message = "Se cargaron " + count + " archivos.";
                
                if (sessionData.IdSistema == 5 || sessionData.IdSistema == 6)
                {
                    try
                    {
                    var dataMail = await _processSignRepository.GetMailCarga(partition.Id);
                    var mail = new System.Text.StringBuilder();
                    mail.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'></head>");
                    mail.Append("<body style='margin:0;padding:0;font-family:Arial,Helvetica,sans-serif;background-color:#f4f4f4;'>");
                    mail.Append("<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4;padding:20px 0;'><tr><td align='center'>");
                    mail.Append("<table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 4px rgba(0,0,0,0.1);'>");
                    mail.Append("<tr><td style='background-color:#6b1d2a;padding:20px 30px;text-align:center;'>");
                    mail.Append("<h2 style='color:#ffffff;margin:0;font-size:20px;'>Comisión Nacional de Vivienda</h2>");
                    mail.Append("<p style='color:#d4a76a;margin:5px 0 0;font-size:14px;'>Sistema de Firma Electrónica</p></td></tr>");
                    mail.Append("<tr><td style='padding:30px;'>");
                    mail.Append("<p style='font-size:15px;color:#333333;margin:0 0 15px;'>Buen día,</p>");
                    mail.Append("<p style='font-size:15px;color:#333333;line-height:1.6;margin:0 0 20px;text-align:justify;'>El proceso de Firma Electrónica pone a su disposición el acta:</p>");
                    mail.Append("<table width='100%' cellpadding='0' cellspacing='0' style='margin:0 0 20px;'><tr>");
                    mail.Append("<td style='background-color:#f8f4ef;border-left:4px solid #b8925f;padding:12px 16px;'>");
                    mail.Append("<strong style='font-size:15px;color:#6b1d2a;'>" + dataMail.Particion + "</strong></td></tr></table>");
                    mail.Append("<p style='font-size:15px;color:#333333;line-height:1.6;margin:0 0 25px;text-align:justify;'>La cual se encuentra disponible para su firma en el siguiente enlace:</p>");
                    mail.Append("<table width='100%' cellpadding='0' cellspacing='0'><tr><td align='center'>");
                    mail.Append("<a href='https://firmaelectronica.conavi.gob.mx:9090' style='display:inline-block;background-color:#6b1d2a;color:#ffffff;text-decoration:none;padding:12px 30px;border-radius:5px;font-size:15px;font-weight:bold;'>Acceder al Sistema de Firma Electrónica</a>");
                    mail.Append("</td></tr></table></td></tr>");
                    mail.Append("<tr><td style='background-color:#f0ebe4;padding:15px 30px;border-top:2px solid #b8925f;'>");
                    mail.Append("<p style='font-size:12px;color:#888888;margin:0;text-align:center;'><strong>NO RESPONDA ESTE CORREO, ES UN ENVÍO AUTOMATIZADO.</strong></p>");
                    mail.Append("<p style='font-size:11px;color:#aaaaaa;margin:8px 0 0;text-align:center;'>Comisión Nacional de Vivienda &bull; Sistema de Firma Electrónica</p>");
                    mail.Append("</td></tr></table></td></tr></table></body></html>");
                    MailRequest mailRequest = new();
                    mailRequest.ToEmail = dataMail.Emails;
                    mailRequest.Subject = "Notificación Firma Electrónica CONAVI.";
                    mailRequest.Body = mail.ToString();
                    bool send = await SendMail(mailRequest);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al enviar correo de notificación para partición {ParticionId}", partition.Id);
                    }
                }
                
            TempData["Alert"] = AlertService.ShowAlert(Alerts.Success, "Se cargaron " + count + " archivos.");

            }
            catch (Exception e)
            {

                throw;
            }

            return RedirectToAction("Index", "UploadFile");
        }
        private async Task<bool> SendMail(MailRequest request)
        {
            try
            {
                await _mailService.SendEmailAsync(request);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
