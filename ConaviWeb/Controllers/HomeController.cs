using ConaviWeb.Commons;
using ConaviWeb.Model;
using ConaviWeb.Model.Response;
using ConaviWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConaviWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        [Route("/Home/Error500")]
        public IActionResult Error500()
        {
            return View("../Home/Error500");
        }
        [AllowAnonymous]
        [Route("/Home/Error404")]
        public IActionResult Error404()
        {
            return View("../Home/Error404");
        }
        [HttpGet]
        public IActionResult DescargarManual()
        {
            var filePath = Path.Combine(_environment.WebRootPath,"doc", "Manual_Firma_Electronica_2025.pdf");
            var mimeType = "application/pdf";
            var fileName = "ManualUsuario.pdf";

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("El archivo no existe.");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, mimeType, fileName);
        }
    }
}
