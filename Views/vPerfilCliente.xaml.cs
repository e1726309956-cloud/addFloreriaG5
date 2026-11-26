using addFloreriaG5.Models;
using addFloreriaG5.Services;

namespace addFloreriaG5.Views;

public partial class vPerfilCliente : ContentPage
{
    private readonly PerfilService servicio = new();
    private PerfilUsuario? perfil;
    private bool ocupado;

    public vPerfilCliente()
    {
        InitializeComponent();
    }

    private async void Recargar_Clicked(object? sender, EventArgs e) => await CargarAsync();
    private void Telefono_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var texto = e.NewTextValue ?? "";
        var digitos = new string(texto.Where(c => c >= '0' && c <= '9').ToArray());
        if (texto != digitos) telefono.Text = digitos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (perfil == null) await CargarAsync();
    }

    private async Task CargarAsync()
    {
        if (ocupado) return;
        ocupado = true;
        guardar.IsEnabled = false;
        estado.Text = "Cargando perfil…";
        try
        {
            perfil = await servicio.ObtenerAsync();
            MostrarPerfil(perfil);
            estado.Text = "";
        }
        catch (Exception ex)
        {
            perfil = null;
            System.Diagnostics.Debug.WriteLine(ex);
            estado.Text = ex is InvalidOperationException ? ex.Message : "No se pudo cargar el perfil. Revisa la conexión y vuelve a intentarlo.";
        }
        finally { ocupado = false; guardar.IsEnabled = perfil != null; }
    }

    private void MostrarPerfil(PerfilUsuario datos)
    {
        nombres.Text = datos.nombres;
        apellidos.Text = datos.apellidos;
        telefono.Text = datos.telefono;
        correo.Text = datos.correo;
    }

    private async void Guardar_Clicked(object? sender, EventArgs e)
    {
        if (ocupado || perfil == null) return;
        string nombre = nombres.Text?.Trim() ?? "";
        string apellido = apellidos.Text?.Trim() ?? "";
        string numero = telefono.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido) || string.IsNullOrWhiteSpace(numero))
        {
            estado.Text = "Completa los nombres, apellidos y teléfono.";
            return;
        }
        if (numero.Any(c => c < '0' || c > '9'))
        {
            estado.Text = "El teléfono debe contener únicamente números.";
            return;
        }
        ocupado = true;
        guardar.IsEnabled = false;
        estado.Text = "Guardando cambios…";
        try
        {
            perfil = await servicio.ActualizarAsync(perfil, nombre, apellido, numero);
            MostrarPerfil(perfil);
            SesionUsuario.Nombre = perfil.nombres;
            SesionUsuario.Telefono = perfil.telefono;
            estado.Text = "Tus datos se actualizaron correctamente.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            estado.Text = ex is InvalidOperationException ? ex.Message : "No se pudieron guardar los cambios. Revisa la conexión y los permisos de tu cuenta.";
        }
        finally { ocupado = false; guardar.IsEnabled = perfil != null; }
    }
}
