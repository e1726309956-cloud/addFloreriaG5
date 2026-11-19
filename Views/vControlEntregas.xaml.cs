namespace addFloreriaG5.Views;
using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;
using System.Globalization;
public partial class vControlEntregas : ContentPage
{
    private readonly Dictionary<int, Guid> solicitudesAbonoPendientes = new();
    public vControlEntregas()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarEntregas();
    }


    private async Task CargarEntregas()
    {
        try
        {
            SupabaseService servicio = new SupabaseService();

            // Aseguramos traer la entrega y los datos completos de 'pedidos' y 'pagos'
            string endpoint = $"{servicio.GetUrl()}/rest/v1/entregas?select=*,pedidos(*,pagos(*))&order=id_entrega.desc";

            var respuesta = await servicio.GetClient().GetAsync(endpoint);
            if (respuesta.IsSuccessStatusCode)
            {
                string contenido = await respuesta.Content.ReadAsStringAsync();
                var listaEntregas = JsonSerializer.Deserialize<List<EntregaModel>>(contenido, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                cvEntregas.ItemsSource = listaEntregas;
            }
            else
            {
                string error = await respuesta.Content.ReadAsStringAsync();
                await DisplayAlert("Error", "No se pudieron cargar las entregas: " + error, "Aceptar");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Excepción: " + ex.Message, "Aceptar");
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EntregaModel entrega)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entrega.direccion))
                {
                    await DisplayAlert("Aviso", "Este pedido no cuenta con una dirección registrada.", "Aceptar");
                    return;
                }

                // Codificamos la dirección para la URL de mapas
                string direccionCodificada = Uri.EscapeDataString(entrega.direccion);
                string urlMapa = $"https://www.google.com/maps/search/?api=1&query={direccionCodificada}";

                await Launcher.OpenAsync(new Uri(urlMapa));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir el mapa: " + ex.Message, "Aceptar");
            }
        }
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EntregaModel entrega)
        {
            bool confirmar = await DisplayAlert("Confirmar", $"¿Deseas marcar el pedido #{entrega.id_pedido} como ENTREGADO?", "Sí", "No");

            if (!confirmar) return;

            try
            {
                SupabaseService servicio = new SupabaseService();
                var client = servicio.GetClient();

                var actualizarEstado = new
                {
                    estado = "Entregado"
                };

                string json = JsonSerializer.Serialize(actualizarEstado);
                StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

                string endpoint = $"{servicio.GetUrl()}/rest/v1/entregas?id_entrega=eq.{entrega.id_entrega}";
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
                {
                    Content = contenido
                };
                request.Headers.Add("Prefer", "return=minimal");

                var respuesta = await client.SendAsync(request);

                if (respuesta.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "La entrega ha sido marcada como completada.", "Aceptar");
                    await CargarEntregas(); // Recargamos la lista
                }
                else
                {
                    string error = await respuesta.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", "No se pudo actualizar: " + error, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }

    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EntregaModel entrega)
        {
            try
            {
                // 1. Obtenemos el teléfono registrado o usamos uno por defecto si está vacío
                string telefonoCrudo = string.IsNullOrWhiteSpace(entrega.telefono_destinatario) ? "593959178595" : entrega.telefono_destinatario.Trim();

                // 2. Limpiamos el número: quitamos espacios, guiones y el símbolo '+'
                string telefonoLimpio = telefonoCrudo.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

                // 3. Si el número empieza con '0' (ej. 0997535512), se lo quitamos y le anteponemos el código de país '593'
                if (telefonoLimpio.StartsWith("0") && telefonoLimpio.Length > 1)
                {
                    telefonoLimpio = "593" + telefonoLimpio.Substring(1);
                }
                // Si el usuario olvidó poner el 0 pero puso los 9 dígitos directos, le agregamos el 593
                else if (!telefonoLimpio.StartsWith("593") && telefonoLimpio.Length == 9)
                {
                    telefonoLimpio = "593" + telefonoLimpio;
                }

                // 4. Armamos el mensaje y la URL limpia para WhatsApp
                string mensaje = Uri.EscapeDataString($"Hola, le escribimos de la florería respecto a la entrega de su Pedido #{entrega.id_pedido}. ¿Se encuentra en la dirección para recibir su arreglo?");
                string urlWhatsApp = $"https://wa.me/{telefonoLimpio}?text={mensaje}";

                await Launcher.OpenAsync(new Uri(urlWhatsApp));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo abrir WhatsApp: " + ex.Message, "Aceptar");
            }
        }

    }

    private async void RegistrarAbonoEntrega_Clicked(
        object sender,
        EventArgs e)
    {
        // VALIDAR BOTÓN Y ENTREGA
        if (sender is not Button boton ||
            boton.CommandParameter is not EntregaModel entrega)
            return;

        // VALIDAR SESIÓN
        if (!SesionAuth.TieneSesion)
        {
            await DisplayAlert(
                "Sesión vencida",
                "Inicia sesión nuevamente.",
                "Aceptar");
            return;
        }

        // VALIDAR PEDIDO ASOCIADO
        if (entrega.pedido == null)
        {
            await DisplayAlert(
                "Error",
                "No se encontró el pedido asociado.",
                "Aceptar");
            return;
        }

        // OBTENER SALDO PENDIENTE
        decimal saldoPendiente = entrega.pedido.total;

        if (saldoPendiente <= 0)
        {
            await DisplayAlert(
                "Pedido pagado",
                "Este pedido no tiene saldo pendiente.",
                "Aceptar");
            return;
        }

        // SELECCIONAR MÉTODO DE PAGO
        string metodoPago = await DisplayActionSheet(
            "Método de pago",
            "Cancelar",
            null,
            "Efectivo",
            "Transferencia");

        if (string.IsNullOrWhiteSpace(metodoPago) ||
            metodoPago == "Cancelar")
            return;

        // SOLICITAR MONTO
        string resultadoMonto = await DisplayPromptAsync(
            "Registrar abono de entrega",
            $"Pedido #{entrega.id_pedido}\n" +
            $"Saldo pendiente: ${saldoPendiente:F2}\n\n" +
            "Ingresa el monto recibido:",
            "Continuar",
            "Cancelar",
            initialValue: saldoPendiente.ToString("F2"),
            placeholder: "Ej. 20.00",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(resultadoMonto))
            return;

        // VALIDAR MONTO
        bool montoValido = decimal.TryParse(
            resultadoMonto,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out decimal montoAbono);

        if (!montoValido)
        {
            montoValido = decimal.TryParse(
                resultadoMonto,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out montoAbono);
        }

        if (!montoValido ||
            montoAbono <= 0 ||
            decimal.Round(montoAbono, 2) != montoAbono)
        {
            await DisplayAlert(
                "Monto inválido",
                "Ingresa un monto válido mayor que cero, " +
                "con máximo dos decimales.",
                "Aceptar");
            return;
        }

        if (montoAbono > saldoPendiente)
        {
            await DisplayAlert(
                "Monto incorrecto",
                "El abono no puede superar " +
                "el saldo pendiente.",
                "Aceptar");
            return;
        }

        // CONFIRMAR EL PAGO
        bool confirmar = await DisplayAlert(
            "Confirmar pago",
            $"Pedido #{entrega.id_pedido}\n" +
            $"Monto: ${montoAbono:F2}\n" +
            $"Método: {metodoPago}\n\n" +
            "¿Confirmas que recibiste este dinero?",
            "Sí, registrar",
            "Cancelar");

        if (!confirmar)
            return;

        boton.IsEnabled = false;

        try
        {
            // CONSERVAR IDENTIFICADOR DE SOLICITUD
            if (!solicitudesAbonoPendientes.TryGetValue(
                entrega.id_pedido,
                out Guid solicitudId))
            {
                solicitudId = Guid.NewGuid();

                solicitudesAbonoPendientes[
                    entrega.id_pedido] = solicitudId;
            }

            // REGISTRAR MEDIANTE LA FUNCIÓN DE SUPABASE
            var pagosService = new PagosService();

            ResultadoPago resultado =
                await pagosService.RegistrarAbonoAsync(
                    entrega.id_pedido,
                    montoAbono,
                    metodoPago,
                    solicitudId);

            // EL SERVIDOR CONFIRMÓ LA OPERACIÓN
            solicitudesAbonoPendientes.Remove(
                entrega.id_pedido);

            // MOSTRAR RESULTADO
            string mensaje =
                $"Abono registrado: ${montoAbono:F2}\n" +
                $"Saldo pendiente: ${resultado.SaldoRestante:F2}\n" +
                $"Estado: {resultado.Estado}";

            if (resultado.SaldoRestante == 0)
            {
                mensaje +=
                    "\n\n¡El pedido está pagado al 100 %!";
            }

            await DisplayAlert(
                "Pago registrado",
                mensaje,
                "Aceptar");

            // ACTUALIZAR LISTA DE ENTREGAS
            await CargarEntregas();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlert(
                "No se pudo confirmar",
                "No se pudo verificar el resultado del pago. " +
                "Consulta el historial antes de repetirlo.\n\n" +
                ex.Message,
                "Aceptar");
        }
        finally
        {
            boton.IsEnabled = true;
        }
    }




}