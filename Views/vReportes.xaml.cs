using System.Globalization;
using addFloreriaG5.Services;
using Microsoft.Maui.Layouts;

namespace addFloreriaG5.Views;

public partial class vReportes : ContentPage
{
    private static readonly Color Vino = Color.FromArgb("#812A44");
    private static readonly Color Texto = Color.FromArgb("#68454D");
    private static readonly string[] Secciones = { "Resumen", "Pedidos", "Pagos", "Arreglos", "Entregas", "Inventario" };
    private readonly List<FilaReporte> filasExportacion = new();
    private DatosReportes? datos;
    private string seccion = "Resumen";
    private bool autorizado;
    private bool cargando;
    private bool actualizandoFiltro;
    private DateTime inicioCargado;
    private DateTime finCargado;

    public vReportes()
    {
        InitializeComponent();
        var hoy = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).Date;
        desde.Date = new DateTime(hoy.Year, hoy.Month, 1);
        hasta.Date = hoy;
        consultar.Clicked += async (_, _) => await CargarAsync();
        consultar.IsEnabled = false;
        filtro.SelectedIndexChanged += (_, _) => { if (!actualizandoFiltro) MostrarSeccion(); };
        foreach (var nombre in Secciones)
        {
            var boton = new Button { Text = nombre, BackgroundColor = Color.FromArgb("#F7E8ED"), TextColor = Vino, CornerRadius = 18, Padding = new Thickness(16, 8), MinimumHeightRequest = 44 };
            boton.Clicked += (_, _) => { seccion = nombre; ActualizarNavegacion(); PrepararFiltro(); MostrarSeccion(); };
            navegacion.Children.Add(boton);
        }
        ActualizarNavegacion();
        periodos.Children.Add(BotonPeriodo("Hoy", () => (hoy: FechaHoy(), inicio: FechaHoy())));
        periodos.Children.Add(BotonPeriodo("Esta semana", () => { var hoy = FechaHoy(); return (hoy, hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7))); }));
        periodos.Children.Add(BotonPeriodo("Este mes", () => { var hoy = FechaHoy(); return (hoy, new DateTime(hoy.Year, hoy.Month, 1)); }));
        CrearBotonesExportacion();
    }

    private static DateTime FechaHoy() => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).Date;
    private Button BotonPeriodo(string texto, Func<(DateTime hoy, DateTime inicio)> rango)
    {
        var boton = new Button { Text = texto, TextColor = Vino, BackgroundColor = Colors.White, BorderColor = Color.FromArgb("#EBDCE0"), BorderWidth = 1, CornerRadius = 12 };
        boton.Clicked += (_, _) => { var fechas = rango(); desde.Date = fechas.inicio; hasta.Date = fechas.hoy; estado.Text = "Pulsa Actualizar reportes para aplicar las fechas."; };
        return boton;
    }
    private void ActualizarNavegacion()
    {
        foreach (var boton in navegacion.Children.OfType<Button>())
        {
            bool activo = boton.Text == seccion;
            boton.BackgroundColor = activo ? Vino : Color.FromArgb("#F7E8ED");
            boton.TextColor = activo ? Colors.White : Vino;
            SemanticProperties.SetDescription(boton, activo ? $"{boton.Text}, sección seleccionada" : $"Ver {boton.Text}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (cargando) return;
        try
        {
            autorizado = await new ReportesService().EsAdministradorAsync();
            if (!autorizado) { estado.Text = SesionAuth.TieneSesion ? "Acceso disponible únicamente para administradores activos." : "La sesión ha vencido. Cierra sesión e ingresa de nuevo."; return; }
            consultar.IsEnabled = true;
            await CargarAsync();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); estado.Text = "No se pudo validar el acceso. Comprueba la conexión y vuelve a abrir Reportería."; }
    }

    private async Task CargarAsync()
    {
        if (!autorizado || cargando) return;
        if (!desde.Date.HasValue || !hasta.Date.HasValue || desde.Date.Value.Date > hasta.Date.Value.Date || hasta.Date.Value.Date >= DateTime.MaxValue.Date)
        { estado.Text = "Selecciona un rango de fechas válido."; return; }
        cargando = true; consultar.IsEnabled = false; datos = null; contenido.Clear(); panelFiltro.IsVisible = false; periodo.Text = ""; estado.Text = "Cargando reportes…";
        try
        {
            var inicio = desde.Date.Value.Date; var fin = hasta.Date.Value.Date;
            datos = await new ReportesService().CargarAsync(inicio, fin);
            inicioCargado = inicio; finCargado = fin;
            periodo.Text = $"Datos del {inicioCargado:dd/MM/yyyy} al {finCargado:dd/MM/yyyy} · Ecuador continental (UTC−5)";
            estado.Text = "";
            PrepararFiltro(); MostrarSeccion();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            estado.Text = "No se pudieron cargar los datos completos. Revisa la conexión, la sesión y los permisos de Supabase.";
        }
        finally { cargando = false; consultar.IsEnabled = autorizado; }
    }

    private void PrepararFiltro()
    {
        actualizandoFiltro = true;
        filtro.Items.Clear(); filtro.Items.Add("Todos los estados");
        if (datos != null)
        {
            IEnumerable<string> estados = seccion switch {
                "Pedidos" => datos.Pedidos.Select(p => Estado(p.estado)),
                "Pagos" => datos.Pagos.Select(p => Estado(p.estado)),
                "Arreglos" => datos.Pedidos.Select(p => Estado(p.estado)),
                "Entregas" => datos.Entregas.Select(e => Estado(e.estado)),
                "Inventario" => new[] { "Activo", "Inactivo" },
                _ => Array.Empty<string>()
            };
            foreach (var valor in estados.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) filtro.Items.Add(valor);
        }
        filtro.SelectedIndex = 0;
        panelFiltro.IsVisible = datos != null && seccion != "Resumen";
        actualizandoFiltro = false;
    }
    private bool Coincide(string? valor) => filtro.SelectedIndex <= 0 || string.Equals(Estado(valor), filtro.SelectedItem?.ToString(), StringComparison.OrdinalIgnoreCase);

    private void MostrarSeccion()
    {
        contenido.Clear();
        filasExportacion.Clear();
        if (datos == null) return;
        switch (seccion)
        {
            case "Resumen": MostrarResumen(); break;
            case "Pedidos": MostrarPedidos(); break;
            case "Pagos": MostrarPagos(); break;
            case "Arreglos": MostrarArreglos(); break;
            case "Entregas": MostrarEntregas(); break;
            case "Inventario": MostrarInventario(); break;
        }
    }

    private void MostrarResumen()
    {
        var d = datos!;
        Titulo("Resumen general", "Pedidos y pagos corresponden al período. Entregas y existencias muestran su estado actual.");
        var tarjetas = new FlexLayout { Wrap = FlexWrap.Wrap, Direction = FlexDirection.Row, JustifyContent = FlexJustify.Start };
        AgregarTarjeta(tarjetas, "Pedidos registrados", d.Pedidos.Count.ToString(), "Todos los estados");
        AgregarTarjeta(tarjetas, "Pagos registrados", d.Pagos.Count.ToString(), "Todos los estados");
        AgregarTarjeta(tarjetas, "Entregas registradas", d.Entregas.Count.ToString(), "Estado actual");
        AgregarTarjeta(tarjetas, "Sin existencias", d.Arreglos.Count(a => a.estado && a.stock <= 0).ToString(), "Arreglos activos");
        contenido.Add(tarjetas);
        Titulo("Pedidos por estado", "Cantidad de pedidos creados en el período.");
        Barras(d.Pedidos.GroupBy(p => Estado(p.estado), StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Count())));
        Titulo("Pagos por estado", "Los pagos se muestran por separado porque sus estados aún deben verificarse.");
        Barras(d.Pagos.GroupBy(p => Estado(p.estado), StringComparer.OrdinalIgnoreCase).Select(g => (g.Key, g.Count())));
    }

    private void MostrarPedidos()
    {
        var pedidos = datos!.Pedidos.Where(p => Coincide(p.estado)).ToList();
        Titulo("Pedidos", "Importes originales y saldos actuales de pedidos creados en el período; se incluyen todos los estados seleccionados.");
        Titulo("Resumen por estado", "Totales de los pedidos que coinciden con el filtro.");
        foreach (var g in pedidos.GroupBy(p => Estado(p.estado), StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key))
            Fila(g.Key, $"{g.Count()} pedidos · importe original {Dinero(g.Sum(p => p.total_original))} · saldo actual {Dinero(g.Sum(p => p.total))}");
        if (pedidos.Count == 0) Vacio("No hay pedidos para este filtro.");
        Titulo("Detalle de pedidos", "Pedidos individuales del período seleccionado.");
        foreach (var p in pedidos.OrderByDescending(p => p.fecha_pedido))
            Fila($"Pedido #{p.id_pedido} · {Estado(p.estado)}", $"{Fecha(p.fecha_pedido)} · original {Dinero(p.total_original)} · saldo actual {Dinero(p.total)}");
    }

    private void MostrarPagos()
    {
        var pagos = datos!.Pagos.Where(p => Coincide(p.estado)).ToList();
        Titulo("Pagos", "Pagos registrados en el período por fecha de pago. Los estados se muestran por separado y no implican cobros confirmados.");
        Titulo("Resumen por estado y método", "Cantidad e importe de los pagos registrados.");
        foreach (var g in pagos.GroupBy(p => (Estado: Estado(p.estado), Metodo: Estado(p.metodo_pago))).OrderBy(g => g.Key.Estado).ThenBy(g => g.Key.Metodo))
            Fila($"{g.Key.Estado} · {g.Key.Metodo}", $"{g.Count()} pagos · {Dinero(g.Sum(p => p.monto))}");
        if (pagos.Count == 0) Vacio("No hay pagos para este filtro.");
        Titulo("Detalle de pagos", "Pagos individuales del período seleccionado.");
        foreach (var p in pagos.OrderByDescending(p => p.fecha_pago))
            Fila($"Pago #{p.id_pago} · pedido #{p.id_pedido}", $"{Fecha(p.fecha_pago)} · {Estado(p.estado)} · {Estado(p.metodo_pago)} · {Dinero(p.monto)}");
    }

    private void MostrarArreglos()
    {
        var d = datos!;
        Titulo("Arreglos en pedidos", "Cantidades e importes al precio del pedido, separados por estado. No representan necesariamente ventas cobradas.");
        var nombres = d.Arreglos.GroupBy(a => a.id_arreglo).ToDictionary(g => g.Key, g => g.First().nombre);
        var pedidos = d.Pedidos.Where(p => Coincide(p.estado)).GroupBy(p => p.id_pedido).ToDictionary(g => g.Key, g => g.First());
        var ranking = d.Detalles.Where(x => pedidos.ContainsKey(x.id_pedido)).GroupBy(x => (x.id_arreglo, Estado: Estado(pedidos[x.id_pedido].estado)))
            .Select(g => new { g.Key.id_arreglo, g.Key.Estado, Cantidad = g.Sum(x => x.cantidad), Importe = g.Sum(x => x.cantidad * x.precio_unitario) }).OrderByDescending(x => x.Cantidad).ThenBy(x => x.id_arreglo).ToList();
        foreach (var x in ranking) Fila(nombres.TryGetValue(x.id_arreglo, out var nombre) ? nombre : $"Arreglo #{x.id_arreglo}", $"{x.Estado} · {x.Cantidad} unidades · {Dinero(x.Importe)}");
        if (ranking.Count == 0) Vacio("No hay detalles de pedidos para este filtro.");
    }

    private void MostrarEntregas()
    {
        var entregas = datos!.Entregas.Where(e => Coincide(e.estado)).ToList();
        Titulo("Entregas", "Estado actual de todas las entregas. La fecha de entrega necesita verificarse y no se aplica el filtro de fechas.");
        Titulo("Resumen por estado", "Cantidad de entregas según su estado actual.");
        foreach (var g in entregas.GroupBy(e => Estado(e.estado), StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key)) Fila(g.Key, $"{g.Count()} entregas");
        if (entregas.Count == 0) Vacio("No hay entregas para este filtro.");
        Titulo("Detalle de entregas", "Entregas individuales y fecha registrada.");
        foreach (var e in entregas.OrderByDescending(e => e.id_entrega)) Fila($"Entrega #{e.id_entrega} · pedido #{e.id_pedido}", $"{Estado(e.estado)} · fecha registrada: {Fecha(e.fecha_entrega)}");
    }

    private void MostrarInventario()
    {
        var arreglos = datos!.Arreglos.Where(a => Coincide(a.estado ? "Activo" : "Inactivo")).OrderBy(a => a.stock).ThenBy(a => a.nombre).ToList();
        Titulo("Inventario", "Existencias actuales, independientes del período. Se muestran primero las cantidades menores.");
        Fila("Resumen", $"{arreglos.Count} arreglos · {arreglos.Count(a => a.stock <= 0)} sin existencias");
        if (arreglos.Count == 0) Vacio("No hay arreglos para este filtro.");
        foreach (var a in arreglos) Fila(a.nombre ?? $"Arreglo #{a.id_arreglo}", $"{a.stock} unidades · {(a.estado ? "Activo" : "Inactivo")}");
    }

    private void Barras(IEnumerable<(string nombre, int cantidad)> elementos)
    {
        var lista = elementos.OrderByDescending(x => x.cantidad).ToList();
        if (lista.Count == 0) { Vacio("No hay datos para el período."); return; }
        var max = lista.Max(x => x.cantidad);
        foreach (var item in lista)
        {
            filasExportacion.Add(new(TipoFilaReporte.Dato, item.nombre, item.cantidad.ToString(CultureInfo.InvariantCulture)));
            contenido.Add(new Label { Text = $"{item.nombre} · {item.cantidad}", TextColor = Texto });
            var resto = Math.Max(0.001, max - item.cantidad);
            contenido.Add(new Grid { HeightRequest = 9, ColumnDefinitions = new ColumnDefinitionCollection { new(new GridLength(item.cantidad, GridUnitType.Star)), new(new GridLength(resto, GridUnitType.Star)) }, Children = { new BoxView { Color = Color.FromArgb("#B75F78"), CornerRadius = 4 } } });
        }
    }
    private void AgregarTarjeta(FlexLayout layout, string titulo, string valor, string descripcion)
    {
        filasExportacion.Add(new(TipoFilaReporte.Indicador, titulo, $"{valor} · {descripcion}"));
        var tarjeta = new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#EBDCE0"), Padding = 14, Margin = new Thickness(0, 0, 10, 10), WidthRequest = 180, Content = new VerticalStackLayout { Spacing = 3, Children = {
            new Label { Text = titulo, TextColor = Texto, FontSize = 13 }, new Label { Text = valor, TextColor = Vino, FontSize = 25, FontAttributes = FontAttributes.Bold }, new Label { Text = descripcion, TextColor = Texto, FontSize = 12 }
        } } };
        layout.Children.Add(tarjeta);
    }
    private void Titulo(string titulo, string explicacion)
    {
        filasExportacion.Add(new(TipoFilaReporte.Seccion, titulo, explicacion));
        contenido.Add(new Label { Text = titulo, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Vino, Margin = new Thickness(0, 8, 0, 0) });
        contenido.Add(new Label { Text = explicacion, FontSize = 13, TextColor = Texto });
    }
    private void Vacio(string texto) { filasExportacion.Add(new(TipoFilaReporte.Dato, texto, "")); contenido.Add(new Label { Text = texto, TextColor = Texto, Margin = new Thickness(0, 8) }); }
    private void Fila(string titulo, string detalle) { filasExportacion.Add(new(TipoFilaReporte.Dato, titulo, detalle)); contenido.Add(new Border { BackgroundColor = Colors.White, Stroke = Color.FromArgb("#EBDCE0"), Padding = 12, Content = new VerticalStackLayout { Spacing = 3, Children = { new Label { Text = titulo, FontAttributes = FontAttributes.Bold, TextColor = Vino }, new Label { Text = detalle, TextColor = Texto } } } }); }

    private void CrearBotonesExportacion()
    {
        foreach (var formato in new[] { "PDF", "Excel" })
        {
            var boton = new Button { Text = $"Exportar {formato}", BackgroundColor = Colors.White, TextColor = Vino, BorderColor = Vino, BorderWidth = 1, CornerRadius = 12 };
            boton.Clicked += async (_, _) => await ExportarAsync(formato, boton);
            exportacion.Children.Add(boton);
        }
    }

    private async Task ExportarAsync(string formato, Button boton)
    {
        if (datos == null || !autorizado || !SesionAuth.TieneSesion)
        { await DisplayAlertAsync("Reporte no disponible", "Carga los reportes con una sesión activa antes de exportar.", "Aceptar"); return; }
        if (desde.Date?.Date != inicioCargado || hasta.Date?.Date != finCargado)
        { await DisplayAlertAsync("Fechas modificadas", "Actualiza los reportes antes de exportar las nuevas fechas.", "Aceptar"); return; }
        boton.IsEnabled = false;
        try
        {
            if (!await new ReportesService().EsAdministradorAsync())
            { await DisplayAlertAsync("Acceso denegado", "Se requiere una sesión de administrador activa.", "Aceptar"); return; }
            var nombreSeccion = seccion;
            var filas = filasExportacion.ToList();
            string seleccion = seccion == "Resumen" || filtro.SelectedIndex <= 0 ? "Todos los estados" : filtro.SelectedItem?.ToString() ?? "Todos los estados";
            string contexto = $"Período cargado: {inicioCargado:dd/MM/yyyy} - {finCargado:dd/MM/yyyy} (Ecuador UTC-5) · Filtro: {seleccion}";
            var exportador = new ExportarReportesService();
            byte[] bytes = formato == "PDF" ? exportador.CrearPdf(nombreSeccion, contexto, filas) : exportador.CrearExcel(nombreSeccion, contexto, filas.Select(f => (f.Titulo, f.Detalle)).ToList());
            string extension = formato == "PDF" ? ".pdf" : ".xlsx";
            bool guardado = await exportador.GuardarAsync(bytes, $"Reporte_{nombreSeccion}_{inicioCargado:yyyyMMdd}_{finCargado:yyyyMMdd}{extension}", extension);
            if (guardado) await DisplayAlertAsync("Reporte generado", "El archivo está listo.", "Aceptar");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlertAsync("No se pudo exportar", "Comprueba la sesión, la conexión y el espacio disponible, e inténtalo de nuevo.", "Aceptar");
        }
        finally { boton.IsEnabled = true; }
    }
    private static string Estado(string? valor) => string.IsNullOrWhiteSpace(valor) ? "Sin estado" : valor.Trim();
    private static string Dinero(decimal valor) => $"${valor:N2}";
    private static string Fecha(string? valor) => DateTimeOffset.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fecha) ? fecha.ToOffset(TimeSpan.FromHours(-5)).ToString("dd/MM/yyyy HH:mm") : "Sin fecha";
}
