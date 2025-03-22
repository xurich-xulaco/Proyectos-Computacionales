using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
    public partial class HomeController : Controller
    {
        private readonly CronometrajeContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor; //agregado

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, CronometrajeContext context, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _httpContextAccessor = httpContextAccessor; //agregado
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
                            _httpContextAccessor.HttpContext.Session.SetString("Usuario", usuario); //Agregado
                            return RedirectToAction("Pantalla_ini"); // Redirigir a la pantalla principal
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

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            HttpContext.Response.Cookies.Delete(".AspNetCore.Session");

            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Pantalla_ini()
        {
            // agregado el if
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Usuario")))
            {
                return RedirectToAction("Index"); // Si la sesión no existe, redirigir al login
            }

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
                    string checkSql = "SELECT COUNT(*) FROM ADMINISTRADOR WHERE uss_admin = @SuperUsuario AND pass_admin = @SuperContrasena AND ID_admin = 1";
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
                    string checkSql = "SELECT COUNT(*) FROM ADMINISTRADOR WHERE uss_admin = @SuperUsuario AND pass_admin = @SuperContrasena AND ID_admin = 1";
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
                        DATEDIFF(SECOND, 0, COALESCE(T4.tiempo_registrado, '00:00:00'))
                    ) AS Posicion,
                    v.num_corredor AS NumCorredor, 
                    c.nom_corredor AS Nombre,
                    c.apP_corredor AS ApellidoPaterno,
                    c.apM_corredor AS ApellidoMaterno,
                    COALESCE(T1.tiempo_registrado, '00:00:00') AS T1,
                    COALESCE(T2.tiempo_registrado, '00:00:00') AS T2,
                    COALESCE(T3.tiempo_registrado, '00:00:00') AS T3,
                    COALESCE(T4.tiempo_registrado, '00:00:00') AS T4
                FROM CARRERA ca
                JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                JOIN Vincula_participante v ON cc.ID_carr_cat = v.ID_Carr_cat
                JOIN dbo.CORREDOR c ON v.ID_corredor = c.ID_corredor
                LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
                LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
                LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
                LEFT JOIN TiemposCorredor T4 ON v.folio_chip = T4.folio_chip AND T4.NumTiempo = 4
                WHERE ca.year_carrera = @YearCarrera
                  AND ca.edi_carrera = @EdiCarrera
                  AND cat.nombre_categoria = @Categoria
            )
            SELECT Posicion, NumCorredor, Nombre, T1, T2, T3, T4
            FROM Posiciones
            WHERE @NombreCorredor IS NULL 
               OR CONCAT(Nombre, ' ', ApellidoPaterno, ' ', ApellidoMaterno) LIKE @NombreCorredor
            ORDER BY 
                DATEDIFF(SECOND, 0, COALESCE(T4, '00:00:00'));";

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
                            { "T4", reader.IsDBNull(6) ? "N/A" : FormatearTiempo(reader.GetTimeSpan(6)) }
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
            SELECT nombre_categoria
            FROM (
                SELECT DISTINCT cat.nombre_categoria,
                    CAST(LEFT(cat.nombre_categoria, CHARINDEX(' ', cat.nombre_categoria) - 1) AS int) AS km
                FROM CATEGORIA cat
                INNER JOIN CARR_Cat cc ON cat.ID_categoria = cc.ID_categoria
                INNER JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                WHERE ca.year_carrera = @YearCarrera AND ca.edi_carrera = @EdiCarrera
            ) AS T
            ORDER BY km";

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

                    // Preparar el valor del correo: si es nulo, vacío o no contiene '@', se usa DBNull.
                    object correoParametro = string.IsNullOrWhiteSpace(Correo) || !Correo.Contains("@")
                        ? (object)DBNull.Value
                        : Correo;

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
                            insertCommand.Parameters.AddWithValue("@Correo", correoParametro);
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

                    // VALIDACIÓN: Evitar que el corredor se inscriba en más de una categoría en la misma carrera.
                    string checkMultiCatSql = @"
                SELECT COUNT(*) 
                FROM Vincula_participante vp
                INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                WHERE vp.ID_corredor = @IDCorredor AND cc.ID_carrera = @CarreraId";

                    using (SqlCommand checkMultiCatCommand = new SqlCommand(checkMultiCatSql, connection))
                    {
                        checkMultiCatCommand.Parameters.AddWithValue("@IDCorredor", corredorId);
                        checkMultiCatCommand.Parameters.AddWithValue("@CarreraId", CarreraId);
                        if ((int)await checkMultiCatCommand.ExecuteScalarAsync() > 0)
                        {
                            return Json(new { success = false, message = "El corredor ya está asociado a una categoría de esta carrera." });
                        }
                    }

                    // Verificar si el corredor ya está vinculado a esta Carrera-Categoría (para evitar duplicados exactos)
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
                            return Json(new { success = false, message = "El corredor ya está asociado a esta categoría de la carrera." });
                        }
                    }

                    // INSERTAR EL VÍNCULO:
                    // Se calcula el num_corredor y el folio_chip basado en la carrera, de modo que sean únicos por carrera.
                    string insertVinculoSql = @"
                INSERT INTO Vincula_participante (ID_corredor, ID_carr_cat, num_corredor, folio_chip) 
                VALUES (
                    @IDCorredor, 
                    @IDCarrCat,
                    (
                        SELECT ISNULL(MAX(vp.num_corredor), 0) + 1 
                        FROM Vincula_participante vp
                        INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                        WHERE cc.ID_carrera = @CarreraId
                    ),
                    'RFID' + CAST(
                        1000000000 + (
                            SELECT ISNULL(MAX(vp.num_corredor), 0) + 1 
                            FROM Vincula_participante vp
                            INNER JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                            WHERE cc.ID_carrera = @CarreraId
                        ) AS VARCHAR
                    )
                )";

                    using (SqlCommand vinculoCommand = new SqlCommand(insertVinculoSql, connection))
                    {
                        vinculoCommand.Parameters.AddWithValue("@IDCorredor", corredorId);
                        vinculoCommand.Parameters.AddWithValue("@IDCarrCat", idCarrCat.Value);
                        vinculoCommand.Parameters.AddWithValue("@CarreraId", CarreraId);

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
            try
            {
                // Reutilizamos la función que obtiene las categorías
                var categorias = await ObtenerCategoriasPorCarrera(carreraId);

                // Ordenamos las categorías de menor a mayor basado en el valor numérico extraído
                var categoriasOrdenadas = categorias.OrderBy(c =>
                {
                    // Se asume el formato "X km". Se divide la cadena para extraer el número.
                    var parts = c.Split(' ');
                    int km = (parts.Length > 0 && int.TryParse(parts[0], out int valor)) ? valor : 0;
                    return km;
                }).ToList();

                return Json(categoriasOrdenadas);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener categorías para corredor: {ex.Message}");
                return Json(new { error = "Ocurrió un error al cargar las categorías." });
            }
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
            // Validar que se hayan seleccionado exactamente tres categorías
            if (categoriasSeleccionadas == null || categoriasSeleccionadas.Count != 3)
            {
                return Json(new { success = false, message = "Debes seleccionar exactamente tres categorías diferentes." });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            int idCarrera;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Insertar la carrera y obtener el ID generado.
                    string insertCarreraSql = @"
                INSERT INTO CARRERA (nom_carrera, year_carrera, edi_carrera)
                OUTPUT INSERTED.ID_carrera
                VALUES (
                    @NombreCarrera, 
                    @YearCarrera,
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

                    /* 
                       Para ordenar las categorías de menor a mayor kilómetro, 
                       se realiza una consulta para obtener el nombre de cada categoría.
                       Se asume que el campo "nombre_categoria" contiene valores como "1 km", "10 km", "50 km".
                    */
                    string queryCategorias = $@"
                SELECT ID_categoria, nombre_categoria 
                FROM CATEGORIA 
                WHERE ID_categoria IN ({string.Join(",", categoriasSeleccionadas)})";

                    // Lista para almacenar (ID_categoria, kilometraje)
                    List<(int id, int km)> categorias = new List<(int, int)>();

                    using (SqlCommand command = new SqlCommand(queryCategorias, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int idCategoria = reader.GetInt32(0);
                                string nombreCategoria = reader.GetString(1);

                                // Extraer el número (kilometraje) del string. Se quita "km" y se hace trim.
                                if (int.TryParse(nombreCategoria.Replace(" km", "").Replace("km", "").Trim(), out int km))
                                {
                                    categorias.Add((idCategoria, km));
                                }
                                else
                                {
                                    _logger.LogWarning($"No se pudo extraer el kilometraje de la categoría '{nombreCategoria}' (ID: {idCategoria}).");
                                }
                            }
                        }
                    }

                    // Ordenar las categorías por el valor numérico (kilometraje) de menor a mayor.
                    var categoriasOrdenadas = categorias.OrderBy(c => c.km).ToList();

                    // Insertar cada relación en la tabla CARR_Cat.
                    string insertCategoriasSql = "INSERT INTO CARR_Cat (ID_carrera, ID_categoria) VALUES (@IDCarrera, @IDCategoria)";
                    foreach (var categoria in categoriasOrdenadas)
                    {
                        using (SqlCommand command = new SqlCommand(insertCategoriasSql, connection))
                        {
                            command.Parameters.AddWithValue("@IDCarrera", idCarrera);
                            command.Parameters.AddWithValue("@IDCategoria", categoria.id);
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
        public async Task<IActionResult> Modificar_Corredor(int year, int edition, string category, int numCorredor)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var corredor = new Dictionary<string, string>();

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
              AND ca.edi_carrera = @Edition 
              AND cat.nombre_categoria = @Category 
              AND vp.num_corredor = @NumCorredor";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Edition", edition);
                        command.Parameters.AddWithValue("@Category", category);
                        command.Parameters.AddWithValue("@NumCorredor", numCorredor);

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
                                corredor["ID_corredor"] = reader["ID_corredor"].ToString();
                            }
                        }
                    }
                }

                ViewBag.Corredor = corredor;
                ViewBag.Year = year;
                ViewBag.Edition = edition;
                ViewBag.Category = category;
                ViewBag.NumCorredor = numCorredor;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al cargar los datos del corredor: {Message}", ex.Message);
                return RedirectToAction("Pantalla_ini");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarCorredor(
            int ID_corredor, string nombre, string apellidoPaterno, string apellidoMaterno,
            string fechaNacimiento, string sexo, string pais, string correo)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

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
            WHERE ID_corredor = @ID_corredor";

                    using (SqlCommand command = new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ID_corredor", ID_corredor);
                        command.Parameters.AddWithValue("@Nombre", nombre);
                        command.Parameters.AddWithValue("@ApellidoPaterno", apellidoPaterno);
                        command.Parameters.AddWithValue("@ApellidoMaterno", apellidoMaterno);
                        command.Parameters.AddWithValue("@FechaNacimiento", DateTime.Parse(fechaNacimiento));
                        command.Parameters.AddWithValue("@Sexo", sexo);
                        command.Parameters.AddWithValue("@Pais", pais);
                        command.Parameters.AddWithValue("@Correo", correo);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return RedirectToAction("Pantalla_ini");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar corredor: {ex.Message}");
                return View("Error");
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


        private class CorredorDeArchivo : Corredor
        {
            public string Telefono { get; set; }

            public CorredorDeArchivo(
                string nom_corredor,
                string apP_corredor,
                string? apM_corredor,
                DateTime f_corredor,
                char sex_corredor,
                string? correo,
                string pais,
                string? telefono)
            {
                Nom_corredor = nom_corredor;
                this.apP_corredor = apP_corredor;
                this.apM_corredor = apM_corredor ?? string.Empty;
                this.f_corredor = f_corredor;
                this.sex_corredor = sex_corredor;
                c_corredor = correo ?? string.Empty;
                pais_corredor = pais;
                Telefono = telefono ?? string.Empty;
            }
        }
        [HttpPost]
        public async Task<IActionResult> Subir_ArchivoCorredor(string rutaArchivo, int carreraId, string categoriaNombre)
        {
            if (string.IsNullOrEmpty(rutaArchivo) || !System.IO.File.Exists(rutaArchivo))
            {
                _logger.LogError("La ruta del archivo no es válida o no existe.");
                return Json(new { success = false, message = "La ruta del archivo no es válida." });
            }

            Queue<CorredorDeArchivo> colaCorredores = new Queue<CorredorDeArchivo>();
            int filasProcesadas = 0;
            int errores = 0;

            // Primer bloque: Abrir y leer el archivo para encolar la información
            try
            {
                _logger.LogInformation($"Procesando archivo en la ruta: {rutaArchivo}");
                using (var workbook = new XLWorkbook(rutaArchivo))
                {
                    var worksheet = workbook.Worksheet(1);
                    var filas = worksheet.RowsUsed();

                    foreach (var fila in filas.Skip(1)) // Saltar encabezados
                    {
                        if (fila.Cells().All(cell => cell.IsEmpty()))
                        {
                            _logger.LogWarning($"Fila {fila.RowNumber()} está vacía. Saltando.");
                            continue;
                        }

                        // Extracción de datos
                        var nom_corredor = fila.Cell(1).GetString();
                        var apellido_paterno = fila.Cell(2).GetString();
                        var apellido_materno = fila.Cell(3).GetString();
                        var fecha_cumpleanios_celda = fila.Cell(4);
                        var sexo_corredor = fila.Cell(5).GetString().Trim();
                        var correo_corredor = fila.Cell(6).GetString();
                        var pais = fila.Cell(7).GetString();
                        var telefono = fila.Cell(8).GetString();

                        if (string.IsNullOrEmpty(nom_corredor) || string.IsNullOrEmpty(apellido_paterno) || string.IsNullOrEmpty(pais))
                        {
                            _logger.LogWarning($"Error en fila {fila.RowNumber()}: Los campos de nombre, apellido paterno y país son obligatorios.");
                            errores++;
                            continue;
                        }

                        // Validar el sexo
                        if (string.IsNullOrEmpty(sexo_corredor) || (sexo_corredor != "M" && sexo_corredor != "F"))
                        {
                            _logger.LogWarning($"Error en fila {fila.RowNumber()}: Sexo '{sexo_corredor}' no válido. Debe ser 'M' o 'F'.");
                            errores++;
                            continue;
                        }

                        DateTime fechaCumpleanios;
                        if (fecha_cumpleanios_celda.TryGetValue(out fechaCumpleanios))
                        {
                            if (fechaCumpleanios < new DateTime(1753, 1, 1) || fechaCumpleanios > new DateTime(9999, 12, 31))
                            {
                                _logger.LogWarning($"Error en fila {fila.RowNumber()}: Fecha de nacimiento '{fechaCumpleanios}' fuera del rango permitido (1753-9999).");
                                errores++;
                                continue;
                            }
                        }
                        else
                        {
                            var fechaTexto = fecha_cumpleanios_celda.GetString().Trim();
                            if (!DateTime.TryParseExact(fechaTexto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaCumpleanios))
                            {
                                _logger.LogWarning($"Error en fila {fila.RowNumber()}: Fecha de nacimiento '{fechaTexto}' no válida. Use el formato ISO 8601 (YYYY-MM-DD).");
                                errores++;
                                continue;
                            }
                            if (fechaCumpleanios < new DateTime(1753, 1, 1) || fechaCumpleanios > new DateTime(9999, 12, 31))
                            {
                                _logger.LogWarning($"Error en fila {fila.RowNumber()}: Fecha de nacimiento '{fechaCumpleanios}' fuera del rango permitido (1753-9999).");
                                errores++;
                                continue;
                            }
                        }

                        _logger.LogInformation($"Encolando fila {fila.RowNumber()}: {nom_corredor} {apellido_paterno}.");

                        // Crear el objeto CorredorDeArchivo y encolarlo
                        var corredorArchivo = new CorredorDeArchivo(
                            nom_corredor,
                            apellido_paterno,
                            apellido_materno,
                            fechaCumpleanios,
                            sexo_corredor[0],
                            correo_corredor,
                            pais,
                            telefono
                        );
                        colaCorredores.Enqueue(corredorArchivo);
                    }
                }
            }
            catch (IOException ex_io)
            {
                // Verificar si el error es por archivo en uso por otro proceso
                if (ex_io.Message.Contains("being used by another process"))
                {
                    _logger.LogError(ex_io, $"Error al abrir y leer el archivo: {ex_io.Message}");
                    return Json(new { success = false, message = "Otro proceso está usando este archivo,\nfavor de cerrarlo." });
                }
                else
                {
                    _logger.LogError(ex_io, $"Error desconocido al abrir y/o leer el archivo: {ex_io.Message}");
                    return Json(new { success = false, message = $"Error al procesar el archivo: {ex_io.Message}" });
                }
            }
            catch (Exception ex_excel)
            {
                _logger.LogError(ex_excel, $"Error al abrir y leer el archivo: {ex_excel.Message}");
                return Json(new { success = false, message = $"Error al procesar el archivo: {ex_excel.Message}" });
            }

            // Segundo bloque: Vaciar la cola e insertar cada corredor usando Crear_Corredor
            try
            {
                while (colaCorredores.Count > 0)
                {
                    var corredor = colaCorredores.Dequeue();
                    try
                    {
                        _logger.LogInformation($"Procesando corredor: {corredor.Nom_corredor} {corredor.apP_corredor}.");
                        await Crear_Corredor(
                            Nombre: corredor.Nom_corredor,
                            Apaterno: corredor.apP_corredor,
                            Amaterno: corredor.apM_corredor,
                            Fnacimiento: corredor.f_corredor,
                            Sexo: corredor.sex_corredor.ToString(),
                            Correo: corredor.c_corredor,
                            Pais: corredor.pais_corredor,
                            Telefono: corredor.Telefono,
                            CarreraId: carreraId,
                            CategoriaNombre: categoriaNombre
                        );
                        filasProcesadas++;
                    }
                    catch (Exception ex_crear)
                    {
                        _logger.LogWarning(ex_crear, $"Error al procesar el corredor {corredor.Nom_corredor} {corredor.apP_corredor}: {ex_crear.Message}");
                        errores++;
                        continue;
                    }
                }
            }
            catch (Exception ex_queue)
            {
                _logger.LogError(ex_queue, "Error al procesar la queue de corredores.");
                return Json(new { success = false, message = "Error al procesar los corredores en la lista." });
            }

            // Construir el mensaje final basado en la cantidad de errores y éxitos
            string mensajeFinal;
            if (errores > 0)
            {
                mensajeFinal = $"Se procesaron {filasProcesadas} corredores, pero se presentaron errores en {errores} número de filas.";
                _logger.LogWarning(mensajeFinal);
            }
            else
            {
                mensajeFinal = $"{filasProcesadas} corredores procesados exitosamente.";
            }

            return Json(new { success = true, message = mensajeFinal, errorCount = errores });
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

        [HttpGet]
        public async Task<IActionResult> Importar_tiempos()
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
        public async Task<IActionResult> Subir_ArchivoTiempos(string rutaArchivo, int carreraId)
        {
            if (!string.IsNullOrEmpty(rutaArchivo) && System.IO.File.Exists(rutaArchivo))
            {
                try
                {
                    _logger.LogInformation($"Procesando archivo de tiempos en la ruta: {rutaArchivo}");
                    int filasProcesadas = 0;
                    int errores = 0;

                    string connectionString = _configuration.GetConnectionString("DefaultConnection");

                    using (var workbook = new XLWorkbook(rutaArchivo))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var filas = worksheet.RowsUsed();

                        // Validar que hay filas en el archivo
                        if (!filas.Any())
                        {
                            _logger.LogWarning("El archivo no contiene datos.");
                            return Json(new { success = false, message = "El archivo no contiene datos." });
                        }

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            SqlTransaction transaction = connection.BeginTransaction();

                            try
                            {
                                foreach (var fila in filas.Skip(1)) // Saltar encabezados
                                {
                                    try
                                    {
                                        if (fila.Cells().All(cell => cell.IsEmpty()))
                                        {
                                            _logger.LogWarning($"Fila {fila.RowNumber()} está vacía. Saltando.");
                                            continue;
                                        }

                                        // Leer el folio del chip
                                        var folioChip = fila.Cell(1).GetString().Trim();

                                        if (string.IsNullOrEmpty(folioChip))
                                        {
                                            _logger.LogWarning($"Fila {fila.RowNumber()}: El folio del chip está vacío. Saltando.");
                                            errores++;
                                            continue;
                                        }

                                        // Leer hasta 4 tiempos de las columnas correspondientes
                                        var tiempos = new List<TimeSpan>();
                                        for (int i = 2; i <= 5; i++) // Columnas 2 a 5
                                        {
                                            var tiempoCelda = fila.Cell(i).GetString().Trim();

                                            if (!string.IsNullOrEmpty(tiempoCelda) && TimeSpan.TryParse(tiempoCelda, out TimeSpan tiempo))
                                            {
                                                tiempos.Add(tiempo);
                                            }
                                        }

                                        // Si faltan tiempos, agregar un tiempo final de 5 horas
                                        while (tiempos.Count < 4)
                                        {
                                            tiempos.Add(TimeSpan.FromHours(5));
                                        }

                                        // Insertar todos los tiempos en una sola operación
                                        string insertTiempoSql = @"
                                    INSERT INTO Tiempo (folio_chip, tiempo_registrado)
                                    VALUES (@FolioChip, @TiempoRegistrado)";

                                        foreach (var tiempoRegistrado in tiempos)
                                        {
                                            using (SqlCommand insertTiempoCommand = new SqlCommand(insertTiempoSql, connection, transaction))
                                            {
                                                insertTiempoCommand.Parameters.AddWithValue("@FolioChip", folioChip);
                                                insertTiempoCommand.Parameters.AddWithValue("@TiempoRegistrado", tiempoRegistrado);
                                                await insertTiempoCommand.ExecuteNonQueryAsync();
                                            }
                                        }

                                        filasProcesadas++;
                                    }
                                    catch (Exception filaEx)
                                    {
                                        _logger.LogError(filaEx, $"Error procesando fila {fila.RowNumber()}: {filaEx.Message}");
                                        errores++;
                                    }
                                }

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                _logger.LogError(ex, "Error en la transacción de inserción. Se realizó un rollback.");
                                throw;
                            }
                        }
                    }

                    var message = errores == 0
                        ? $"{filasProcesadas} tiempos registrados exitosamente."
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
                    CONCAT(
                        ca.nom_carrera, ' - ', ca.year_carrera, ' (Edición: ', ca.edi_carrera, ')',
                        '(', 
                        STRING_AGG(
                            cat.nombre_categoria, ', '
                        ) WITHIN GROUP (
                            ORDER BY CAST(LEFT(cat.nombre_categoria, CHARINDEX(' ' , cat.nombre_categoria) - 1) AS int)
                        ), 
                        ')'
                    ) AS Carrera
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

        [HttpGet]
        public async Task<IActionResult> ObtenerCategoriasPorCarrera_Reporte(int carreraId)
        {
            try
            {
                // Reutilizamos la función ya existente para obtener la lista base de categorías
                var categorias = await ObtenerCategoriasPorCarrera(carreraId);

                if (categorias.Count > 0)
                {
                    // Convertir cada categoría a un objeto anónimo que incluye el valor numérico extraído in-line
                    var categoriasKm = categorias.Select(c =>
                    {
                        // Se asume el formato "X km", se divide la cadena y se intenta convertir la primera parte a entero
                        var parts = c.Split(' ');
                        int km = (parts.Length > 0 && int.TryParse(parts[0], out int valor)) ? valor : 0;
                        return new { Nombre = c, Km = km };
                    }).ToList();

                    // Descartar la(s) categoría(s) con el valor mínimo
                    int minKm = categoriasKm.Min(x => x.Km);
                    var filtradas = categoriasKm.Where(x => x.Km != minKm).ToList();

                    // Ordenar de forma descendente por el valor numérico
                    var ordenadasDesc = filtradas.OrderByDescending(x => x.Km).ToList();

                    // Si hay más de dos categorías, tomamos únicamente las dos de mayor valor
                    if (ordenadasDesc.Count > 2)
                    {
                        ordenadasDesc = ordenadasDesc.Take(2).ToList();
                    }
                    categorias = ordenadasDesc.Select(x => x.Nombre).ToList();
                }

                return Json(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener categorías para reporte: {ex.Message}");
                return Json(new { error = "Ocurrió un error al cargar las categorías." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarEliminarCorredor([FromBody] BajaCorredorRequest request)
        {
            if (request.YearCarrera <= 0 || request.EdiCarrera <= 0 || string.IsNullOrEmpty(request.Categoria) || request.NumCorredor <= 0)
            {
                return Json(new { success = false, message = "Parámetros inválidos." });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // **Verificar si el corredor tiene tiempos registrados**
                    string verificarTiemposQuery = @"
                SELECT COUNT(*) 
                FROM TIEMPO t
                JOIN Vincula_participante vp ON t.folio_chip = vp.folio_chip
                JOIN CARR_Cat cc ON vp.ID_Carr_cat = cc.ID_carr_cat
                JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE ca.year_carrera = @YearCarrera
                  AND ca.edi_carrera = @EdiCarrera
                  AND cat.nombre_categoria = @Categoria
                  AND vp.num_corredor = @NumCorredor";

                    using (SqlCommand verificarCommand = new SqlCommand(verificarTiemposQuery, connection))
                    {
                        verificarCommand.Parameters.AddWithValue("@YearCarrera", request.YearCarrera);
                        verificarCommand.Parameters.AddWithValue("@EdiCarrera", request.EdiCarrera);
                        verificarCommand.Parameters.AddWithValue("@Categoria", request.Categoria);
                        verificarCommand.Parameters.AddWithValue("@NumCorredor", request.NumCorredor);

                        int tiemposRegistrados = (int)await verificarCommand.ExecuteScalarAsync();
                        if (tiemposRegistrados > 0)
                        {
                            return Json(new { success = false, message = "No se puede eliminar el corredor porque tiene tiempos registrados." });
                        }
                    }

                    // **Eliminar el corredor**
                    string eliminarQuery = @"
                DELETE FROM Vincula_participante
                WHERE ID_carr_cat IN (
                    SELECT cc.ID_carr_cat
                    FROM CARR_Cat cc
                    JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                    WHERE ca.year_carrera = @YearCarrera
                      AND ca.edi_carrera = @EdiCarrera
                      AND cat.nombre_categoria = @Categoria
                )
                AND num_corredor = @NumCorredor";

                    using (SqlCommand eliminarCommand = new SqlCommand(eliminarQuery, connection))
                    {
                        eliminarCommand.Parameters.AddWithValue("@YearCarrera", request.YearCarrera);
                        eliminarCommand.Parameters.AddWithValue("@EdiCarrera", request.EdiCarrera);
                        eliminarCommand.Parameters.AddWithValue("@Categoria", request.Categoria);
                        eliminarCommand.Parameters.AddWithValue("@NumCorredor", request.NumCorredor);

                        int filasAfectadas = await eliminarCommand.ExecuteNonQueryAsync();
                        if (filasAfectadas == 0)
                        {
                            return Json(new { success = false, message = "No se encontró al corredor." });
                        }
                    }
                }

                return Json(new { success = true, message = "Corredor eliminado correctamente." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar el corredor: {ex.Message}");
                return Json(new { success = false, message = "Error al eliminar el corredor." });
            }
        }

        public class BajaCorredorRequest
        {
            public int YearCarrera { get; set; }
            public int EdiCarrera { get; set; }
            public string Categoria { get; set; }
            public int NumCorredor { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> Eliminar_Corredor(int yearCarrera, int ediCarrera, string categoria, int numCorredor)
        {
            if (yearCarrera <= 0 || ediCarrera <= 0 || string.IsNullOrEmpty(categoria) || numCorredor <= 0)
            {
                return RedirectToAction("Pantalla_ini"); // Redirigir si los parámetros no son válidos
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            Dictionary<string, string> corredor = new Dictionary<string, string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT c.nom_corredor, c.apP_corredor, c.apM_corredor, c.f_corredor, 
                       c.sex_corredor, c.pais, c.c_corredor
                FROM CORREDOR c
                JOIN Vincula_participante vp ON c.ID_corredor = vp.ID_corredor
                JOIN CARR_Cat cc ON vp.ID_carr_cat = cc.ID_carr_cat
                JOIN CARRERA ca ON cc.ID_carrera = ca.ID_carrera
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE ca.year_carrera = @YearCarrera 
                  AND ca.edi_carrera = @EdiCarrera 
                  AND cat.nombre_categoria = @Categoria 
                  AND vp.num_corredor = @NumCorredor";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@YearCarrera", yearCarrera);
                        command.Parameters.AddWithValue("@EdiCarrera", ediCarrera);
                        command.Parameters.AddWithValue("@Categoria", categoria);
                        command.Parameters.AddWithValue("@NumCorredor", numCorredor);

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
                            }
                            else
                            {
                                return RedirectToAction("Pantalla_ini"); // Redirigir si no encuentra al corredor
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al buscar corredor: {ex.Message}");
                return RedirectToAction("Pantalla_ini");
            }

            ViewBag.YearCarrera = yearCarrera;
            ViewBag.Edicion = ediCarrera;
            ViewBag.Categoria = categoria;
            ViewBag.NumCorredor = numCorredor;
            ViewBag.Corredor = corredor;

            return View();
        }

    }
}