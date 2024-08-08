using PCDOCUMENTOS.Models;
using System.Data.Entity;
using System;
using System.Linq;
using System.Data.Odbc;


public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext() : base("name=Coneccion")
    {
        // No necesitas abrir la conexión aquí
    }

    public DbSet<LoginUsuarios> usuariosdocumentos { get; set; }
}
