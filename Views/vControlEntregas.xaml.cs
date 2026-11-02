namespace addFloreriaG5.Views;


using addFloreriaG5.Models;
using addFloreriaG5.Services;
using System.Text;
using System.Text.Json;

public partial class vControlEntregas : ContentPage
{
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

    private async void Button_Clicked_3(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is EntregaModel entrega)
        {
            if (entrega.pedido == null)
            {
                await DisplayAlert("Error", "No se encontró información del pedido asociado.", "Aceptar");
                return;
            }

            // Si por alguna razón el total del objeto pedido viniera en 0, 
            // intentamos rescatarlo extrayéndolo de las observaciones de la entrega
            decimal saldoPendiente = entrega.pedido.total;
            if (saldoPendiente <= 0 && !string.IsNullOrEmpty(entrega.observaciones))
            {
                try
                {
                    // Extrae el número después del símbolo $ en las observaciones
                    string obs = entrega.observaciones;
                    int index = obs.IndexOf('$');
                    if (index != -1)
                    {
                        string sub = obs.Substring(index + 1).Trim();
                        decimal.TryParse(sub, out saldoPendiente);
                    }
                }
                catch { }
            }

            string saldoActualStr = saldoPendiente.ToString("F2");

            string resultadoMonto = await DisplayPromptAsync(
                "Registrar Cobro / Abono",
                $"Pedido #{entrega.id_pedido}\nSaldo pendiente exacto: ${saldoActualStr}\n\nIngrese el monto recibido:",
                "Guardar",
                "Cancelar",
                initialValue: saldoActualStr, // <-- ¡Coloca el saldo exacto para que el usuario pague el total de una vez si desea!
                placeholder: "Ej. 10.00",
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(resultadoMonto))
                return;

            if (!decimal.TryParse(resultadoMonto, out decimal montoAbono) || montoAbono <= 0)
            {
                await DisplayAlert("Error", "Ingrese un monto válido.", "Aceptar");
                return;
            }

            if (montoAbono > (saldoPendiente > 0 ? saldoPendiente : entrega.pedido.total))
            {
                await DisplayAlert("Aviso", "El abono no puede ser mayor al saldo restante.", "Aceptar");
                return;
            }

            try
            {
                SupabaseService servicio = new SupabaseService();
                var client = servicio.GetClient();

                // 1. Insertar el pago en la tabla 'pagos'
                var nuevoPago = new
                {
                    id_pedido = entrega.id_pedido,
                    monto = montoAbono,
                    metodo_pago = "Efectivo / Contra Entrega",
                    fecha_pago = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    estado = "Completado"
                };

                string jsonPago = JsonSerializer.Serialize(nuevoPago);
                StringContent contenidoPago = new StringContent(jsonPago, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Prefer", "return=minimal");

                string endpointPago = $"{servicio.GetUrl()}/rest/v1/pagos";
                var respuestaPago = await client.PostAsync(endpointPago, contenidoPago);

                if (respuestaPago.IsSuccessStatusCode)
                {
                    // 2. Calcular nuevo total restante
                    decimal baseTotal = saldoPendiente > 0 ? saldoPendiente : entrega.pedido.total;
                    decimal nuevoTotal = baseTotal - montoAbono;
                    if (nuevoTotal < 0) nuevoTotal = 0;

                    string nuevoEstadoPedido = nuevoTotal == 0 ? "Pagado Completo" : "Pago Minimo";

                    // 3. Actualizar el pedido en la tabla 'pedidos'
                    var actualizarPedido = new
                    {
                        total = nuevoTotal,
                        estado = nuevoEstadoPedido
                    };

                    string jsonPedido = JsonSerializer.Serialize(actualizarPedido);
                    StringContent contenidoPedido = new StringContent(jsonPedido, Encoding.UTF8, "application/json");

                    string endpointPedido = $"{servicio.GetUrl()}/rest/v1/pedidos?id_pedido=eq.{entrega.id_pedido}";
                    var requestPedido = new HttpRequestMessage(new HttpMethod("PATCH"), endpointPedido)
                    {
                        Content = contenidoPedido
                    };
                    requestPedido.Headers.Add("Prefer", "return=minimal");

                    await client.SendAsync(requestPedido);

                    // 4. Actualizar las observaciones de la entrega para que la tarjeta refleje el nuevo saldo de inmediato
                    var actualizarEntrega = new
                    {
                        observaciones = $"Saldo pendiente a cobrar: ${nuevoTotal:F2}"
                    };
                    string jsonEntrega = JsonSerializer.Serialize(actualizarEntrega);
                    StringContent contenidoEntrega = new StringContent(jsonEntrega, Encoding.UTF8, "application/json");

                    string endpointEntrega = $"{servicio.GetUrl()}/rest/v1/entregas?id_entrega=eq.{entrega.id_entrega}";
                    var requestEntrega = new HttpRequestMessage(new HttpMethod("PATCH"), endpointEntrega)
                    {
                        Content = contenidoEntrega
                    };
                    requestEntrega.Headers.Add("Prefer", "return=minimal");
                    await client.SendAsync(requestEntrega);

                    // 5. ACTUALIZACIÓN EN MEMORIA DE LA TARJETA (Para reflejarlo al instante sin recargar toda la vista si se desea)
                    entrega.pedido.total = nuevoTotal;
                    entrega.observaciones = $"Saldo pendiente a cobrar: ${nuevoTotal:F2}";

                    await DisplayAlert("¡Éxito!", $"Se registró el cobro de ${montoAbono:F2}.\nNuevo saldo restante: ${nuevoTotal:F2}", "Aceptar");

                    // Recargamos la lista completa de entregas desde la BD para refrescar la interfaz visual
                    await CargarEntregas();
                }
                else
                {
                    string error = await respuestaPago.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", "No se pudo registrar el pago: " + error, "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "Aceptar");
            }
        }


    }
}