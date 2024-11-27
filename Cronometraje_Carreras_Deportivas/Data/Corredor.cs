namespace Cronometraje_Carreras_Deportivas.Data
{
    public class Corredor
    {
        public byte[] ID_corredor { get; set; } // Representa varbinary(5) como byte[]
        public string Num_corredor { get; set; } // Suponiendo que "num_corredor" es un identificador de corredor en formato string
        public string Nom_corredor { get; set; } // Nombre del corredor
        public string apP_corredor { get; set; } // Apellido paterno del corredor
        public string apM_corredor { get; set; } // Apellido materno del corredor
        public DateTime f_corredor { get; set; }
        public char sex_corredor { get; set; }
        public string c_corredor { get; set; }
        public string pais_corredor { get; set; }
    }

}
