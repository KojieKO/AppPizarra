using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace PizarrasPro;

public sealed partial class MainWindow : SukiUI.Controls.SukiWindow
{
    private readonly Configuration config = new(AppContext.BaseDirectory);
    private readonly Appearance appearance;
    private readonly DataGrid table = new() { AutoGenerateColumns = false, IsReadOnly = true, CanUserResizeColumns = true, CanUserSortColumns = true, SelectionMode = DataGridSelectionMode.Extended, GridLinesVisibility = DataGridGridLinesVisibility.Horizontal };
    private readonly ComboBox locations = new() { MinWidth = 160, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBlock status = new() { Text = "Selecciona una ubicación para comenzar.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock previewText = new() { Text = "Selecciona un PDF", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12) };
    private readonly TextBlock pageText = new() { VerticalAlignment = VerticalAlignment.Center, Text = "—" };
    private readonly Image previewImage = new() { Stretch = Stretch.None, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top };
    private readonly ScrollViewer previewScroll;
    private readonly StackPanel actions = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly Button cancelButton = new() { Content = "Cancelar operación", IsVisible = false };
    private readonly Grid body = new() { ColumnDefinitions = new ColumnDefinitions("3*,6,2*") };
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(2500) };
    private CancellationTokenSource? previewCancellation, operationCancellation;
    private List<DocumentItem> items = [];
    private string root = "";
    private string? previewPath;
    private int page, pageCount, previewGeneration;
    private double zoom = 1;
    private bool busy, scanning, modal, initializing = true;
    private string[] clipboard = [];
    private bool cut;
    private readonly Button moveRoot, moveDestination;
    private readonly Button previousPage, nextPage, zoomOut, zoomIn, fitPage;
    private readonly TextBlock zoomText = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
    private readonly TextBlock emptyTable = new() { Text = "Elige una ubicación para ver sus PDF y WBH.", TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(24), IsHitTestVisible = false };
    private readonly TabControl sections = new();
    private readonly TextBlock optionsRootPath = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock optionsDestinationPath = new() { TextWrapping = TextWrapping.Wrap };

    public MainWindow()
    {
        appearance = new Appearance(config, Application.Current!.PlatformSettings);
        Closed += (_, _) => appearance.Dispose();
        Title = AppVersion.WindowTitle; Width = 1280; Height = 780; MinWidth = 900; MinHeight = 560;
        FontFamily = new FontFamily("Segoe UI");
        // Avoid corrupt LCD glyph edges on translucent Suki surfaces / bitmap renders.
        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);
        var outer = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), Margin = new Thickness(24) };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 0, 0, 12) };
        Add(header, new TextBlock { Text = AppVersion.Name, FontSize = 20, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }, 0);
        var locationLabel = new TextBlock { Text = "UBICACIÓN", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("PizarrasSecondaryTextBrush") };
        Add(header, locationLabel, 1);
        Add(header, locations, 2); Add(header, Button("Cambiar carpeta…", ChooseRoot), 3); Add(header, Button("Horario", () => { SelectSection(1); return Task.CompletedTask; }), 4); Add(header, Button("Refrescar", () => Refresh(true)), 5);
        header.SizeChanged += (_, _) => { locationLabel.IsVisible = header.Bounds.Width >= 1080; };
        locations.Bind(ToolTip.TipProperty, new Binding("SelectedItem") { Source = locations });
        outer.Children.Add(header);
        AddColumn("ARCHIVO", nameof(DocumentItem.Name), 300); AddColumn("TIPO", nameof(DocumentItem.Kind), 65); AddColumn("TAMAÑO", nameof(DocumentItem.Size), 92); AddColumn("FECHA", nameof(DocumentItem.Date), 105); AddColumn("HORA", nameof(DocumentItem.Time), 70); AddColumn("CLASE", nameof(DocumentItem.Class), 80);
        table.Classes.Add("documents"); table.RowHeight = 32; table.ColumnHeaderHeight = 34;
        var tablePanel = new Grid(); tablePanel.Children.Add(table); tablePanel.Children.Add(emptyTable);
        emptyTable[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("PizarrasSecondaryTextBrush");
        body.Children.Add(Surface(tablePanel));
        body.ColumnDefinitions[0].MinWidth = 240; body.ColumnDefinitions[2].MinWidth = 280;
        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns, HorizontalAlignment = HorizontalAlignment.Stretch, [!BackgroundProperty] = new DynamicResourceExtension("PizarrasWorkspaceBrush") }; Add(body, splitter, 1);
        var preview = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), Margin = new Thickness(8) };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        previousPage = PreviewButton("◀", "Página anterior", async () => { if (page > 0) { page--; await RenderPreview(); } });
        nextPage = PreviewButton("▶", "Página siguiente", async () => { if (page + 1 < pageCount) { page++; await RenderPreview(); } });
        zoomOut = PreviewButton("−", "Reducir zoom · Ctrl + rueda", () => SetZoom(zoom / 1.25));
        zoomIn = PreviewButton("+", "Ampliar zoom · Ctrl + rueda", () => SetZoom(zoom * 1.25));
        fitPage = PreviewButton("Ajustar", "Ajustar la página al espacio disponible", () => SetZoom(1));
        controls.Children.Add(previousPage);
        controls.Children.Add(pageText);
        controls.Children.Add(nextPage); controls.Children.Add(zoomOut); controls.Children.Add(zoomText); controls.Children.Add(zoomIn); controls.Children.Add(fitPage);
        previewText.Margin = new Thickness(4, 6); previewText.FontSize = 12;
        previewText.TextWrapping = TextWrapping.NoWrap; previewText.TextTrimming = TextTrimming.CharacterEllipsis;
        previewText.Bind(ToolTip.TipProperty, new Binding("Text") { Source = previewText });
        preview.Children.Add(controls); Grid.SetRow(previewText, 1); preview.Children.Add(previewText);
        previewScroll = new ScrollViewer { Content = previewImage, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, [!BackgroundProperty] = new DynamicResourceExtension("PizarrasWorkspaceBrush") }; Grid.SetRow(previewScroll, 2); preview.Children.Add(previewScroll);
        preview.ContextMenu = DocumentMenu(true);
        Add(body, Surface(preview), 2);
        Grid.SetRow(body, 0);
        var footer = new WrapPanel { Margin = new Thickness(0, 12, 0, 8) };
        var options = new Button { Content = "Opciones ▾" };
        var menu = new ContextMenu();
        menu.Items.Add(Menu("Apariencia…", ShowAppearance));
        menu.Items.Add(Menu("Acerca de " + AppVersion.Name, ShowAbout));
        menu.Items.Add(new Separator());
        menu.Items.Add(Menu("Cambiar carpeta de destino…", ChooseDestination));
        menu.Items.Add(Menu("Abrir carpeta de origen", () => { OpenPath(root); return Task.CompletedTask; }));
        menu.Items.Add(Menu("Abrir carpeta de destino", () => { OpenPath(config.Destination); return Task.CompletedTask; }));
        menu.Items.Add(Menu("Borrar seleccionados…", DeleteSelected));
        menu.Items.Add(Menu("Configurar almacenamiento…", () => CloudSettings()));
        menu.Items.Add(Menu("Subir a OneDrive", () => UploadCloud("OneDrive")));
        menu.Items.Add(Menu("Subir a Google Drive", () => UploadCloud("Google Drive")));
        menu.Items.Add(Menu("Ver registro", () => { AppLog.Write("Registro abierto."); OpenPath(AppLog.Path); return Task.CompletedTask; }));
        options.ContextMenu = menu; options.Click += (_, _) => { if (!busy) menu.Open(options); };
        actions.Children.Add(options);
        actions.Children.Add(Button("Convertir WBH", ConvertSelected));
        moveRoot = Button("Mover a raíz", () => MoveSelected(true)); actions.Children.Add(moveRoot);
        moveDestination = Button("Mover PDF al destino", () => MoveSelected(false)); actions.Children.Add(moveDestination);
        footer.Children.Add(actions); footer.Children.Add(cancelButton); Grid.SetRow(footer, 1);
        status.FontSize = 12;
        status[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("PizarrasSecondaryTextBrush");
        Grid.SetRow(status, 3); outer.Children.Add(status);
        var filesPage = new Grid { RowDefinitions = new RowDefinitions("*,Auto") }; filesPage.Children.Add(body); filesPage.Children.Add(footer);
        var schedulePage = new StackPanel { Spacing = 12, Margin = new Thickness(24) };
        schedulePage.Children.Add(new TextBlock { Text = "Horario", FontSize = 22, FontWeight = FontWeight.SemiBold });
        schedulePage.Children.Add(new TextBlock { Text = "Configura las clases, los recreos y sus intervalos para asignar cada archivo automáticamente.", TextWrapping = TextWrapping.Wrap });
        schedulePage.Children.Add(Button("Editar horario", EditSchedule));
        var optionsContent = new StackPanel { Spacing = 12 };
        optionsContent.Children.Add(OptionsCard("Apariencia", "Elige el modo visual de la aplicación. Se aplica al instante y se conserva al reiniciar.", new AppearanceControl(appearance)));
        optionsRootPath.Text = "Carpeta actual: " + (string.IsNullOrWhiteSpace(root) ? "Sin seleccionar" : root);
        optionsDestinationPath.Text = "Destino actual: " + (string.IsNullOrWhiteSpace(config.Destination) ? "Sin seleccionar" : config.Destination);
        optionsContent.Children.Add(OptionsCard("Almacenamiento",
            "Configura la carpeta de origen y el destino de los PDF convertidos. Estas elecciones usan las preferencias existentes.",
            optionsRootPath, Button("Elegir carpeta de origen…", ChooseRoot), optionsDestinationPath, Button("Elegir destino de PDF…", ChooseDestination)));
        optionsContent.Children.Add(OptionsCard("Servicios conectados",
            "Las conexiones actuales se mantienen en la sesión de la aplicación. OneDrive y Google Drive se preparan desde sus diálogos actuales; la autenticación web de 1.1.00 queda fuera de esta versión.",
            Button("Configurar OneDrive…", () => CloudSettings("OneDrive")), Button("Configurar Google Drive…", () => CloudSettings("Google Drive"))));
        optionsContent.Children.Add(OptionsCard("Acerca de",
            "Consulta la identidad y la versión real de la compilación.", Button("Acerca de " + AppVersion.Name, ShowAbout)));
        var optionsPage = new ScrollViewer { Content = new StackPanel { Spacing = 12, Margin = new Thickness(24), Children = { new TextBlock { Text = "Opciones", FontSize = 22, FontWeight = FontWeight.SemiBold }, optionsContent } } };
        sections.ItemsSource = new[] { new TabItem { Header = "Archivos", Content = filesPage }, new TabItem { Header = "Horario", Content = schedulePage }, new TabItem { Header = "Opciones", Content = optionsPage } };
        sections.SelectedIndex = 0; sections.SelectionChanged += (_, _) => { if (sections.SelectedIndex == 1) { } };
        Grid.SetRow(sections, 1); outer.Children.Add(sections); Content = outer;
        table.ContextMenu = DocumentMenu(false);
        table.AddHandler(PointerPressedEvent, (_, e) =>
        {
            if (busy || !e.GetCurrentPoint(table).Properties.IsRightButtonPressed) return;
            var row = (e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<DataGridRow>().FirstOrDefault();
            SelectContextRow(row?.DataContext as DocumentItem);
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        table.DoubleTapped += async (_, _) => await Guard(OpenSelected);
        table.SelectionChanged += async (_, _) => { if (!scanning) await Guard(SelectionPreview); };
        table.KeyDown += async (_, e) =>
        {
            if (busy) return;
            Func<Task>? action = e.Key == Key.Delete ? DeleteSelected : e.KeyModifiers.HasFlag(KeyModifiers.Control) ? e.Key switch { Key.C => () => Copy(false), Key.X => () => Copy(true), Key.V => Paste, _ => null } : null;
            if (action is not null) { e.Handled = true; await Guard(action); }
        };
        cancelButton.Click += (_, _) => operationCancellation?.Cancel();
        locations.SelectionChanged += async (_, _) => { if (!initializing && locations.SelectedItem is string location && !FileService.Same(location, root)) await Guard(() => SetRoot(location)); };
        previewScroll.PointerWheelChanged += async (_, e) => { if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) { e.Handled = true; if (!busy && previewPath is not null) await Guard(() => SetZoom(zoom * (e.Delta.Y > 0 ? 1.15 : 1 / 1.15))); } };
        UpdatePreviewControls();
        var resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        resizeTimer.Tick += async (_, _) => { resizeTimer.Stop(); await Guard(RenderPreview); };
        previewScroll.SizeChanged += (_, _) => { resizeTimer.Stop(); resizeTimer.Start(); };
        timer.Tick += async (_, _) => await Guard(() => Refresh(false));
        Opened += async (_, _) => await Guard(async () =>
        {
            LoadColumnWidths();
            if (config.LoadError is not null) await Inform("Configuración", config.LoadError + "\nLos archivos originales se conservan.");
            UpdateLocations(); root = config.Root;
            if (!Directory.Exists(root)) root = FileService.DetectRoots().FirstOrDefault() ?? "";
            optionsRootPath.Text = "Carpeta actual: " + (string.IsNullOrWhiteSpace(root) ? "Sin seleccionar" : root);
            optionsDestinationPath.Text = "Destino actual: " + (string.IsNullOrWhiteSpace(config.Destination) ? "Sin seleccionar" : config.Destination);
            if (root != "") { UpdateLocations(); locations.SelectedItem = root; }
            initializing = false; await Refresh(true); timer.Start();
        });
        Closing += (_, e) => { if (busy) { e.Cancel = true; operationCancellation?.Cancel(); status.Text = "Cancelando… Cierra la ventana cuando termine la operación actual."; return; } try { SaveColumnWidths(); } catch (Exception ex) { AppLog.Write(ex.Message); } timer.Stop(); resizeTimer.Stop(); previewCancellation?.Cancel(); (previewImage.Source as IDisposable)?.Dispose(); };
    }
    private void SelectSection(int index) { if (!busy) sections.SelectedIndex = index; }
    private static Border Surface(Control content) => new() { Child = content, CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), ClipToBounds = true, [!Border.BackgroundProperty] = new DynamicResourceExtension("PizarrasSurfaceBrush"), [!Border.BorderBrushProperty] = new DynamicResourceExtension("PizarrasBorderBrush") };
    private static Border OptionsCard(string title, string description, params Control[] controls)
    {
        var content = new StackPanel { Spacing = 10, Margin = new Thickness(16) };
        content.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = FontWeight.SemiBold });
        content.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap });
        foreach (var control in controls) content.Children.Add(control);
        return Surface(content);
    }
    private Button PreviewButton(string text, string tip, Func<Task> action)
    {
        var button = Button(text, action); button.Padding = new Thickness(6, 5); button.Margin = new Thickness(0); button.MinWidth = 28;
        ToolTip.SetTip(button, tip); Avalonia.Automation.AutomationProperties.SetName(button, tip); return button;
    }
    private Task SetZoom(double value) { zoom = Math.Clamp(value, .25, 5); return RenderPreview(); }
    private void UpdatePreviewControls()
    {
        var available = !busy && previewPath is not null && pageCount > 0;
        previousPage.IsEnabled = available && page > 0; nextPage.IsEnabled = available && page + 1 < pageCount;
        zoomOut.IsEnabled = available && zoom > .25; zoomIn.IsEnabled = available && zoom < 5; fitPage.IsEnabled = available;
        zoomText.Text = previewPath is null ? "—" : $"{zoom:P0}";
        ToolTip.SetTip(zoomText, "Zoom relativo al ajuste de página");
    }
    private void SelectContextRow(DocumentItem? item)
    {
        if (item is not null && !table.SelectedItems.Contains(item)) { table.SelectedItems.Clear(); table.SelectedItem = item; }
    }
    private ContextMenu DocumentMenu(bool viewer)
    {
        var context = new ContextMenu(); context.Classes.Add("documentMenu");
        RenderOptions.SetTextRenderingMode(context, TextRenderingMode.Antialias);
        string[] snapshot = [];
        bool TargetValid() => !busy && snapshot.SequenceEqual(Selected().Select(x => x.FullPath)) && (!viewer || (snapshot.Length == 1 && snapshot[0] == previewPath));
        var entries = new List<(MenuItem Item, Func<bool> Enabled)>();
        void Command(string title, Func<Task> action, Func<bool> enabled)
        {
            var item = Menu(title, () => TargetValid() && enabled() ? action() : Task.CompletedTask);
            entries.Add((item, enabled)); context.Items.Add(item);
        }
        bool Any() => Selected().Count > 0;
        bool Pdf() => Selected().Any(x => x.Kind == "PDF");
        Command("Abrir en visor del sistema", OpenSelected, Any);
        Command("Abrir carpeta", () => { OpenPath(Path.GetDirectoryName(Selected()[0].FullPath)!); return Task.CompletedTask; }, () => Selected().Count == 1);
        Command("Cambiar clase…", ChangeClass, () => Selected().Count == 1);
        if (!viewer)
        {
            context.Items.Add(new Separator());
            Command("Copiar", () => Copy(false), Any); Command("Cortar", () => Copy(true), Any);
            Command("Pegar", Paste, () => clipboard.Length > 0 && Directory.Exists(root));
            context.Items.Add(new Separator());
            Command("Convertir WBH", ConvertSelected, () => Selected().Any(x => x.Kind == "WBH"));
            Command("Mover PDF a raíz", () => MoveSelected(true), () => Pdf() && FileService.RootMoveAllowed(root));
            Command("Mover PDF al destino", () => MoveSelected(false), Pdf);
            Command("Borrar…", DeleteSelected, Any);
        }
        context.Opened += (_, _) =>
        {
            snapshot = Selected().Select(x => x.FullPath).ToArray();
            foreach (var entry in entries) entry.Item.IsEnabled = TargetValid() && entry.Enabled();
        };
        return context;
    }
    private void AddColumn(string header, string property, double width) => table.Columns.Add(new DataGridTextColumn { Header = header, Binding = new Binding(property), SortMemberPath = property == nameof(DocumentItem.Date) ? nameof(DocumentItem.Timestamp) : property == nameof(DocumentItem.Size) ? nameof(DocumentItem.Bytes) : property, Width = new DataGridLength(width), MinWidth = 40 });
    private static void Add(Grid grid, Control child, int column) { Grid.SetColumn(child, column); grid.Children.Add(child); }
    private Button Button(string text, Func<Task> action)
    {
        var button = new Button { Content = text, Margin = new Thickness(2), Padding = new Thickness(12, 8) };
        button.Click += async (_, _) => { if (!busy) await Guard(action); }; return button;
    }
    private MenuItem Menu(string text, Func<Task> action) { var item = new MenuItem { Header = text }; item.Click += async (_, _) => { if (!busy) await Guard(action); }; return item; }
    private async Task Guard(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) { status.Text = "Operación cancelada. Los archivos pendientes se conservan."; }
        catch (Exception ex) { AppLog.Write(ex.ToString()); await Inform("No se pudo completar", ex.Message); }
    }
    private static void OpenPath(string path) { if (string.IsNullOrWhiteSpace(path)) return; Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
    private List<DocumentItem> Selected() => table.SelectedItems.Cast<DocumentItem>().ToList();
    private async Task<string?> PickFolder(string title)
    { var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title, AllowMultiple = false }); return folders.FirstOrDefault()?.TryGetLocalPath(); }
    private async Task ChooseRoot() { var folder = await PickFolder("Carpeta de origen"); if (folder is not null) await SetRoot(folder); }
    private async Task SetRoot(string folder)
    {
        if (busy) return;
        config.Set("selected_root", config.Paths.Store(folder));
        var recent = (config.Preferences["recent_roots"] as JsonArray ?? []).Select(n => config.Paths.Resolve(n!.GetValue<string>())).Prepend(folder).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).Select(x => (JsonNode?)JsonValue.Create(config.Paths.Store(x))).ToArray();
        config.Set("recent_roots", new JsonArray(recent)); root = folder; UpdateLocations(); await Refresh(true);
        optionsRootPath.Text = "Carpeta actual: " + root;
    }
    private void UpdateLocations()
    {
        var all = FileService.DetectRoots().Concat((config.Preferences["recent_roots"] as JsonArray ?? []).Select(n => config.Paths.Resolve(n!.GetValue<string>())));
        if (root != "") all = all.Prepend(root);
        var prior = initializing; initializing = true; locations.ItemsSource = all.Distinct(StringComparer.OrdinalIgnoreCase).ToList(); locations.SelectedItem = root; initializing = prior;
    }
    private async Task ChooseDestination() { var folder = await PickFolder("Destino de los PDF"); if (folder is not null) { config.Set("pdf_destination", config.Paths.Store(folder)); optionsDestinationPath.Text = "Destino actual: " + folder; UpdateButtons(); } }
    private void UpdateButtons()
    {
        UpdatePreviewControls();
        moveRoot.IsEnabled = !busy && FileService.RootMoveAllowed(root); moveDestination.IsEnabled = !busy && !string.IsNullOrEmpty(root) && !string.IsNullOrEmpty(config.Destination) && !FileService.Same(root, config.Destination);
        ToolTip.SetTip(moveDestination, string.IsNullOrEmpty(config.Destination) ? "Elige el destino en Opciones" : config.Destination);
    }
    private async Task Refresh(bool force)
    {
        if (scanning || busy || modal) return;
        scanning = true;
        try
        {
            if (!force) UpdateLocations();
            if (string.IsNullOrEmpty(root)) { UpdateButtons(); return; }
            var scanRoot = root; var next = await Task.Run(() => FileService.Scan(scanRoot));
            if (scanRoot != root) return;
            foreach (var item in next)
            {
                var choices = ScheduleRules.Choices(item.Timestamp, config.Schedule); var assignment = config.Assignment(item);
                var value = choices.FirstOrDefault(x => assignment is not null && x.SequenceEqual(assignment)) ?? choices.FirstOrDefault();
                item.Slot = value?[0] ?? (config.Schedule.Count == 0 ? "Sin horario" : "Fuera de horario"); item.Class = value?[1] ?? "—";
            }
            if (force || !items.Select(x => (x.FullPath, x.Bytes, x.Timestamp, x.Class)).SequenceEqual(next.Select(x => (x.FullPath, x.Bytes, x.Timestamp, x.Class))))
            {
                var selected = Selected().Select(x => x.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                items = next; table.ItemsSource = items;
                foreach (var item in items.Where(x => selected.Contains(x.FullPath))) table.SelectedItems.Add(item);
            }
            status.Text = Directory.Exists(root) ? $"{items.Count} archivos · {items.Count(x => x.Kind == "PDF")} PDF · {items.Count(x => x.Kind == "WBH")} WBH" : "La ubicación no está disponible. Conecta el USB o elige otra carpeta.";
            emptyTable.IsVisible = items.Count == 0;
            emptyTable.Text = Directory.Exists(root) ? "No hay archivos PDF ni WBH en esta ubicación." : "Ubicación no disponible. Conecta el USB o cambia de carpeta.";
            UpdateButtons();
        }
        finally { scanning = false; }
        await SelectionPreview();
    }
    private async Task SelectionPreview()
    {
        var selected = Selected(); var path = selected.Count == 1 && selected[0].Kind == "PDF" ? selected[0].FullPath : null;
        if (path == previewPath && path is not null) return;
        previewPath = path; page = 0; pageCount = 0; zoom = 1;
        (previewImage.Source as IDisposable)?.Dispose(); previewImage.Source = null;
        previewImage.Width = double.NaN; previewImage.Height = double.NaN; pageText.Text = "—";
        previewScroll.Offset = default; await RenderPreview();
    }
    private async Task RenderPreview()
    {
        var generation = ++previewGeneration; previewCancellation?.Cancel(); previewCancellation?.Dispose(); previewCancellation = new(TimeSpan.FromSeconds(45));
        var path = previewPath;
        UpdatePreviewControls();
        if (path is null) { (previewImage.Source as IDisposable)?.Dispose(); previewImage.Source = null; pageCount = 0; previewText.Text = Selected().Count > 1 ? "Varios archivos seleccionados. Elige un PDF para verlo." : Selected().FirstOrDefault()?.Kind == "WBH" ? "Archivo WBH. Usa Convertir WBH para generar su PDF." : "Selecciona un PDF para verlo aquí."; pageText.Text = "—"; UpdatePreviewControls(); return; }
        previewText.Text = "Cargando " + Path.GetFileName(path) + "…";
        try
        {
            var width = Math.Max(200, previewScroll.Bounds.Width - 24); var height = Math.Max(200, previewScroll.Bounds.Height - 24); var requestedPage = page; var requestedZoom = zoom; var ct = previewCancellation.Token;
            var result = await Task.Run(() => PdfPreview.Render(path, requestedPage, width, height, requestedZoom, ct), ct);
            if (generation != previewGeneration) return;
            using var stream = new MemoryStream(result.Png); var bitmap = new Bitmap(stream);
            // Keep the displayed size in logical units, independent of the
            // native renderer's output resolution and display scaling.
            var displayScale = Math.Min(width / bitmap.PixelSize.Width, height / bitmap.PixelSize.Height) * requestedZoom;
            previewImage.Width = bitmap.PixelSize.Width * displayScale; previewImage.Height = bitmap.PixelSize.Height * displayScale;
            previewImage.Stretch = Stretch.Fill;
            var previous = previewImage.Source as IDisposable; previewImage.Source = bitmap; previous?.Dispose();
            page = result.Index; pageCount = result.Count; pageText.Text = $" {page + 1} / {pageCount} "; previewText.Text = Path.GetFileName(path);
            UpdatePreviewControls();
        }
        catch (Exception ex)
        {
            if (generation != previewGeneration) return;
            (previewImage.Source as IDisposable)?.Dispose(); previewImage.Source = null;
            previewText.Text = ex is OperationCanceledException ? "El PDF ha tardado demasiado. Prueba el visor del sistema." : "No se puede mostrar este PDF: " + ex.Message;
            pageCount = 0; pageText.Text = "—"; UpdatePreviewControls();
        }
    }
    private Task OpenSelected() { foreach (var item in Selected()) OpenPath(item.FullPath); return Task.CompletedTask; }
    private Task Copy(bool asCut) { clipboard = Selected().Select(x => x.FullPath).ToArray(); cut = asCut; status.Text = $"{clipboard.Length} archivos preparados para {(cut ? "cortar" : "copiar")}."; return Task.CompletedTask; }
    private async Task RunBatch(string title, List<string> paths, Func<string, CancellationToken, Task> action)
    {
        busy = true; actions.IsEnabled = false; locations.IsEnabled = false; cancelButton.IsVisible = true; UpdateButtons(); operationCancellation = new();
        var completed = 0; var errors = new List<string>(); var cancelled = false;
        try
        {
            foreach (var path in paths)
            {
                if (operationCancellation.IsCancellationRequested) { cancelled = true; break; }
                status.Text = $"{title}: {completed + errors.Count + 1}/{paths.Count} · {Path.GetFileName(path)}";
                try { await Task.Run(() => action(path, operationCancellation.Token)); completed++; }
                catch (OperationCanceledException) { cancelled = true; break; }
                catch (Exception ex) { errors.Add(Path.GetFileName(path) + ": " + ex.Message); AppLog.Write(ex.ToString()); }
            }
        }
        finally { busy = false; actions.IsEnabled = true; locations.IsEnabled = true; cancelButton.IsVisible = false; operationCancellation.Dispose(); operationCancellation = null; await Refresh(true); }
        await Inform(title, $"Completados: {completed}. Fallidos: {errors.Count}." + (cancelled ? "\nOperación cancelada; los pendientes se conservan." : "") + (errors.Count > 0 ? "\n\n" + string.Join("\n", errors.Take(8)) : ""));
    }
    private async Task Paste()
    {
        if (clipboard.Length == 0 || !Directory.Exists(root)) return;
        var paths = clipboard.Where(File.Exists).ToList(); var destination = root; var isMove = cut;
        if (isMove && !await Confirm("Pegar", $"¿Mover {paths.Count} archivos a {destination}?")) return;
        await RunBatch("Pegar", paths, async (path, token) => { await FileService.Transfer(path, destination, isMove, false, token); });
        if (isMove) clipboard = clipboard.Where(File.Exists).ToArray();
    }
    private async Task MoveSelected(bool toRoot)
    {
        if (toRoot && !FileService.RootMoveAllowed(root)) { await Inform("Mover a raíz", "Disponible en unidades USB y carpetas Archivo de pizarra."); return; }
        if (!toRoot && string.IsNullOrEmpty(config.Destination)) await ChooseDestination();
        var destination = toRoot ? root : config.Destination; if (string.IsNullOrEmpty(destination)) return;
        var selected = Selected(); var paths = (selected.Count > 0 ? selected : items).Where(x => x.Kind == "PDF" && !FileService.Same(Path.GetDirectoryName(x.FullPath)!, destination)).Select(x => x.FullPath).ToList();
        if (paths.Count == 0) { await Inform("Mover", "No hay PDF pendientes de mover en la selección."); return; }
        var cleanup = new CheckBox { Content = "Limpiar carpetas de origen sin PDF (borra también WBH y otros archivos)", IsChecked = false };
        if (!await Confirm("Mover PDF", $"¿Mover {paths.Count} PDF a\n{destination}?", toRoot ? cleanup : null)) return;
        var clean = toRoot && cleanup.IsChecked == true; var sourceRoot = root;
        await RunBatch("Mover PDF", paths, async (path, token) => { await FileService.Transfer(path, destination, true, toRoot, token); if (clean) FileService.Cleanup(Path.GetDirectoryName(path)!, sourceRoot); });
    }
    private async Task DeleteSelected()
    {
        var paths = Selected().Select(x => x.FullPath).ToList(); if (paths.Count == 0) return;
        if (!await Confirm("Borrar archivos", $"¿Eliminar definitivamente {paths.Count} archivos seleccionados?\nNo se enviarán a la papelera.", action: "Eliminar", destructive: true)) return;
        await RunBatch("Borrar", paths, (path, token) => { token.ThrowIfCancellationRequested(); File.Delete(path); return Task.CompletedTask; });
    }
    private async Task ConvertSelected()
    {
        if (!Directory.Exists(root)) return;
        var selected = Selected(); var paths = (selected.Count > 0 ? selected : items).Where(x => x.Kind == "WBH").Select(x => x.FullPath).ToList();
        if (paths.Count == 0) { await Inform("Convertir", "No hay WBH en la selección."); return; }
        var remove = new CheckBox { Content = "Eliminar cada WBH después de comprobar su PDF", IsChecked = false };
        if (!await Confirm("Convertir WBH", $"¿Convertir {paths.Count} WBH a PDF en\n{root}?", remove)) return;
        var delete = remove.IsChecked == true; var destination = root;
        await RunBatch("Conversión WBH", paths, async (path, token) =>
        {
            var hash = FileService.Hash(path); var output = FileService.Unique(destination, Path.GetFileNameWithoutExtension(path) + ".pdf");
            var result = WbhConverter.Convert(path, output);
            var rendered = await PdfPreview.Render(output, 0, 100, 100, 1, token);
            if (rendered.Count != result.Pages) throw new IOException("No coincide el número de páginas; se conserva el WBH.");
            if (delete) { if (!hash.SequenceEqual(FileService.Hash(path))) throw new IOException("El WBH cambió; se conserva."); File.Delete(path); }
        });
    }
    private void LoadColumnWidths()
    {
        if (config.Preferences["column_widths"] is JsonArray widths)
            for (var i = 0; i < Math.Min(widths.Count, table.Columns.Count); i++) if (widths[i]?.GetValue<double>() is double w && w >= 40 && w < 3000) table.Columns[i].Width = new DataGridLength(w);
    }
    private void SaveColumnWidths() => config.Set("column_widths", new JsonArray(table.Columns.Select(c => (JsonNode?)JsonValue.Create(c.ActualWidth)).ToArray()));
    private async Task Inform(string title, string message) => await Dialog(title, message, false, null);
    private static Window CreateAboutWindow() => SecondaryUi.Message("Acerca de " + AppVersion.Name,
        AppVersion.Name + "\nVersión " + AppVersion.Display, false, null, "Aceptar", false);
    private async Task ShowAbout()
    {
        var wasModal = modal; modal = true;
        try { await CreateAboutWindow().ShowDialog<bool>(this); }
        finally { modal = wasModal; }
    }
    private async Task ShowAppearance()
    {
        var wasModal = modal; modal = true;
        try { await SecondaryUi.Message("Apariencia", "", false, new AppearanceControl(appearance), "Aceptar", false).ShowDialog<bool>(this); }
        finally { modal = wasModal; }
    }
    private Task<bool> Confirm(string title, string message, Control? extra = null, string action = "Continuar", bool destructive = false) => Dialog(title, message, true, extra, action, destructive);
    private async Task<bool> Dialog(string title, string message, bool confirm, Control? extra, string action = "Continuar", bool destructive = false)
    {
        var win = SecondaryUi.Message(title, message, confirm, extra, action, destructive);
        var wasModal = modal; modal = true; try { return await win.ShowDialog<bool>(this); } finally { modal = wasModal; }
    }
}
