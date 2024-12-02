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
        public async Task<IActionResult> BuscarCorredores(int? yearCarrera, int? ediCarrera, string categoria)
        {
            if (!yearCarrera.HasValue || !ediCarrera.HasValue || string.IsNullOrEmpty(categoria))
            {
                ViewBag.Error = "Por favor selecciona el año, edición y categoría.";
                await InicializarAnios();
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
            )
            SELECT 
                RANK() OVER (ORDER BY 
                    DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
                    DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
                    DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00'))
                ) AS Posicion,
                v.num_corredor AS NumCorredor, 
                c.nom_corredor AS Nombre,
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
            ORDER BY Posicion;";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@YearCarrera", yearCarrera.Value);
                        command.Parameters.AddWithValue("@EdiCarrera", ediCarrera.Value);
                        command.Parameters.AddWithValue("@Categoria", categoria);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fila = new Dictionary<string, object>
                                {
                                    { "Posicion", reader.GetInt64(0) }, // Leer como Int64 si la base de datos usa BIGINT
                                    { "NumCorredor", reader.GetInt32(1) },
                                    { "NombreCorredor", reader.GetString(2) },
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

                // Guardar las selecciones para mantener los valores en el formulario
                ViewBag.SelectedYear = yearCarrera;
                ViewBag.SelectedEdition = ediCarrera;
                ViewBag.SelectedCategory = categoria;

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

        [HttpPost]
        public async Task<IActionResult> BuscarYAgregarCorredor(string nombreCorredor)
        {
            if (string.IsNullOrEmpty(nombreCorredor))
            {
                ViewBag.Error = "Por favor, proporciona un nombre o parte del nombre del corredor.";
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
                SELECT 
                    c.num_corredor AS NumCorredor,
                    c.nom_corredor AS Nombre,
                    c.apP_corredor AS ApellidoPaterno,
                    c.apM_corredor AS ApellidoMaterno
                FROM CORREDOR c
                WHERE c.nom_corredor LIKE @NombreCorredor
                   OR c.apP_corredor LIKE @NombreCorredor
                   OR c.apM_corredor LIKE @NombreCorredor
                ORDER BY c.num_corredor;";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NombreCorredor", $"%{nombreCorredor}%");

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var fila = new Dictionary<string, object>
                        {
                            { "NumCorredor", reader.GetInt32(0) },
                            { "Nombre", reader.GetString(1) },
                            { "ApellidoPaterno", reader.IsDBNull(2) ? "" : reader.GetString(2) },
                            { "ApellidoMaterno", reader.IsDBNull(3) ? "" : reader.GetString(3) }
                        };
                                resultados.Add(fila);
                            }
                        }
                    }
                }

                ViewBag.ResultadosBusqueda = resultados;
                ViewBag.NombreBuscado = nombreCorredor; // Para mantener el valor ingresado en la vista
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

        public async Task<IActionResult> Alta_Corredor(string Nombre, string Apaterno, string Amaterno, DateOnly Fnacimiento, string pais, string sexo, string telefono, string correo)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Verificar si el corredor ya existe en la base de datos
                    string checkSql = "SELECT COUNT(*) FROM Corredor WHERE Nombre = @Nombre AND Apaterno = @Apaterno AND Amaterno = @Amaterno AND Fnacimiento = @Fnacimiento";
                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Nombre", Nombre);
                        checkCommand.Parameters.AddWithValue("@Apaterno", Apaterno);
                        checkCommand.Parameters.AddWithValue("@Amaterno", Amaterno);
                        checkCommand.Parameters.AddWithValue("@Fnacimiento", Fnacimiento);

                        int count = (int)await checkCommand.ExecuteScalarAsync();
                        if (count > 0)
                        {
                            // Corredor ya existe
                            _logger.LogWarning($"Corredor {Nombre} {Apaterno} ya existente");
                            ModelState.AddModelError(string.Empty, "El corredor ya est� registrado.");
                            return View();
                        }
                    }

                    // Insertar el nuevo corredor
                    string sql = "INSERT INTO Corredor (Nombre, Apaterno, Amaterno, Fnacimiento, Pais, Sexo, Telefono, Correo) VALUES (@Nombre, @Apaterno, @Amaterno, @Fnacimiento, @Pais, @Sexo, @Telefono, @Correo)";
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", Nombre);
                        command.Parameters.AddWithValue("@Apaterno", Apaterno);
                        command.Parameters.AddWithValue("@Amaterno", Amaterno);
                        command.Parameters.AddWithValue("@Fnacimiento", Fnacimiento);
                        command.Parameters.AddWithValue("@Pais", pais);
                        command.Parameters.AddWithValue("@Sexo", sexo);
                        command.Parameters.AddWithValue("@Telefono", telefono);
                        command.Parameters.AddWithValue("@Correo", correo);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Registro exitoso
                _logger.LogInformation($"Nuevo corredor {Nombre} {Apaterno} registrado exitosamente.");
                TempData["SuccessMessage"] = "Corredor registrado exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error durante el registro del corredor: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al registrar el corredor. Por favor, int�ntelo de nuevo m�s tarde.");
                return View();
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



        public async Task<IActionResult> Editar_Corredor(int corredorId, string Nombre, string Apaterno, string Amaterno, DateOnly Fnacimiento, string pais, string sexo, string telefono, string correo)
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

                    // Actualizar los datos del corredor
                    string updateSql = "UPDATE Corredor SET Nombre = @Nombre, Apaterno = @Apaterno, Amaterno = @Amaterno, Fnacimiento = @Fnacimiento, Pais = @Pais, Sexo = @Sexo, Telefono = @Telefono, Correo = @Correo WHERE CorredorId = @CorredorId";
                    using (SqlCommand updateCommand = new SqlCommand(updateSql, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@CorredorId", corredorId);
                        updateCommand.Parameters.AddWithValue("@Nombre", Nombre);
                        updateCommand.Parameters.AddWithValue("@Apaterno", Apaterno);
                        updateCommand.Parameters.AddWithValue("@Amaterno", Amaterno);
                        updateCommand.Parameters.AddWithValue("@Fnacimiento", Fnacimiento);
                        updateCommand.Parameters.AddWithValue("@Pais", pais);
                        updateCommand.Parameters.AddWithValue("@Sexo", sexo);
                        updateCommand.Parameters.AddWithValue("@Telefono", telefono);
                        updateCommand.Parameters.AddWithValue("@Correo", correo);

                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }

                // Edici�n exitosa
                _logger.LogInformation($"Corredor con ID {corredorId} editado exitosamente.");
                TempData["SuccessMessage"] = "Datos del corredor actualizados exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al editar los datos del corredor: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al actualizar los datos del corredor. Por favor, int�ntelo de nuevo m�s tarde.");
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


        public async Task<IActionResult> Editar_Carrera(int carreraId, string nom_carrera, int year_carrera)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Actualizar los datos de la carrera
                    string updateSql = "UPDATE CARRERA SET nom_carrera = @nom_carrera, year_carrera = @year_carrera WHERE carreraId = @carreraId";
                    using (SqlCommand updateCommand = new SqlCommand(updateSql, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@carreraId", carreraId);
                        updateCommand.Parameters.AddWithValue("@nom_carrera", nom_carrera);
                        updateCommand.Parameters.AddWithValue("@year_carrera", year_carrera);

                        await updateCommand.ExecuteNonQueryAsync();
                    }
                }

                _logger.LogInformation($"Carrera con ID {carreraId} editada exitosamente.");
                TempData["SuccessMessage"] = "Datos de la carrera actualizados exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al editar los datos de la carrera: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Error al actualizar los datos de la carrera. Por favor, inténtelo de nuevo más tarde.");
                return View();
            }
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




        public IActionResult Error()
        {
            return View();
        }
    }
}