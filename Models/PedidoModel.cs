using System;
using System.Collections.Generic;
using System.Text;

namespace addFloreriaG5.Models
{
    public class PedidoModel
    {
        public int id_pedido { get; set; }
        public int id_cliente { get; set; }
        public string fecha_pedido { get; set; } = string.Empty;
        public decimal total { get; set; }
        public string estado { get; set; } = string.Empty;
        public string direccion_envio { get; set; } = string.Empty;
        public string telefono_envio { get; set; } = string.Empty;

        // Lista para almacenar los pagos asociados a este pedido
        public List<PagoModel> pagos { get; set; } = new();
    }

    // Modelo independiente para los pagos
    public class PagoModel
    {
        public int id_pago { get; set; }
        public int id_pedido { get; set; }
        public decimal monto { get; set; }
        public string metodo_pago { get; set; } = string.Empty;
        public string fecha_pago { get; set; } = string.Empty;
        public string estado { get; set; } = string.Empty;
    }
}
