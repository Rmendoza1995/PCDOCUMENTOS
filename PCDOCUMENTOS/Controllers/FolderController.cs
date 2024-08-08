using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PCDOCUMENTOS.Controllers
{
    public class FolderController : Controller
    {
        public ActionResult Index(string folderPath = "")
        {
            string rootPath = Server.MapPath("~/all");
            string currentPath = Path.Combine(rootPath, folderPath);

            try
            {
                if (Directory.Exists(currentPath))
                {
                    // Obtener la lista de carpetas dentro de la carpeta actual
                    string[] folders = Directory.GetDirectories(currentPath);

                    // Convertir la ruta completa a solo el nombre de la carpeta
                    List<string> folderNames = new List<string>();
                    foreach (var folder in folders)
                    {
                        folderNames.Add(Path.GetFileName(folder));
                    }

                    ViewBag.CurrentPath = folderPath;
                    ViewBag.Folders = folderNames;
                    ViewBag.Message = $"Carpetas en la carpeta:";
                }
                else
                {
                    ViewBag.Message = $"La carpeta '{folderPath}' no existe.";
                    ViewBag.Folders = new List<string>();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error al mostrar las carpetas: " + ex.Message;
                ViewBag.Folders = new List<string>();
            }

            return View();
        }

        [HttpGet]
        public ActionResult ObtenerSubcarpetas(string carpeta)
        {
            string fullPath = Server.MapPath("~/all/" + carpeta);

            try
            {
                if (Directory.Exists(fullPath))
                {
                    string[] subfolders = Directory.GetDirectories(fullPath);
                    List<string> subfolderNames = subfolders.Select(Path.GetFileName).ToList();
                    return Json(subfolderNames, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new List<string>(), JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult CrearCarpeta(string subcarpeta,string carpetaPadre, string nombreCarpeta)
        {
            string fullPathPadre="";
            if (subcarpeta == "Seleccione una subcarpeta" || subcarpeta =="")
            {
                fullPathPadre = Server.MapPath("~/all/" + carpetaPadre);
            }
            else
            {
                 fullPathPadre = Server.MapPath("~/all/" + carpetaPadre + "/" + subcarpeta);
            }



            string fullPathNuevaCarpeta = Path.Combine(fullPathPadre, nombreCarpeta);

            try
            {
                if (!Directory.Exists(fullPathNuevaCarpeta))
                {
                    Directory.CreateDirectory(fullPathNuevaCarpeta);
                    return Json(new { success = true, message = "Carpeta creada exitosamente." });
                }
                else
                {
                    return Json(new { success = false, message = "Ya existe una carpeta con ese nombre." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al crear la carpeta: " + ex.Message });
            }
        }
    }
}
