using ConaviWeb.Commons;
using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ConaviWeb.Controllers
{
    public class FirmadosController : Controller
    {
        private readonly ISecurityRepository _securityRepository;
        private readonly IUserRepository _userRepository;
        public FirmadosController(ISecurityRepository securityRepository, IUserRepository userRepository)
        {
            _securityRepository = securityRepository;
            _userRepository = userRepository;
        }
        public async Task<IActionResult> IndexAsync()
        {
            var user = HttpContext.Session.GetObject<UserResponse>("ComplexObject");
            //User user = await _userRepository.GetUserDetails(Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
            IEnumerable<Partition> partitions = await _securityRepository.GetPartitionsD(user.IdSistema, user.Id);
            ViewData["Partitions"] = partitions;
            return View("../EFirma/Firmados");
        }
        public async Task<IActionResult> GetFirmadosAsync(int idParticion)
        {
            IEnumerable<Firmados> firmados = await _securityRepository.GetFirmados(idParticion);
            return Json(new { data = firmados });
        }
    }
}
