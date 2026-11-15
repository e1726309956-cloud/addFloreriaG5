namespace addFloreriaG5.Views;

using System.Text;
using addFloreriaG5.Services;

public partial class vRegistro : ContentPage
{
    public vRegistro()
    {
        InitializeComponent();
    }


    private async void Registro_Clicked(object sender, EventArgs e)
    {
        string nombres =
            txtNombres.Text?.Trim() ?? "";

        string apellidos =
            txtApellidos.Text?.Trim() ?? "";

        string correo =
            txtCorreo.Text?.Trim() ?? "";

        string telefono =
            txtTelefono.Text?.Trim() ?? "";

        string password =
            txtPassword.Text ?? "";

        string confirmarPassword =
            txtConfirmPassword.Text ?? "";

        // 1. CAMPOS OBLIGATORIOS
        if (string.IsNullOrWhiteSpace(nombres) ||
            string.IsNullOrWhiteSpace(apellidos) ||
            string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(telefono) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmarPassword))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Campos obligatorios",
                "Completa todos los campos para registrarte.",
                "advertencia");

            return;
        }

        // 2. VALIDAR CORREO
        if (!System.Net.Mail.MailAddress.TryCreate(
                correo,
                out var direccion) ||
            !string.Equals(
                direccion.Address,
                correo,
                StringComparison.OrdinalIgnoreCase))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Correo incorrecto",
                "Ingresa un correo electrónico válido.",
                "advertencia");

            return;
        }

        // 3. VALIDAR CONTRASEÑAS
        if (password != confirmarPassword)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Contraseñas diferentes",
                "Las contraseñas no coinciden.",
                "advertencia");

            return;
        }

        if (password.Length < 8)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Contraseña insegura",
                "Utiliza una contraseña de al menos 8 caracteres.",
                "advertencia");

            return;
        }

        // 4. REGISTRAR EN SUPABASE AUTH
        try
        {
            AuthService authService = new AuthService();

            await authService.RegistrarClienteAsync(
                nombres,
                apellidos,
                correo,
                telefono,
                password);

            // Limpiar contraseñas
            txtPassword.Text = string.Empty;
            txtConfirmPassword.Text = string.Empty;

            // 5. MOSTRAR MENSAJE
            await MensajeModal.MostrarAsync(
                this,
                "Registro recibido",
                "Revisa tu correo para confirmar tu cuenta si se solicita. Después podrás iniciar sesión.",
                "exito");

            // 6. REGRESAR AL LOGIN
            await Navigation.PopAsync();
        }
        catch (InvalidOperationException)
        {
            await MensajeModal.MostrarAsync(
                this,
                "No se pudo registrar",
                "Verifica la información e inténtalo nuevamente.",
                "error");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error de registro",
                "Ocurrió un problema al crear la cuenta.",
                "error");
        }
    }


    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void TogglePassword_Clicked(object sender, EventArgs e)
    {
        txtPassword.IsPassword = !txtPassword.IsPassword;
    }

    private void ToggleConfirmPassword_Clicked(object sender, EventArgs e)
    {
        txtConfirmPassword.IsPassword = !txtConfirmPassword.IsPassword;
    }

    private void txtNombres_Completed(object sender, EventArgs e)
    {
        txtApellidos.Focus();
    }

    private void txtApellidos_Completed(object sender, EventArgs e)
    {
        txtCorreo.Focus();
    }

    private void txtCorreo_Completed(object sender, EventArgs e)
    {
        txtTelefono.Focus();
    }

    private void txtTelefono_Completed(object sender, EventArgs e)
    {
        txtPassword.Focus();
    }

    private void txtPassword_Completed(object sender, EventArgs e)
    {
        txtConfirmPassword.Focus();
    }
}