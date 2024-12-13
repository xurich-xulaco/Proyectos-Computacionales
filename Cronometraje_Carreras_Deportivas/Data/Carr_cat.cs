namespace Cronometraje_Carreras_Deportivas.Data
{
    public class Carr_cat
    {
        public int ID_carr_cat { get; set; }
        public int ID_carrera { get; set; }
        public int ID_categoria { get; set; }
        public Carrera Carrera { get; set; }
        public Categoria Categoria { get; set; }
    }

}
