using ConaviWeb.Commons;
using ConaviWeb.Data.Repositories;
using ConaviWeb.Model;
using ConaviWeb.Model.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

public class NavBarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        // Ejemplo: obtén el usuario de la sesión
        var user = HttpContext.Session.GetObject<UserResponse>("ComplexObject");
        // Lógica para obtener los módulos permitidos
        IEnumerable<Module> modules = ObtenerModulosPorUsuario(user);

        return View(modules);
    }

    private IEnumerable<Module> ObtenerModulosPorUsuario(UserResponse user)
    {
        List<Module> modules = user.Modules.Select(m => new Module
        {
            Url = m.Url,
            Text = m.Text,
            Ico = m.Ico
        }).ToList();
        return modules;
    }
}