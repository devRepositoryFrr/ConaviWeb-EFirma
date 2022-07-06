using ConaviWeb.Data.Repositories;
using ConaviWeb.Model.Request;
using ConaviWeb.Services;
using ConaviWeb.Tools;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ConaviWeb.Models.AlertsViewModel;

namespace ConaviWeb.Controllers
{
    public class CreateUserController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ISecurityTools _securityTools;
        public CreateUserController(IUserRepository userRepository, ISecurityTools securityTools)
        {
            _userRepository = userRepository;
            _securityTools = securityTools;
        }
        public IActionResult Index()
        {
            if (TempData.ContainsKey("Alert"))
                ViewBag.Alert = TempData["Alert"].ToString();
            return View("../EFirma/CreateUser");
        }
        public async Task<IActionResult> AddUserAsync(CreateUser user)
        {
            string spassword = _securityTools.GetSHA256(user.Password);
            user.Password = spassword;
            user.IdSistema = 5;
            user.IdRol = 4;
            try { 
            var success = await _userRepository.InsertUser(user);
            }
            catch
            {
                TempData["Alert"] = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error al registrarse o el email ya cuenta con registro previo.");
                return RedirectToAction("Index");
            }

            TempData["Alert"] = AlertService.ShowAlert(Alerts.Success, "Usuario registrado exitosamente.");
            return RedirectToAction("Index");
        }
    }
}
