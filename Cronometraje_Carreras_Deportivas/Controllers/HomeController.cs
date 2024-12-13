﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

using System.Linq;
using System.Threading.Tasks;
using Cronometraje_Carreras_Deportivas.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


#nullable enable
namespace Cronometraje_Carreras_Deportivas.Controllers
{
    public class HomeController : Controller
    {
        private readonly CronometrajeContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, CronometrajeContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string usuario, string contrasena)
        {
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            {
                ModelState.AddModelError(string.Empty, "Usuario y contraseña son requeridos");
                return View();
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT COUNT(*) FROM ADMINISTRADOR WHERE uss_admin = @Usuario AND pass_admin = @Contrasena";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Usuario", usuario);
                        command.Parameters.AddWithValue("@Contrasena", contrasena);
                        int count = (int)await command.ExecuteScalarAsync();
                        if (count > 0)
                        {
                            _logger.LogInformation($"Usuario {usuario} ha iniciado sesión");
                            return RedirectToAction("Pantalla_ini");
                        }
                        else
                        {
                            _logger.LogWarning($"Intento de inicio de sesión fallido para el usuario {usuario}");
                            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
                            return View();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error durante la autenticación: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al intentar iniciar sesión. Por favor, inténtelo de nuevo más tarde.");
                return View();
            }
        }
        public IActionResult Privacy()
        {
            return View();
        }

       public async Task<IActionResult> Pantalla_ini()
        {
            var anios = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT DISTINCT year_carrera FROM CARRERA";
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        anios.Add(reader.GetInt32(0));
                    }
                }
            }
            ViewBag.Anios = anios;
            return View();
        }

        [HttpGet]
        public IActionResult Alta_administrador()
        {
            return View("Alta_administrador");
        }

        [HttpPost]
        public async Task<IActionResult> Alta_administrador(string usuario, string contrasena, string superUsuario, string superContrasena)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Verificar credenciales del super administrador (sin hashing)
                    string checkSql = "SELECT COUNT(*) FROM ADMINISTRADOR WHERE uss_admin = @SuperUsuario AND pass_admin = @SuperContrasena AND ID_admin = 10";
                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@SuperUsuario", superUsuario);
                        checkCommand.Parameters.AddWithValue("@SuperContrasena", superContrasena); // Usa la contraseña en texto plano

                        int count = (int)await checkCommand.ExecuteScalarAsync();
                        if (count == 0)
                        {
                            ModelState.AddModelError(string.Empty, "Usuario o contraseña de super administrador incorrectos.");
                            return View();
                        }
                    }

                    // Si la verificación fue exitosa, insertar el nuevo administrador (sin hashing)
                    string sql = "INSERT INTO ADMINISTRADOR (uss_admin, pass_admin) VALUES (@Usuario, @Contrasena)";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Usuario", usuario);
                        command.Parameters.AddWithValue("@Contrasena", contrasena); // Almacena la contraseña en texto plano
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Registro exitoso
                _logger.LogInformation($"Nuevo administrador {usuario} registrado.");
                TempData["SuccessMessage"] = "Datos registrados exitosamente.";
                return RedirectToAction("Index"); // Redirige a la pantalla de inicio de sesión
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error durante el registro del usuario: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al registrar el usuario. Por favor, inténtelo de nuevo más tarde.");
                return View();
            }
        }

        public IActionResult Baja_administrador()
        {
            return View("Baja_administrador");
        }
        [HttpPost]
        public async Task<IActionResult> Baja_administrador(string usuario, string superUsuario, string superContrasena)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Verificar credenciales del superadministrador
                    string checkSql = "SELECT COUNT(*) FROM ADMINISTRADOR WHERE uss_admin = @SuperUsuario AND pass_admin = @SuperContrasena AND ID_admin = 10";
                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@SuperUsuario", superUsuario);
                        checkCommand.Parameters.AddWithValue("@SuperContrasena", superContrasena);

                        int count = (int)await checkCommand.ExecuteScalarAsync();
                        if (count == 0)
                        {
                            ModelState.AddModelError(string.Empty, "Credenciales de superadministrador incorrectas.");
                            return View("Baja_administrador");
                        }
                    }

                    // Eliminar el usuario si las credenciales fueron correctas
                    string deleteSql = "DELETE FROM ADMINISTRADOR WHERE uss_admin = @Usuario";
                    using (SqlCommand deleteCommand = new SqlCommand(deleteSql, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@Usuario", usuario);
                        int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            ModelState.AddModelError(string.Empty, "Usuario no encontrado.");
                            return View("Baja_administrador");
                        }
                    }
                }

                TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar el usuario: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al intentar eliminar el usuario. Inténtelo nuevamente.");
                return View("Baja_administrador");
            }
        }

        [HttpPost]
        public async Task<IActionResult> BuscarCorredores(int? yearCarrera, int? ediCarrera, string categoria, string nombreCorredor)
        {
            if (!yearCarrera.HasValue || !ediCarrera.HasValue || string.IsNullOrEmpty(categoria))
            {
                ViewBag.Error = "Por favor selecciona el año, edición y categoría.";
                await InicializarAnios(); // Cargar los años para el selector
                return View("Pantalla_ini");
            }

            List<Dictionary<string, object>> resultados = new List<Dictionary<string, object>>();

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                WITH TiemposCorredor AS (
                SELECT 
                    folio_chip,
                    tiempo_registrado,
                    ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
                FROM dbo.Tiempo
            ),
            Posiciones AS (
                SELECT 
                    RANK() OVER (ORDER BY 
                        DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
                        DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
                        DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00'))
                    ) AS Posicion,
                    v.num_corredor AS NumCorredor, 
                    c.nom_corredor AS Nombre,
                    c.apP_corredor AS ApellidoPaterno,
                    c.apM_corredor AS ApellidoMaterno,
                    COALESCE(T1.tiempo_registrado, '00:00:00') AS T1,
                    COALESCE(T2.tiempo_registrado, '00:00:00') AS T2,
                    COALESCE(T3.tiempo_registrado, '00:00:00') AS T3,
                    CONVERT(TIME, DATEADD(SECOND, 
                        DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
                        DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
                        DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00')),
                        0
                    )) AS TiempoTotal
                FROM CARRERA ca
                JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                JOIN Vincula_participante v ON cc.ID_carr_cat = v.ID_Carr_cat
                JOIN dbo.CORREDOR c ON v.ID_corredor = c.ID_corredor
                LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
                LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
                LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
                WHERE ca.year_carrera = @YearCarrera
                  AND ca.edi_carrera = @EdiCarrera
                  AND cat.nombre_categoria = @Categoria
            )
            SELECT Posicion, NumCorredor, Nombre, T1, T2, T3, TiempoTotal
            FROM Posiciones
            WHERE @NombreCorredor IS NULL 
               OR CONCAT(Nombre, ' ', ApellidoPaterno, ' ', ApellidoMaterno) LIKE @NombreCorredor
            ORDER BY Posicion;
                        ";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@YearCarrera", yearCarrera.Value);
                        command.Parameters.AddWithValue("@EdiCarrera", ediCarrera.Value);
                        command.Parameters.AddWithValue("@Categoria", categoria);
                        command.Parameters.AddWithValue("@NombreCorredor", string.IsNullOrEmpty(nombreCorredor) ? DBNull.Value : (object)$"%{nombreCorredor}%");

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fila = new Dictionary<string, object>
                        {
                            { "Posicion", reader.GetInt64(0) },
                            { "NumCorredor", reader.GetInt32(1) },
                            { "Nombre", reader.GetString(2) },
                            { "T1", reader.IsDBNull(3) ? "N/A" : FormatearTiempo(reader.GetTimeSpan(3)) },
                            { "T2", reader.IsDBNull(4) ? "N/A" : FormatearTiempo(reader.GetTimeSpan(4)) },
                            { "T3", reader.IsDBNull(5) ? "N/A" : FormatearTiempo(reader.GetTimeSpan(5)) },
                            { "TiempoTotal", reader.IsDBNull(6) ? "N/A" : FormatearTiempo(reader.GetTimeSpan(6)) }
                        };
                                resultados.Add(fila);
                            }
                        }
                    }
                }

                ViewBag.ResultadosBusqueda = resultados;
                ViewBag.SelectedYear = yearCarrera;
                ViewBag.SelectedEdition = ediCarrera;
                ViewBag.SelectedCategory = categoria;
                ViewBag.NombreBuscado = nombreCorredor;

                await InicializarAnios(); // Cargar los años para el selector
                return View("Pantalla_ini");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al buscar corredores: {ex.Message}");
                ViewBag.Error = "Hubo un error al realizar la búsqueda. Inténtalo de nuevo más tarde.";
                await InicializarAnios();
                return View("Pantalla_ini");
            }
        }

        private string FormatearTiempo(TimeSpan tiempo)
        {
            return $"{(int)tiempo.TotalHours:D2}:{tiempo.Minutes:D2}:{tiempo.Seconds:D2}.{tiempo.Milliseconds:D3}";
        }

        private async Task InicializarAnios()
        {
            var anios = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT DISTINCT year_carrera FROM CARRERA";
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        anios.Add(reader.GetInt32(0));
                    }
                }
            }

            ViewBag.Anios = anios;
        }

        [HttpGet]
        public async Task<IActionResult> CargarEdiciones(int yearCarrera)
        {
            var ediciones = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
            SELECT DISTINCT ca.edi_carrera
            FROM CARRERA ca
            WHERE ca.year_carrera = @YearCarrera
            ORDER BY ca.edi_carrera";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@YearCarrera", yearCarrera);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ediciones.Add(reader.GetInt32(0));
                        }
                    }
                }
            }

            return Json(ediciones);
        }


        [HttpGet]
        public async Task<IActionResult> CargarCategorias(int yearCarrera, int ediCarrera)
        {
            var categorias = new List<string>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
            SELECT DISTINCT cat.nombre_categoria
            FROM CATEGORIA cat
            INNER JOIN CARR_Cat cc ON cat.ID_categoria = cc.ID_categoria
            INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
            WHERE ca.year_carrera = @YearCarrera AND ca.edi_carrera = @EdiCarrera
            ORDER BY cat.nombre_categoria";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@YearCarrera", yearCarrera);
                    command.Parameters.AddWithValue("@EdiCarrera", ediCarrera);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categorias.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return Json(categorias);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerAnios()
        {
            var anios = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT DISTINCT year_carrera FROM CARRERA";
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        anios.Add(reader.GetInt32(0));
                    }
                }
            }
            return Json(anios);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEdiciones(int year)
        {
            var ediciones = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT edi_carrera FROM CARRERA WHERE year_carrera = @Year";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Year", year);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ediciones.Add(reader.GetInt32(0));
                        }
                    }
                }
            }
            return Json(ediciones);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCategorias(int year, int edicion)
        {
            var categorias = new List<string>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Consulta SQL corregida
                string sql = @"
        SELECT DISTINCT CATEGORIA.nombre_categoria
        FROM CATEGORIA
        INNER JOIN CARR_Cat ON CATEGORIA.ID_categoria = CARR_Cat.ID_categoria
        INNER JOIN CARRERA ON CARR_Cat.ID_carrera = CARRERA.ID_carrera
        WHERE CARRERA.year_carrera = @Year AND CARRERA.edi_carrera = @Edicion
        ORDER BY CATEGORIA.nombre_categoria";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Year", year);
                    command.Parameters.AddWithValue("@Edicion", edicion);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categorias.Add(reader.GetString(0)); // nombre_categoria
                        }
                    }
                }
            }

            return Json(categorias);
        }


        [HttpGet]
        public async Task<IActionResult> Crear_Corredor()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var carreras = new List<SelectListItem>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                SELECT 
                    ca.ID_carrera,
                    CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')', 
                           '(', STRING_AGG(cat.nombre_categoria, ', '), ')') AS Carrera
                FROM CARRERA ca
                JOIN CARR_CAT cc ON ca.ID_carrera = cc.ID_carrera
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
                ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            carreras.Add(new SelectListItem
                            {
                                Value = reader["ID_carrera"].ToString(),
                                Text = reader["Carrera"].ToString()
                            });
                        }
                    }
                }

                ViewBag.Carreras = carreras;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al cargar carreras: {ex.Message}");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Crear_Corredor(string Nombre, string Apaterno, string? Amaterno, DateTime Fnacimiento, string Sexo, string? Correo, string Pais, string? Telefono, int CarreraId, string CategoriaNombre)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Iniciar una transacción
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Verificar si el corredor ya existe
                            string checkSql = @"SELECT COUNT(*) 
                                                 FROM CORREDOR 
                                                 WHERE nom_corredor = @Nombre AND apP_corredor = @Apaterno 
                                                   AND (apM_corredor = @Amaterno OR @Amaterno IS NULL)
                                                   AND f_corredor = @Fnacimiento";

                            using (SqlCommand checkCommand = new SqlCommand(checkSql, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@Nombre", Nombre);
                                checkCommand.Parameters.AddWithValue("@Apaterno", Apaterno);
                                checkCommand.Parameters.AddWithValue("@Amaterno", (object?)Amaterno ?? DBNull.Value);
                                checkCommand.Parameters.AddWithValue("@Fnacimiento", Fnacimiento);

                                if ((int)await checkCommand.ExecuteScalarAsync() > 0)
                                {
                                    transaction.Rollback();
                                    return Json(new { success = false, message = "El corredor ya está registrado." });
                                }
                            }

                            // Insertar nuevo corredor
                            string insertCorredorSql = @"INSERT INTO CORREDOR (nom_corredor, apP_corredor, apM_corredor, 
                                                                             f_corredor, sex_corredor, c_corredor, pais) 
                                                         OUTPUT INSERTED.ID_corredor
                                                         VALUES (@Nombre, @Apaterno, @Amaterno, @Fnacimiento, @Sexo, @Correo, @Pais)";

                            byte[] corredorId;
                            using (SqlCommand insertCommand = new SqlCommand(insertCorredorSql, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@Nombre", Nombre);
                                insertCommand.Parameters.AddWithValue("@Apaterno", Apaterno);
                                insertCommand.Parameters.AddWithValue("@Amaterno", (object?)Amaterno ?? DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@Fnacimiento", Fnacimiento);
                                insertCommand.Parameters.AddWithValue("@Sexo", Sexo);
                                insertCommand.Parameters.AddWithValue("@Correo", (object?)Correo ?? DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@Pais", Pais ?? (object)DBNull.Value);

                                corredorId = (byte[])await insertCommand.ExecuteScalarAsync();
                            }

                            // Insertar número de teléfono si se proporciona
                            if (!string.IsNullOrEmpty(Telefono))
                            {
                                string insertTelefonoSql = @"INSERT INTO TELEFONO (numero, ID_corredor) VALUES (@Numero, @IDCorredor)";

                                using (SqlCommand telefonoCommand = new SqlCommand(insertTelefonoSql, connection, transaction))
                                {
                                    telefonoCommand.Parameters.AddWithValue("@Numero", Telefono);
                                    telefonoCommand.Parameters.AddWithValue("@IDCorredor", corredorId);
                                    await telefonoCommand.ExecuteNonQueryAsync();
                                }
                            }

                            // Obtener ID de la categoría
                            string getCategoriaSql = @"SELECT ID_categoria 
                                                     FROM CATEGORIA 
                                                     WHERE nombre_categoria = @CategoriaNombre";

                            int? idCategoria = null;
                            using (SqlCommand getCategoriaCommand = new SqlCommand(getCategoriaSql, connection, transaction))
                            {
                                getCategoriaCommand.Parameters.AddWithValue("@CategoriaNombre", CategoriaNombre);
                                idCategoria = (int?)await getCategoriaCommand.ExecuteScalarAsync();
                            }

                            if (!idCategoria.HasValue)
                            {
                                transaction.Rollback();
                                return Json(new { success = false, message = "Categoría seleccionada no válida." });
                            }

                            // Obtener ID de la combinación Carrera-Categoría
                            string getCarrCatSql = @"SELECT ID_carr_cat 
                                                     FROM CARR_CAT 
                                                     WHERE ID_carrera = @CarreraId AND ID_categoria = @IDCategoria";

                            int? idCarrCat = null;
                            using (SqlCommand getCarrCatCommand = new SqlCommand(getCarrCatSql, connection, transaction))
                            {
                                getCarrCatCommand.Parameters.AddWithValue("@CarreraId", CarreraId);
                                getCarrCatCommand.Parameters.AddWithValue("@IDCategoria", idCategoria.Value);
                                idCarrCat = (int?)await getCarrCatCommand.ExecuteScalarAsync();
                            }

                            if (!idCarrCat.HasValue)
                            {
                                transaction.Rollback();
                                return Json(new { success = false, message = "La combinación de Carrera y Categoría no es válida." });
                            }

                            // Insertar en Vincula_participante
                            string insertVinculoSql = @"INSERT INTO Vincula_participante (ID_vinculo, ID_corredor, ID_carr_cat, num_corredor, folio_chip) 
                                                         VALUES (NEWID(), @CorredorId, @IDCarrCat,
                                                                 (SELECT ISNULL(MAX(num_corredor), 0) + 1 FROM Vincula_participante),
                                                                 'RFID' + CAST(1000000000 + (SELECT COUNT(*) + 1 FROM Vincula_participante) AS VARCHAR))";

                            using (SqlCommand vinculoCommand = new SqlCommand(insertVinculoSql, connection, transaction))
                            {
                                vinculoCommand.Parameters.AddWithValue("@CorredorId", corredorId);
                                vinculoCommand.Parameters.AddWithValue("@IDCarrCat", idCarrCat.Value);
                                await vinculoCommand.ExecuteNonQueryAsync();
                            }

                            // Confirmar la transacción
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                // Respuesta de éxito
                return Json(new { success = true, message = "Corredor creado exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear el corredor: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al crear el corredor." });
            }
        }



        [HttpGet]
        public async Task<IActionResult> ObtenerCategoriasPorCarrera_Corredor(int carreraId)
        {
            var categorias = new List<string>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
            SELECT DISTINCT cat.nombre_categoria
            FROM CATEGORIA cat
            INNER JOIN CARR_CAT cc ON cat.ID_categoria = cc.ID_categoria
            WHERE cc.ID_carrera = @CarreraId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CarreraId", carreraId);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categorias.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return Json(categorias);
        }



        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleCorredorPorNumero(

    int numero, int year, int edicion, string categoria)
        {
            // Log inicial para depurar los parámetros recibidos
            _logger.LogInformation("Iniciando búsqueda del corredor...");
            _logger.LogInformation($"Parámetros recibidos: Numero={numero}, Year={year}, Edicion={edicion}, Categoria={categoria}");

            var detalles = new
            {
                Success = false,
                Nombre = "",
                ApellidoPaterno = "",
                ApellidoMaterno = "",
                FechaNacimiento = "",
                Sexo = "",
                Pais = "",
                Correo = ""
            };

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Conexión a la base de datos abierta.");

                    // Consulta SQL ajustada
                    string query = @"
                SELECT c.nom_corredor, c.apP_corredor, c.apM_corredor, 
                       c.f_corredor, c.sex_corredor, c.pais, c.c_corredor
                FROM CORREDOR c
                INNER JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
                INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                INNER JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE vp.num_corredor = @Numero
                  AND ca.year_carrera = @Year
                  AND ca.edi_carrera = @Edicion
                  AND cat.nombre_categoria = @Categoria";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Asignar parámetros
                        command.Parameters.AddWithValue("@Numero", numero);
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Edicion", edicion);
                        command.Parameters.AddWithValue("@Categoria", categoria);

                        _logger.LogInformation("Consulta SQL preparada con los parámetros:");
                        _logger.LogInformation($"@Numero={numero}, @Year={year}, @Edicion={edicion}, @Categoria={categoria}");

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                _logger.LogInformation("Corredor encontrado en la base de datos.");

                                // Extraer datos del corredor
                                detalles = new
                                {
                                    Success = true,
                                    Nombre = reader.GetString(0),
                                    ApellidoPaterno = reader.GetString(1),
                                    ApellidoMaterno = reader.GetString(2),
                                    FechaNacimiento = reader.GetDateTime(3).ToString("yyyy-MM-dd"),
                                    Sexo = reader.GetString(4),
                                    Pais = reader.GetString(5),
                                    Correo = reader.IsDBNull(6) ? "" : reader.GetString(6)
                                };

                                // Log de los datos obtenidos
                                _logger.LogInformation($"Detalles obtenidos: {detalles}");
                            }
                            else
                            {
                                _logger.LogWarning("No se encontró ningún corredor con los parámetros proporcionados.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log de error
                _logger.LogError($"Error al buscar detalles del corredor: {ex.Message}");
            }

            // Log final
            _logger.LogInformation("Proceso de búsqueda finalizado.");
            return Json(detalles);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCorredores(int year, int edicion, string categoria)
        {
            var corredores = new List<object>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            _logger.LogInformation("Obteniendo corredores para Año: {Year}, Edición: {Edicion}, Categoría: {Categoria}", year, edicion, categoria);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                SELECT c.ID_corredor, CONCAT(c.nom_corredor, ' ', c.apP_corredor, ' ', c.apM_corredor) AS NombreCompleto
                FROM CORREDOR c
                INNER JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
                INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                INNER JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE ca.year_carrera = @Year AND ca.edi_carrera = @Edicion AND cat.nombre_categoria = @Categoria
                ORDER BY c.apP_corredor, c.apM_corredor, c.nom_corredor";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Edicion", edicion);
                        command.Parameters.AddWithValue("@Categoria", categoria);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Verificar el tipo de ID_corredor
                                var idCorredor = reader["ID_corredor"];
                                if (idCorredor is byte[] byteArray)
                                {
                                    corredores.Add(new
                                    {
                                        ID_corredor = Convert.ToBase64String(byteArray), // Codificamos correctamente a Base64
                                        NombreCompleto = reader["NombreCompleto"].ToString()
                                    });
                                }
                                else
                                {
                                    _logger.LogError("ID_corredor no es de tipo VARBINARY, es: " + idCorredor.GetType().ToString());
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("Corredores obtenidos exitosamente: {Count}", corredores.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al obtener corredores: {Message}", ex.Message);
            }

            return Json(corredores);
        }

        [HttpGet]
        public async Task<IActionResult> Crear_Carrera()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var categorias = new List<SelectListItem>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT ID_categoria, nombre_categoria FROM CATEGORIA ORDER BY ID_categoria";
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categorias.Add(new SelectListItem
                        {
                            Value = reader["ID_categoria"].ToString(),
                            Text = reader["nombre_categoria"].ToString()
                        });
                    }
                }
            }

            ViewBag.Categorias = categorias;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear_Carrera(string nombreCarrera, int yearCarrera, List<int> categoriasSeleccionadas)
        {
            if (categoriasSeleccionadas == null || categoriasSeleccionadas.Count != 3)
            {
                return Json(new { success = false, message = "Debes seleccionar exactamente tres categorías diferentes." });
            }

            categoriasSeleccionadas.Sort(); // Ordenar las categorías seleccionadas de menor a mayor.

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            int idCarrera;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string insertCarreraSql = @"
                INSERT INTO CARRERA (nom_carrera, year_carrera, edi_carrera)
                OUTPUT INSERTED.ID_carrera
                VALUES (@NombreCarrera, @YearCarrera,
                    COALESCE(
                        (SELECT MAX(edi_carrera) + 1 
                         FROM CARRERA 
                         WHERE nom_carrera = @NombreCarrera AND year_carrera = @YearCarrera),
                        1 -- Si no hay registros previos, inicia con edición 1
                    )
                );";

                    using (SqlCommand command = new SqlCommand(insertCarreraSql, connection))
                    {
                        command.Parameters.AddWithValue("@NombreCarrera", nombreCarrera);
                        command.Parameters.AddWithValue("@YearCarrera", yearCarrera);

                        idCarrera = (int)await command.ExecuteScalarAsync();
                    }

                    string insertCategoriasSql = "INSERT INTO CARR_Cat (ID_carrera, ID_categoria) VALUES (@IDCarrera, @IDCategoria)";
                    foreach (var categoriaId in categoriasSeleccionadas)
                    {
                        using (SqlCommand command = new SqlCommand(insertCategoriasSql, connection))
                        {
                            command.Parameters.AddWithValue("@IDCarrera", idCarrera);
                            command.Parameters.AddWithValue("@IDCategoria", categoriaId);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Json(new { success = true, message = "Carrera creada exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al crear la carrera: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al crear la carrera." });
            }
        }



        [HttpGet]
        public async Task<IActionResult> ObtenerCarrerasConCategorias()
        {
            var carreras = new List<object>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
            SELECT 
                ca.ID_carrera,
                CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')', ' (', STRING_AGG(cat.nombre_categoria, ', '), ')') AS Carrera
            FROM CARRERA ca
            JOIN CARR_CAT cc ON ca.ID_carrera = cc.ID_carrera
            JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
            GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
            HAVING COUNT(cc.ID_categoria) = 3
            ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        carreras.Add(new
                        {
                            Id = reader.GetInt32(0), // ID de la carrera
                            Descripcion = reader.GetString(1) // Descripción formateada
                        });
                    }
                }
            }

            // Retornar JSON con las carreras
            return Json(carreras);
        }


        [HttpGet]
        public async Task<IActionResult> Eliminar_Carrera()
        {
            var carreras = new List<object>();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
            SELECT 
                ca.ID_carrera,
                CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')', ' (', STRING_AGG(cat.nombre_categoria, ', '), ')') AS Carrera
            FROM CARRERA ca
            JOIN CARR_CAT cc ON ca.ID_carrera = cc.ID_carrera
            JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
            GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
            HAVING COUNT(cc.ID_categoria) = 3
            ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        carreras.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Descripcion = reader.GetString(1)
                        });
                    }
                }
            }

            ViewBag.Carreras = carreras;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar_Carrera(int carreraId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Eliminar los registros relacionados en la tabla CARR_CAT
                string deleteCarrCatQuery = "DELETE FROM CARR_CAT WHERE ID_carrera = @ID_carrera";
                using (SqlCommand command = new SqlCommand(deleteCarrCatQuery, connection))
                {
                    command.Parameters.AddWithValue("@ID_carrera", carreraId);
                    await command.ExecuteNonQueryAsync();
                }

                // Ahora eliminar la carrera de la tabla CARRERA
                string deleteCarreraQuery = "DELETE FROM CARRERA WHERE ID_carrera = @ID_carrera";
                using (SqlCommand command = new SqlCommand(deleteCarreraQuery, connection))
                {
                    command.Parameters.AddWithValue("@ID_carrera", carreraId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            // Después de eliminar la carrera y los registros relacionados, redirigir con un mensaje de éxito
            return Json(new { success = true });
        }

        private async Task<List<string>> ObtenerCategoriasPorCarrera(int carreraId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var categorias = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT DISTINCT cat.nombre_categoria
                FROM CATEGORIA cat
                INNER JOIN CARR_Cat cc ON cat.ID_categoria = cc.ID_categoria
                WHERE cc.ID_carrera = @CarreraId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarreraId", carreraId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                categorias.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener categorías de la carrera: {ex.Message}");
            }

            return categorias;
        }

        private async Task<List<Dictionary<string, object>>> BuscarCorredoresHelper(int carreraId, string categoria)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var resultados = new List<Dictionary<string, object>>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT t.folio_chip
                FROM Tiempo t
                JOIN Vincula_participante vp ON t.folio_chip = vp.folio_chip
                JOIN CARR_Cat cc ON vp.ID_Carr_cat = cc.ID_carr_cat
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE cc.ID_carrera = @CarreraId AND cat.nombre_categoria = @Categoria";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarreraId", carreraId);
                        command.Parameters.AddWithValue("@Categoria", categoria);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                resultados.Add(new Dictionary<string, object>
                        {
                            { "FolioChip", reader["folio_chip"] }
                        });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al buscar corredores para la categoría {categoria}: {ex.Message}");
            }

            return resultados;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCarreras()
        {
            var carreras = new List<object>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
        SELECT ID_carrera, CONCAT(nom_carrera, ' - ', year_carrera, ' (Edición: ', edi_carrera, ')') AS CarreraInfo
        FROM CARRERA
        ORDER BY year_carrera DESC, edi_carrera DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        carreras.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Descripcion = reader.GetString(1)
                        });
                    }
                }
            }

            return Json(carreras);
        }


        [HttpPost]
        public async Task<IActionResult> VerificarTiempos(int carreraId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT COUNT(*) 
                FROM dbo.Tiempo t
                JOIN Vincula_participante vp ON t.folio_chip = vp.folio_chip
                JOIN CARR_Cat cc ON vp.ID_Carr_cat = cc.ID_carr_cat
                WHERE cc.ID_carrera = @CarreraId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CarreraId", carreraId);

                        int count = (int)await command.ExecuteScalarAsync();
                        if (count > 0)
                        {
                            return Json(new { success = false, message = "No se puede eliminar la carrera porque tiene tiempos registrados." });
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al verificar tiempos: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al verificar los tiempos." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar_Carrera()
        {
            var carreras = new List<object>();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
            SELECT 
                ca.ID_carrera,
                CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')', ' (', STRING_AGG(cat.nombre_categoria, ', '), ')') AS Carrera
            FROM CARRERA ca
            JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera
            JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
            GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera
            HAVING COUNT(cc.ID_categoria) = 3
            ORDER BY ca.year_carrera DESC, ca.edi_carrera DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        carreras.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Descripcion = reader.GetString(1)
                        });
                    }
                }
            }

            ViewBag.Carreras = carreras;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Actualizar_Carrera(int carreraId, string nombreCarrera, int yearCarrera, List<int> categoriasSeleccionadas)
        {
            if (categoriasSeleccionadas == null || categoriasSeleccionadas.Count != 3)
            {
                return Json(new { success = false, message = "Selecciona exactamente tres categorías." });
            }

            // Verificar si las categorías son únicas
            if (categoriasSeleccionadas.Distinct().Count() != 3)
            {
                return Json(new { success = false, message = "Debes seleccionar tres categorías diferentes." });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Actualizar el nombre y el año de la carrera
                    string updateCarrera = "UPDATE CARRERA SET nom_carrera = @Nombre, year_carrera = @Year WHERE ID_carrera = @Id";
                    using (SqlCommand command = new SqlCommand(updateCarrera, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", nombreCarrera);
                        command.Parameters.AddWithValue("@Year", yearCarrera);
                        command.Parameters.AddWithValue("@Id", carreraId);
                        await command.ExecuteNonQueryAsync();
                    }

                    // Ahora actualizar solo los IDs de las categorías
                    // Obtener los ID_carr_cat para cada categoría asociada a la carrera
                    string getCarrCatIds = "SELECT ID_carr_cat, ID_categoria FROM CARR_CAT WHERE ID_carrera = @Id";
                    var categoriaIds = new List<int>(); // Para almacenar los ID_carr_cat que necesitan ser actualizados

                    using (SqlCommand command = new SqlCommand(getCarrCatIds, connection))
                    {
                        command.Parameters.AddWithValue("@Id", carreraId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                categoriaIds.Add(reader.GetInt32(0)); // ID_carr_cat
                            }
                        }
                    }

                    // Actualizar las categorías con el nuevo ID_categoria
                    string updateCategorias = @"
                UPDATE CARR_CAT
                SET ID_categoria = @CategoriaId
                WHERE ID_carr_cat = @IdCarrCat";

                    for (int i = 0; i < 3; i++)
                    {
                        using (SqlCommand command = new SqlCommand(updateCategorias, connection))
                        {
                            command.Parameters.AddWithValue("@CategoriaId", categoriasSeleccionadas[i]);
                            command.Parameters.AddWithValue("@IdCarrCat", categoriaIds[i]); // Actualizamos el ID_carr_cat

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Json(new { success = true, message = "Carrera actualizada exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar la carrera: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al actualizar la carrera." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> CargarDatosCarrera(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var resultado = new DatosCarrera();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Obtener nombre y año de la carrera
                string queryCarrera = "SELECT nom_carrera, year_carrera FROM CARRERA WHERE ID_carrera = @Id";
                using (SqlCommand command = new SqlCommand(queryCarrera, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            resultado.Nombre = reader.GetString(0);   // Verifica que se está obteniendo el nombre
                            resultado.Year = reader.GetInt32(1);     // Verifica que se está obteniendo el año
                        }
                    }
                }

                // Obtener categorías asociadas a la carrera seleccionada
                string queryCategorias = @"
            SELECT cc.ID_categoria, c.nombre_categoria 
            FROM CARR_CAT cc 
            JOIN CATEGORIA c ON cc.ID_categoria = c.ID_categoria 
            WHERE cc.ID_carrera = @Id";
                using (SqlCommand command = new SqlCommand(queryCategorias, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultado.Categorias.Add(new Categoria
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1)
                            });
                        }
                    }
                }

                // Obtener todas las categorías disponibles
                string queryTodasCategorias = "SELECT ID_categoria, nombre_categoria FROM CATEGORIA";
                using (SqlCommand command = new SqlCommand(queryTodasCategorias, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        resultado.TodasCategorias.Add(new Categoria
                        {
                            Id = reader.GetInt32(0),
                            Nombre = reader.GetString(1)
                        });
                    }
                }
            }

            // Verifica que resultado contiene todos los valores correctos
            Console.WriteLine($"Datos de la Carrera: {resultado.Nombre}, {resultado.Year}, Categorías: {resultado.Categorias.Count}");

            return Json(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> Eliminar_Corredor()
        {
            // Obtener los datos para los menús desplegables
            var anios = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Cargar años
                string aniosQuery = "SELECT DISTINCT year_carrera FROM CARRERA";
                using (SqlCommand command = new SqlCommand(aniosQuery, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        anios.Add(reader.GetInt32(0));
                    }
                }
            }

            ViewBag.Anios = anios;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BuscarCorredor(int year, int edicion, string categoria, int numero)
        {
            if (string.IsNullOrEmpty(categoria))
            {
                return Json(new { success = false, message = "La categoría es obligatoria." });
            }

            var corredor = new Dictionary<string, string>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT c.nom_corredor, c.apP_corredor, c.apM_corredor, c.f_corredor, 
                       c.sex_corredor, c.pais, c.c_corredor, vp.ID_corredor
                FROM CORREDOR c
                JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
                JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE ca.year_carrera = @Year 
                  AND ca.edi_carrera = @Edicion 
                  AND cat.nombre_categoria = @Categoria 
                  AND vp.num_corredor = @Numero";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Edicion", edicion);
                        command.Parameters.AddWithValue("@Categoria", categoria);
                        command.Parameters.AddWithValue("@Numero", numero);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                corredor["Nombre"] = reader.GetString(0);
                                corredor["ApellidoPaterno"] = reader.GetString(1);
                                corredor["ApellidoMaterno"] = reader.GetString(2);
                                corredor["FechaNacimiento"] = reader.GetDateTime(3).ToString("yyyy-MM-dd");
                                corredor["Sexo"] = reader.GetString(4);
                                corredor["Pais"] = reader.GetString(5);
                                corredor["Correo"] = reader.IsDBNull(6) ? "" : reader.GetString(6);
                                corredor["ID_corredor"] = Convert.ToBase64String((byte[])reader["ID_corredor"]);
                            }
                            else
                            {
                                return Json(new { success = false, message = "No se encontró ningún corredor con los parámetros proporcionados." });
                            }
                        }
                    }
                }

                return Json(new { success = true, corredor });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al buscar corredor: {Message}", ex.Message);
                return Json(new { success = false, message = "Ocurrió un error al buscar el corredor." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EliminarCorredor(int year, int edicion, string categoria, int numero)
        {
            _logger.LogInformation($"Inicio del proceso para eliminar corredor: Año={year}, Edición={edicion}, Categoría={categoria}, Número={numero}");

            // Validar que los parámetros sean válidos
            if (year <= 0 || edicion <= 0 || string.IsNullOrEmpty(categoria) || numero <= 0)
            {
                _logger.LogError("Parámetros inválidos para eliminar corredor.");
                return Json(new { success = false, message = "Parámetros inválidos. Verifica tu selección e inténtalo de nuevo." });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Verificar si el corredor tiene tiempos registrados
                    string verificarTiemposQuery = @"
                SELECT COUNT(*)
                FROM TIEMPO t
                JOIN Vincula_participante vp ON t.folio_chip = vp.folio_chip
                JOIN CARR_Cat cc ON vp.ID_Carr_cat = cc.ID_carr_cat
                JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE ca.year_carrera = @Year
                  AND ca.edi_carrera = @Edicion
                  AND cat.nombre_categoria = @Categoria
                  AND vp.num_corredor = @Numero";

                    using (SqlCommand verificarCommand = new SqlCommand(verificarTiemposQuery, connection))
                    {
                        verificarCommand.Parameters.AddWithValue("@Year", year);
                        verificarCommand.Parameters.AddWithValue("@Edicion", edicion);
                        verificarCommand.Parameters.AddWithValue("@Categoria", categoria);
                        verificarCommand.Parameters.AddWithValue("@Numero", numero);

                        int tiemposRegistrados = (int)await verificarCommand.ExecuteScalarAsync();
                        if (tiemposRegistrados > 0)
                        {
                            _logger.LogWarning("No se puede eliminar el corredor porque tiene tiempos registrados.");
                            return Json(new { success = false, message = "No se puede eliminar el corredor porque tiene tiempos registrados." });
                        }
                    }

                    // Eliminar el corredor de la tabla `Vincula_participante`
                    string eliminarCorredorQuery = @"
                DELETE FROM Vincula_participante
                WHERE ID_carr_cat IN (
                    SELECT cc.ID_carr_cat
                    FROM CARR_Cat cc
                    JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                    WHERE ca.year_carrera = @Year
                      AND ca.edi_carrera = @Edicion
                      AND cat.nombre_categoria = @Categoria
                  )
                  AND num_corredor = @Numero";

                    using (SqlCommand eliminarCommand = new SqlCommand(eliminarCorredorQuery, connection))
                    {
                        eliminarCommand.Parameters.AddWithValue("@Year", year);
                        eliminarCommand.Parameters.AddWithValue("@Edicion", edicion);
                        eliminarCommand.Parameters.AddWithValue("@Categoria", categoria);
                        eliminarCommand.Parameters.AddWithValue("@Numero", numero);

                        int filasAfectadas = await eliminarCommand.ExecuteNonQueryAsync();
                        if (filasAfectadas == 0)
                        {
                            _logger.LogWarning("No se encontró ningún corredor con los parámetros proporcionados.");
                            return Json(new { success = false, message = "No se encontró ningún corredor con los parámetros proporcionados." });
                        }
                    }
                }

                _logger.LogInformation("Corredor eliminado exitosamente.");
                return Json(new { success = true, message = "Corredor eliminado exitosamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar el corredor: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al intentar eliminar el corredor. Inténtalo más tarde." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Modificar_Corredor()
        {
            // Obtener los datos para los menús desplegables
            var anios = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Cargar años
                string aniosQuery = "SELECT DISTINCT year_carrera FROM CARRERA";
                using (SqlCommand command = new SqlCommand(aniosQuery, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        anios.Add(reader.GetInt32(0));
                    }
                }
            }

            ViewBag.Anios = anios;
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> EditarCorredor(
    int numero, string nombre, string apellidoPaterno, string apellidoMaterno,
    DateTime fechaNacimiento, string sexo, string pais, string correo,
    int? nuevaCarreraId, string nuevaCategoria)
        {
            _logger.LogInformation($"Iniciando actualización del corredor con número {numero}");

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Buscar el ID_corredor basado en el número
                            string getCorredorIdQuery = @"
                        SELECT c.ID_corredor, vp.ID_carr_cat 
                        FROM CORREDOR c
                        INNER JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
                        WHERE vp.num_corredor = @Numero";

                            byte[] idCorredor = null;
                            int idCarrCatActual = 0;

                            using (SqlCommand command = new SqlCommand(getCorredorIdQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Numero", numero);
                                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        idCorredor = (byte[])reader["ID_corredor"];
                                        idCarrCatActual = reader.GetInt32(1);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No se encontró el corredor.");
                                        return Json(new { success = false, message = "Corredor no encontrado." });
                                    }
                                }
                            }

                            // 2. Actualizar la tabla CORREDOR
                            string updateCorredorQuery = @"
                        UPDATE CORREDOR
                        SET nom_corredor = @Nombre, apP_corredor = @ApellidoPaterno, 
                            apM_corredor = @ApellidoMaterno, f_corredor = @FechaNacimiento, 
                            sex_corredor = @Sexo, pais = @Pais, c_corredor = @Correo
                        WHERE ID_corredor = @ID_corredor";

                            using (SqlCommand command = new SqlCommand(updateCorredorQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Nombre", nombre);
                                command.Parameters.AddWithValue("@ApellidoPaterno", apellidoPaterno);
                                command.Parameters.AddWithValue("@ApellidoMaterno", apellidoMaterno);
                                command.Parameters.AddWithValue("@FechaNacimiento", fechaNacimiento);
                                command.Parameters.AddWithValue("@Sexo", sexo);
                                command.Parameters.AddWithValue("@Pais", pais);
                                command.Parameters.AddWithValue("@Correo", (object)correo ?? DBNull.Value);
                                command.Parameters.AddWithValue("@ID_corredor", idCorredor);

                                await command.ExecuteNonQueryAsync();
                            }

                            // 3. Verificar si hay cambio en carrera/categoría y actualizar Vincula_participante
                            if (nuevaCarreraId.HasValue && !string.IsNullOrEmpty(nuevaCategoria))
                            {
                                // Obtener el nuevo ID_carr_cat
                                string getNuevoCarrCatQuery = @"
                            SELECT ID_carr_cat 
                            FROM CARR_Cat cc
                            INNER JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                            WHERE cc.ID_carrera = @NuevaCarreraId AND cat.nombre_categoria = @NuevaCategoria";

                                int nuevoIdCarrCat = 0;
                                using (SqlCommand command = new SqlCommand(getNuevoCarrCatQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@NuevaCarreraId", nuevaCarreraId.Value);
                                    command.Parameters.AddWithValue("@NuevaCategoria", nuevaCategoria);

                                    object result = await command.ExecuteScalarAsync();
                                    if (result != null)
                                    {
                                        nuevoIdCarrCat = (int)result;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No se encontró la nueva combinación de carrera y categoría.");
                                        return Json(new { success = false, message = "La nueva carrera o categoría no es válida." });
                                    }
                                }

                                // Actualizar Vincula_participante solo si el ID_carr_cat es diferente
                                if (nuevoIdCarrCat != idCarrCatActual)
                                {
                                    string updateVinculaQuery = @"
                                UPDATE Vincula_participante
                                SET ID_carr_cat = @NuevoIdCarrCat
                                WHERE ID_corredor = @ID_corredor";

                                    using (SqlCommand command = new SqlCommand(updateVinculaQuery, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@NuevoIdCarrCat", nuevoIdCarrCat);
                                        command.Parameters.AddWithValue("@ID_corredor", idCorredor);
                                        await command.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            // Commit de la transacción
                            transaction.Commit();
                            _logger.LogInformation("Corredor actualizado exitosamente.");
                            return Json(new { success = true, message = "Corredor actualizado correctamente." });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError($"Error durante la actualización: {ex.Message}");
                            return Json(new { success = false, message = "Error durante la actualización del corredor." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error general: {ex.Message}");
                return Json(new { success = false, message = "Error inesperado. Inténtalo más tarde." });
            }
        }








        public class DatosCarrera
        {
            public string Nombre { get; set; }
            public int Year { get; set; }
            public List<Categoria> Categorias { get; set; } = new List<Categoria>();
            public List<Categoria> TodasCategorias { get; set; } = new List<Categoria>();
        }

        public class Categoria
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public class CorredorViewModel
        {
            public string ID { get; set; }
            public string Nombre { get; set; }
            public string Pais { get; set; }
            public string Sexo { get; set; }
            public string FechaNacimiento { get; set; }
        }
        public class EditarCorredorViewModel
        {
            public string Nombre { get; set; }
            public string ApellidoPaterno { get; set; }
            public string ApellidoMaterno { get; set; }
            public string FechaNacimiento { get; set; } // Formato yyyy-MM-dd
            public string Sexo { get; set; }
            public string Pais { get; set; }
            public string Correo { get; set; } // Campo opcional
            public int Year { get; set; }
            public int Edicion { get; set; }
            public string Categoria { get; set; }
            public int Numero { get; set; }
            public int Carrera { get; set; } // ID de la nueva carrera seleccionada
            public string NuevaCategoria { get; set; } // Nombre de la nueva categoría seleccionada
        }
    }
}