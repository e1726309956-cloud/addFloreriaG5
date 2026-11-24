
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using addFloreriaG5.Configuration;

namespace addFloreriaG5.Services;

public class AuthService
{
    private readonly HttpClient _http = new();

    public async Task SolicitarRecuperacionAsync(string correo)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseSettings.Url}/auth/v1/recover");
        request.Headers.Add("apikey", SupabaseSettings.PublishableKey);
        request.Content = JsonContent.Create(new { email = correo.Trim() });
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var estado = (int)response.StatusCode;
            if (estado == 429)
                throw new InvalidOperationException("Se alcanzó el límite de solicitudes. Espera unos minutos antes de intentarlo de nuevo.");
            if (estado >= 500)
                throw new InvalidOperationException($"Supabase no pudo procesar el envío (código {estado}). Revisa la configuración SMTP y los registros de autenticación en Supabase.");
            throw new InvalidOperationException($"Supabase rechazó la solicitud (código {estado}). Revisa la configuración de autenticación y los registros en Supabase.");
        }
    }

    public async Task<string> VerificarRecuperacionAsync(string correo, string codigo)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseSettings.Url}/auth/v1/verify");
        request.Headers.Add("apikey", SupabaseSettings.PublishableKey);
        request.Content = JsonContent.Create(new { email = correo.Trim(), token = codigo.Trim(), type = "recovery" });
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("El código no es válido o ha vencido. Solicita uno nuevo.");
        var sesion = await response.Content.ReadFromJsonAsync<AuthSession>();
        return !string.IsNullOrWhiteSpace(sesion?.AccessToken) ? sesion.AccessToken :
            throw new InvalidOperationException("No se pudo verificar el código de recuperación.");
    }

    public async Task CambiarPasswordAsync(string token, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{SupabaseSettings.Url}/auth/v1/user");
        request.Headers.Add("apikey", SupabaseSettings.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { password });
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("No se pudo actualizar la contraseña. Revisa los requisitos de seguridad o solicita un código nuevo.");
    }

    // AUTENTICAR EN SUPABASE AUTH
    public async Task<AuthSession> IniciarSesionAsync(
        string correo,
        string password)
    {
        string endpoint =
            $"{SupabaseSettings.Url}/auth/v1/token?grant_type=password";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        request.Headers.Add(
            "apikey",
            SupabaseSettings.PublishableKey);

        request.Content = JsonContent.Create(new
        {
            email = correo.Trim(),
            password = password
        });

        using var response =
            await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "No se pudo iniciar sesión. Verifica el correo, la contraseña y la confirmación de la cuenta.");
        }

        var sesion = await response.Content
            .ReadFromJsonAsync<AuthSession>();

        if (sesion == null ||
            sesion.User == null ||
            string.IsNullOrWhiteSpace(sesion.AccessToken))
        {
            throw new InvalidOperationException(
                "No se recibió una sesión válida.");
        }

        return sesion;
    }

    // CONSULTAR EL PERFIL DEL USUARIO
    public async Task<PerfilUsuario?> ObtenerPerfilAsync(
        string accessToken,
        Guid authUserId)
    {
        string endpoint =
            $"{SupabaseSettings.Url}/rest/v1/usuarios" +
            "?select=id_usuario,auth_user_id,nombres,apellidos,correo,telefono,id_rol,estado" +
            $"&auth_user_id=eq.{authUserId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            endpoint);

        request.Headers.Add(
            "apikey",
            SupabaseSettings.PublishableKey);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using var response =
            await _http.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var perfiles = await response.Content
            .ReadFromJsonAsync<List<PerfilUsuario>>();

        return perfiles?.FirstOrDefault();
    }


    public async Task RegistrarClienteAsync(
        string nombres,
        string apellidos,
        string correo,
        string telefono,
        string password)
    {
        string endpoint =
            $"{SupabaseSettings.Url}/auth/v1/signup";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            endpoint);

        request.Headers.Add(
            "apikey",
            SupabaseSettings.PublishableKey);

        // Enviar datos y contraseña en el BODY
        request.Content = JsonContent.Create(new
        {
            email = correo.Trim(),
            password = password,
            data = new
            {
                nombres = nombres.Trim(),
                apellidos = apellidos.Trim(),
                telefono = telefono.Trim()
            }
        });

        using var response =
            await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string detalle =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"Error registro Auth: {(int)response.StatusCode} - {detalle}");

            throw new InvalidOperationException(
                "No fue posible registrar la cuenta. Revisa los datos e inténtalo nuevamente.");
        }
    }

}

// RESPUESTA DE AUTENTICACIÓN
public class AuthSession
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("user")]
    public AuthUser? User { get; set; }
}

public class AuthUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";
}

// PERFIL DE LA TABLA public.usuarios
public class PerfilUsuario
{
    public int id_usuario { get; set; }
    public Guid? auth_user_id { get; set; }
    public string nombres { get; set; } = "";
    public string apellidos { get; set; } = "";
    public string correo { get; set; } = "";
    public string telefono { get; set; } = "";
    public int id_rol { get; set; }
    public bool estado { get; set; }
}
