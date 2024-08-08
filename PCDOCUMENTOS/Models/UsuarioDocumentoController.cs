using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PCDOCUMENTOS.Models
{
    public class UsuarioDocumento
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; }
        public string Nivel { get; set; }
        public string Usuario { get; set; }
        public string ClaveAcceso { get; set; }
        public string Correo { get; set; }
        public string Permisos { get; set; }
        public string Carpetasp { get; set; }
        public string Logs { get; set; }
    }

}