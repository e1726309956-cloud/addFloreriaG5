namespace addFloreriaG5.Views;

using addFloreriaG5.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using addFloreriaG5.Views;


public partial class vEditarArreglo : ContentPage
{
    private int idArregloActual;
    private FileResult? imagenSeleccionada;

    public vEditarArreglo(ArregloModel arregloAEditar)
	{
		InitializeComponent();
        idArregloActual = arregloAEditar.id_arreglo;

        // Rellenamos los campos con la información actual del producto
        txtNombre.Text = arregloAEditar.nombre;
        txtDescripcion.Text = arregloAEditar.descripcion;
        txtPrecio.Text = arregloAEditar.precio.ToString();
        txtStock.Text = arregloAEditar.stock.ToString();
        txtImagenUrl.Text = arregloAEditar.imagen;

        if (!string.IsNullOrEmpty(arregloAEditar.imagen))
        {
            imgPrevia.Source = arregloAEditar.imagen;
        }

        // Cargamos las categorías para que el administrador pueda seleccionarla
        _ = CargarCategoriasYSeleccionarActual(arregloAEditar.id_categoria);
    }

    private async Task CargarCategoriasYSeleccionarActual(int idCategoriaActual)
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

                // Buscamos y preseleccionamos la categoría que ya tenía el arreglo
                var categoriaEncontrada = listaCategorias.FirstOrDefault(c => c.id_categoria == idCategoriaActual);
                if (categoriaEncontrada != null)
                {
                    pickerCategorias.SelectedItem = categoriaEncontrada;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Aviso", "No se pudieron cargar las categorías: " + ex.Message, "Aceptar");
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una nueva imagen",
                FileTypes = FilePickerFileType.Images
            });

            if (resultado != null)
            {
                imagenSeleccionada = resultado;

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
            string.IsNullOrWhiteSpace(txtStock.Text))
        {
            await MensajeModal.MostrarAsync(
                this,
                "Campos incompletos",
                "Completa los campos obligatorios.",
                "advertencia");

            return;
        }

        // 2. VALIDAR CATEGORÍA
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

        // 3. VALIDAR PRECIO
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

        // 4. VALIDAR STOCK
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

        // 5. ACTUALIZAR EL ARREGLO EN SUPABASE
        try
        {
            SupabaseService servicio = new SupabaseService();


            string urlOImagenFinal = txtImagenUrl.Text?.Trim()
                ?? string.Empty;

            // SI EL ADMINISTRADOR SELECCIONÓ UNA NUEVA FOTOGRAFÍA,
            // SUBIRLA AUTOMÁTICAMENTE A STORAGE
            if (imagenSeleccionada != null)
            {
                var imagenService = new ImagenStorageService();

                urlOImagenFinal = await imagenService
                    .SubirImagenAsync(imagenSeleccionada);
            }

            // CONSERVAR LA IMAGEN EXISTENTE SI NO SE CAMBIÓ.
            // txtImagenUrl inicialmente contiene la URL guardada.

            if (!string.IsNullOrWhiteSpace(urlOImagenFinal) &&
                (!Uri.TryCreate(urlOImagenFinal, UriKind.Absolute, out var uri) ||
                 uri.Scheme != Uri.UriSchemeHttps))
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "Imagen inválida",
                    "Selecciona una nueva fotografía o introduce una URL HTTPS válida.",
                    "advertencia");
                return;
            }

            var arregloActualizado = new
            {
                nombre = txtNombre.Text.Trim(),
                descripcion = txtDescripcion.Text?.Trim()
                    ?? string.Empty,
                precio = precioValidado,
                stock = stockValidado,
                imagen = urlOImagenFinal,
                id_categoria = categoriaSeleccionada.id_categoria
            };

            string json = JsonSerializer.Serialize(arregloActualizado);

            using var contenido = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            string endpoint =
                $"{servicio.GetUrl()}/rest/v1/arreglos?id_arreglo=eq.{idArregloActual}";

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                endpoint)
            {
                Content = contenido
            };

            using var respuesta = await servicio
                .GetClient()
                .SendAsync(request);

            // 6. ACTUALIZACIÓN EXITOSA
            if (respuesta.IsSuccessStatusCode)
            {
                await MensajeModal.MostrarAsync(
                    this,
                    "¡Cambios guardados!",
                    "El arreglo floral se actualizó correctamente.",
                    "exito");

                await Navigation.PopAsync();
            }
            else
            {
                // Guardar detalles técnicos para depuración
                string error = await respuesta.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(
                    $"Error de Supabase al editar arreglo: {error}");

                await MensajeModal.MostrarAsync(
                    this,
                    "No se pudo actualizar",
                    "Ocurrió un problema al guardar los cambios.",
                    "error");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error al editar arreglo: {ex}");

            await MensajeModal.MostrarAsync(
                this,
                "Error al guardar",
                "No fue posible actualizar el arreglo. Inténtalo nuevamente.",
                "error");
        }
    }

    private async void Volver_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }


}
