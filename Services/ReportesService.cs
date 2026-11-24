using System.Globalization;
using System.Net.Http.Json;
using addFloreriaG5.Models;
using addFloreriaG5.Views;

namespace addFloreriaG5.Services;

public class DatosReportes
{
    public List<PedidoModel> Pedidos { get; init; } = new();
    public List<PagoModel> Pagos { get; init; } = new();
    public List<DetalleEntregaConsulta> Detalles { get; init; } = new();
    public List<ArregloModel> Arreglos { get; init; } = new();
    public List<EntregaModel> Entregas { get; init; } = new();
}

public class ReportesService
{
    private readonly SupabaseService servicio = new();
    private HttpClient Cliente => servicio.GetClient();
    private string Url => servicio.GetUrl().TrimEnd('/');

    public async Task<bool> EsAdministradorAsync()
    {
        if (!SesionAuth.TieneSesion) return false;
        using var userResponse = await Cliente.GetAsync($"{Url}/auth/v1/user");
        userResponse.EnsureSuccessStatusCode();
        var user = await userResponse.Content.ReadFromJsonAsync<AuthUser>();
        if (user == null || user.Id == Guid.Empty) return false;
        var perfil = await new AuthService().ObtenerPerfilAsync(SesionAuth.AccessToken, user.Id);
        return perfil != null && perfil.estado && perfil.id_rol == 1;
    }

    public async Task<DatosReportes> CargarAsync(DateTime desde, DateTime hasta)
    {
        if (!SesionAuth.TieneSesion) throw new InvalidOperationException("La sesión ha vencido. Inicia sesión nuevamente.");
        // Los límites representan días de Ecuador continental (UTC-5), con fin exclusivo.
        var inicio = new DateTimeOffset(desde.Date, TimeSpan.FromHours(-5)).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        var fin = new DateTimeOffset(hasta.Date.AddDays(1), TimeSpan.FromHours(-5)).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        string Filtro(string campo) => $"&{campo}=gte.{Uri.EscapeDataString(inicio)}&{campo}=lt.{Uri.EscapeDataString(fin)}";
        var pedidos = await ObtenerTodosAsync<PedidoModel>("pedidos?select=id_pedido,fecha_pedido,total_original,total,estado&order=id_pedido" + Filtro("fecha_pedido"));
        var pagos = await ObtenerTodosAsync<PagoModel>("pagos?select=id_pago,id_pedido,monto,metodo_pago,fecha_pago,estado&order=id_pago" + Filtro("fecha_pago"));
        var detalles = await ObtenerTodosAsync<DetalleEntregaConsulta>("detalle_pedido?select=id_pedido,id_arreglo,cantidad,precio_unitario&order=id_pedido,id_arreglo");
        var arreglos = await ObtenerTodosAsync<ArregloModel>("arreglos?select=id_arreglo,nombre,stock,estado&order=id_arreglo");
        var entregas = await ObtenerTodosAsync<EntregaModel>("entregas?select=id_entrega,id_pedido,fecha_entrega,estado&order=id_entrega");
        return new DatosReportes { Pedidos = pedidos, Pagos = pagos, Detalles = detalles, Arreglos = arreglos, Entregas = entregas };
    }

    private async Task<List<T>> ObtenerTodosAsync<T>(string ruta)
    {
        var resultado = new List<T>();
        const int pagina = 500;
        for (int inicio = 0; ; )
        {
            if (!SesionAuth.TieneSesion) throw new InvalidOperationException("La sesión ha vencido. Inicia sesión nuevamente.");
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, $"{Url}/rest/v1/{ruta}");
            solicitud.Headers.TryAddWithoutValidation("Range-Unit", "items");
            solicitud.Headers.TryAddWithoutValidation("Range", $"{inicio}-{inicio + pagina - 1}");
            solicitud.Headers.TryAddWithoutValidation("Prefer", "count=exact");
            using var respuesta = await Cliente.SendAsync(solicitud);
            respuesta.EnsureSuccessStatusCode();
            var filas = await respuesta.Content.ReadFromJsonAsync<List<T>>() ?? throw new InvalidOperationException("Respuesta de datos inválida.");
            long? total = respuesta.Content.Headers.ContentRange?.Length;
            // PostgREST puede devolver Content-Range como "0-499/1000", sin unidad.
            // Algunos manejadores HTTP no interpretan ese formato automáticamente.
            if (!total.HasValue && respuesta.Content.Headers.TryGetValues("Content-Range", out var rangos))
            {
                var parteTotal = rangos.FirstOrDefault()?.Split('/').LastOrDefault();
                if (long.TryParse(parteTotal, NumberStyles.None, CultureInfo.InvariantCulture, out var cantidad))
                    total = cantidad;
            }
            if (!total.HasValue) throw new InvalidOperationException("No se pudo confirmar que los datos estén completos.");
            resultado.AddRange(filas);
            if (resultado.Count >= total.Value) return resultado;
            if (filas.Count == 0) throw new InvalidOperationException("No se pudieron cargar todos los datos.");
            inicio += filas.Count;
        }
    }
}
