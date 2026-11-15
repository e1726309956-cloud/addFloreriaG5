
using addFloreriaG5.Configuration;
using System.Net.Http.Headers;

namespace addFloreriaG5.Services;

public class SupabaseService
{
    private readonly HttpClient client;

    public SupabaseService()
    {
        client = new HttpClient();

        // CLAVE PÚBLICA DEL NUEVO PROYECTO
        client.DefaultRequestHeaders.Add(
            "apikey",
            SupabaseSettings.PublishableKey);

        // TOKEN DEL USUARIO AUTENTICADO
        if (SesionAuth.TieneSesion)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    SesionAuth.AccessToken);
        }
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
