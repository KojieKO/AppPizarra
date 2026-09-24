using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace PizarrasPro;

public sealed partial class MainWindow
{
    private readonly Dictionary<string, CloudDrive> clouds = new() { ["OneDrive"] = new("OneDrive"), ["Google Drive"] = new("Google Drive") };
    private async Task CloudSettings(string? service = null)
    {
        var win = service is null ? CreateCloudWindow() : CreateCloudWindowForService(service);
        modal = true; try { await win.ShowDialog(this); } finally { modal = false; }
    }
    private Window CreateCloudWindow() => CreateCloudWindowForService(null);
    private Window CreateCloudWindowForService(string? initialService)
    {
        var win = SecondaryUi.Window("Almacenamiento en la nube", 690, 580, 520, 400);
        var tabs = new TabControl();
        foreach (var pair in clouds)
        {
            var client = pair.Value;
            var panel = new StackPanel { Margin = new Thickness(24), Spacing = 14 };
            panel.Children.Add(new TextBlock { Text = "La sesión solo dura mientras la aplicación está abierta.\nLas contraseñas y los tokens no se guardan en el USB.", TextWrapping = TextWrapping.Wrap });
            var input = new TextBox { Text = pair.Key == "OneDrive" ? config.Text("microsoft_client_id") : "", Watermark = "Identificador de aplicación de Microsoft Entra", IsVisible = pair.Key == "OneDrive" }; panel.Children.Add(input);
            panel.Children.Add(new TextBlock { Text = pair.Key == "OneDrive" ? "Aplicación de escritorio/pública: redirección http://localhost y permiso Files.ReadWrite." : "Selecciona el JSON OAuth de tipo Escritorio de Google Cloud, con Drive API habilitada.", TextWrapping = TextWrapping.Wrap });
            string? googleJson = null;
            var read = new Button { Content = "Seleccionar JSON de Google…", IsVisible = pair.Key != "OneDrive" };
            var state = new TextBlock { Text = client.Account + "\n" + (client.Folder?.Name ?? "Sin carpeta elegida"), TextWrapping = TextWrapping.Wrap };
            read.Click += async (_, _) =>
            {
                try
                {
                    var files = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Configuración OAuth de Google", FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }] });
                    if (files.FirstOrDefault()?.TryGetLocalPath() is string file) { googleJson = await File.ReadAllTextAsync(file); state.Text = "Configuración cargada en memoria."; }
                }
                catch (Exception ex) { state.Text = ex.Message; }
            }; panel.Children.Add(read);
            var connect = new Button { Content = "Conectar cuenta…" }; panel.Children.Add(connect);
            var choose = new Button { Content = "Elegir carpeta remota…", IsEnabled = client.Connected }; panel.Children.Add(choose);
            connect.Click += async (_, _) =>
            {
                connect.IsEnabled = false; choose.IsEnabled = false;
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                try
                {
                    if (pair.Key == "OneDrive") config.Set("microsoft_client_id", input.Text?.Trim() ?? "");
                    state.Text = "Completa el inicio de sesión en tu navegador…";
                    await client.Connect(input.Text?.Trim() ?? "", googleJson, timeout.Token); state.Text = client.Account + "\nSin carpeta elegida";
                }
                catch (Exception ex) { state.Text = ex is OperationCanceledException ? "Conexión cancelada o tiempo agotado." : ex.Message; }
                finally { connect.IsEnabled = true; choose.IsEnabled = client.Connected; }
            };
            choose.Click += async (_, _) => { try { await PickCloudFolder(win, client); state.Text = client.Account + "\n" + (client.Folder?.Name ?? "Sin carpeta elegida"); } catch (Exception ex) { state.Text = ex.Message; } };
            var disconnect = new Button { Content = "Desconectar" }; disconnect.Click += (_, _) => { client.Disconnect(); choose.IsEnabled = false; state.Text = "Sin conectar"; }; panel.Children.Add(disconnect); panel.Children.Add(state);
            tabs.Items.Add(new TabItem { Header = pair.Key, Content = new ScrollViewer { Content = panel } });
        }
        if (initialService is not null) tabs.SelectedIndex = initialService.Equals("Google Drive", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var layout = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Thickness(12) };
        layout.Children.Add(tabs);
        var close = SecondaryUi.CloseButton(win, "Cerrar"); close.HorizontalAlignment = HorizontalAlignment.Right;
        close.Margin = new Thickness(12); Grid.SetRow(close, 1); layout.Children.Add(close);
        win.Content = layout; win.Opened += (_, _) => tabs.Focus();
        return win;
    }
    private static async Task PickCloudFolder(Window owner, CloudDrive client)
    {
        var win = CreateCloudFolderWindow(client, out var load);
        win.Opened += async (_, _) => await load();
        await win.ShowDialog(owner);
    }
    private static Window CreateCloudFolderWindow(CloudDrive client, out Func<Task> load)
    {
        var win = SecondaryUi.Window("Carpeta de " + client.Service, 620, 510, 580, 360);
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(20) };
        var label = new TextBlock { Text = "Mi unidad", Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap };
        var list = new ListBox(); var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 12, 0, 0) };
        var back = new Button { Content = "Atrás" }; var enter = new Button { Content = "Abrir carpeta" }; var select = new Button { Content = "Elegir esta carpeta" }; buttons.Children.Add(back); buttons.Children.Add(enter); buttons.Children.Add(select); buttons.Children.Add(SecondaryUi.CloseButton(win));
        layout.Children.Add(label); Grid.SetRow(list, 1); layout.Children.Add(list); Grid.SetRow(buttons, 2); layout.Children.Add(buttons); win.Content = layout;
        var stack = new List<CloudFolder> { new("root", "Mi unidad") };
        var cancellation = new CancellationTokenSource(); win.Closed += (_, _) => { cancellation.Cancel(); cancellation.Dispose(); };
        async Task Load()
        {
            buttons.IsEnabled = false;
            try { var folders = await client.Folders(stack[^1].Id, cancellation.Token); list.ItemsSource = folders; label.Text = string.Join(" / ", stack.Select(x => x.Name)); }
            catch (Exception ex) { label.Text = ex.Message; }
            finally { buttons.IsEnabled = true; back.IsEnabled = stack.Count > 1; }
        }
        enter.Click += async (_, _) => { if (list.SelectedItem is CloudFolder folder) { stack.Add(folder); await Load(); } };
        list.DoubleTapped += async (_, _) => { if (buttons.IsEnabled && list.SelectedItem is CloudFolder folder) { stack.Add(folder); await Load(); } };
        back.Click += async (_, _) => { if (stack.Count > 1) { stack.RemoveAt(stack.Count - 1); await Load(); } };
        select.Click += (_, _) => { client.Folder = stack[^1]; win.Close(); }; load = Load; return win;
    }
    private async Task UploadCloud(string service)
    {
        var client = clouds[service]; if (!client.Connected || client.Folder is null) { await CloudSettings(); return; }
        var paths = Selected().Where(x => x.Kind == "PDF").Select(x => x.FullPath).ToList(); if (paths.Count == 0) { await Inform(service, "Selecciona uno o varios PDF."); return; }
        if (!await Confirm(service, $"¿Subir {paths.Count} PDF a {client.Account} / {client.Folder.Name}?\nDespués de verificar cada subida se eliminará su original local.", action: "Subir y eliminar", destructive: true)) return;
        var stopped = false;
        await RunBatch("Subir a " + service, paths, async (path, token) =>
        {
            if (stopped) throw new OperationCanceledException("Se detuvo la subida tras un fallo.");
            try
            {
                var hash = FileService.Hash(path); await client.Upload(path, token);
                if (!hash.SequenceEqual(FileService.Hash(path))) throw new IOException("El original cambió durante la subida; se conserva.");
                File.Delete(path);
            }
            catch { stopped = true; throw; }
        });
    }
}
