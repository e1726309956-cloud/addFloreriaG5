
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;

namespace addFloreriaG5.Models
{
    public class PedidoModel
    {
        // DATOS DE SUPABASE
        public int id_pedido { get; set; }
        public int id_cliente { get; set; }

        public string fecha_pedido { get; set; } = string.Empty;

        public decimal total { get; set; }

        public string estado { get; set; } = string.Empty;

        public string direccion_envio { get; set; } = string.Empty;

        public string telefono_envio { get; set; } = string.Empty;

        // INFORMACIÓN DEL ARREGLO
        [JsonIgnore]
        public string imagenArreglo { get; set; } = string.Empty;

        [JsonIgnore]
        public string nombreArreglo { get; set; } = string.Empty;

        // HISTORIAL DE PAGOS
        public List<PagoModel> pagos { get; set; } = new();

        // CONTROL DE ROLES
        [JsonIgnore]
        public bool MostrarAccionesAdmin { get; set; }

        [JsonIgnore]
        public bool MostrarAccionesCliente { get; set; }

        public decimal total_original { get; set; }

        // FECHA FORMATEADA

        [JsonIgnore]
        public string FechaFormateada
        {
            get
            {
                if (DateTimeOffset.TryParse(
                    fecha_pedido,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var fecha))
                {
                    // Convertir UTC a hora de Ecuador (UTC-5)
                    return fecha.ToOffset(
                        TimeSpan.FromHours(-5)
                    ).ToString("dd/MM/yyyy");
                }

                return "Sin fecha";
            }
        }


        // TOTAL FORMATEADO
        [JsonIgnore]
        public string TotalFormateado =>
            $"${total:N2}";

        // ESTADO DEL PEDIDO
        [JsonIgnore]
        public string EstadoFormateado =>
            string.IsNullOrWhiteSpace(estado)
                ? "Pendiente"
                : estado;

        // VERIFICAR SI TIENE ABONOS
        [JsonIgnore]
        public bool TienePagos =>
            pagos != null && pagos.Count > 0;


        public DateTime? fecha_entrega_solicitada { get; set; }

        [JsonIgnore]
        public string FechaEntregaFormateada =>
            fecha_entrega_solicitada.HasValue
                ? fecha_entrega_solicitada.Value.ToString("dd/MM/yyyy HH:mm")
                : "Fecha pendiente de coordinación";

    }

    // MODELO DE PAGOS
    public class PagoModel
    {
        public int id_pago { get; set; }

        public int id_pedido { get; set; }

        public decimal monto { get; set; }

        public string metodo_pago { get; set; } = string.Empty;

        public string fecha_pago { get; set; } = string.Empty;

        public string estado { get; set; } = string.Empty;

        // FECHA DEL ABONO FORMATEADA
        [JsonIgnore]
        public string FechaPagoFormateada
        {
            get
            {
                if (DateTimeOffset.TryParse(
                    fecha_pago,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var fecha))
                {
                    return fecha.ToString("dd/MM/yyyy");
                }

                return fecha_pago;
            }
        }

        // MONTO DEL ABONO FORMATEADO
        [JsonIgnore]
        public string MontoFormateado =>
            $"${monto:N2}";
    }
}
