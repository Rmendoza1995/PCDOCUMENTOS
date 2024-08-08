using System;
using System.Data.Odbc;
using System.Linq;
using System.Web.Mvc;
using PCDOCUMENTOS.Models;

namespace PCDOCUMENTOS.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }
     
        // POST: Account/Login
        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(LoginUsuarios user)
        {
            if (ModelState.IsValid)
            {
                // Obtener el nivel del usuario
                int? userLevel = GetUserLevel(user);

                if (userLevel.HasValue)
                {
                    if (userLevel.Value == 1)
                    {
                       
                        // Redirige a la página "Folder" si el nivel es 1
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        // Redirige a la página "Default" si el nivel no es 1
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Nombre de usuario o contraseña incorrectos.");
                }
            }

            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        // Imprime errores en el log o en la vista para depuración
                        Console.WriteLine($"Error en {modelState.Key}: {error.ErrorMessage}");
                    }
                }
            }

            // Si el modelo no es válido o las credenciales son incorrectas, vuelve a mostrar el formulario con el mensaje de error
            return View(user);
        }

        private int? GetUserLevel(LoginUsuarios user)
        {
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Coneccion"].ConnectionString;

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT nivel FROM UsuariosDocumentos WHERE usuario = ? AND claveacceso = ?";
                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", user.usuario);
                        command.Parameters.AddWithValue("?", user.claveacceso);

                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Retorna el nivel del usuario si se encuentra
                                return reader.GetInt32(0);
                            }
                            else
                            {
                                // Retorna null si no se encuentra el usuario
                                return null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
        }
    }
}
