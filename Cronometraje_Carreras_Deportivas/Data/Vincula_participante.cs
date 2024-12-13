namespace Cronometraje_Carreras_Deportivas.Data
{
    public class Vincula_participante
    {
        public int ID_vinculo { get; set; }
        public byte[] ID_corredor { get; set; }
        public int ID_carr_cat { get; set; }
        public int num_corredor { get; set; }
        public string folio_chip { get; set; }
        public Corredor Corredor { get; set; }
        public Carr_cat Carr_cat { get; set; }
    }

}
