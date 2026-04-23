using System.ComponentModel.DataAnnotations;

namespace ConaviWeb.Model.Request
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Usuario")]
        public string SUser { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Modulo")]
        public int Modulo { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Contraseña actual")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Nueva contraseña")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Confirmar nueva contraseña")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "La contraseña no coincide.")]
        public string ConfirmNewPassword { get; set; }
    }
}