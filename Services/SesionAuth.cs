
using addFloreriaG5.Models;

namespace addFloreriaG5.Services;

public static class SesionAuth
{
    public static string AccessToken { get; private set; }
        = string.Empty;

    public static string RefreshToken { get; private set; }
        = string.Empty;

    public static DateTimeOffset ExpiraEn { get; private set; }

    public static bool TieneSesion =>
        !string.IsNullOrWhiteSpace(AccessToken) &&
        DateTimeOffset.UtcNow < ExpiraEn;

    public static void GuardarSesion(AuthSession sesion)
    {
        AccessToken = sesion.AccessToken;
        RefreshToken = sesion.RefreshToken;

        ExpiraEn = DateTimeOffset.UtcNow.AddSeconds(
            Math.Max(0, sesion.ExpiresIn - 60));
    }

    public static void CerrarSesionLocal()
    {
        AccessToken = string.Empty;
        RefreshToken = string.Empty;
        ExpiraEn = default;
        SesionUsuario.Nombre = string.Empty;
        SesionUsuario.Telefono = string.Empty;
        SesionUsuario.IdUsuario = 0;
        CarritoCompras.Limpiar();
    }
}
