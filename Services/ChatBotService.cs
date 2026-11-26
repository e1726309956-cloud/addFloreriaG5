using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using addFloreriaG5.Configuration;
using addFloreriaG5.Models;

namespace addFloreriaG5.Services;

public sealed class ChatBotException : Exception
{
    public ChatBotException(string mensaje) : base(mensaje)
    {
    }
}

public class ChatBotService
{
    // Un solo cliente HTTP para reutilizar conexiones.
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(100)
    };

    public async Task<string> EnviarAsync(
        string mensaje,
        IReadOnlyList<MensajeChat> historial,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            throw new ChatBotException("Escribe un mensaje para continuar.");

        if (!SesionAuth.TieneSesion)
        {
            throw new ChatBotException(
                "Tu sesión ha vencido. Cierra sesión y vuelve a ingresar.");
        }

        var cuerpo = new
        {
            mensaje = mensaje.Trim(),
            historial = historial
                .Where(m =>
                    !m.EsCargando &&
                    !m.EsError &&
                    !string.IsNullOrWhiteSpace(m.Texto))
                .TakeLast(10)
                .Select(m => new
                {
                    rol = m.EsUsuario ? "usuario" : "asistente",
                    texto = m.Texto
                })
                .ToList()
        };

        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            $"{SupabaseSettings.Url}/functions/v1/chatbot");

        solicitud.Headers.Add(
            "apikey",
            SupabaseSettings.PublishableKey);

        solicitud.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                SesionAuth.AccessToken);

        solicitud.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        solicitud.Content = JsonContent.Create(cuerpo);

        HttpResponseMessage respuesta;

        try
        {
            respuesta = await Http.SendAsync(
                solicitud,
                cancellationToken);
        }
        catch (TaskCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatBotException(
                "El asistente tardó demasiado en responder. Inténtalo de nuevo.");
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            throw new ChatBotException(
                "No pude conectar con el asistente. Revisa tu conexión e inténtalo de nuevo.");
        }

        using (respuesta)
        {
            string contenido = await respuesta.Content
                .ReadAsStringAsync(cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ChatBot HTTP {(int)respuesta.StatusCode}: {contenido}");

                throw new ChatBotException(
                    MensajeParaRespuesta(
                        respuesta.StatusCode,
                        contenido));
            }

            ChatBotRespuesta? resultado;

            try
            {
                resultado = JsonSerializer.Deserialize<ChatBotRespuesta>(
                    contenido,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                throw new ChatBotException(
                    "El asistente devolvió una respuesta no válida. Inténtalo de nuevo.");
            }

            string texto = resultado?.Respuesta?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(texto))
            {
                throw new ChatBotException(
                    "El asistente no devolvió una respuesta. Inténtalo de nuevo.");
            }

            return texto.Length > 4000
                ? texto[..4000]
                : texto;
        }
    }

    private static string MensajeParaRespuesta(
        HttpStatusCode estado,
        string contenido)
    {
        try
        {
            using var documento = JsonDocument.Parse(contenido);

            if (documento.RootElement.ValueKind ==
                    JsonValueKind.Object &&
                documento.RootElement.TryGetProperty(
                    "error",
                    out var error) &&
                error.ValueKind == JsonValueKind.String)
            {
                string? servidor = error.GetString()?.Trim();

                // Los mensajes del servidor son más útiles para diagnosticar
                // la configuración del asistente.
                if (servidor is { Length: > 0 })
                {
                    return servidor.Length <= 600
                        ? servidor
                        : servidor[..600];
                }
            }
        }
        catch (JsonException)
        {
            // La respuesta no era JSON; se usa el mensaje por estado.
        }

        return MensajeParaEstado(estado);
    }

    private static string MensajeParaEstado(HttpStatusCode estado)
    {
        return estado switch
        {
            HttpStatusCode.Unauthorized =>
                "Tu sesión ha vencido. Cierra sesión y vuelve a ingresar.",

            HttpStatusCode.Forbidden =>
                "Tu cuenta no tiene permiso para usar el asistente.",

            HttpStatusCode.NotFound =>
                "El ChatBot no está disponible en este momento. " +
                "Si eres el administrador, revisa la configuración de la función en Supabase.",

            HttpStatusCode.TooManyRequests =>
                "El asistente está recibiendo muchas solicitudes. " +
                "Espera un momento e inténtalo de nuevo.",

            HttpStatusCode.BadRequest =>
                "No pude procesar tu mensaje. Intenta reformularlo.",

            HttpStatusCode.ServiceUnavailable =>
                "El asistente no está configurado en este momento. " +
                "Inténtalo más tarde.",

            _ when (int)estado >= 500 =>
                "El asistente no está disponible en este momento. " +
                "Inténtalo de nuevo más tarde.",

            _ =>
                "No pude comunicarme con el asistente. Inténtalo de nuevo."
        };
    }

    private sealed class ChatBotRespuesta
    {
        [JsonPropertyName("respuesta")]
        public string? Respuesta { get; set; }
    }
}
