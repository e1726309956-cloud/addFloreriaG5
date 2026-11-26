using System.Net.Http.Json;

namespace addFloreriaG5.Services;

public class PerfilService
{
    public async Task<PerfilUsuario> ObtenerAsync()
    {
        if (!SesionAuth.TieneSesion)
            throw new InvalidOperationException("La sesión ha vencido. Vuelve a iniciar sesión.");

        var servicio = new SupabaseService();
        using var respuesta = await servicio.GetClient().GetAsync($"{servicio.GetUrl().TrimEnd('/')}/auth/v1/user");
        respuesta.EnsureSuccessStatusCode();
        var usuario = await respuesta.Content.ReadFromJsonAsync<AuthUser>();
        if (usuario == null || usuario.Id == Guid.Empty)
            throw new InvalidOperationException("No se pudo identificar la cuenta.");

        var perfil = await new AuthService().ObtenerPerfilAsync(SesionAuth.AccessToken, usuario.Id);
        if (perfil == null || !perfil.estado || perfil.id_rol != 3)
            throw new InvalidOperationException("No se encontró un perfil de cliente activo.");
        return perfil;
    }

    public async Task<PerfilUsuario> ActualizarAsync(PerfilUsuario perfil, string nombres, string apellidos, string telefono)
    {
        if (!SesionAuth.TieneSesion || !perfil.auth_user_id.HasValue)
            throw new InvalidOperationException("La sesión ha vencido. Vuelve a iniciar sesión.");

        var servicio = new SupabaseService();
        var url = $"{servicio.GetUrl().TrimEnd('/')}/rest/v1/rpc/actualizar_mi_perfil";
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { p_nombres = nombres, p_apellidos = apellidos, p_telefono = telefono })
        };
        using var respuesta = await servicio.GetClient().SendAsync(solicitud);
        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("La actualización de perfil aún no está configurada en Supabase. Contacta al administrador.");
        respuesta.EnsureSuccessStatusCode();
        var perfiles = await respuesta.Content.ReadFromJsonAsync<List<PerfilUsuario>>();
        if (perfiles == null || perfiles.Count != 1)
            throw new InvalidOperationException("No se actualizó el perfil. Comprueba que tu cuenta siga activa.");
        return perfiles[0];
    }
}
