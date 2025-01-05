using Microsoft.AspNetCore.Mvc;
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
using ClosedXML.Excel;
using System.Globalization;
using System.Data;
using Microsoft.IdentityModel.Tokens;


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
                string sql = @"
            SELECT c.Categoria
            FROM CATEGORIAS c
            INNER JOIN Vincula_Participante vp ON vp.Categoria = c.Categoria
            INNER JOIN CARRERA ca ON vp.ID_carrera = ca.ID_carrera
            WHERE ca.year_carrera = @Year AND ca.edi_carrera = @Edicion";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Year", year);
                    command.Parameters.AddWithValue("@Edicion", edicion);
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

                    // Intentar insertar el corredor (si ya existe, se lanzará una excepción)
                    byte[]? corredorId = null;
                    try
                    {
                        string insertCorredorSql = @"
                    INSERT INTO CORREDOR (nom_corredor, apP_corredor, apM_corredor, 
                                          f_corredor, sex_corredor, c_corredor, pais) 
                    OUTPUT INSERTED.ID_corredor
                    VALUES (@Nombre, @Apaterno, @Amaterno, @Fnacimiento, @Sexo, @Correo, @Pais)";

                        using (SqlCommand insertCommand = new SqlCommand(insertCorredorSql, connection))
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
                    }
                    catch (SqlException ex) when (ex.Number == 2627) // Error de clave duplicada
                    {
                        // Si ocurre un duplicado, obtener el ID del corredor existente
                        string getCorredorSql = @"
                    SELECT ID_corredor 
                    FROM CORREDOR 
                    WHERE nom_corredor = @Nombre AND apP_corredor = @Apaterno 
                      AND (apM_corredor = @Amaterno OR @Amaterno IS NULL)
                      AND f_corredor = @Fnacimiento";

                        using (SqlCommand getCorredorCommand = new SqlCommand(getCorredorSql, connection))
                        {
                            getCorredorCommand.Parameters.AddWithValue("@Nombre", Nombre);
                            getCorredorCommand.Parameters.AddWithValue("@Apaterno", Apaterno);
                            getCorredorCommand.Parameters.AddWithValue("@Amaterno", (object?)Amaterno ?? DBNull.Value);
                            getCorredorCommand.Parameters.AddWithValue("@Fnacimiento", Fnacimiento);

                            corredorId = (byte[]?)await getCorredorCommand.ExecuteScalarAsync();
                        }
                    }

                    if (corredorId == null)
                        return Json(new { success = false, message = "No se pudo obtener o crear el corredor." });

                    // Verificar existencia de la categoría
                    string getCategoriaSql = @"
                SELECT ID_categoria 
                FROM CATEGORIA 
                WHERE nombre_categoria = @CategoriaNombre";

                    int? idCategoria;
                    using (SqlCommand getCategoriaCommand = new SqlCommand(getCategoriaSql, connection))
                    {
                        getCategoriaCommand.Parameters.AddWithValue("@CategoriaNombre", CategoriaNombre);
                        idCategoria = (int?)await getCategoriaCommand.ExecuteScalarAsync();
                    }

                    if (!idCategoria.HasValue)
                        return Json(new { success = false, message = "Categoría seleccionada no válida." });

                    // Verificar existencia de la combinación Carrera-Categoría
                    string getCarrCatSql = @"
                SELECT ID_carr_cat 
                FROM CARR_Cat 
                WHERE ID_carrera = @CarreraId AND ID_categoria = @IDCategoria";

                    int? idCarrCat;
                    using (SqlCommand getCarrCatCommand = new SqlCommand(getCarrCatSql, connection))
                    {
                        getCarrCatCommand.Parameters.AddWithValue("@CarreraId", CarreraId);
                        getCarrCatCommand.Parameters.AddWithValue("@IDCategoria", idCategoria.Value);
                        idCarrCat = (int?)await getCarrCatCommand.ExecuteScalarAsync();
                    }

                    if (!idCarrCat.HasValue)
                        return Json(new { success = false, message = "La combinación de Carrera y Categoría no es válida." });

                    // Verificar si el corredor ya está vinculado a esta Carrera-Categoría
                    string checkVinculoSql = @"
                SELECT COUNT(*) 
                FROM Vincula_participante 
                WHERE ID_corredor = @IDCorredor AND ID_carr_cat = @IDCarrCat";

                    using (SqlCommand checkVinculoCommand = new SqlCommand(checkVinculoSql, connection))
                    {
                        checkVinculoCommand.Parameters.AddWithValue("@IDCorredor", corredorId);
                        checkVinculoCommand.Parameters.AddWithValue("@IDCarrCat", idCarrCat.Value);

                        if ((int)await checkVinculoCommand.ExecuteScalarAsync() > 0)
                        {
                            return Json(new { success = false, message = "El corredor ya está asociado a esta carrera." });
                        }
                    }

                    // Insertar vínculo
                    string insertVinculoSql = @"
                INSERT INTO Vincula_participante (ID_corredor, ID_carr_cat, num_corredor, folio_chip) 
                VALUES (@IDCorredor, 
                        @IDCarrCat,
                        (SELECT ISNULL(MAX(num_corredor), 0) + 1 FROM Vincula_participante),
                        'RFID' + CAST(1000000000 + (SELECT COUNT(*) + 1 FROM Vincula_participante) AS VARCHAR))";

                    using (SqlCommand vinculoCommand = new SqlCommand(insertVinculoSql, connection))
                    {
                        vinculoCommand.Parameters.AddWithValue("@IDCorredor", corredorId);
                        vinculoCommand.Parameters.AddWithValue("@IDCarrCat", idCarrCat.Value);

                        await vinculoCommand.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Corredor vinculado exitosamente a la nueva carrera." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al vincular el corredor: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al vincular el corredor." });
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



        public async Task<IActionResult> Baja_Corredor(int corredorId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Verificar si el corredor existe
                    string checkSql = "SELECT COUNT(*) FROM Corredor WHERE CorredorId = @CorredorId";
                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@CorredorId", corredorId);
                        int count = (int)await checkCommand.ExecuteScalarAsync();

                        if (count == 0)
                        {
                            // Corredor no encontrado
                            _logger.LogWarning($"Corredor con ID {corredorId} no encontrado.");
                            ModelState.AddModelError(string.Empty, "Corredor no encontrado.");
                            return View();
                        }
                    }

                    // Eliminar el corredor
                    string deleteSql = "DELETE FROM Corredor WHERE CorredorId = @CorredorId";
                    using (SqlCommand deleteCommand = new SqlCommand(deleteSql, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@CorredorId", corredorId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }
                }

                // Baja exitosa
                _logger.LogInformation($"Corredor con ID {corredorId} dado de baja exitosamente.");
                TempData["SuccessMessage"] = "Corredor dado de baja exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al dar de baja al corredor: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al dar de baja al corredor. Por favor, int�ntelo de nuevo m�s tarde.");
                return View();
            }
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


        [HttpGet]
        public async Task<IActionResult> CargarDatosCarreras(int id)
        {
            // Obtener la cadena de conexión desde el archivo de configuración
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            var carreraSeleccionada = new object();
            var todasCategorias = new List<object>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Obtener datos de la carrera seleccionada
                string carreraQuery = @"
            SELECT 
                ca.ID_carrera, 
                ca.nom_carrera, 
                ca.year_carrera, 
                ca.edi_carrera,
                STRING_AGG(cat.ID_categoria, ',') AS CategoriasIds,
                STRING_AGG(cat.nombre_categoria, ',') AS CategoriasNombres
            FROM CARRERA ca
            JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera
            JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
            WHERE ca.ID_carrera = @CarreraId
            GROUP BY ca.ID_carrera, ca.nom_carrera, ca.year_carrera, ca.edi_carrera";

                using (SqlCommand carreraCommand = new SqlCommand(carreraQuery, connection))
                {
                    carreraCommand.Parameters.AddWithValue("@CarreraId", id);

                    using (SqlDataReader reader = await carreraCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            carreraSeleccionada = new
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Año = reader.GetInt32(2),
                                Edicion = reader.GetInt32(3),
                                Categorias = reader.GetString(4)
                                                  .Split(',')
                                                  .Select(c => new { Id = int.Parse(c) })
                                                  .ToList()
                            };
                        }
                    }
                }

                // Obtener todas las categorías
                string categoriasQuery = "SELECT ID_categoria, nombre_categoria FROM CATEGORIA";

                using (SqlCommand categoriasCommand = new SqlCommand(categoriasQuery, connection))
                using (SqlDataReader reader = await categoriasCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        todasCategorias.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Nombre = reader.GetString(1)
                        });
                    }
                }
            }

            // Devolver los datos como JSON
            return Json(new
            {
                carrera = carreraSeleccionada,
                todasCategorias = todasCategorias
            });
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
        public IActionResult GetCategoriasPorCarrera(int carreraId)
        {
            var categorias = _context.CARR_Cat
                .Where(cc => cc.ID_carrera == carreraId)
                .Join(_context.CATEGORIA,
                      cc => cc.ID_categoria,
                      cat => cat.ID_categoria,
                      (cc, cat) => new { cat.ID_categoria, cat.nombre_categoria })
                .ToList();

            return Json(categorias);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarEdicionCorredor(Corredor corredor)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string updateQuery = @"
            UPDATE CORREDOR
            SET nom_corredor = @Nombre, apP_corredor = @Apaterno, apM_corredor = @Amaterno, 
                f_corredor = @Fnacimiento, pais = @Pais, c_corredor = @Correo
            WHERE ID_corredor = @ID";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ID", corredor.ID_corredor);
                        command.Parameters.AddWithValue("@Nombre", corredor.Nom_corredor);
                        command.Parameters.AddWithValue("@Apaterno", corredor.apP_corredor);
                        command.Parameters.AddWithValue("@Amaterno", (object?)corredor.apM_corredor ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Fnacimiento", corredor.f_corredor);
                        command.Parameters.AddWithValue("@Pais", corredor.pais_corredor);
                        command.Parameters.AddWithValue("@Correo", (object?)corredor.c_corredor ?? DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Corredor editado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar edición del corredor: {ex.Message}");
                return Json(new { success = false, message = "Error al guardar los cambios." });
            }
        }


        public IActionResult Error()
        {
            return View();
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



        [HttpGet]
        public async Task<IActionResult> BuscarEditarCorredor()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var carreras = new List<object>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT ID_carrera, nom_carrera, year_carrera FROM CARRERA";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            carreras.Add(new
                            {
                                ID = reader.GetInt32(0),
                                Nombre = reader.GetString(1),
                                Año = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }

            return Json(carreras);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarCorredoresPorCarrera(int idCarrera, string filtro)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var corredores = new List<object>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
            SELECT c.ID_corredor, c.nom_corredor, c.apP_corredor, c.apM_corredor, c.f_corredor, c.sex_corredor, c.c_corredor, c.pais
            FROM CORREDOR c
            INNER JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
            INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
            WHERE cc.ID_carrera = @IdCarrera
              AND (c.nom_corredor LIKE @Filtro OR c.c_corredor LIKE @Filtro)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdCarrera", idCarrera);
                    command.Parameters.AddWithValue("@Filtro", $"%{filtro}%");

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            corredores.Add(new
                            {
                                ID = reader.GetValue(0),
                                Nombre = reader.GetString(1),
                                ApellidoPaterno = reader.GetString(2),
                                ApellidoMaterno = reader.GetString(3),
                                FechaNacimiento = reader.GetDateTime(4).ToString("yyyy-MM-dd"),
                                Sexo = reader.GetString(5),
                                Correo = reader.GetString(6),
                                Pais = reader.GetString(7)
                            });
                        }
                    }
                }
            }

            return Json(corredores);
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
                                corredor["Pais"] = reader.GetString(5); // Cambiado para que coincida con el JavaScript
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
        public async Task<IActionResult> ActualizarCorredor(
     int year, int edicion, string categoria, int numero,
     string nombre, string apellidoPaterno, string apellidoMaterno,
     string fechaNacimiento, string sexo, string pais, string correo)
        {
            _logger.LogInformation($"Parámetros recibidos: Año={year}, Edición={edicion}, Categoría={categoria}, Número={numero}, Nombre={nombre}, ApellidoPaterno={apellidoPaterno},ApellidoMaterno={apellidoMaterno}, FechaNacimiento={fechaNacimiento}");

            if (year <= 0 || edicion <= 0 || string.IsNullOrEmpty(categoria) || numero <= 0)
            {
                _logger.LogError("Parámetros inválidos para actualizar corredor.");
                return Json(new { success = false, message = "Parámetros inválidos. Verifica tu selección e inténtalo de nuevo." });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string updateQuery = @"
            UPDATE CORREDOR
            SET nom_corredor = @Nombre, 
                apP_corredor = @ApellidoPaterno, 
                apM_corredor = @ApellidoMaterno, 
                f_corredor = @FechaNacimiento, 
                sex_corredor = @Sexo, 
                pais = @Pais, 
                c_corredor = @Correo
            WHERE ID_corredor = (
                SELECT vp.ID_corredor
                FROM Vincula_participante vp
                JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE ca.year_carrera = @Year 
                  AND ca.edi_carrera = @Edicion 
                  AND cat.nombre_categoria = @Categoria 
                  AND vp.num_corredor = @Numero
            )";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", nombre);
                        command.Parameters.AddWithValue("@ApellidoPaterno", apellidoPaterno);
                        command.Parameters.AddWithValue("@ApellidoMaterno", apellidoMaterno);
                        command.Parameters.AddWithValue("@FechaNacimiento", DateTime.Parse(fechaNacimiento));
                        command.Parameters.AddWithValue("@Sexo", sexo);
                        command.Parameters.AddWithValue("@Pais", pais);
                        command.Parameters.AddWithValue("@Correo", correo);
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Edicion", edicion);
                        command.Parameters.AddWithValue("@Categoria", categoria);
                        command.Parameters.AddWithValue("@Numero", numero);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            _logger.LogInformation("Información del corredor actualizada exitosamente.");
                            return Json(new { success = true, message = "Información del corredor actualizada exitosamente." });
                        }
                        else
                        {
                            _logger.LogWarning("No se encontró el corredor con los parámetros proporcionados.");
                            return Json(new { success = false, message = "No se encontró el corredor con los parámetros proporcionados." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar el corredor: {ex.Message}");
                return Json(new { success = false, message = "Ocurrió un error al intentar actualizar la información del corredor. Inténtalo más tarde." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Importar_Corredores()
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
        public async Task<IActionResult> Subir_ArchivoCorredor(string rutaArchivo, int carreraId, string categoriaNombre)
        {
            if (!string.IsNullOrEmpty(rutaArchivo) && System.IO.File.Exists(rutaArchivo))
            {
                try
                {
                    _logger.LogInformation($"Procesando archivo en la ruta: {rutaArchivo}");
                    int filasProcesadas = 0;
                    int errores = 0;

                    using (var workbook = new XLWorkbook(rutaArchivo))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var filas = worksheet.RowsUsed();

                        foreach (var fila in filas.Skip(1)) // Saltar encabezados
                        {
                            try
                            {
                                if (fila.Cells().All(cell => cell.IsEmpty()))
                                {
                                    _logger.LogWarning($"Fila {fila.RowNumber()} está vacía. Saltando.");
                                    continue;
                                }

                                var nom_corredor = fila.Cell(1).GetString();
                                var apellido_paterno = fila.Cell(2).GetString();
                                var apellido_materno = fila.Cell(3).GetString();
                                var fecha_cumpleanios_celda = fila.Cell(4);
                                var sexo_corredor = fila.Cell(5).GetString().Trim();
                                var correo_corredor = fila.Cell(6).GetString();
                                var pais = fila.Cell(7).GetString();
                                var telefono = fila.Cell(8).GetString();

                                if (sexo_corredor != "M" && sexo_corredor != "F")
                                {
                                    _logger.LogError($"Error en fila {fila.RowNumber()}: Sexo '{sexo_corredor}' no válido.");
                                    errores++;
                                    continue;
                                }

                                if (fecha_cumpleanios_celda.TryGetValue(out DateTime fechaCumpleanios))
                                {
                                    // La celda ya es de tipo fecha y se puede procesar directamente
                                    if (fechaCumpleanios < new DateTime(1753, 1, 1) || fechaCumpleanios > new DateTime(9999, 12, 31))
                                    {
                                        _logger.LogError($"Error en fila {fila.RowNumber()}: Fecha de nacimiento '{fechaCumpleanios}' fuera del rango permitido (1753-9999).");
                                        errores++;
                                        continue;
                                    }
                                }
                                else
                                {
                                    var fechaTexto = fecha_cumpleanios_celda.GetString().Trim();
                                    if (!DateTime.TryParseExact(fechaTexto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaCumpleanios))
                                    {
                                        _logger.LogError($"Error en fila {fila.RowNumber()}: Fecha de nacimiento '{fechaTexto}' no válida. Asegúrese de usar el formato ISO 8601 (YYYY-MM-DD).");
                                        errores++;
                                        continue;
                                    }

                                    // Validar rango nuevamente
                                    if (fechaCumpleanios < new DateTime(1753, 1, 1) || fechaCumpleanios > new DateTime(9999, 12, 31))
                                    {
                                        _logger.LogError($"Error en fila {fila.RowNumber()}: Fecha de nacimiento '{fechaCumpleanios}' fuera del rango permitido (1753-9999).");
                                        errores++;
                                        continue;
                                    }
                                }

                                _logger.LogInformation($"Procesando fila {fila.RowNumber()}: {nom_corredor} {apellido_paterno}.");
                                await Crear_Corredor(
                                    Nombre: nom_corredor,
                                    Apaterno: apellido_paterno,
                                    Amaterno: apellido_materno,
                                    Fnacimiento: fechaCumpleanios,
                                    Sexo: sexo_corredor,
                                    Correo: correo_corredor,
                                    Pais: pais,
                                    Telefono: telefono,
                                    CarreraId: carreraId,
                                    CategoriaNombre: categoriaNombre
                                );

                                filasProcesadas++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error procesando fila {fila.RowNumber()}: {ex.Message}");
                                errores++;
                                continue;
                            }
                        }
                    }

                    var message = errores == 0
                        ? $"{filasProcesadas} corredores procesados exitosamente."
                        : $"Archivo procesado con errores: {filasProcesadas} filas exitosas, {errores} errores.";

                    _logger.LogInformation(message);
                    return Json(new { success = true, message });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al procesar el archivo: {ex.Message}");
                    return Json(new { success = false, message = $"Error al procesar el archivo: {ex.Message}" });
                }
            }

            _logger.LogError("La ruta del archivo no es válida o no existe.");
            return Json(new { success = false, message = "La ruta del archivo no es válida." });
        }

        [HttpGet]
        public async Task<IActionResult> Reporte_Corredor()
        {
            var anios = new List<int>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Cargar años de carreras
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
        public async Task<IActionResult> GenerarReporteCorredor(int year, int edicion, string categoria, int numero, string rutaArchivo)
        {
            try
            {
                // 1. Verificación de la Ruta del Archivo
                var directorio = Path.GetDirectoryName(rutaArchivo);
                var archivo = Path.GetFileName(rutaArchivo);

                if (string.IsNullOrEmpty(directorio) || string.IsNullOrEmpty(archivo))
                {
                    return Json(new { success = false, message = "La ruta proporcionada no es válida." });
                }

                if (!Directory.Exists(directorio))
                {
                    return Json(new { success = false, message = "El directorio especificado no existe." });
                }

                if (System.IO.File.Exists(rutaArchivo))
                {
                    return Json(new { success = false, message = "El archivo ya existe. No se desea sobrescribir." });
                }

                _logger.LogInformation($"Generando reporte desde la ruta: {rutaArchivo}");

                // Conexión a la base de datos
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                byte[] idCorredor = null; // Variable para almacenar el ID del corredor
                string corredorInfo = string.Empty;
                List<string> carreras = new List<string>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 2. Consulta del ID_Corredor
                    string queryIDCorredor = @"
    SELECT c.ID_corredor
    FROM CORREDOR c
    JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
    JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
    JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
    WHERE 
        ca.year_carrera = @Year
        AND ca.edi_carrera = @Edicion
        AND cc.ID_categoria = (SELECT ID_categoria FROM CATEGORIA WHERE nombre_categoria = @Categoria)
        AND vp.num_corredor = @Numero;";

                    using (SqlCommand command = new SqlCommand(queryIDCorredor, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Edicion", edicion);
                        command.Parameters.AddWithValue("@Categoria", categoria);
                        command.Parameters.AddWithValue("@Numero", numero);

                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result is byte[])
                        {
                            idCorredor = (byte[])result; // Mantener como byte[]
                        }
                    }

                    if (idCorredor.IsNullOrEmpty())
                    {
                        return Json(new { success = false, message = "No se encontró el ID del corredor." });
                    }

                    // 3. Consulta de Información del Corredor
                    string queryCorredor = @"
                SELECT c.nom_corredor, c.apP_corredor, c.apM_corredor, c.f_corredor, c.sex_corredor, c.pais, c.c_corredor
                FROM CORREDOR c
                WHERE c.ID_corredor = @IDCorredor;";

                    using (SqlCommand command = new SqlCommand(queryCorredor, connection))
                    {
                        command.Parameters.Add("@IDCorredor", SqlDbType.VarBinary).Value = idCorredor;

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                corredorInfo = $@"Nombre: {reader.GetString(0)} {reader.GetString(1)} {reader.GetString(2)}
Fecha de nacimiento: {reader.GetDateTime(3):yyyy-MM-dd}
Sexo del corredor: {reader.GetString(4)}
Correo del corredor: {(reader.IsDBNull(6) ? "N/A" : reader.GetString(6))}";
                            }
                            else
                            {
                                _logger.LogWarning("No se encontró información del corredor.");
                            }
                        }
                    }

                    // 4. Consultar todas las carreras en las que ha estado
                    List<string> carrerasTemp = new List<string>(); // Nueva lista temporal para almacenar la información de las carreras

                    string queryCarreras = @"
                SELECT ca.nom_carrera AS NombreCarrera, ca.year_carrera AS Año, cat.nombre_categoria AS Categoria, ca.edi_carrera AS Edicion, v.num_corredor AS NumCorredor
                FROM CARRERA ca
                JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                JOIN Vincula_participante v ON cc.ID_carr_cat = v.ID_carr_cat
                WHERE v.ID_corredor = @IDCorredor;";

                    using (SqlCommand command = new SqlCommand(queryCarreras, connection))
                    {
                        command.Parameters.Add("@IDCorredor", SqlDbType.VarBinary).Value = idCorredor;

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                // Obtener los datos de cada carrera
                                string nombreCarrera = reader["NombreCarrera"].ToString();
                                string añoCarrera = reader["Año"].ToString();
                                string categoriaCarrera = reader["Categoria"].ToString();
                                string edicionCarrera = reader["Edicion"].ToString();
                                string numCorredor = reader["NumCorredor"].ToString();

                                // Agregar la información de la carrera a la lista temporal
                                carrerasTemp.Add($@"Nombre de la carrera: {nombreCarrera}
Año: {añoCarrera}
Categoría: {categoriaCarrera}
Edición: {edicionCarrera}
Número de corredor: {numCorredor}");
                            }
                            // Verifica si se encontraron carreras
                            if (carrerasTemp.Count == 0)
                            {
                                _logger.LogWarning("No se encontraron carreras para el corredor.");
                            }
                        }
                    }

                    // 5. Consultar los datos y tiempos de corredor de cada carrera
                    // Luego, en la sección donde consultas los tiempos, puedes usar la lista temporal
                    List<string> resultadosCarreras = new List<string>(); // Nueva lista para almacenar los resultados de tiempos

                    foreach (var carrera in carrerasTemp)
                    {
                        // Extraer los valores necesarios de la cadena 'carrera'
                        string[] partes = carrera.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        string nombreCarrera = partes[0].Split(':')[1].Trim();
                        string añoCarrera = partes[1].Split(':')[1].Trim();
                        string categoriaCarrera = partes[2].Split(':')[1].Trim();
                        string edicionCarrera = partes[3].Split(':')[1].Trim();
                        string numCorredor = partes[4].Split(':')[1].Trim();

                        // Consulta para obtener los tiempos y posición del corredor en la carrera
                        string queryTiempos = @"
        WITH TiemposCorredor AS (
            SELECT 
                folio_chip,
                tiempo_registrado,
                ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
            FROM dbo.TIEMPO
        )
        SELECT 
            RANK() OVER (ORDER BY 
                DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
                DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
                DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00'))
            ) AS Posicion,
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
        JOIN Vincula_participante v ON cc.ID_carr_cat = v.ID_carr_cat
        LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
        LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
        LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
        WHERE 
            ca.nom_carrera = @NombreCarrera
            AND ca.year_carrera = @Año
            AND ca.edi_carrera = @Edicion
            AND cc.ID_categoria = (SELECT ID_categoria FROM CATEGORIA WHERE nombre_categoria = @Categoria)
            AND v.num_corredor = @NumCorredor;";
                        using (SqlCommand command = new SqlCommand(queryTiempos, connection))
                        {
                            command.Parameters.AddWithValue("@NombreCarrera", nombreCarrera);
                            command.Parameters.AddWithValue("@Año", añoCarrera);
                            command.Parameters.AddWithValue("@Edicion", edicionCarrera);
                            command.Parameters.AddWithValue("@Categoria", categoriaCarrera);
                            command.Parameters.AddWithValue("@NumCorredor", numCorredor);

                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    // Obtener los datos de la posición y tiempos
                                    string posicion = reader["Posicion"].ToString();
                                    string tiempo1 = reader["T1"].ToString();
                                    string tiempo2 = reader["T2"].ToString();
                                    string tiempo3 = reader["T3"].ToString();
                                    string tiempoTotal = reader["TiempoTotal"].ToString();

                                    // Agregar la información de la carrera y los tiempos al reporte
                                    resultadosCarreras.Add($@"Carrera: {nombreCarrera}
Año: {añoCarrera}
Edición: {edicionCarrera}
Categoría: {categoriaCarrera}
Número de corredor: {numCorredor}
Posición: {posicion}
Tiempo 1: {tiempo1}
Tiempo 2: {tiempo2}
Tiempo 3: {tiempo3}
Tiempo Total: {tiempoTotal}");
                                }
                            }
                        }
                    }

                    // Generar el contenido del archivo
                    string contenidoReporte = corredorInfo + "\n\n" + string.Join("\n\n", resultadosCarreras);
                    if (string.IsNullOrWhiteSpace(contenidoReporte))
                    {
                        _logger.LogWarning("El contenido del reporte está vacío.");
                    }

                    // Crear el archivo
                    await System.IO.File.WriteAllTextAsync(rutaArchivo, contenidoReporte);
                    _logger.LogInformation("El reporte se ha escrito en el archivo: " + rutaArchivo);

                    return Json(new { success = true, message = "El reporte se generó exitosamente en la ruta especificada." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el reporte.");
                return Json(new { success = false, message = "Ocurrió un error al generar el reporte." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reporte_Carrera()
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
                    CONCAT(ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')') AS Carrera
                FROM CARRERA ca
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

        [HttpGet]
        public async Task<IActionResult> ObtenerCategoriasPorCarrera_Reporte(int carreraId)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var categorias = new List<string>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                SELECT cat.nombre_categoria
                FROM CATEGORIA cat
                JOIN CARR_CAT cc ON cat.ID_categoria = cc.ID_categoria
                WHERE cc.ID_carrera = @carreraId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@carreraId", carreraId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                categorias.Add(reader["nombre_categoria"].ToString());
                            }
                        }
                    }
                }

                return Json(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al cargar categorías: {ex.Message}");
                return Json(new { error = "Ocurrió un error al cargar las categorías." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerarReporteCarrera(int carreraId, string rutaArchivo)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var reporteData = new List<string>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Obtener las categorías asociadas a la carrera
                    var categorias = new List<string>();
                    string categoriasQuery = @"
                SELECT cat.nombre_categoria
                FROM CARR_Cat cc
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE cc.ID_carrera = @carreraId";

                    using (SqlCommand categoriasCmd = new SqlCommand(categoriasQuery, connection))
                    {
                        categoriasCmd.Parameters.AddWithValue("@carreraId", carreraId);
                        using (SqlDataReader reader = await categoriasCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                categorias.Add(reader["nombre_categoria"].ToString());
                            }
                        }
                    }

                    // 2. Iterar sobre cada categoría para construir el reporte
                    foreach (var categoria in categorias)
                    {
                        reporteData.Add($"Categoría: {categoria}");
                        reporteData.Add(new string('-', 50));

                        string corredoresQuery = @"
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
                        WHERE ca.ID_carrera = @carreraId
                          AND cat.nombre_categoria = @categoria
                    )
                    SELECT Posicion, NumCorredor, Nombre, ApellidoPaterno, ApellidoMaterno, T1, T2, T3, TiempoTotal
                    FROM Posiciones
                    ORDER BY Posicion";

                        using (SqlCommand corredoresCmd = new SqlCommand(corredoresQuery, connection))
                        {
                            corredoresCmd.Parameters.AddWithValue("@carreraId", carreraId);
                            corredoresCmd.Parameters.AddWithValue("@categoria", categoria);

                            using (SqlDataReader reader = await corredoresCmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string line = $"Posición: {reader["Posicion"]}, Número: {reader["NumCorredor"]}, " +
                                                  $"Nombre: {reader["Nombre"]} {reader["ApellidoPaterno"]} {reader["ApellidoMaterno"]}, " +
                                                  $"T1: {reader["T1"]}, T2: {reader["T2"]}, T3: {reader["T3"]}, Tiempo Total: {reader["TiempoTotal"]}";
                                    reporteData.Add(line);
                                }
                            }
                        }

                        reporteData.Add("\n");
                    }
                }

                // 3. Escribir el archivo
                await System.IO.File.WriteAllLinesAsync(rutaArchivo, reporteData);

                return Json(new { success = true, message = "Reporte generado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al generar el reporte: {ex.Message}");
                return Json(new { success = false, message = "Error al generar el reporte." });
            }
        }

    }


}