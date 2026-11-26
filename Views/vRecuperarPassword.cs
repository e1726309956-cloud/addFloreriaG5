using System.Net.Mail;
using addFloreriaG5.Services;
namespace addFloreriaG5.Views;
public class vRecuperarPassword : ContentPage
{
    private readonly Entry correo = new() { Placeholder = "Correo electrónico", Keyboard = Keyboard.Email };
    private readonly Button enviar = new() { Text = "Enviar enlace" };
    private readonly Entry[] digitos = new Entry[8];
    private bool actualizandoCodigo;
    private readonly Entry clave = new() { Placeholder = "Nueva contraseña", IsPassword = true };
    private readonly Entry confirmar = new() { Placeholder = "Confirmar contraseña", IsPassword = true };
    private readonly Button cambiar = new() { Text = "Cambiar contraseña", IsEnabled = false };
    private readonly Button verificar = new() { Text = "Verificar código", IsEnabled = false };
    private readonly AuthService auth = new();
    private string? correoSolicitado;
    private string? token;
    public vRecuperarPassword(string correoInicial)
    {
        Title = "Recuperar contraseña";
        BackgroundColor = Color.FromArgb("#FFF9F6");
        foreach (var campo in new[] { correo, clave, confirmar })
        {
            campo.TextColor = Color.FromArgb("#35232A");
            campo.PlaceholderColor = Color.FromArgb("#765B63");
            campo.BackgroundColor = Colors.Transparent;
            campo.HeightRequest = 50;
        }
        foreach (var boton in new[] { enviar, verificar, cambiar })
        {
            boton.BackgroundColor = Color.FromArgb("#812A44");
            boton.TextColor = Colors.White;
            boton.CornerRadius = 12;
            boton.HeightRequest = 50;
        }
        correo.Text = correoInicial;
        enviar.Text = "Enviar código";
        enviar.Clicked += Enviar_Clicked;
        verificar.Clicked += Verificar_Clicked;
        cambiar.Clicked += Cambiar_Clicked;
        clave.IsEnabled = confirmar.IsEnabled = false;
        var codigo = CrearCasillasCodigo();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 16, MaximumWidthRequest = 450, Children = {
            new Label { Text = "Recuperar contraseña", FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#7D273E") },
            new Label { Text = "Solicita un código y consulta tu correo desde cualquier dispositivo.", TextColor = Color.FromArgb("#35232A") }, Campo(correo), enviar,
            new Label { Text = "Código de verificación (8 dígitos)", TextColor = Color.FromArgb("#35232A") }, codigo, verificar,
            Campo(clave), new Label { Text = ValidadorPassword.Requisitos, FontSize = 12, TextColor = Color.FromArgb("#68454D") }, Campo(confirmar), cambiar } } };
    }

    private Grid CrearCasillasCodigo()
    {
        var fila = new Grid { ColumnSpacing = 4 };
        for (int i = 0; i < digitos.Length; i++)
        {
            fila.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var indice = i;
            var entrada = new Entry { Keyboard = Keyboard.Numeric, HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.FromArgb("#35232A"), BackgroundColor = Colors.Transparent, HeightRequest = 50, FontSize = 18 };
            SemanticProperties.SetDescription(entrada, $"Dígito {i + 1} de 8 del código de verificación");
            digitos[i] = entrada;
            entrada.TextChanged += (_, e) => CodigoCambiado(indice, e.OldTextValue, e.NewTextValue);
            var borde = Campo(entrada);
            borde.Padding = 0;
            Grid.SetColumn(borde, i);
            fila.Children.Add(borde);
        }
        return fila;
    }

    private void CodigoCambiado(int indice, string? anterior, string? texto)
    {
        if (actualizandoCodigo) return;
        var numeros = new string((texto ?? "").Where(c => c >= '0' && c <= '9').ToArray());
        if (anterior?.Length == 1 && numeros.Length == 2 && texto?.Length == 2 && texto.StartsWith(anterior, StringComparison.Ordinal))
            numeros = numeros[1..];
        actualizandoCodigo = true;
        try
        {
            if (numeros.Length == 0) digitos[indice].Text = "";
            else
            {
                // Al pegar varios dígitos se distribuyen desde la casilla actual.
                for (int i = 0; i < numeros.Length && indice + i < digitos.Length; i++)
                    digitos[indice + i].Text = numeros[i].ToString();
            }
        }
        finally { actualizandoCodigo = false; }
        if (numeros.Length > 0 && indice + numeros.Length < digitos.Length)
            digitos[indice + numeros.Length].Focus();
    }

    private void LimpiarCodigo()
    {
        actualizandoCodigo = true;
        try { foreach (var digito in digitos) digito.Text = ""; }
        finally { actualizandoCodigo = false; }
    }

    private static Border Campo(Entry entrada) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#9B6574"),
        StrokeThickness = 1.5,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
        Padding = new Thickness(12, 0),
        Content = entrada
    };
    private async void Enviar_Clicked(object? sender, EventArgs e)
    {
        var direccion = correo.Text?.Trim() ?? "";
        if (!MailAddress.TryCreate(direccion, out var valida) || valida.Address != direccion)
        {
            await DisplayAlertAsync("Correo inválido", "Ingresa un correo electrónico válido.", "Aceptar"); return;
        }
        enviar.IsEnabled = false;
        verificar.IsEnabled = cambiar.IsEnabled = clave.IsEnabled = confirmar.IsEnabled = false;
        correoSolicitado = null;
        token = null;
        correo.IsEnabled = true;
        foreach (var digito in digitos) digito.IsEnabled = true;
        clave.Text = confirmar.Text = string.Empty;
        LimpiarCodigo();
        try {
            await auth.SolicitarRecuperacionAsync(direccion);
            correoSolicitado = direccion;
            verificar.IsEnabled = true;
            await DisplayAlertAsync("Revisa tu correo", "Si existe una cuenta con ese correo, recibirás un código de recuperación.", "Aceptar");
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("Recuperación no disponible", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
            await DisplayAlertAsync("No se pudo enviar", "Comprueba tu conexión o espera antes de volver a intentarlo.", "Aceptar");
        }
        finally { enviar.IsEnabled = true; }
    }

    private async void Verificar_Clicked(object? sender, EventArgs e)
    {
        if (correoSolicitado == null || !string.Equals(correo.Text?.Trim(), correoSolicitado, StringComparison.OrdinalIgnoreCase))
        { await DisplayAlertAsync("Correo modificado", "Solicita un código para este correo.", "Aceptar"); return; }
        if (digitos.Any(d => string.IsNullOrEmpty(d.Text)))
        { await DisplayAlertAsync("Código incompleto", "Ingresa los 8 dígitos recibidos por correo.", "Aceptar"); return; }
        verificar.IsEnabled = enviar.IsEnabled = false;
        try
        {
            token = await auth.VerificarRecuperacionAsync(correoSolicitado, string.Concat(digitos.Select(d => d.Text)));
            clave.IsEnabled = confirmar.IsEnabled = cambiar.IsEnabled = true;
            foreach (var digito in digitos) digito.IsEnabled = false;
            correo.IsEnabled = false;
            await DisplayAlertAsync("Código verificado", "Ingresa y confirma tu nueva contraseña.", "Aceptar");
            clave.Focus();
        }
        catch (InvalidOperationException ex) { await DisplayAlertAsync("Código no válido", ex.Message, "Aceptar"); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        { await DisplayAlertAsync("Error de conexión", "Comprueba tu conexión e inténtalo nuevamente.", "Aceptar"); }
        finally { enviar.IsEnabled = true; verificar.IsEnabled = token == null; }
    }

    private async void Cambiar_Clicked(object? sender, EventArgs e)
    {
        if (correoSolicitado == null || !string.Equals(correo.Text?.Trim(), correoSolicitado, StringComparison.OrdinalIgnoreCase)) {
            await DisplayAlertAsync("Correo modificado", "Solicita un código para este correo.", "Aceptar"); return;
        }
        if (token == null) {
            await DisplayAlertAsync("Código pendiente", "Verifica primero el código recibido por correo.", "Aceptar"); return;
        }
        if (!ValidadorPassword.EsValida(clave.Text)) {
            await DisplayAlertAsync("Contraseña inválida", ValidadorPassword.Requisitos, "Aceptar"); return;
        }
        if (clave.Text != confirmar.Text) {
            await DisplayAlertAsync("Contraseñas diferentes", "Las contraseñas deben coincidir.", "Aceptar"); return;
        }
        enviar.IsEnabled = cambiar.IsEnabled = false;
        try {
            await auth.CambiarPasswordAsync(token, clave.Text);
            token = null; LimpiarCodigo(); clave.Text = confirmar.Text = string.Empty;
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
