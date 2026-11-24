using System.Net.Mail;
using addFloreriaG5.Services;
namespace addFloreriaG5.Views;
public class vRecuperarPassword : ContentPage
{
    private readonly Entry correo = new() { Placeholder = "Correo electrónico", Keyboard = Keyboard.Email };
    private readonly Button enviar = new() { Text = "Enviar enlace" };
    private readonly Entry codigo = new() { Placeholder = "Código del correo", Keyboard = Keyboard.Numeric };
    private readonly Entry clave = new() { Placeholder = "Nueva contraseña", IsPassword = true };
    private readonly Entry confirmar = new() { Placeholder = "Confirmar contraseña", IsPassword = true };
    private readonly Button cambiar = new() { Text = "Cambiar contraseña", IsEnabled = false };
    private readonly AuthService auth = new();
    private string? correoSolicitado;
    private string? token;
    public vRecuperarPassword(string correoInicial)
    {
        Title = "Recuperar contraseña";
        BackgroundColor = Color.FromArgb("#FFF9F6");
        correo.Text = correoInicial;
        enviar.Text = "Enviar código";
        enviar.Clicked += Enviar_Clicked;
        cambiar.Clicked += Cambiar_Clicked;
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 16, MaximumWidthRequest = 450, Children = {
            new Label { Text = "Recuperar contraseña", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#7D273E") },
            new Label { Text = "Solicita un código y consulta tu correo desde cualquier dispositivo." }, correo, enviar,
            codigo, clave, confirmar, cambiar } } };
    }
    private async void Enviar_Clicked(object? sender, EventArgs e)
    {
        var direccion = correo.Text?.Trim() ?? "";
        if (!MailAddress.TryCreate(direccion, out var valida) || valida.Address != direccion)
        {
            await DisplayAlertAsync("Correo inválido", "Ingresa un correo electrónico válido.", "Aceptar"); return;
        }
        enviar.IsEnabled = false;
        cambiar.IsEnabled = false;
        correoSolicitado = null;
        token = null;
        try {
            await auth.SolicitarRecuperacionAsync(direccion);
            correoSolicitado = direccion;
            cambiar.IsEnabled = true;
            await DisplayAlertAsync("Revisa tu correo", "Si existe una cuenta con ese correo, recibirás un código de recuperación.", "Aceptar");
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("Recuperación no disponible", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            await DisplayAlertAsync("No se pudo enviar", "Comprueba tu conexión o espera antes de volver a intentarlo.", "Aceptar");
        }
        finally { enviar.IsEnabled = true; }
    }

    private async void Cambiar_Clicked(object? sender, EventArgs e)
    {
        if (correoSolicitado == null || !string.Equals(correo.Text?.Trim(), correoSolicitado, StringComparison.OrdinalIgnoreCase)) {
            await DisplayAlertAsync("Correo modificado", "Solicita un código para este correo.", "Aceptar"); return;
        }
        if (token == null && string.IsNullOrWhiteSpace(codigo.Text)) {
            await DisplayAlertAsync("Código requerido", "Ingresa el código recibido por correo.", "Aceptar"); return;
        }
        if (string.IsNullOrWhiteSpace(clave.Text) || clave.Text.Length < 8 || clave.Text != confirmar.Text) {
            await DisplayAlertAsync("Contraseña inválida", "Las contraseñas deben coincidir y tener al menos 8 caracteres.", "Aceptar"); return;
        }
        enviar.IsEnabled = cambiar.IsEnabled = false;
        try {
            token ??= await auth.VerificarRecuperacionAsync(correoSolicitado, codigo.Text!);
            await auth.CambiarPasswordAsync(token, clave.Text);
            token = null; codigo.Text = clave.Text = confirmar.Text = string.Empty;
            await DisplayAlertAsync("Contraseña actualizada", "Ya puedes iniciar sesión con tu nueva contraseña.", "Aceptar");
            await Navigation.PopAsync();
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("No se pudo continuar", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            await DisplayAlertAsync("Error de conexión", "Comprueba tu conexión e inténtalo nuevamente.", "Aceptar");
        }
        finally { enviar.IsEnabled = cambiar.IsEnabled = true; }
    }
}
