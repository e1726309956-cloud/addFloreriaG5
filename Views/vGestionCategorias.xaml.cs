
using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

namespace addFloreriaG5.Views;

public partial class vGestionCategorias : ContentPage
{
    private int? idCategoriaEdicion = null;
    private bool ocupada = false;

    public vGestionCategorias()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCategoriasAsync();
    }

    // LISTAR CATEGORÍAS
    private async Task CargarCategoriasAsync()
    {
        try
        {
            indicadorCarga.IsVisible = true;
            indicadorCarga.IsRunning = true;

            var servicio = new SupabaseService();

            string url = $"{servicio.GetUrl()}/rest/v1/categorias" +
                         "?select=id_categoria,nombre&order=nombre.asc";

            using var respuesta = await servicio
                .GetClient()
                .GetAsync(url);

            if (!respuesta.IsSuccessStatusCode)
                throw new Exception(
                    await respuesta.Content.ReadAsStringAsync());

            string json = await respuesta.Content.ReadAsStringAsync();

            var categorias = JsonSerializer.Deserialize<List<CategoriaModel>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new();

            cvCategorias.ItemsSource = categorias;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error",
                "No se pudieron cargar las categorías.",
                "error");
        }
        finally
        {
            indicadorCarga.IsRunning = false;
            indicadorCarga.IsVisible = false;
        }
    }

    // CREAR O ACTUALIZAR
    private async void Guardar_Clicked(
        object sender,
        EventArgs e)
    {
        if (ocupada)
            return;

        string nombre = txtNombreCategoria.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Campo obligatorio",
                "Ingresa el nombre de la categoría.",
                "advertencia");
            return;
        }

        ocupada = true;
        btnGuardar.IsEnabled = false;

        try
        {
            var servicio = new SupabaseService();
            var cliente = servicio.GetClient();

            var datos = new { nombre };
            string json = JsonSerializer.Serialize(datos);

            using var contenido = new StringContent(
                json, Encoding.UTF8, "application/json");

            bool editando = idCategoriaEdicion.HasValue;

            string url = editando
                ? $"{servicio.GetUrl()}/rest/v1/categorias" +
                  $"?id_categoria=eq.{idCategoriaEdicion.Value}"
                : $"{servicio.GetUrl()}/rest/v1/categorias";

            using var solicitud = new HttpRequestMessage(
                editando ? HttpMethod.Patch : HttpMethod.Post,
                url)
            {
                Content = contenido
            };

            solicitud.Headers.Add(
                "Prefer", "return=representation");

            using var respuesta = await cliente.SendAsync(solicitud);

            if (!respuesta.IsSuccessStatusCode)
            {
                string error = await respuesta.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine(error);

                await MensajeModal.MostrarAsync(
                    this,
                    "Error",
                    "No se pudo guardar la categoría.",
                    "error");
                return;
            }

            string resultado = await respuesta.Content.ReadAsStringAsync();

            // PATCH podría responder correctamente sin modificar filas.
            // Verificar que Supabase devuelva la categoría.
            var registros = JsonSerializer.Deserialize<List<CategoriaModel>>(
                resultado,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (registros == null || registros.Count == 0)
                throw new InvalidOperationException(
                    "La operación no modificó ninguna categoría.");

            await MensajeModal.MostrarAsync(
                this,
                "Operación exitosa",
                editando
                    ? "Categoría actualizada correctamente."
                    : "Categoría registrada correctamente.",
                "exito");

            LimpiarFormulario();
            await CargarCategoriasAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await MensajeModal.MostrarAsync(
                this,
                "Error",
                "No fue posible completar la operación.",
                "error");
        }
        finally
        {
            ocupada = false;
            btnGuardar.IsEnabled = true;
        }
    }

    // SELECCIONAR CATEGORÍA PARA EDITAR
    private void Editar_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button boton ||
            boton.CommandParameter is not CategoriaModel categoria)
            return;

        idCategoriaEdicion = categoria.id_categoria;
        txtNombreCategoria.Text = categoria.nombre;

        lblTituloFormulario.Text = "Editar categoría";
        btnGuardar.Text = "Guardar cambios";
        btnCancelar.IsVisible = true;

        txtNombreCategoria.Focus();
    }

    // CANCELAR EDICIÓN
    private void Cancelar_Clicked(
        object sender,
        EventArgs e)
    {
        LimpiarFormulario();
    }

    private void LimpiarFormulario()
    {
        idCategoriaEdicion = null;
        txtNombreCategoria.Text = string.Empty;

        lblTituloFormulario.Text = "Nueva categoría";
        btnGuardar.Text = "Guardar categoría";
        btnCancelar.IsVisible = false;
    }

    // VOLVER
    private async void Volver_Tapped(
        object sender,
        TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}
