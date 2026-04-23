using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Request;
using ConaviWeb.Model.Response;
using ConaviWeb.Commons;
using ConaviWeb.Models;
using ConaviWeb.Services;
using ConaviWeb.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ConaviWeb.Models.AlertsViewModel;

namespace ConaviWeb.Controllers
{
    public class LoginController : Controller
    {
        private readonly ISecurityRepository _securityRepository;
        private readonly ISecurityTools _securityTools;

        public LoginController(ISecurityRepository securityRepository, ISecurityTools securityTools)
        {
            _securityRepository = securityRepository;
            _securityTools = securityTools;
        }

        public IActionResult Index()
        {
            return View("../Login/Login");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View("../Login/ChangePassword", new ChangePasswordRequest());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View("../Login/ChangePassword", request);
            }

            // 1) Validar contraseña actual + módulo (misma regla que login)
            var loginCheck = new UserRequest
            {
                SUser = request.SUser,
                Password = _securityTools.GetSHA256(request.CurrentPassword),
                Modulo = request.Modulo
            };

            var userResponse = await _securityRepository.GetLoginByCredentials(loginCheck);
            if (userResponse == null)
            {
                ViewBag.Alert = AlertService.ShowAlert(Alerts.Danger, "Usuario/contraseña actual incorrectos o sin acceso al módulo");
                return View("../Login/ChangePassword", request);
            }

            // 2) Actualizar password
            var newHash = _securityTools.GetSHA256(request.NewPassword);
            var rows = await _securityRepository.UpdatePassword(request.SUser, newHash);

            if (rows <= 0)
            {
                ViewBag.Alert = AlertService.ShowAlert(Alerts.Danger, "No fue posible actualizar la contraseña");
                return View("../Login/ChangePassword", request);
            }

            ViewBag.Alert = AlertService.ShowAlert(Alerts.Success, "Contraseña actualizada correctamente");
            ModelState.Clear();
            return View("../Login/ChangePassword", new ChangePasswordRequest { SUser = request.SUser, Modulo = request.Modulo });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Auth([FromForm] UserRequest userRequest)
        {
            Response response = new Response();
            string spassword = _securityTools.GetSHA256(userRequest.Password);
            userRequest.Password = spassword;
            var userResponse = await _securityRepository.GetLoginByCredentials(userRequest);
            List<Module> modules = new();
            if (userResponse != null)
            {
                userResponse.IdSistema = userRequest.Modulo;
                userResponse.AccessToken = await _securityTools.GetToken(userResponse);
                userResponse.Modules = await _securityRepository.GetModules(Convert.ToInt32(userResponse.Rol));
                if (userResponse.AccessToken != null)
                {
                    HttpContext.Session.SetObject("ComplexObject", userResponse);
                    HttpContext.Session.SetString("Token", userResponse.AccessToken);
                    return (RedirectToAction("Index", "Home"));
                }
                else
                {
                    return (RedirectToAction("Error"));
                }

            }
            else
            {
                ViewBag.Alert = AlertService.ShowAlert(Alerts.Danger, "Usuario y/o Contraseña incorrectos.");
                return View("../Login/Login");
            }

        }

        [Authorize]
        [Route("LogOut")]
        public IActionResult LogOut()
        {
            HttpContext.Session.Clear();
            HttpContext.Session.Remove("Token");
            HttpContext.Session.Remove("ComplexObject");
            return RedirectToAction("Index");
        }

        [Authorize]
        [Route("mainwindow")]
        [HttpGet]
        public IActionResult MainWindow()
        {
            string token = HttpContext.Session.GetString("Token");
            if (token == null)
            {
                return (RedirectToAction("Index"));
            }
            if (Convert.ToInt32(_securityTools.GetUserFromAccessToken(token)) == 0)
            {
                return (RedirectToAction("Index"));
            }
            ViewBag.Message = BuildMessage(token, 50);
            return View();
        }

        public IActionResult Error()
        {
            ViewBag.Message = "An error occured...";
            return View();
        }

        private string BuildMessage(string stringToSplit, int chunkSize)
        {
            var data = Enumerable.Range(0, stringToSplit.Length / chunkSize).Select(i => stringToSplit.Substring(i * chunkSize, chunkSize));
            string result = "The generated token is:";
            foreach (string str in data)
            {
                result += Environment.NewLine + str;
            }
            return result;
        }
    

    // GET: api/Users
    [HttpPost("GetUserByAccessToken")]
        public async Task<IActionResult> GetUserByAccessToken([FromBody] string accessToken)
        {
            Response response = new Response();
            int userId = _securityTools.GetUserFromAccessToken(accessToken);

            if (userId != 0)
            {
                var userResponse = await _securityRepository.GetLoginByUserId(userId);
                userResponse.AccessToken = accessToken;
                response.Success = 1;
                response.Data = userResponse;
                return Ok(response);
            }

            response.Success = 0;
            response.Message = "Token expirado";
            response.Data = null;
            return BadRequest(response);
        }
    }
}
