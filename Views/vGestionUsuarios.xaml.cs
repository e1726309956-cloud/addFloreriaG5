using System.Net.Http.Json;
using addFloreriaG5.Models;
using addFloreriaG5.Services;

namespace addFloreriaG5.Views;

public partial class vGestionUsuarios : ContentPage
{
    private List<UsuarioGestionModel> usuarios = new();
    private bool procesando = false;

    public vGestionUsuarios()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarUsuarios();
    }

    // CONSULTAR USUARIOS DESDE SUPABASE
    private async Task CargarUsuarios()
    {
        try
        {
            if (!SesionAuth.TieneSesion)
            {
                await MensajeModal.MostrarAsync(
                    this, "Sesión vencida",
                    "Inicia sesión nuevamente.",
                    "advertencia");
                return;
            }

            var servicio = new SupabaseService();

            string endpoint =
                $"{servicio.GetUrl().TrimEnd('/')}" +
                "/rest/v1/rpc/listar_usuarios_nova";

            using var respuesta =
                await servicio.GetClient().PostAsJsonAsync(
                    endpoint, new { });

            if (!respuesta.IsSuccessStatusCode)
            {
                string error =
                    await respuesta.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(error);

                throw new InvalidOperationException(
                    "No fue posible obtener los usuarios.");
            }

            usuarios = await respuesta.Content
                .ReadFromJsonAsync<List<UsuarioGestionModel>>()
                ?? new();

            FiltrarUsuarios();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this, "Error",
                "No se pudieron cargar los usuarios. " +
                "Verifica la conexión y los permisos.",
                "error");
        }
        finally
        {
            refreshUsuarios.IsRefreshing = false;
        }
    }

    // BUSCAR USUARIOS
    private void Buscar_TextChanged(
        object sender, TextChangedEventArgs e)
    {
        FiltrarUsuarios();
    }


    private void FiltrarUsuarios()
    {
        string buscar = txtBuscar.Text?.Trim() ?? "";

        var filtrados = usuarios
            .Where(u =>
                string.IsNullOrWhiteSpace(buscar) ||

                (u.NombreCompleto ?? "").Contains(
                    buscar, StringComparison.OrdinalIgnoreCase) ||

                (u.correo ?? "").Contains(
                    buscar, StringComparison.OrdinalIgnoreCase) ||

                (u.telefono ?? "").Contains(
                    buscar, StringComparison.OrdinalIgnoreCase))
            .ToList();

        cvUsuarios.ItemsSource = filtrados;
    }


    // ACTIVAR O DESACTIVAR
    private async void CambiarEstado_Clicked(
        object sender, EventArgs e)
    {
        if (procesando) return;

        if (sender is not Button boton ||
            boton.CommandParameter is not UsuarioGestionModel usuario)
            return;

        bool nuevoEstado = !usuario.estado;

        string accion =
            nuevoEstado ? "activar" : "desactivar";

        bool confirmar =
            await MensajeModal.ConfirmarAsync(
                this,
                "Confirmar cambio",
                $"¿Deseas {accion} a {usuario.NombreCompleto}?");

        if (!confirmar) return;

        procesando = true;
        boton.IsEnabled = false;

        try
        {
            var servicio = new SupabaseService();

            string endpoint =
                $"{servicio.GetUrl().TrimEnd('/')}" +
                "/rest/v1/rpc/cambiar_estado_usuario_nova";

            var solicitud = new
            {
                p_id_usuario = usuario.id_usuario,
                p_estado = nuevoEstado
            };

            using var respuesta =
                await servicio.GetClient()
                    .PostAsJsonAsync(endpoint, solicitud);

            if (!respuesta.IsSuccessStatusCode)
            {
                string detalle =
                    await respuesta.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(detalle);

                throw new InvalidOperationException(
                    "No se pudo actualizar el estado.");
            }

            await MensajeModal.MostrarAsync(
                this,
                "Estado actualizado",
                $"La cuenta fue {(nuevoEstado ? "activada" : "desactivada")}.",
                "exito");

            await CargarUsuarios();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error",
                "No se pudo cambiar el estado del usuario.",
                "error");
        }
        finally
        {
            procesando = false;
            boton.IsEnabled = true;
        }
    }

    private async void Refrescar_Refreshing(
        object sender, EventArgs e)
    {
        await CargarUsuarios();
    }

    private async void Volver_Clicked(
        object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
