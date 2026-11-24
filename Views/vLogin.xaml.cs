namespace addFloreriaG5.Views;
using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text.Json;

public partial class vLogin : ContentPage
{
	public vLogin()
	{
		InitializeComponent();
	}


    private async void Ingresar_Clicked(
        object sender,
        EventArgs e)
    {
        string correo = txtCorreo.Text?.Trim() ?? "";
        string password = txtPassword.Text ?? "";

        // VALIDAR CAMPOS
        if (string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(password))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Campos obligatorios",
                "Ingresa tu correo y contraseña.",
                "advertencia");
            return;
        }

        try
        {
            AuthService authService = new AuthService();

            // 1. VALIDAR CREDENCIALES EN SUPABASE AUTH
            AuthSession sesion =
                await authService.IniciarSesionAsync(
                    correo,
                    password);

            // 2. CONSULTAR EL PERFIL DEL USUARIO
            PerfilUsuario? perfil =
                await authService.ObtenerPerfilAsync(
                    sesion.AccessToken,
                    sesion.User!.Id);

            if (perfil == null)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Acceso denegado",
                    "Tu cuenta no tiene un perfil registrado.",
                    "error");
                return;
            }

            // 3. VERIFICAR ESTADO
            if (!perfil.estado)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Cuenta inactiva",
                    "Comunícate con el administrador.",
                    "advertencia");
                return;
            }

            // 4. VALIDAR ROLES PERMITIDOS
            if (perfil.id_rol != 1 &&
                perfil.id_rol != 3)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Acceso denegado",
                    "Tu usuario no tiene un rol autorizado.",
                    "error");
                return;
            }

            // 5. GUARDAR SESIÓN
            SesionAuth.GuardarSesion(sesion);

            SesionUsuario.Nombre = perfil.nombres;
            SesionUsuario.Telefono = perfil.telefono;

            // 6. ABRIR MENÚ CORRESPONDIENTE
            if (perfil.id_rol == 1)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Bienvenido",
                    $"Hola, {perfil.nombres}.",
                    "exito");

                await Navigation.PushAsync(
                    new vMenuAdministrador(
                        perfil.nombres));
            }
            else if (perfil.id_rol == 3)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Bienvenido",
                    $"Hola, {perfil.nombres}.",
                    "exito");

                await Navigation.PushAsync(
                    new vMenuCliente(
                        perfil.nombres));
            }

            // No conservar la contraseña en el formulario
            txtPassword.Text = string.Empty;
        }
        catch (InvalidOperationException)
        {
            await MensajeModal.MostrarAsync(
                this,
                "No se pudo iniciar sesión",
                "Verifica tu correo, contraseña y confirmación de cuenta.",
                "error");
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error de conexión",
                "No fue posible conectar con Supabase.",
                "error");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error",
                "Ocurrió un problema al iniciar sesión.",
                "error");
        }
    }


    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRegistro());
    }

    private async void RecuperarPassword_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new vRecuperarPassword(txtCorreo.Text?.Trim() ?? ""));
    }

    private void TogglePassword_Clicked(
        object sender,
        EventArgs e)
    {
        txtPassword.IsPassword =
            !txtPassword.IsPassword;
    }


    private void txtCorreo_Completed(object sender, EventArgs e)
    {
        txtPassword.Focus();
    }


}