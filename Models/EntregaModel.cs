
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace addFloreriaG5.Models
{
    public class EntregaModel
    {
        // DATOS DE SUPABASE
        public int id_entrega { get; set; }
        public int id_pedido { get; set; }

        public string direccion { get; set; } = string.Empty;
        public string destinatario { get; set; } = string.Empty;
        public string telefono_destinatario { get; set; } = string.Empty;
        public string fecha_entrega { get; set; } = string.Empty;
        public string observaciones { get; set; } = string.Empty;
        public string estado { get; set; } = string.Empty;

        // INFORMACIÓN DEL PEDIDO
        public PedidoModel pedido { get; set; } = new();

        // ARREGLOS QUE SE VAN A ENTREGAR
        [JsonIgnore]
        public List<ArregloEntregaModel> Arreglos { get; set; } = new();

        // FECHA DE ENTREGA FORMATEADA
        [JsonIgnore]
        public string FechaEntregaFormateada =>
            DateTime.TryParse(fecha_entrega, out var fecha)
                ? fecha.ToString("dd/MM/yyyy HH:mm")
                : "Fecha pendiente de coordinación";

        // RESUMEN FINANCIERO
        [JsonIgnore]
        public decimal TotalOriginal =>
            pedido?.total_original ?? 0m;

        [JsonIgnore]
        public decimal TotalAbonado =>
            pedido == null ? 0m : TotalOriginal - pedido.total;

        [JsonIgnore]
        public decimal SaldoPendiente =>
            pedido?.total ?? 0m;

        [JsonIgnore]
        public string TotalFormateado =>
            $"Total del pedido: ${TotalOriginal:N2}";

        [JsonIgnore]
        public string AbonadoFormateado =>
            $"Total abonado: ${TotalAbonado:N2}";

        [JsonIgnore]
        public string SaldoFormateado =>
            $"Saldo pendiente: ${SaldoPendiente:N2}";

        // ESTADOS DE ENTREGA
        [JsonIgnore]
        public bool TieneSaldoPendiente =>
            SaldoPendiente > 0;

        [JsonIgnore]
        public bool EstaEntregado =>
            string.Equals(
                estado,
                "Entregado",
                StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool PuedeMarcarEntregado =>
            !EstaEntregado;

        [JsonIgnore]
        public string TextoEstadoEntrega =>
            EstaEntregado
                ? "Entrega completada"
                : "Entrega pendiente";
    }

    // ARREGLOS ASOCIADOS A CADA ENTREGA
    public class ArregloEntregaModel
    {
        public int id_arreglo { get; set; }

        public string nombre { get; set; } = string.Empty;
        public string imagen { get; set; } = string.Empty;

        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }

        public string CantidadTexto =>
            $"Cantidad: {cantidad}";

        public string PrecioTexto =>
            $"Precio unitario: ${precio_unitario:N2}";
    }

    // RESULTADO DE LA CONSULTA A DETALLE_PEDIDO
    public class DetalleEntregaConsulta
    {
        public int id_pedido { get; set; }
        public int id_arreglo { get; set; }
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
    }

    // RESULTADO DE LA CONSULTA A ARREGLOS
    public class ArregloEntregaConsulta
    {
        public int id_arreglo { get; set; }

        public string nombre { get; set; } = string.Empty;
        public string imagen { get; set; } = string.Empty;
    }
}
