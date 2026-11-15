
using addFloreriaG5.Configuration;

namespace addFloreriaG5.Services;

public class SupabaseService
{
    private readonly HttpClient client;

    public SupabaseService()
    {
        // Obtener configuración centralizada
        string apiKey = SupabaseSettings.PublishableKey;

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(SupabaseSettings.Url))
        {
            throw new InvalidOperationException(
                "La configuración de Supabase no está inicializada.");
        }

        client = new HttpClient();

        // Clave pública del proyecto
        client.DefaultRequestHeaders.Add(
            "apikey",
            apiKey);
    }

    public HttpClient GetClient()
    {
        return client;
    }

    public string GetUrl()
    {
        return SupabaseSettings.Url;
    }
}
