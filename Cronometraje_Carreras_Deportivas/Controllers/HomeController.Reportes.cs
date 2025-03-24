using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.IdentityModel.Tokens;
using iTextSharp.text;
using iTextSharp.text.pdf;

using Microsoft.Data.SqlClient;

namespace Cronometraje_Carreras_Deportivas.Controllers
{
    public partial class HomeController : Controller
    {

        private struct ReporteCorredor_Corredor
        {
            public ReporteCorredor_Corredor(string nombre, string fecha, char sexo, string pais, string correo, List<string> telefono)
            {
                Nombre = nombre;
                Fecha = fecha;
                Sexo = sexo;
                Pais = pais;
                Correo = correo;
                Telefono = telefono;
            }

            public string Nombre { get; init; }
            public string Fecha { get; init; }
            public char Sexo { get; init; }
            public string Pais { get; init; }
            public string Correo { get; init; } = string.Empty;
            public List<string> Telefono { get; init; } = new List<string>();
        }

        private struct ReporteCorredor_CarreraReferencia
        {
            public ReporteCorredor_CarreraReferencia(int numCorredor, int idCategoria, int idCarrera)
            {
                NumCorredor = numCorredor;
                IdCategoria = idCategoria;
                IdCarrera = idCarrera;
            }

            public int NumCorredor { get; init; }
            public int IdCategoria { get; init; }
            public int IdCarrera { get; init; }
        }

        private struct ReporteCorredor_CarCatInfo
        {
            public ReporteCorredor_CarCatInfo(string nombreCarrera, string añoCarrera, string edicionCarrera, string categoria, string numCorredor)
            {
                Nombre = nombreCarrera;
                Año = añoCarrera;
                Edicion = edicionCarrera;
                Categoria = categoria;
                NumCorredor = numCorredor;
            }
            public ReporteCorredor_CarCatInfo(string nombreCarrera, string añoCarrera, string edicionCarrera, string categoria, string numCorredor, string posicion, string t1, string t2, string t3, string tfinal)
            {
                Nombre = nombreCarrera;
                Año = añoCarrera;
                Edicion = edicionCarrera;
                Categoria = categoria;
                NumCorredor = numCorredor;
                Posicion = posicion;
                T1 = t1;
                T2 = t2;
                T3 = t3;
                TFinal = tfinal;
            }

            public string Nombre { get; init; }
            public string Año { get; init; }
            public string Edicion { get; init; }
            public string Categoria { get; init; }
            public string NumCorredor { get; init; }
            public string Posicion { get; init; } = string.Empty;
            public string T1 { get; init; } = string.Empty;
            public string T2 { get; init; } = string.Empty;
            public string T3 { get; init; } = string.Empty;
            public string TFinal { get; init; } = string.Empty;
        }

        [HttpGet]
        public IActionResult DownloadReporte(string fileName, string downloadName = null)
        {
            try
            {
                // Validar que se reciba un nombre de archivo
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    _logger.LogError("Se ha recibido un nombre de archivo vacío o nulo.");
                    return BadRequest("El nombre de archivo es requerido.");
                }

                _logger.LogInformation("Generando link de descarga.");

                string tempFolder = Path.Combine(Path.GetTempPath(), "ReportesCronos");
                string filePath = Path.Combine(tempFolder, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError("El archivo no existe o ya fue descargado.");
                    return NotFound("El archivo no existe o ya fue descargado.");
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

                // Opcional: si el crash ocurre al eliminar, comenta la siguiente línea
                try
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Archivo eliminado después de la descarga.");
                }
                catch (Exception exDelete)
                {
                    _logger.LogError($"Error al eliminar el archivo: {exDelete}");
                    // Se puede continuar aunque falle la eliminación, ya que es opcional.
                }

                _logger.LogInformation("Link de descarga completamente generado.");
                // Si downloadName se suministra, se usa para la descarga; de lo contrario, se usa el nombre almacenado.
                string finalDownloadName = string.IsNullOrWhiteSpace(downloadName) ? "Reporte.pdf" : downloadName;
                return File(fileBytes, "application/pdf", finalDownloadName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al descargar el reporte: {ex}");
                return Content("Error al descargar el reporte.");
            }
        }


        private async Task<byte[]> ReporteCorredor_CorredorID(int year, int edicion, string categoria, int numero, SqlConnection connection)
        {
            byte[] idCorredor = null;

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
                idCorredor = (byte[])result; // Mantener como byte[]
            }

            return idCorredor;
        }

        private async Task<ReporteCorredor_Corredor> ReporteCorredor_CorredorInfo(byte[] idCorredor, SqlConnection connection)
        {
            ReporteCorredor_Corredor corredorInfo = default;

            string queryCorredor = @"
        SELECT 
            c.nom_corredor, 
            c.apP_corredor, 
            c.apM_corredor, 
            c.f_corredor, 
            c.sex_corredor, 
            c.pais, 
            c.c_corredor,
            (SELECT STRING_AGG(t.numero, ',') 
             FROM TELEFONO t 
             WHERE t.ID_Corredor = CONVERT(varchar, c.ID_corredor)
            ) AS telefonos
        FROM CORREDOR c
        WHERE c.ID_corredor = @IDCorredor;";

            using (SqlCommand command = new SqlCommand(queryCorredor, connection))
            {
                command.Parameters.Add("@IDCorredor", SqlDbType.VarBinary).Value = idCorredor;

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Construir el nombre completo
                        string nombreCompleto = $"{reader.GetString(0)} {reader.GetString(1)} {reader.GetString(2)}";
                        // Formatear la fecha de nacimiento
                        string fecha = reader.GetDateTime(3).ToString("yyyy-MM-dd");
                        // Obtener el primer carácter del sexo
                        char sexo = reader.GetString(4)[0];
                        // Obtener el país
                        string pais = reader.GetString(5);
                        // Obtener el correo, verificando si es nulo
                        string correo = reader.IsDBNull(6) ? "N/A" : reader.GetString(6);

                        // Procesar la lista de teléfonos
                        List<string> telefonos = new List<string>();
                        if (!reader.IsDBNull(7))
                        {
                            string telefonosAggregados = reader.GetString(7);
                            // Separa la cadena en base a la coma y quita espacios en blanco adicionales
                            telefonos = telefonosAggregados.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(t => t.Trim())
                                                          .ToList();
                        }

                        corredorInfo = new ReporteCorredor_Corredor(nombreCompleto, fecha, sexo, pais, correo, telefonos);
                    }
                    else
                    {
                        _logger.LogWarning("No se encontró información del corredor.");
                    }
                }
            }

            return corredorInfo;
        }

        private async Task<List<ReporteCorredor_CarreraReferencia>> ReporteCorredor_ListaCarreras(byte[] idCorredor, SqlConnection connection)
        {
            List<ReporteCorredor_CarreraReferencia> listaCarreras = new List<ReporteCorredor_CarreraReferencia>();

            string queryCarreras = @"
        SELECT 
            v.num_corredor AS NumCorredor, 
            cc.ID_categoria AS IdCategoria, 
            cc.ID_carrera AS IdCarrera
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
                        // Convertir los valores obtenidos a int
                        int numCorredor = Convert.ToInt32(reader["NumCorredor"]);
                        int idCategoria = Convert.ToInt32(reader["IdCategoria"]);
                        int idCarrera = Convert.ToInt32(reader["IdCarrera"]);

                        // Crear la instancia del struct con la información obtenida
                        ReporteCorredor_CarreraReferencia carrera = new ReporteCorredor_CarreraReferencia(numCorredor, idCategoria, idCarrera);
                        listaCarreras.Add(carrera);
                    }
                    if (listaCarreras.Count == 0)
                    {
                        _logger.LogWarning("No se encontraron carreras para el corredor.");
                    }
                }
            }

            return listaCarreras;
        }

        private async Task<ReporteCorredor_CarCatInfo> ReporteCorredor_InfoCarreras(int idCarrera, int idCategoria, int numCorredor, SqlConnection connection)
        {
            ReporteCorredor_CarCatInfo carreraInfo = default;

            string queryTiempos = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.TIEMPO
)
SELECT 
    ca.nom_carrera,
    ca.year_carrera,
    ca.edi_carrera,
    (SELECT nombre_categoria FROM CATEGORIA WHERE ID_categoria = cc.ID_categoria) AS Categoria,
    RANK() OVER (ORDER BY 
        (DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
         DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
         DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00')) +
         DATEDIFF(SECOND, 0, COALESCE(T4.tiempo_registrado, '00:00:00')))
    ) AS Posicion,
    COALESCE(T1.tiempo_registrado, '00:00:00') AS T1,
    COALESCE(T2.tiempo_registrado, '00:00:00') AS T2,
    COALESCE(T3.tiempo_registrado, '00:00:00') AS T3,
    CONVERT(TIME, DATEADD(SECOND, 
        DATEDIFF(SECOND, 0, COALESCE(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T3.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, COALESCE(T4.tiempo_registrado, '00:00:00')),
        0
    )) AS TiempoTotal
FROM CARRERA ca
JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
LEFT JOIN TiemposCorredor T4 ON v.folio_chip = T4.folio_chip AND T4.NumTiempo = 4
WHERE 
    ca.ID_carrera = @idCarrera
    AND cc.ID_categoria = @idCategoria
    AND v.num_corredor = @numCorredor;
";

            using (SqlCommand command = new SqlCommand(queryTiempos, connection))
            {
                command.Parameters.AddWithValue("@idCarrera", idCarrera);
                command.Parameters.AddWithValue("@idCategoria", idCategoria);
                command.Parameters.AddWithValue("@numCorredor", numCorredor);

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Mapear la información obtenida de la consulta a las propiedades del struct
                        string nombreCarrera = reader["nom_carrera"].ToString();
                        string añoCarrera = reader["year_carrera"].ToString();
                        string edicionCarrera = reader["edi_carrera"].ToString();
                        string categoria = reader["Categoria"].ToString();
                        // Convertir numCorredor a string, en caso de que se requiera en la estructura
                        string numCorredorStr = numCorredor.ToString();

                        string posicion = reader["Posicion"].ToString();
                        string t1 = reader["T1"].ToString();
                        string t2 = reader["T2"].ToString();
                        string t3 = reader["T3"].ToString();
                        string tfinal = reader["TiempoTotal"].ToString();

                        carreraInfo = new ReporteCorredor_CarCatInfo(
                            nombreCarrera,
                            añoCarrera,
                            edicionCarrera,
                            categoria,
                            numCorredorStr,
                            posicion,
                            t1,
                            t2,
                            t3,
                            tfinal
                        );
                    }
                    else
                    {
                        _logger.LogWarning("No se encontró información de la carrera para el corredor.");
                    }
                }
            }

            return carreraInfo;
        }

        [HttpPost]
        public async Task<IActionResult> GenerarReporteCorredor(int year, int edicion, string categoria, int numero)
        {
            ReporteCorredor_Corredor corredorInfo = default;
            List<ReporteCorredor_CarCatInfo> carrerasInfo = new List<ReporteCorredor_CarCatInfo>();
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                byte[] idCorredor = null;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    _logger.LogInformation("Abriendo conexión con la base de datos para generar un reporte de corredor.");

                    // 2. Obtener ID del corredor
                    idCorredor = await ReporteCorredor_CorredorID(year, edicion, categoria, numero, connection);
                    if (idCorredor.IsNullOrEmpty())
                        return Json(new { success = false, message = "No se encontró el ID del corredor." });

                    // 3. Obtener información personal del corredor
                    corredorInfo = await ReporteCorredor_CorredorInfo(idCorredor, connection);

                    // 4. Obtener lista de carreras en las que ha participado
                    List<ReporteCorredor_CarreraReferencia> carrerasTemp = await ReporteCorredor_ListaCarreras(idCorredor, connection);

                    // 5. Obtener detalles y tiempos por cada carrera
                    foreach (var carrera in carrerasTemp)
                    {
                        carrerasInfo.Add(await ReporteCorredor_InfoCarreras(carrera.IdCarrera, carrera.IdCategoria, carrera.NumCorredor, connection));
                    }

                    if (carrerasInfo.Count == 0)
                        _logger.LogWarning("El contenido del reporte está vacío.");

                    _logger.LogInformation("Cerrando conexión con la base de datos.");
                }
            }
            catch (Exception ex_sql)
            {
                _logger.LogError(ex_sql, "Error en la conexión con la base de datos.");
                return Json(new { success = false, message = "Ocurrió un error al conectarse a la base de datos." });
            }

            // ===============================================
            // Generación del PDF con iTextSharp (Tamaño Carta)
            // ===============================================
            try
            {
                using (var ms = new MemoryStream())
                {
                    _logger.LogInformation("Generando reporte de corredor en PDF.");

                    // Crear documento en tamaño carta, con márgenes de 50 unidades
                    Document pdfDoc = new Document(PageSize.LETTER, 50, 50, 50, 50);
                    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                    pdfDoc.Open();

                    // Definición de fuentes: buscamos un estilo limpio y profesional
                    Font titleFont = FontFactory.GetFont("Helvetica-Bold", 24, BaseColor.BLACK);
                    Font headerFont = FontFactory.GetFont("Helvetica-Bold", 16, BaseColor.BLACK);
                    Font labelFont = FontFactory.GetFont("Helvetica", 12, BaseColor.BLACK);
                    Font valueFont = FontFactory.GetFont("Helvetica", 12, BaseColor.DARK_GRAY);

                    // Título principal del reporte
                    Paragraph titleParagraph = new Paragraph("Reporte de Corredor", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    pdfDoc.Add(titleParagraph);

                    // Sección: Información Personal
                    Paragraph infoHeader = new Paragraph("Información Personal", headerFont)
                    {
                        SpacingAfter = 10f
                    };
                    pdfDoc.Add(infoHeader);

                    // Tabla para la información personal (2 columnas: etiqueta - valor)
                    PdfPTable infoTable = new PdfPTable(2) { WidthPercentage = 100 };
                    infoTable.SetWidths(new float[] { 30, 70 });

                    infoTable.AddCell(new PdfPCell(new Phrase("Nombre:", labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    infoTable.AddCell(new PdfPCell(new Phrase(corredorInfo.Nombre, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });

                    infoTable.AddCell(new PdfPCell(new Phrase("Fecha de Nacimiento:", labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    infoTable.AddCell(new PdfPCell(new Phrase(corredorInfo.Fecha, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });

                    infoTable.AddCell(new PdfPCell(new Phrase("Sexo:", labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    infoTable.AddCell(new PdfPCell(new Phrase((corredorInfo.Sexo == 'M') ? "Hombre" : "Mujer", valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });

                    infoTable.AddCell(new PdfPCell(new Phrase("País:", labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    infoTable.AddCell(new PdfPCell(new Phrase(corredorInfo.Pais, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });

                    infoTable.AddCell(new PdfPCell(new Phrase("Correo:", labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    infoTable.AddCell(new PdfPCell(new Phrase(corredorInfo.Correo, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });

                    infoTable.AddCell(new PdfPCell(new Phrase("Teléfonos:", labelFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    string telefonos = (corredorInfo.Telefono != null && corredorInfo.Telefono.Any())
                                         ? string.Join(", ", corredorInfo.Telefono)
                                         : "N/A";
                    infoTable.AddCell(new PdfPCell(new Phrase(telefonos, valueFont)) { Border = Rectangle.NO_BORDER, Padding = 5 });
                    pdfDoc.Add(infoTable);

                    // Espacio entre secciones
                    pdfDoc.Add(new Paragraph("\n"));

                    // Sección: Participaciones en Carreras
                    Paragraph carrerasHeader = new Paragraph("Participaciones en Carreras", headerFont)
                    {
                        SpacingAfter = 10f
                    };
                    pdfDoc.Add(carrerasHeader);

                    // Tabla para la información de cada carrera
                    PdfPTable carrerasTable = new PdfPTable(10) { WidthPercentage = 100 };
                    carrerasTable.SetWidths(new float[] { 12, 10, 8, 10, 8, 6, 11, 11, 11, 12 });
                    string[] tableHeaders = { "Carr.", "Año", "Ed.", "Cat.", "N° Corr.", "Pos.", "T1", "T2", "T3", "T.Total" };
                    foreach (string header in tableHeaders)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(header, labelFont))
                        {
                            BackgroundColor = new BaseColor(230, 230, 230),
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5
                        };
                        carrerasTable.AddCell(cell);
                    }

                    foreach (var carrera in carrerasInfo)
                    {
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.Nombre, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.Año, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.Edicion, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.Categoria, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.NumCorredor, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.Posicion, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.T1, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.T2, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.T3, valueFont)) { Padding = 5 });
                        carrerasTable.AddCell(new PdfPCell(new Phrase(carrera.TFinal, valueFont)) { Padding = 5 });
                    }
                    pdfDoc.Add(carrerasTable);

                    pdfDoc.Close();

                    _logger.LogInformation("Reporte generado completamente.");

                    // Guardar el PDF en una carpeta temporal y generar URL de descarga
                    string tempFolder = Path.Combine(Path.GetTempPath(), "ReportesCronos");
                    if (!Directory.Exists(tempFolder))
                        Directory.CreateDirectory(tempFolder);
                    // Nombre para almacenar (con GUID para evitar colisiones)
                    string storageFileName = $"Reporte Corredor_{Guid.NewGuid()}.pdf";
                    // Nombre limpio para mostrar al usuario
                    string downloadFileName = string.Empty;
                    if (!string.IsNullOrWhiteSpace(corredorInfo.Nombre))
                    {
                        var nombres = corredorInfo.Nombre.Split(' ');
                        if (nombres.Length > 1)
                        {
                            var apellidoPaterno = nombres[1];
                            downloadFileName = $"Reporte Corredor_{apellidoPaterno}.pdf";
                        }
                    }
                    string filePath = Path.Combine(tempFolder, storageFileName);
                    System.IO.File.WriteAllBytes(filePath, ms.ToArray());

                    // Generar URL de descarga pasando ambos parámetros
                    string downloadUrl = Url.Action("DownloadReporte", "Home", new { fileName = storageFileName, downloadName = downloadFileName });
                    return Json(new { success = true, downloadUrl = downloadUrl });
                }
            }
            catch (Exception ex_pdf)
            {
                _logger.LogError(ex_pdf, "Error en la generación del PDF");
                return Json(new { success = false, message = "Hubo un error generando el PDF." });
            }
        }



        private struct ReporteCarreras_CarreraInfo
        {
            public ReporteCarreras_CarreraInfo (string CarreraNombre, string CarreraAño, string CarreraEdicion)
            {
                Nombre = CarreraNombre;
                Año = CarreraAño;
                Edicion = CarreraEdicion;
            }

            public string Nombre { get; init; }
            public string Año { get; init; }
            public string Edicion { get; init; }
        }


        private struct ReporteCarreras_Podio
        {
            public ReporteCarreras_Podio (string NPosicion, string NumeroCorredor, string NombreCorredor, string Tiempo1, string Tiempo2, string Tiempo3, string TiempoTotal)
            {
                posicion = NPosicion;
                numero = NumeroCorredor;
                nombre = NombreCorredor;
                T1 = Tiempo1;
                T2 = Tiempo2;
                T3 = Tiempo3;
                Ttotal = TiempoTotal;
            }

            public string posicion { get; init; }
            public string numero { get; init; }
            public string nombre { get; init; }
            public string T1 { get; init; }
            public string T2 { get; init; }
            public string T3 { get; init; }
            public string Ttotal { get; init; }
        }
        

        private async Task<ReporteCarreras_CarreraInfo> ReporteCarreras_GetInfo(int IDCarrera, SqlConnection connection)
        {
            ReporteCarreras_CarreraInfo InformacionDeCarrera = default;

            string carreraInfoQuery = @"
                SELECT 
                    nom_carrera AS NombreCarrera, 
                    year_carrera AS Año, 
                    edi_carrera AS Edición
                FROM 
                    CARRERA
                WHERE 
                    ID_carrera = @ID_carrera";

            using (SqlCommand carreraInfoCmd = new SqlCommand(carreraInfoQuery, connection))
            {
                carreraInfoCmd.Parameters.AddWithValue("@ID_carrera", IDCarrera);
                using (SqlDataReader reader = await carreraInfoCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string carreraNombre = reader["NombreCarrera"].ToString();
                        string carreraAño = reader["Año"].ToString();
                        string carreraEdicion = reader["Edición"].ToString();

                        InformacionDeCarrera = new ReporteCarreras_CarreraInfo(carreraNombre, carreraAño, carreraEdicion);
                    }
                }
            }

            return InformacionDeCarrera;
        }


        private async Task<List<string>> ReporteCarreras_ListaCategorias(int IDCarrera, SqlConnection connection)
        {
            List<string> TodasLasCategorias = new List<string>();

            string categoriasQuery = @"
                SELECT DISTINCT cat.nombre_categoria
                FROM CARR_Cat cc
                JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                WHERE cc.ID_carrera = @carreraId";
            using (SqlCommand categoriasCmd = new SqlCommand(categoriasQuery, connection))
            {
                categoriasCmd.Parameters.AddWithValue("@carreraId", IDCarrera);
                using (SqlDataReader reader = await categoriasCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        TodasLasCategorias.Add(reader["nombre_categoria"].ToString());
                    }
                }
            }

            return TodasLasCategorias;
        }


        private async Task<int> ReporteCarreras_TodosLosHombres(int IDCarrera, string categoria, SqlConnection connection)
        {
            int hombres = 0;

            string totalHombresQuery = @"
                        SELECT COUNT(*) 
                        FROM CARR_Cat cc
                        JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
                        JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
                        JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                        WHERE cc.ID_carrera = @carreraId
                          AND cat.nombre_categoria = @categoria
                          AND c.sex_corredor = 'M';";
            using (SqlCommand hombresCmd = new SqlCommand(totalHombresQuery, connection))
            {
                hombresCmd.Parameters.AddWithValue("@carreraId", IDCarrera);
                hombresCmd.Parameters.AddWithValue("@categoria", categoria);
                hombres = (int)await hombresCmd.ExecuteScalarAsync();
            }

            return hombres;
        }


        private async Task<int> ReporteCarreras_TodasLasMujeres(int IDCarrera, string categoria, SqlConnection connection)
        {
            int mujeres = 0;

            string totalMujeresQuery = @"
                        SELECT COUNT(*) 
                        FROM CARR_Cat cc
                        JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
                        JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
                        JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                        WHERE cc.ID_carrera = @carreraId
                          AND cat.nombre_categoria = @categoria
                          AND c.sex_corredor = 'F';";
            using (SqlCommand mujeresCmd = new SqlCommand(totalMujeresQuery, connection))
            {
                mujeresCmd.Parameters.AddWithValue("@carreraId", IDCarrera);
                mujeresCmd.Parameters.AddWithValue("@categoria", categoria);
                mujeres = (int)await mujeresCmd.ExecuteScalarAsync();
            }

            return mujeres;
        }


        private async Task<int> ReporteCarreras_VerificarParticipantes(int IDCarrera, string categoria, SqlConnection connection)
        {
            int participantes = 0;

            string verificarParticipantesQuery = @"
                        SELECT COUNT(*) AS Participantes
                        FROM CARR_Cat cc
                        JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
                        JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
                        WHERE cc.ID_carrera = @carreraId
                          AND cat.nombre_categoria = @categoria";
            using (SqlCommand verificarCmd = new SqlCommand(verificarParticipantesQuery, connection))
            {
                verificarCmd.Parameters.AddWithValue("@carreraId", IDCarrera);
                verificarCmd.Parameters.AddWithValue("@categoria", categoria);
                participantes = (int)await verificarCmd.ExecuteScalarAsync();
            }

            return participantes;
        }


        //A modo de reducir el código, se usa una variable para definir de qué sexo se quiere el podio
        // 0 - Mixto
        // 1 - Mujeres
        // 2 - Hombres
        private async Task<Queue<ReporteCarreras_Podio>> ReporteCarreras_ObtenerPodio(int IDcarrera, string categoria, short TipoDeSexo, SqlConnection connection)
        {
            var ListaPodio = new Queue<ReporteCarreras_Podio>();
            string PodioQuery;

            switch (TipoDeSexo)
            {
                case 0: // Mixto
                    PodioQuery = @"
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
            (DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00'))),
            v.num_corredor
        ) AS Posicion,
        v.num_corredor,
        c.nom_corredor,
        c.apP_corredor,
        c.apM_corredor,
        ISNULL(T1.tiempo_registrado, '00:00:00') AS T1,
        ISNULL(T2.tiempo_registrado, '00:00:00') AS T2,
        ISNULL(T3.tiempo_registrado, '00:00:00') AS T3,
        CONVERT(TIME, DATEADD(SECOND, 
            (DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00'))), 0)) AS TiempoTotal
    FROM CARRERA ca
    JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE ca.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
)
SELECT TOP 3 
    Posicion, 
    num_corredor, 
    nom_corredor, 
    apP_corredor, 
    apM_corredor, 
    T1, T2, T3, 
    TiempoTotal
FROM Posiciones
ORDER BY Posicion;";
                    break;

                case 1: // Mujeres
                    PodioQuery = @"
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
            (DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00'))),
            v.num_corredor
        ) AS Posicion,
        v.num_corredor,
        c.nom_corredor,
        c.apP_corredor,
        c.apM_corredor,
        ISNULL(T1.tiempo_registrado, '00:00:00') AS T1,
        ISNULL(T2.tiempo_registrado, '00:00:00') AS T2,
        ISNULL(T3.tiempo_registrado, '00:00:00') AS T3,
        CONVERT(TIME, DATEADD(SECOND, 
            (DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00'))), 0)) AS TiempoTotal
    FROM CARRERA ca
    JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE ca.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
      AND c.sex_corredor = 'F'
)
SELECT TOP 3 
    Posicion, 
    num_corredor, 
    nom_corredor, 
    apP_corredor, 
    apM_corredor, 
    T1, T2, T3, 
    TiempoTotal
FROM Posiciones
ORDER BY Posicion;";
                    break;

                case 2: // Hombres
                    PodioQuery = @"
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
            (DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00'))),
            v.num_corredor
        ) AS Posicion,
        v.num_corredor,
        c.nom_corredor,
        c.apP_corredor,
        c.apM_corredor,
        ISNULL(T1.tiempo_registrado, '00:00:00') AS T1,
        ISNULL(T2.tiempo_registrado, '00:00:00') AS T2,
        ISNULL(T3.tiempo_registrado, '00:00:00') AS T3,
        CONVERT(TIME, DATEADD(SECOND, 
            (DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
             DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00'))), 0)) AS TiempoTotal
    FROM CARRERA ca
    JOIN CARR_Cat cc ON ca.ID_carrera = cc.ID_carrera  
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE ca.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
      AND c.sex_corredor = 'M'
)
SELECT TOP 3 
    Posicion, 
    num_corredor, 
    nom_corredor, 
    apP_corredor, 
    apM_corredor, 
    T1, T2, T3, 
    TiempoTotal
FROM Posiciones
ORDER BY Posicion;";
                    break;

                default:
                    PodioQuery = string.Empty;
                    _logger.LogWarning("No se eligió un podio válido para consultas en el reporte de la carrera.");
                    return ListaPodio;
            }

            using (SqlCommand podioCmd = new SqlCommand(PodioQuery, connection))
            {
                podioCmd.Parameters.AddWithValue("@carreraId", IDcarrera);
                podioCmd.Parameters.AddWithValue("@categoria", categoria);
                using (SqlDataReader reader = await podioCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string nombreCompleto = $"{reader["nom_corredor"]} {reader["apP_corredor"]} {reader["apM_corredor"]}";
                        var reporte = new ReporteCarreras_Podio(
                            NPosicion: reader["Posicion"].ToString(),
                            NumeroCorredor: reader["num_corredor"].ToString(),
                            NombreCorredor: nombreCompleto,
                            Tiempo1: reader["T1"].ToString(),
                            Tiempo2: reader["T2"].ToString(),
                            Tiempo3: reader["T3"].ToString(),
                            TiempoTotal: reader["TiempoTotal"].ToString()
                        );
                        ListaPodio.Enqueue(reporte);
                    }
                }
            }

            return ListaPodio;
        }

        private async Task<string> ReporteCarreras_ObtenerMenorTiempo(int IDcarrera, string categoria, short TipoDeSexo, SqlConnection connection)
        {
            string MenorTiempo = "00:00:00";
            string TiempoQuery;

            switch (TipoDeSexo)
            {
                case 0: // Mixto
                    TiempoQuery = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
),
TotalTiempos AS (
    SELECT 
        DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00')) AS TiempoTotalSegundos
    FROM CARR_Cat cc
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE cc.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
)
SELECT CONVERT(TIME, DATEADD(SECOND, MIN(TiempoTotalSegundos), 0)) AS MenorTiempoMixto
FROM TotalTiempos;";
                    break;

                case 1: // Mujeres
                    TiempoQuery = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
),
TotalTiempos AS (
    SELECT 
        DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00')) AS TiempoTotalSegundos
    FROM CARR_Cat cc
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE cc.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
      AND c.sex_corredor = 'F'
)
SELECT CONVERT(TIME, DATEADD(SECOND, MIN(TiempoTotalSegundos), 0)) AS MenorTiempoMujeres
FROM TotalTiempos;";
                    break;

                case 2: // Hombres
                    TiempoQuery = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
),
TotalTiempos AS (
    SELECT 
        DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00')) AS TiempoTotalSegundos
    FROM CARR_Cat cc
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE cc.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
      AND c.sex_corredor = 'M'
)
SELECT CONVERT(TIME, DATEADD(SECOND, MIN(TiempoTotalSegundos), 0)) AS MenorTiempoHombres
FROM TotalTiempos;";
                    break;

                default:
                    MenorTiempo = string.Empty;
                    _logger.LogWarning("No se eligió un sexo válido para consultas de menor tiempo en el reporte de la carrera.");
                    return MenorTiempo;
            }
            using (SqlCommand menorTiempoCmd = new SqlCommand(TiempoQuery, connection))
            {
                menorTiempoCmd.Parameters.AddWithValue("@carreraId", IDcarrera);
                menorTiempoCmd.Parameters.AddWithValue("@categoria", categoria);
                MenorTiempo = (await menorTiempoCmd.ExecuteScalarAsync()).ToString();
            }

            return MenorTiempo;
        }

        private async Task<string> ReporteCarreras_ObtenerTiempoPromedio(int IDcarrera, string categoria, short TipoDeSexo, SqlConnection connection)
        {
            string TiempoPromedio = "00:00:00";
            string TiempoQuery;

            switch (TipoDeSexo)
            {
                case 0: // Mixto
                    TiempoQuery = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
),
TotalTiempos AS (
    SELECT 
        DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00')) AS TiempoTotalSegundos
    FROM CARR_Cat cc
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE cc.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
)
SELECT CONVERT(TIME, DATEADD(SECOND, CAST(AVG(TiempoTotalSegundos) AS INT), 0)) AS TiempoPromedioMixto
FROM TotalTiempos;";
                    break;

                case 1: // Mujeres
                    TiempoQuery = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
),
TotalTiempos AS (
    SELECT 
        DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00')) AS TiempoTotalSegundos
    FROM CARR_Cat cc
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE cc.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
      AND c.sex_corredor = 'F'
)
SELECT CONVERT(TIME, DATEADD(SECOND, CAST(AVG(TiempoTotalSegundos) AS INT), 0)) AS TiempoPromedioMujeres
FROM TotalTiempos;";
                    break;

                case 2: // Hombres
                    TiempoQuery = @"
WITH TiemposCorredor AS (
    SELECT 
        folio_chip,
        tiempo_registrado,
        ROW_NUMBER() OVER (PARTITION BY folio_chip ORDER BY tiempo_registrado) AS NumTiempo
    FROM dbo.Tiempo
),
TotalTiempos AS (
    SELECT 
        DATEDIFF(SECOND, 0, ISNULL(T1.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T2.tiempo_registrado, '00:00:00')) +
        DATEDIFF(SECOND, 0, ISNULL(T3.tiempo_registrado, '00:00:00')) AS TiempoTotalSegundos
    FROM CARR_Cat cc
    JOIN Vincula_participante v ON cc.ID_Carr_cat = v.ID_Carr_cat
    JOIN CATEGORIA cat ON cc.ID_categoria = cat.ID_categoria
    JOIN CORREDOR c ON v.ID_corredor = c.ID_corredor
    LEFT JOIN TiemposCorredor T1 ON v.folio_chip = T1.folio_chip AND T1.NumTiempo = 1
    LEFT JOIN TiemposCorredor T2 ON v.folio_chip = T2.folio_chip AND T2.NumTiempo = 2
    LEFT JOIN TiemposCorredor T3 ON v.folio_chip = T3.folio_chip AND T3.NumTiempo = 3
    WHERE cc.ID_carrera = @carreraId
      AND cat.nombre_categoria = @categoria
      AND c.sex_corredor = 'M'
)
SELECT CONVERT(TIME, DATEADD(SECOND, CAST(AVG(TiempoTotalSegundos) AS INT), 0)) AS TiempoPromedioHombres
FROM TotalTiempos;";
                    break;

                default:
                    TiempoPromedio = string.Empty;
                    _logger.LogWarning("No se eligió un sexo válido para consultas de tiempo promedio en el reporte de la carrera.");
                    return TiempoPromedio;
            }
            using (SqlCommand TiempoPromedioCmd = new SqlCommand(TiempoQuery, connection))
            {
                TiempoPromedioCmd.Parameters.AddWithValue("@carreraId", IDcarrera);
                TiempoPromedioCmd.Parameters.AddWithValue("@categoria", categoria);
                TiempoPromedio = (await TiempoPromedioCmd.ExecuteScalarAsync()).ToString();
            }

            return TiempoPromedio;
        }


        /// Clase que automatiza la inserción de encabezado y pie de página en cada página del PDF.
        public class PdfPageEvents : PdfPageEventHelper
        {
            /// Se ejecuta al finalizar cada página, agregando el encabezado y pie automáticamente.
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                // Encabezado: Tabla de 1 columna, sin bordes, centrada.
                PdfPTable headerTable = new PdfPTable(1)
                {
                    TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin
                };
                PdfPCell headerCell = new PdfPCell(
                    new Phrase("Reporte de Carrera", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK))
                )
                {
                    Border = 0,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };
                headerTable.AddCell(headerCell);
                headerTable.WriteSelectedRows(0, -1, document.LeftMargin, document.PageSize.Height - 10, writer.DirectContent);

                // Pie de página: Tabla de 1 columna, sin bordes, muestra el número de página.
                PdfPTable footerTable = new PdfPTable(1)
                {
                    TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin
                };
                PdfPCell footerCell = new PdfPCell(
                    new Phrase("Página " + writer.PageNumber, FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK))
                )
                {
                    Border = 0,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };
                footerTable.AddCell(footerCell);
                footerTable.WriteSelectedRows(0, -1, document.LeftMargin, document.BottomMargin - 5, writer.DirectContent);
            }
        }

        // Se basa en un arreglo de booleanos de 11*2
        // 11 por cada caja que se pueda seleccionar
        // 2 porque solo tomaremos en cuenta las 2 últimas categorías, la categoría más baja (de convivencia) se descarta
        // De 0 a 10 es el siguiente orden:
        // 0 - Podio mixto
        // 1 - Menor tiempo mixto
        // 2 - Tiempo promedio mixto
        // 3 - No. de hombres
        // 4 - No. de mujeres
        // 5 - Podio de mujeres
        // 6 - Menor tiempo de mujeres
        // 7 - Tiempo promedio de mujeres
        // 8 - Podio de hombres
        // 9 - Menor tiempo de hombres
        // 10 - Tiempo promedio de hombres
        [HttpPost]
        public async Task<IActionResult> GenerarReporteCarrera(int carreraId, bool[] lista1, bool[] lista2)
        {
            ReporteCarreras_CarreraInfo carreraInfo = default;
            List<string> categorias = new List<string>();
            Queue<int> numeroCorredores = new Queue<int>();
            Queue<ReporteCarreras_Podio> ListaPodios = new Queue<ReporteCarreras_Podio>();
            Queue<string> ListaDeTiempoMenor = new Queue<string>();
            Queue<string> ListaDeTiempoPromedio = new Queue<string>();
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                // Lista de categorías

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    _logger.LogInformation("Abriendo conexión con la base de datos para reporte de carrera.");

                    // 1. Obtener datos básicos de la carrera
                    carreraInfo = await ReporteCarreras_GetInfo(carreraId, connection);

                    // 2. Obtener categorías asociadas a la carrera
                    categorias = await ReporteCarreras_ListaCategorias(carreraId, connection);

                    // 3. Filtrar la categoría con menor km y limitar a 2 si es necesario
                    if (categorias.Count > 0)
                    {
                        var categoriasKm = categorias.Select(c =>
                        {
                            // Se espera el formato "N km"
                            string[] parts = c.Split(' ');
                            int km = int.TryParse(parts[0], out int valor) ? valor : 0;
                            return new { Nombre = c, Km = km };
                        }).ToList();
                        int minKm = categoriasKm.Min(x => x.Km);
                        var categoriasFiltradas = categoriasKm.Where(x => x.Km != minKm).ToList();
                        if (categoriasFiltradas.Count > 2)
                        {
                            categoriasFiltradas = categoriasFiltradas.OrderByDescending(x => x.Km)
                                                                       .Take(2)
                                                                       .ToList();
                        }
                        categorias = categoriasFiltradas.Select(x => x.Nombre).ToList();
                    }

                    short cat_cont = 0;
                    bool existeAlgunCorredor = false;

                    foreach (var categoria in categorias)
                    {
                        // Seleccionar el arreglo de opciones correspondiente
                        bool[] orden;
                        if (cat_cont == 0)
                            orden = lista1;
                        else if (cat_cont == 1)
                            orden = lista2;
                        else
                        {
                            _logger.LogError("Error al procesar las opciones del reporte");
                            return Json(new { success = false, message = "Error al procesar las opciones del reporte." });
                        }

                        // Verificar participantes en la categoría
                        int participantes = await ReporteCarreras_VerificarParticipantes(carreraId, categoria, connection);
                        if (participantes == 0)
                        {
                            numeroCorredores.Enqueue(-1); // Marcar que no hay hombres
                            numeroCorredores.Enqueue(-1); // Marcar que no hay mujeres
                            cat_cont++;
                            continue;
                        }
                        else
                        {
                            existeAlgunCorredor = true;
                        }

                        // Obtener totales de participantes por género para la categoría actual
                        int totalHombres = await ReporteCarreras_TodosLosHombres(carreraId, categoria, connection);
                        int totalMujeres = await ReporteCarreras_TodasLasMujeres(carreraId, categoria, connection);

                        // Encolar para la sección de Hombres y Mujeres respectivamente.
                        // Se encola -1 si no hay corredores en la sección.
                        numeroCorredores.Enqueue(totalHombres > 0 ? totalHombres : -1);
                        numeroCorredores.Enqueue(totalMujeres > 0 ? totalMujeres : -1);

                        if (orden[0])
                        {
                            foreach(var podio in await ReporteCarreras_ObtenerPodio(carreraId, categoria, 0, connection))
                            {
                                ListaPodios.Enqueue(podio);
                            }
                        }
                        if (orden[1])
                        {
                            ListaDeTiempoMenor.Enqueue(await ReporteCarreras_ObtenerMenorTiempo(carreraId, categoria, 0, connection));
                        }
                        if (orden[2])
                        {
                            ListaDeTiempoPromedio.Enqueue(await ReporteCarreras_ObtenerTiempoPromedio(carreraId, categoria, 0, connection));
                        }
                        if (orden[5])
                        {
                            foreach (var podio in await ReporteCarreras_ObtenerPodio(carreraId, categoria, 1, connection))
                            {
                                ListaPodios.Enqueue(podio);
                            }
                        }
                        if (orden[6])
                        {
                            ListaDeTiempoMenor.Enqueue(await ReporteCarreras_ObtenerMenorTiempo(carreraId, categoria, 1, connection));
                        }
                        if (orden[7])
                        {
                            ListaDeTiempoPromedio.Enqueue(await ReporteCarreras_ObtenerTiempoPromedio(carreraId, categoria, 1, connection));
                        }
                        if (orden[8])
                        {
                            foreach (var podio in await ReporteCarreras_ObtenerPodio(carreraId, categoria, 2, connection))
                            {
                                ListaPodios.Enqueue(podio);
                            }
                        }
                        if (orden[9])
                        {
                            ListaDeTiempoMenor.Enqueue(await ReporteCarreras_ObtenerMenorTiempo(carreraId, categoria, 2, connection));
                        }
                        if (orden[10])
                        {
                            ListaDeTiempoPromedio.Enqueue(await ReporteCarreras_ObtenerTiempoPromedio(carreraId, categoria, 2, connection));
                        }
                        cat_cont++;
                    }

                    // Antes de iniciar la generación del PDF, verificamos si se encontró algún corredor
                    if (!existeAlgunCorredor)
                    {
                        _logger.LogError("No hay corredores en la carrera seleccionada.");
                        return Json(new { success = false, message = "No hay ningún corredor en la carrera." });
                    }

                    _logger.LogInformation("Cerrando conexión con la base de datos.");
                }
            }
            catch (Exception ex_sql)
            {
                _logger.LogError(ex_sql, "Hubo un error inesperado en la conexión con la base de datos.");
                return Json(new { success = false, message = "Ocurrió un error al conectarse a la base de datos." });
            }

            try
            {
                using (var ms = new MemoryStream())
                {
                    _logger.LogInformation("Generando reporte de carrera en PDF.");

                    // Crear el documento en tamaño carta con márgenes de 50 unidades
                    Document pdfDoc = new Document(PageSize.LETTER, 50, 50, 50, 50);
                    PdfWriter writer = PdfWriter.GetInstance(pdfDoc, ms);
                    writer.PageEvent = new PdfPageEvents(); // Asigna la clase de eventos
                    pdfDoc.Open();

                    // Definición de fuentes actualizadas para un look profesional
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 26, BaseColor.BLACK);
                    var subTitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.DARK_GRAY);
                    var sectionFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.BLACK);

                    // Título principal del reporte
                    Paragraph mainTitle = new Paragraph("Reporte de carrera", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 10
                    };
                    pdfDoc.Add(mainTitle);

                    // Línea separadora con tono sutil (gris claro)
                    iTextSharp.text.pdf.draw.LineSeparator ls = new iTextSharp.text.pdf.draw.LineSeparator(2f, 100, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2);
                    pdfDoc.Add(new Chunk(ls));
                    pdfDoc.Add(new Paragraph(" "));

                    // Información básica de la carrera
                    Paragraph carreraDatos = new Paragraph($"{carreraInfo.Nombre}  •  {carreraInfo.Año}  •  {carreraInfo.Edicion}", subTitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20
                    };
                    pdfDoc.Add(carreraDatos);

                    // Función para agregar la tabla del podio para una sección
                    void AgregarPodioPorSeccion(Queue<ReporteCarreras_Podio> listaPodios, int totalParticipantes)
                    {
                        if (totalParticipantes <= 0)
                        {
                            pdfDoc.Add(new Paragraph("Sin participantes en esta sección.", normalFont));
                            return;
                        }

                        int podioMax = Math.Min(3, totalParticipantes);
                        PdfPTable tabla = new PdfPTable(7)
                        {
                            WidthPercentage = 100,
                            SpacingBefore = 10,
                            SpacingAfter = 10
                        };

                        // Encabezados de la tabla
                        string[] headers = { "Posición", "Número", "Nombre", "Tiempo 1", "Tiempo 2", "Tiempo 3", "Tiempo Total" };
                        foreach (string header in headers)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(header, normalFont))
                            {
                                BackgroundColor = new BaseColor(230, 230, 230),
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Padding = 8,
                                BorderWidth = 0.5f
                            };
                            tabla.AddCell(cell);
                        }

                        // Ciclo para extraer datos del podio (máximo 3 o según total participantes)
                        for (int i = 0; i < podioMax && listaPodios.Count > 0; i++)
                        {
                            ReporteCarreras_Podio podio = listaPodios.Dequeue();
                            tabla.AddCell(new PdfPCell(new Phrase(podio.posicion, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                            tabla.AddCell(new PdfPCell(new Phrase(podio.numero, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                            tabla.AddCell(new PdfPCell(new Phrase(podio.nombre, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                            tabla.AddCell(new PdfPCell(new Phrase(podio.T1, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                            tabla.AddCell(new PdfPCell(new Phrase(podio.T2, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                            tabla.AddCell(new PdfPCell(new Phrase(podio.T3, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                            tabla.AddCell(new PdfPCell(new Phrase(podio.Ttotal, normalFont)) { Padding = 5, BorderWidth = 0.5f });
                        }
                        pdfDoc.Add(tabla);
                    }

                    // Función para agregar un párrafo con el tiempo (mínimo o promedio)
                    void AgregarParrafoTiempo(string titulo, Queue<string> colaTiempos)
                    {
                        Paragraph pTitulo = new Paragraph(titulo, normalFont)
                        {
                            SpacingBefore = 5,
                            SpacingAfter = 5
                        };
                        pdfDoc.Add(pTitulo);
                        if (colaTiempos.Count > 0)
                        {
                            string tiempo = colaTiempos.Dequeue();
                            if (string.IsNullOrEmpty(tiempo))
                                tiempo = "00:00:00";
                            pdfDoc.Add(new Paragraph(tiempo, normalFont));
                        }
                        else
                        {
                            pdfDoc.Add(new Paragraph("00:00:00", normalFont));
                        }
                    }

                    // Procesar cada categoría. Se asume que 'categorias' y 'listas' ya están definidos y llenos.
                    // 'listas' contiene los dos arreglos booleanos (lista1 y lista2) correspondientes a cada categoría.
                    List<bool[]> listas = new List<bool[]> { lista1, lista2 };
                    for (int i = 0; i < categorias.Count && i < listas.Count; i++)
                    {
                        string categoriaActual = categorias[i];
                        bool[] orden = listas[i];

                        // Título de la categoría
                        Paragraph catTitle = new Paragraph(categoriaActual, subTitleFont)
                        {
                            SpacingBefore = 15,
                            SpacingAfter = 8,
                            Alignment = Element.ALIGN_LEFT
                        };
                        pdfDoc.Add(catTitle);

                        // Extraer el total de participantes para hombres y mujeres de la cola
                        int participantesHombres = numeroCorredores.Dequeue();
                        int participantesMujeres = numeroCorredores.Dequeue();

                        // Si el valor es -1, lo consideramos como 0
                        participantesHombres = (participantesHombres < 0) ? 0 : participantesHombres;
                        participantesMujeres = (participantesMujeres < 0) ? 0 : participantesMujeres;

                        // Sección Mixta (suma de ambos)
                        Paragraph seccionMixta = new Paragraph("Sección Mixta", sectionFont)
                        {
                            SpacingBefore = 10,
                            SpacingAfter = 5
                        };
                        pdfDoc.Add(seccionMixta);
                        int totalMixto = participantesHombres + participantesMujeres;
                        if (orden[0])
                        {
                            AgregarPodioPorSeccion(ListaPodios, totalMixto);
                            pdfDoc.Add(new Paragraph(" "));
                        }
                        if (orden[1])
                        {
                            AgregarParrafoTiempo("Tiempo Menor (Mixta):", ListaDeTiempoMenor);
                            pdfDoc.Add(new Paragraph(" "));
                        }
                        if (orden[2])
                        {
                            AgregarParrafoTiempo("Tiempo Promedio (Mixta):", ListaDeTiempoPromedio);
                            pdfDoc.Add(new Paragraph(" "));
                        }

                        // Sección Mujeres
                        Paragraph seccionMujeres = new Paragraph("Sección Mujeres", sectionFont)
                        {
                            SpacingBefore = 10,
                            SpacingAfter = 5
                        };
                        pdfDoc.Add(seccionMujeres);
                        if (participantesMujeres <= 0)
                        {
                            pdfDoc.Add(new Paragraph("Sin participantes en la sección Mujeres.", normalFont));
                        }
                        else
                        {
                            pdfDoc.Add(new Paragraph($"Número de participantes en Mujeres: {participantesMujeres}", normalFont));
                            if (orden[5])
                            {
                                AgregarPodioPorSeccion(ListaPodios, participantesMujeres);
                                pdfDoc.Add(new Paragraph(" "));
                            }
                            if (orden[6])
                            {
                                AgregarParrafoTiempo("Tiempo Menor (Mujeres):", ListaDeTiempoMenor);
                                pdfDoc.Add(new Paragraph(" "));
                            }
                            if (orden[7])
                            {
                                AgregarParrafoTiempo("Tiempo Promedio (Mujeres):", ListaDeTiempoPromedio);
                                pdfDoc.Add(new Paragraph(" "));
                            }
                        }

                        // Sección Hombres
                        Paragraph seccionHombres = new Paragraph("Sección Hombres", sectionFont)
                        {
                            SpacingBefore = 10,
                            SpacingAfter = 5
                        };
                        pdfDoc.Add(seccionHombres);
                        if (participantesHombres <= 0)
                        {
                            pdfDoc.Add(new Paragraph("Sin participantes en la sección Hombres.", normalFont));
                        }
                        else
                        {
                            pdfDoc.Add(new Paragraph($"Número de participantes en Hombres: {participantesHombres}", normalFont));
                            if (orden[8])
                            {
                                AgregarPodioPorSeccion(ListaPodios, participantesHombres);
                                pdfDoc.Add(new Paragraph(" "));
                            }
                            if (orden[9])
                            {
                                AgregarParrafoTiempo("Tiempo Menor (Hombres):", ListaDeTiempoMenor);
                                pdfDoc.Add(new Paragraph(" "));
                            }
                            if (orden[10])
                            {
                                AgregarParrafoTiempo("Tiempo Promedio (Hombres):", ListaDeTiempoPromedio);
                                pdfDoc.Add(new Paragraph(" "));
                            }
                        }

                        // Separador entre categorías
                        pdfDoc.Add(new Paragraph(new string('-', 66), normalFont));
                    }

                    pdfDoc.Close();

                    _logger.LogInformation("Reporte generado completamente.");

                    // Guardar el PDF en una carpeta temporal
                    string tempFolder = Path.Combine(Path.GetTempPath(), "ReportesCronos");
                    if (!Directory.Exists(tempFolder))
                        Directory.CreateDirectory(tempFolder);
                    // Nombre limpio para la descarga: Se incluye el año de la carrera en el nombre del reporte
                    string downloadFileName = $"Reporte Carrera_{carreraInfo.Año}.pdf";
                    // Nombre para almacenar (con GUID para unicidad)
                    string storageFileName = $"Reporte Carrera_{carreraInfo.Año}_{Guid.NewGuid()}.pdf";
                    string filePath = Path.Combine(tempFolder, storageFileName);
                    System.IO.File.WriteAllBytes(filePath, ms.ToArray());

                    // Generar la URL de descarga y retornar el resultado
                    string downloadUrl = Url.Action("DownloadReporte", "Home", new { fileName = storageFileName, downloadName = downloadFileName });
                    return Json(new { success = true, downloadUrl = downloadUrl });
                }
            }
            catch (Exception ex_pdf)
            {
                _logger.LogError(ex_pdf, "Hubo un error inesperado en la generación del PDF");
                return Json(new { success = false, message = "Hubo un error generando el PDF." });
            }
        }

    }
}
