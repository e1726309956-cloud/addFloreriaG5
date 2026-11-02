using System;
using System.Collections.Generic;
using System.Text;

namespace addFloreriaG5.Models
{
    public class EntregaModel
    {
        public int id_entrega { get; set; }
        public int id_pedido { get; set; }
        public string direccion { get; set; } = string.Empty;
        public string destinatario { get; set; } = string.Empty;
        public string telefono_destinatario { get; set; } = string.Empty;
        public string fecha_entrega { get; set; } = string.Empty;
        public string observaciones { get; set; } = string.Empty;
        public string estado { get; set; } = string.Empty;

        public PedidoModel pedido { get; set; } = new();
    }
}
