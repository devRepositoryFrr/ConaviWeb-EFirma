using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConaviWeb.Model.Request
{
    public class CreateUser
    {
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Nombre { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string PApellido { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string SApellido { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Usuario { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Password { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string Email { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public string RFC { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public int Dependencia { get; set; }
        public int IdRol { get; set; }
        public int IdSistema { get; set; }
    }
}
