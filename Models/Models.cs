using System;
using System.Collections.Generic;
using System.Text;

namespace addFloreriaG5.Views
{
    internal class Models
    {
    }

    public class ArregloModel
    {
        public int id_arreglo { get; set; }
        public string nombre { get; set; }
        public string descripcion { get; set; }
        public decimal precio { get; set; }
        public int stock { get; set; }
        public string imagen { get; set; }
        public int id_categoria { get; set; }
    }

    public class CategoriaModel
    {
        public int id_categoria { get; set; }
        public string nombre { get; set; }
    }

}
