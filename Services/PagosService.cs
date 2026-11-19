
using addFloreriaG5.Services;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace addFloreriaG5.Services;

public class ResultadoPago
{
    [JsonPropertyName("id_pago")]
    public int IdPago { get; set; }

    [JsonPropertyName("saldo_restante")]
    public decimal SaldoRestante { get; set; }

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = "";

    [JsonPropertyName("entrega_creada")]
    public bool EntregaCreada { get; set; }

    [JsonPropertyName("ya_registrado")]
    public bool YaRegistrado { get; set; }
}

public class PagosService
{
    public async Task<ResultadoPago> RegistrarAbonoAsync(
        int idPedido,
        decimal monto,
        string metodoPago,
        Guid solicitudId)
    {
        if (!SesionAuth.TieneSesion)
            throw new InvalidOperationException(
                "La sesión ha vencido.");

        var servicio = new SupabaseService();

        var datos = new
        {
            p_id_pedido = idPedido,
            p_monto = monto,
            p_metodo_pago = metodoPago,
            p_solicitud_id = solicitudId
        };

        string endpoint =
            $"{servicio.GetUrl()}/rest/v1/rpc/registrar_abono_nova";

        using var respuesta = await servicio
            .GetClient()
            .PostAsJsonAsync(endpoint, datos);

        if (!respuesta.IsSuccessStatusCode)
        {
            string detalle =
                await respuesta.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"No se pudo registrar el abono: {detalle}");
        }

        return await respuesta.Content
            .ReadFromJsonAsync<ResultadoPago>()
            ?? throw new InvalidOperationException(
                "Supabase no devolvió un resultado válido.");
    }
}
