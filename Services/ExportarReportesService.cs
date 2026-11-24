using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace addFloreriaG5.Services;

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

    public byte[] CrearPdf(string seccion, string contexto, IReadOnlyList<(string Titulo, string Detalle)> filas)
    {
        if (GlobalFontSettings.FontResolver == null) GlobalFontSettings.FontResolver = new FuenteReportes();
        using var documento = new PdfDocument();
        documento.Info.Title = $"Nova Florería - {seccion}";
        var titulo = new XFont("ReporteOpenSans", 17);
        var normal = new XFont("ReporteOpenSans", 10);
        XGraphics? grafico = null;
        double y = 0, ancho = 0, alto = 0;
        int numeroPagina = 0;
        var vino = new XSolidBrush(XColor.FromArgb(125, 39, 62));
        void Pagina()
        {
            grafico?.Dispose();
            var pagina = documento.AddPage();
            numeroPagina++;
            grafico = XGraphics.FromPdfPage(pagina);
            ancho = pagina.Width.Point - 72;
            alto = pagina.Height.Point;
            grafico.DrawRectangle(vino, 0, 0, pagina.Width.Point, 28);
            grafico.DrawString("NOVA FLORERÍA  |  REPORTES", normal, XBrushes.White, new XPoint(36, 19));
            y = 57;
            grafico.DrawString(seccion, titulo, vino, new XPoint(36, y));
            grafico.DrawString($"Página {numeroPagina}", normal, vino, new XPoint(36, alto - 20));
            y += 24;
        }
        void Linea(string texto, XFont fuente, double sangria = 0)
        {
            foreach (var linea in DividirLineas(grafico!, texto, fuente, ancho - sangria))
            {
                if (y > alto - 45) Pagina();
                grafico!.DrawString(linea, fuente, XBrushes.Black, new XPoint(36 + sangria, y));
                y += 15;
            }
        }
        Pagina();
        Linea(contexto, normal);
        y += 10;
        foreach (var fila in filas)
        {
            if (y > alto - 65) Pagina();
            grafico!.DrawLine(new XPen(XColor.FromArgb(235, 220, 224)), 36, y - 10, 36 + ancho, y - 10);
            Linea(fila.Titulo ?? "", normal);
            if (!string.IsNullOrWhiteSpace(fila.Detalle)) Linea(fila.Detalle, normal, 12);
            y += 5;
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
