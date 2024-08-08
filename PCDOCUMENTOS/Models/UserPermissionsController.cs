using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PCDOCUMENTOS.Models
{
    public class UserModel
    {
        public string Usuario { get; set; }
        public string NombreCompleto { get; set; }
        public string Nivel { get; set; }
        public string Correo { get; set; }
        public UserPermissions Permisos { get; set; }
        public List<string> Carpetasp { get; set; }
        public string Logs { get; set; }
    }
    public class UserPermissions
    {
        public int Editar { get; set; }
        public int Eliminar { get; set; }
        public int Crear { get; set; }
        public int Visualizar { get; set; }
        public int Descargar { get; set; }
    }



    public class UserRutas
    {
        public string ruta { get; set; }

    }
}
