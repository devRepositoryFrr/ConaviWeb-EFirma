using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConaviWeb.Model
{
    public class Partition
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Partición")]
        public string Text { get; set; }
        public string PathPartition { get; set; }
        [Required(ErrorMessage = "El campo {0} es requerido")]
        [Display(Name = "Usuarios")]
        public string JsonUsers { get; set; }
        public int Firmas { get; set; }
    }
}
