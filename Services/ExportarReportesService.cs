using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace addFloreriaG5.Services;

public enum TipoFilaReporte { Seccion, Indicador, Dato }
public record FilaReporte(TipoFilaReporte Tipo, string Titulo, string Detalle);

public class ExportarReportesService
{
    public byte[] CrearExcel(string seccion, string contexto, IReadOnlyList<(string Titulo, string Detalle)> filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Reporte");
        hoja.ShowGridLines = false;
        hoja.Cell(1, 1).Value = $"Nova Florería - {seccion}";
        hoja.Range(1, 1, 1, 2).Merge().Style.Font.SetBold().Font.SetFontSize(16);
        hoja.Range(1, 1, 1, 2).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#7D273E")).Font.SetFontColor(XLColor.White);
        hoja.Row(1).Height = 32;
        hoja.Cell(2, 1).Value = contexto;
        hoja.Range(2, 1, 2, 2).Merge();
        hoja.Range(2, 1, 2, 2).Style.Font.SetFontColor(XLColor.FromHtml("#69545B")).Alignment.SetWrapText();
        hoja.Row(2).Height = 30;
        hoja.Cell(4, 1).Value = "Concepto";
        hoja.Cell(4, 2).Value = "Detalle";
        hoja.Range(4, 1, 4, 2).Style.Font.SetBold();
        hoja.Range(4, 1, 4, 2).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#9B3E5C")).Font.SetFontColor(XLColor.White);
        hoja.Row(4).Height = 24;
        for (int i = 0; i < filas.Count; i++)
        {
            // Asignar texto explícitamente evita interpretar entradas como fórmulas.
            hoja.Cell(i + 5, 1).Value = filas[i].Titulo ?? "";
            hoja.Cell(i + 5, 2).Value = filas[i].Detalle ?? "";
            hoja.Range(i + 5, 1, i + 5, 2).Style.Fill.SetBackgroundColor(i % 2 == 0 ? XLColor.FromHtml("#F9EEF1") : XLColor.White);
            hoja.Cell(i + 5, 1).Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#7D273E"));
        }
        hoja.Column(1).Width = 36;
        hoja.Column(2).Width = 90;
        hoja.Columns(1, 2).Style.Alignment.WrapText = true;
        if (filas.Count > 0) hoja.Rows(5, filas.Count + 4).AdjustToContents();
        hoja.SheetView.FreezeRows(4);
        if (filas.Count > 0) hoja.Range(4, 1, filas.Count + 4, 2).SetAutoFilter();
        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] CrearPdf(string seccion, string contexto, IReadOnlyList<FilaReporte> filas)
    {
        if (GlobalFontSettings.FontResolver == null) GlobalFontSettings.FontResolver = new FuenteReportes();
        using var documento = new PdfDocument();
        documento.Info.Title = $"Nova Florería - {seccion}";
        var titulo = new XFont("ReporteOpenSans", 21);
        var subtitulo = new XFont("ReporteOpenSans", 13);
        var normal = new XFont("ReporteOpenSans", 10);
        var pequeno = new XFont("ReporteOpenSans", 9);
        var cifra = new XFont("ReporteOpenSans", 22);
        XGraphics? grafico = null;
        double y = 0, ancho = 0, alto = 0;
        int numeroPagina = 0;
        var vino = new XSolidBrush(XColor.FromArgb(125, 39, 62));
        var gris = new XSolidBrush(XColor.FromArgb(104, 69, 77));
        var fondo = new XSolidBrush(XColor.FromArgb(249, 238, 241));
        var borde = new XPen(XColor.FromArgb(235, 220, 224));
        void Pagina()
        {
            grafico?.Dispose();
            var pagina = documento.AddPage();
            numeroPagina++;
            grafico = XGraphics.FromPdfPage(pagina);
            ancho = pagina.Width.Point - 72;
            alto = pagina.Height.Point;
            grafico.DrawRectangle(vino, 0, 0, pagina.Width.Point, 34);
            grafico.DrawString("NOVA FLORERÍA  /  REPORTES", pequeno, XBrushes.White, new XPoint(36, 22));
            y = 65;
            grafico.DrawString(seccion, titulo, vino, new XPoint(36, y));
            grafico.DrawString($"Página {numeroPagina}", pequeno, gris, new XPoint(36, alto - 20));
            y += 23;
            foreach (var linea in DividirLineas(grafico, contexto, pequeno, ancho))
            { grafico.DrawString(linea, pequeno, gris, new XPoint(36, y)); y += 13; }
            y += 12;
        }
        void Espacio(double altura)
        {
            if (y + altura > alto - 42) Pagina();
        }
        Pagina();
        for (int i = 0; i < filas.Count; i++)
        {
            var fila = filas[i];
            if (fila.Tipo == TipoFilaReporte.Seccion)
            {
                var lineas = DividirLineas(grafico!, fila.Detalle, pequeno, ancho - 16).ToList();
                Espacio(45 + lineas.Count * 12);
                grafico!.DrawString(fila.Titulo, subtitulo, vino, new XPoint(36, y + 13));
                y += 20;
                foreach (var linea in lineas) { grafico.DrawString(linea, pequeno, gris, new XPoint(36, y + 8)); y += 12; }
                y += 9;
            }
            else if (fila.Tipo == TipoFilaReporte.Indicador)
            {
                const double hueco = 10;
                double anchoTarjeta = (ancho - hueco) / 2;
                int cantidad = Math.Min(2, filas.Skip(i).TakeWhile(f => f.Tipo == TipoFilaReporte.Indicador).Count());
                var tarjetas = filas.Skip(i).Take(cantidad).Select(f => new {
                    Fila = f,
                    Titulo = DividirLineas(grafico!, f.Titulo, pequeno, anchoTarjeta - 24).ToList(),
                    Partes = f.Detalle.Split('·', 2, StringSplitOptions.TrimEntries)
                }).ToList();
                double altura = Math.Max(80, 62 + tarjetas.Max(t => t.Titulo.Count) * 12);
                Espacio(altura + 10);
                for (int j = 0; j < tarjetas.Count; j++)
                {
                    var tarjeta = tarjetas[j]; double x = 36 + j * (anchoTarjeta + hueco);
                    grafico!.DrawRoundedRectangle(borde, fondo, x, y, anchoTarjeta, altura, 8, 8);
                    double ty = y + 17;
                    foreach (var linea in tarjeta.Titulo) { grafico.DrawString(linea, pequeno, gris, new XPoint(x + 12, ty)); ty += 12; }
                    grafico.DrawString(tarjeta.Partes[0], cifra, vino, new XPoint(x + 12, ty + 18));
                    if (tarjeta.Partes.Length > 1) grafico.DrawString(tarjeta.Partes[1], pequeno, gris, new XPoint(x + 12, ty + 34));
                }
                y += altura + 10; i += cantidad - 1;
            }
            else
            {
                var titulos = DividirLineas(grafico!, fila.Titulo, normal, ancho * 0.38 - 20).ToList();
                var detalles = DividirLineas(grafico!, fila.Detalle, normal, ancho * 0.62 - 20).ToList();
                double altura = Math.Max(titulos.Count, detalles.Count) * 14 + 16;
                // Las filas largas se distribuyen en varias páginas sin perder texto.
                int maxLineas = Math.Max(titulos.Count, detalles.Count), indice = 0;
                while (indice < maxLineas)
                {
                    Espacio(30);
                    int disponibles = Math.Max(1, (int)((alto - 42 - y - 16) / 14));
                    int cantidad = Math.Min(disponibles, maxLineas - indice);
                    altura = cantidad * 14 + 16;
                    grafico!.DrawRectangle(borde, indice == 0 && i % 2 == 0 ? fondo : XBrushes.White, 36, y, ancho, altura);
                    for (int j = 0; j < cantidad; j++)
                    {
                        if (indice + j < titulos.Count) grafico.DrawString(titulos[indice + j], normal, vino, new XPoint(46, y + 17 + j * 14));
                        if (indice + j < detalles.Count) grafico.DrawString(detalles[indice + j], normal, XBrushes.Black, new XPoint(36 + ancho * 0.38, y + 17 + j * 14));
                    }
                    y += altura; indice += cantidad;
                    if (indice < maxLineas) Pagina();
                }
            }
        }
        grafico?.Dispose();
        using var stream = new MemoryStream();
        documento.Save(stream, false);
        return stream.ToArray();
    }

    private static IEnumerable<string> DividirLineas(XGraphics grafico, string texto, XFont fuente, double ancho)
    {
        var linea = "";
        foreach (char letra in texto.Replace('\r', ' '))
        {
            if (letra == '\n') { yield return linea; linea = ""; continue; }
            var siguiente = linea + letra;
            if (linea.Length > 0 && grafico.MeasureString(siguiente, fuente).Width > ancho)
            { yield return linea; linea = letra.ToString(); }
            else linea = siguiente;
        }
        yield return linea;
    }

    public async Task<bool> GuardarAsync(byte[] bytes, string nombre, string extension)
    {
#if WINDOWS
        var picker = new Windows.Storage.Pickers.FileSavePicker { SuggestedFileName = Path.GetFileNameWithoutExtension(nombre) };
        picker.FileTypeChoices.Add(extension == ".pdf" ? "Documento PDF" : "Libro de Excel", new List<string> { extension });
        var ventana = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            ?? throw new InvalidOperationException("No se encontró la ventana de la aplicación.");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(ventana));
        var archivo = await picker.PickSaveFileAsync();
        if (archivo == null) return false;
        await Windows.Storage.FileIO.WriteBytesAsync(archivo, bytes);
        return true;
#else
        var ruta = Path.Combine(FileSystem.CacheDirectory, nombre);
        await File.WriteAllBytesAsync(ruta, bytes);
        await Share.Default.RequestAsync(new ShareFileRequest { Title = "Guardar o compartir reporte", File = new ShareFile(ruta) });
        return false; // La hoja de compartir no informa si el usuario guardó el archivo.
#endif
    }

    private sealed class FuenteReportes : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            using var archivo = typeof(ExportarReportesService).Assembly.GetManifestResourceStream("Reportes.OpenSans-Regular.ttf")
                ?? throw new InvalidOperationException("No se encontró la fuente del reporte.");
            using var memoria = new MemoryStream();
            archivo.CopyTo(memoria);
            return memoria.ToArray();
        }
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) => new("OpenSansRegular");
    }
}
