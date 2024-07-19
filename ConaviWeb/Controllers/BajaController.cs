using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static ConaviWeb.Models.AlertsViewModel;

namespace ConaviWeb.Controllers
{
    public class BajaController : Controller
    {
        private readonly ISecurityRepository _securityRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProcessSignRepository _processSignRepository;
        public BajaController(ISecurityRepository securityRepository, IUserRepository userRepository, IProcessSignRepository processSignRepository)
        {
            _securityRepository = securityRepository;
            _processSignRepository = processSignRepository;
            _userRepository = userRepository;
        }
        public async Task<IActionResult> Index()
        {
            User user = await _userRepository.GetUserDetails(Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
            IEnumerable<Partition> partitions = await _securityRepository.GetPartitionsBaja(user.IdSystem);
            

            ViewData["Partitions"] = partitions;
            ViewBag.Alert = TempData["Alert"];
            ViewBag.Sistema = user.IdSystem;
            return View("../EFirma/BajaFirmados");
        }
        [HttpPost]
        public async Task<IActionResult> BajaArchivos(string ids, string obs)
        {
            string alert = "";
            string[] arrpath = ids.Split(@",");
            var response = await _processSignRepository.BajaArchivos(ids, obs);
            if (!response)
            {
                alert = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error, favor de contactar al administrador del sistema.");
                return Ok(new
                {
                    success = false,
                    message = alert
                });
            }

            alert = AlertService.ShowAlert(Alerts.Success, "Se dieron de baja exitosamente "+ arrpath.Length +" archivo(s)");
            return Ok(new
            {
                success = true,
                message = alert
            });
        }
        [HttpPost]
        public async Task<IActionResult> BajaParticion(string ids, string obs)
        {
            string alert = "";
            var response = await _processSignRepository.BajaArchivos(ids, obs);
            if (!response)
            {
                alert = AlertService.ShowAlert(Alerts.Danger, "Ocurrio un error, favor de contactar al administrador del sistema");
                return Ok(new
                {
                    success = false,
                    message = alert
                });
            }

            alert = AlertService.ShowAlert(Alerts.Success, "Se dio de baja la partición exitosamente");
            return Ok(new
            {
                success = true,
                message = alert
            });
        }
    }
}
