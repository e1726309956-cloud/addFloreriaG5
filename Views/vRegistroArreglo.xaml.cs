namespace addFloreriaG5.Views;

using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

public partial class vRegistroArreglo : ContentPage
{
    private FileResult? imagenSeleccionada;
    public vRegistroArreglo()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCategoriasEnPicker();
    }

    private async Task CargarCategoriasEnPicker()
    {
        try
        {
            SupabaseService servicio = new SupabaseService();
            string endpoint = $"{servicio.GetUrl()}/rest/v1/categorias?select=*";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);
            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();
                var listaCategorias = JsonSerializer.Deserialize<List<CategoriaModel>>(contenido, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                pickerCategorias.ItemsSource = listaCategorias;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Aviso", "No se pudieron cargar las categorías: " + ex.Message, "Aceptar");
        }
    }


    private async void btnSeleccionarImagen(object sender, EventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una imagen",
                FileTypes = FilePickerFileType.Images
            });

            if (resultado != null)
            {
                imagenSeleccionada = resultado;

                lblRutaArchivo.Text = resultado.FileName;

                imgPrevia.Source = ImageSource.FromStream(
                    () => resultado.OpenReadAsync().GetAwaiter().GetResult());

                txtImagenUrl.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "Aceptar");
        }


    }


    private async void Guardar_Clicked(object sender, EventArgs e)
    {
        // 1. VALIDAR CAMPOS OBLIGATORIOS
        if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtPrecio.Text) ||
            string.IsNullOrWhiteSpace(txtStock.Text) ||
            pickerCategorias.SelectedItem == null)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Campos incompletos",
                "Completa los campos obligatorios y selecciona una categoría.",
                "advertencia");

            return;
        }

        // 2. VALIDAR PRECIO
        if (!decimal.TryParse(
            txtPrecio.Text?.Trim(),
            out decimal precioValidado) || precioValidado <= 0)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Precio inválido",
                "Ingresa un precio válido mayor a cero.",
                "error");

            txtPrecio.Focus();
            return;
        }

        // 3. VALIDAR STOCK
        if (!int.TryParse(
            txtStock.Text?.Trim(),
            out int stockValidado) || stockValidado < 0)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Stock inválido",
                "Ingresa un número entero igual o mayor a cero.",
                "error");

            txtStock.Focus();
            return;
        }

        // 4. VALIDAR CATEGORÍA
        if (pickerCategorias.SelectedItem is not CategoriaModel
            categoriaSeleccionada)
        {
            await MensajeModal.MostrarAsync(
                this,
                "Categoría requerida",
                "Selecciona una categoría para continuar.",
                "advertencia");

            return;
        }

        // 5. GUARDAR EL ARREGLO EN SUPABASE
        try
        {
            SupabaseService servicio = new SupabaseService();


            string urlOImagenFinal = txtImagenUrl.Text?.Trim()
                ?? string.Empty;

            // SUBIR AUTOMÁTICAMENTE LA FOTOGRAFÍA SELECCIONADA
            if (imagenSeleccionada != null)
            {
                var imagenService = new ImagenStorageService();

                urlOImagenFinal = await imagenService
                    .SubirImagenAsync(imagenSeleccionada);
            }

            // VALIDAR URL SI FUE ESCRITA MANUALMENTE
            if (!string.IsNullOrWhiteSpace(urlOImagenFinal) &&
                (!Uri.TryCreate(urlOImagenFinal, UriKind.Absolute, out var uri) ||
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Imagen inválida",
                    "La dirección de la fotografía debe ser una URL HTTPS válida.",
                    "advertencia");
                return;
            }


            var nuevoArreglo = new
            {
                nombre = txtNombre.Text.Trim(),
                descripcion = txtDescripcion.Text?.Trim() ?? string.Empty,
                precio = precioValidado,
                stock = stockValidado,
                imagen = urlOImagenFinal,
                id_categoria = categoriaSeleccionada.id_categoria,
                estado = true
            };

            string json = JsonSerializer.Serialize(nuevoArreglo);

            using StringContent contenido = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            string endpoint =
                $"{servicio.GetUrl()}/rest/v1/arreglos";

            using var respuesta = await servicio
                .GetClient()
                .PostAsync(endpoint, contenido);

            // 6. REGISTRO EXITOSO
            if (respuesta.IsSuccessStatusCode)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "¡Arreglo registrado!",
                    "El nuevo arreglo floral se guardó correctamente.",
                    "exito");

                await Navigation.PopAsync();
            }
            else
            {
                // Conservar el error técnico para depuración
                string error = await respuesta.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Error Supabase: {error}");

                await MensajeModal.MostrarAsync(
                    this,
                    "No se pudo guardar",
                    "Ocurrió un problema al registrar el arreglo. Inténtalo nuevamente.",
                    "error");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error al guardar arreglo: {ex}");

            await MensajeModal.MostrarAsync(
                this,
                "Error de conexión",
                "No fue posible completar el registro. Verifica la conexión e inténtalo nuevamente.",
                "error");
        }
    }



    private async void Volver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void Button_Clicked_2(object sender, EventArgs e)
    {

    }
}
