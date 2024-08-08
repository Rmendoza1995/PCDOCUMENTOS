using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using Newtonsoft.Json; // Asegúrate de tener Newtonsoft.Json (Json.NET) instalado
using PCDOCUMENTOS.Models;
using System.Data.Odbc;
using System.Linq;
using System.Data.Entity;
using System.Web.Script.Serialization;

namespace PCDOCUMENTOS.Controllers
{
    public class AdminController : Controller
    {
        private string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["Coneccion"].ConnectionString;

        [HttpGet]
        public JsonResult GetCarpetas()
        {
            string carpetasPath = Server.MapPath("~/all/");
            var result = new List<object>();

            try
            {
                var carpetas = Directory.GetDirectories(carpetasPath);

                foreach (var carpeta in carpetas)
                {
                    var nombreCarpeta = Path.GetFileName(carpeta);
                    var rutaCompleta = Path.Combine("/all/", nombreCarpeta);
                    var subcarpetasList = new List<object>();

                    var subcarpetas = Directory.GetDirectories(carpeta);
                    foreach (var subcarpeta in subcarpetas)
                    {
                        var nombreSubcarpeta = Path.GetFileName(subcarpeta);
                        var rutaSubcarpetaCompleta = Path.Combine(rutaCompleta, nombreSubcarpeta);
                        subcarpetasList.Add(new { value = rutaSubcarpetaCompleta, text = nombreSubcarpeta });
                    }

                    result.Add(new
                    {
                        value = rutaCompleta,
                        text = nombreCarpeta,
                        children = subcarpetasList
                    });
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return Json(new { error = "Error al obtener carpetas: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Index(string searchQuery = null, int page = 1, int pageSize = 70)
        {
            var usuarios = GetUsuarios(searchQuery, page, pageSize);
            int totalUsuarios = GetTotalUsuarios(searchQuery);

            ViewBag.CurrentSearch = searchQuery;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsuarios / pageSize);
            ViewBag.CurrentPage = page;

            return View(usuarios ?? new List<LoginUsuarios>());
        }

        private List<LoginUsuarios> GetUsuarios(string searchQuery, int page, int pageSize)
        {
            var usuarios = new List<LoginUsuarios>();

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT usuario, nombrecompleto, nivel, permisos 
                        FROM UsuariosDocumentos 
                        WHERE (? IS NULL OR usuario LIKE ? OR nombrecompleto LIKE ?) 
                        ORDER BY usuario 
                        OFFSET ? ROWS FETCH NEXT ? ROWS ONLY";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", string.IsNullOrEmpty(searchQuery) ? (object)DBNull.Value : searchQuery);
                        command.Parameters.AddWithValue("?", string.IsNullOrEmpty(searchQuery) ? (object)DBNull.Value : "%" + searchQuery + "%");
                        command.Parameters.AddWithValue("?", string.IsNullOrEmpty(searchQuery) ? (object)DBNull.Value : "%" + searchQuery + "%");
                        command.Parameters.AddWithValue("?", (page - 1) * pageSize);
                        command.Parameters.AddWithValue("?", pageSize);

                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var permisosJson = reader.GetString(3);
                                var permisos = JsonConvert.DeserializeObject<UserPermissions>(permisosJson);

                                usuarios.Add(new LoginUsuarios
                                {
                                    usuario = reader.GetString(0),
                                    nombrecompleto = reader.GetString(1),
                                    nivel = reader.GetString(2),
                                    permisos = permisos // Cambia el tipo de permisos a UserPermissions
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            return usuarios;
        }

        private int GetTotalUsuarios(string searchQuery)
        {
            int totalUsuarios = 0;

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT COUNT(*) 
                        FROM UsuariosDocumentos 
                        WHERE (? IS NULL OR usuario LIKE ? OR nombrecompleto LIKE ?)";

                    using (OdbcCommand command = new OdbcCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("?", string.IsNullOrEmpty(searchQuery) ? (object)DBNull.Value : searchQuery);
                        command.Parameters.AddWithValue("?", string.IsNullOrEmpty(searchQuery) ? (object)DBNull.Value : "%" + searchQuery + "%");
                        command.Parameters.AddWithValue("?", string.IsNullOrEmpty(searchQuery) ? (object)DBNull.Value : "%" + searchQuery + "%");

                        totalUsuarios = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    // Manejo de errores
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            return totalUsuarios;
        }

        [HttpPost]
        public ActionResult AddUser(string nombrecompleto, string nivel, string usuario, string claveacceso, string correo, IDictionary<string, int> permisos, string[] carpetasp)
        {
            try
            {
                using (OdbcConnection connection = new OdbcConnection(connectionString))
                {
                    connection.Open();

                    // Construir el JSON para los permisos
                    var permisosJson = new
                    {
                        Editar = permisos.ContainsKey("Editar") ? permisos["Editar"] : 0,
                        Eliminar = permisos.ContainsKey("Eliminar") ? permisos["Eliminar"] : 0,
                        Crear = permisos.ContainsKey("Crear") ? permisos["Crear"] : 0,
                        Visualizar = permisos.ContainsKey("Visualizar") ? permisos["Visualizar"] : 0,
                        Descargar = permisos.ContainsKey("Descargar") ? permisos["Descargar"] : 0
                    };

                    // Convertir las carpetas a JSON con la estructura requerida
                    var carpetasJson = carpetasp.Select(carpeta => new { ruta = carpeta }).ToList();
                    var carpetasJsonString = JsonConvert.SerializeObject(carpetasJson);

                    // Crear la cadena SQL de inserción
                    string sql = @"
                INSERT INTO UsuariosDocumentos (
                    nombrecompleto, 
                    nivel, 
                    usuario, 
                    claveacceso, 
                    correo, 
                    permisos, 
                    carpetasp, 
                    logs
                ) VALUES (
                    ?, ?, ?, ?, ?, ?, ?, ?
                )";

                    using (OdbcCommand command = new OdbcCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("?", nombrecompleto);
                        command.Parameters.AddWithValue("?", nivel);
                        command.Parameters.AddWithValue("?", usuario);
                        command.Parameters.AddWithValue("?", claveacceso);
                        command.Parameters.AddWithValue("?", correo);
                        command.Parameters.AddWithValue("?", JsonConvert.SerializeObject(permisosJson));
                        command.Parameters.AddWithValue("?", carpetasJsonString);
                        command.Parameters.AddWithValue("?", $"Registro de creación - {DateTime.Now}");

                        command.ExecuteNonQuery();
                    }

                    // Redirigir a la vista principal con un mensaje de éxito
                    TempData["SuccessMessage"] = "Usuario agregado exitosamente.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                TempData["ErrorMessage"] = $"Ocurrió un error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }



        [HttpGet]
        public ActionResult GetUserById(string usuario)
        {
            string sql = "SELECT * FROM usuariosdocumentos WHERE usuario = ?";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command = new OdbcCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("?", usuario);

                    using (OdbcDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var userModel = new UserModel
                            {
                                Usuario = reader["usuario"].ToString(),
                                NombreCompleto = reader["nombrecompleto"].ToString(),
                                Nivel = reader["nivel"].ToString(),
                                Correo = reader["correo"].ToString(),
                                Logs = reader["logs"].ToString()
                            };

                            // Deserializar permisos
                            var permisosJson = reader["permisos"].ToString();
                            if (!string.IsNullOrEmpty(permisosJson))
                            {
                                userModel.Permisos = JsonConvert.DeserializeObject<UserPermissions>(permisosJson);
                            }

                            // Deserializar carpetas
                            var carpetaspJson = reader["carpetasp"].ToString();
                            if (!string.IsNullOrEmpty(carpetaspJson))
                            {
                                var carpetas = JsonConvert.DeserializeObject<List<UserRutas>>(carpetaspJson);
                                userModel.Carpetasp = carpetas.Select(c => c.ruta).ToList();
                            }

                            return Json(userModel, JsonRequestBehavior.AllowGet);
                        }
                        else
                        {
                            return HttpNotFound(); // Maneja el caso en el que el usuario no se encuentra
                        }
                    }
                }
            }
        }


        public ActionResult EditUser(UserModel model)
        {
            // Obtener los datos actuales del usuario
            UserModel existingUser = null;
            string sqlSelect = "SELECT * FROM usuariosdocumentos WHERE usuario = ?";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command = new OdbcCommand(sqlSelect, connection))
                {
                    command.Parameters.AddWithValue("?", model.Usuario);
                    using (OdbcDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existingUser = new UserModel
                            {
                                Usuario = reader["usuario"].ToString(),
                                NombreCompleto = reader["nombrecompleto"].ToString(),
                                Nivel = reader["nivel"].ToString(),
                                Correo = reader["correo"].ToString(),
                                Permisos = JsonConvert.DeserializeObject<UserPermissions>(reader["permisos"].ToString()),
                                Carpetasp = JsonConvert.DeserializeObject<List<string>>(reader["carpetasp"].ToString()),
                                Logs = reader["logs"].ToString()
                            };
                        }
                    }
                }
            }

            if (existingUser != null)
            {
                // Construir JSON para permisos y carpetas
                string permisosJson = JsonConvert.SerializeObject(model.Permisos);
                string carpetaspJson = JsonConvert.SerializeObject(model.Carpetasp);

                // Actualizar el usuario
                string sqlUpdate = @"UPDATE usuarios
                             SET nombrecompleto = ?, nivel = ?, correo = ?, permisos = ?, carpetasp = ?
                             WHERE usuario = ?";

                using (OdbcConnection connection = new OdbcConnection(connectionString))
                {
                    connection.Open();
                    using (OdbcCommand command = new OdbcCommand(sqlUpdate, connection))
                    {
                        command.Parameters.AddWithValue("?", model.NombreCompleto);
                        command.Parameters.AddWithValue("?", model.Nivel);
                        command.Parameters.AddWithValue("?", model.Correo);
                        command.Parameters.AddWithValue("?", permisosJson);
                        command.Parameters.AddWithValue("?", carpetaspJson);
                        command.Parameters.AddWithValue("?", model.Usuario);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            return new HttpStatusCodeResult(500, "No se pudo actualizar el usuario.");
                        }
                    }
                }
            }
            else
            {
                return HttpNotFound(); // O maneja el caso en que el usuario no se encuentra
            }
        }





        [HttpPost]
        public ActionResult DeleteUser(string usuario)
        {
            string sql = "DELETE FROM usuariosdocumentos WHERE usuario = ?";

            using (OdbcConnection connection = new OdbcConnection(connectionString))
            {
                connection.Open();
                using (OdbcCommand command = new OdbcCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("?", usuario);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return new HttpStatusCodeResult(500, "No se pudo eliminar el usuario.");
                    }
                }
            }
        }




    }
}
