using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCDOCUMENTOS.Models
{
    [Table("MAGICADM.usuariosdocumentos")]
    public class LoginUsuarios
    {
        public string id { get; set; }

        public string usuario { get; set; }
        public string claveacceso { get; set; }
        public string nombrecompleto { get; set; }
        public string nivel { get; set; }
        public string correo { get; set; }
        public UserPermissions permisos { get; set; } // Cambiado a UserPermissions
        public string carpetasp { get; set; }
        public string logs { get; set; }
    }
}
