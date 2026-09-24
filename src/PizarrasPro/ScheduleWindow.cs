using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Text.Json;

namespace PizarrasPro;

public sealed partial class MainWindow
{
    private async Task EditSchedule()
    {
        var win = CreateScheduleWindow();
        modal = true; try { await win.ShowDialog(this); } finally { modal = false; } await Refresh(true);
    }
    private Window CreateScheduleWindow()
    {
        var win = SecondaryUi.Window("Horario semanal", 1040, 620, 740, 420);
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), Margin = new Thickness(20) };
        layout.Children.Add(new TextBlock { Text = "Lunes a viernes · Horas HH:MM · Casilla vacía = sin clase\nTolerancia de 5 minutos al final; se prioriza la clase anterior, excepto frente a Recreo.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14) });
        var rows = new StackPanel { Spacing = 6, MinWidth = 920 };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("80,80,*,*,*,*,*,80"), ColumnSpacing = 6 };
        var names = new[] { "Inicio", "Fin", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "" };
        for (var i = 0; i < names.Length; i++) Add(header, new TextBlock { Text = names[i], FontWeight = FontWeight.SemiBold }, i);
        rows.Children.Add(header);
        var entries = new List<(string Kind, TextBox Start, TextBox End, TextBox[] Classes, Grid Grid)>();
        void AddRow(ScheduleRow row)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("80,80,*,*,*,*,*,80"), ColumnSpacing = 6 };
            var start = new TextBox { Text = row.Start, Watermark = "08:30" }; var end = new TextBox { Text = row.End, Watermark = "09:25" }; Add(grid, start, 0); Add(grid, end, 1);
            var classes = row.Classes.Select(x => new TextBox { Text = x }).ToArray();
            if (row.Kind == "break")
            {
                var band = new Border { CornerRadius = new CornerRadius(4), Child = new TextBlock { Text = "RECREO", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } }; band[!Border.BackgroundProperty] = new DynamicResourceExtension("PizarrasWorkspaceBrush"); Grid.SetColumnSpan(band, 5); Add(grid, band, 2);
            }
            else for (var i = 0; i < 5; i++) Add(grid, classes[i], i + 2);
            var remove = new Button { Content = "Quitar" }; remove.Click += (_, _) => { rows.Children.Remove(grid); entries.RemoveAll(e => e.Grid == grid); }; Add(grid, remove, 7);
            entries.Add((row.Kind, start, end, classes, grid)); rows.Children.Add(grid);
        }
        foreach (var row in config.Schedule) AddRow(row);
        var scroll = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto }; Grid.SetRow(scroll, 1); layout.Children.Add(scroll);
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10) }; error[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("PizarrasDangerBrush"); Grid.SetRow(error, 2); layout.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }; Grid.SetRow(buttons, 3); layout.Children.Add(buttons);
        var add = new Button { Content = "Añadir clase" }; add.Click += (_, _) => AddRow(new()); buttons.Children.Add(add);
        var recess = new Button { Content = "Añadir recreo" }; recess.Click += (_, _) => AddRow(new() { Kind = "break" }); buttons.Children.Add(recess);
        var save = new Button { Content = "Guardar horario" }; save.Click += (_, _) =>
        {
            try { config.SaveSchedule(entries.Select(e => new ScheduleRow { Kind = e.Kind, Start = e.Start.Text?.Trim() ?? "", End = e.End.Text?.Trim() ?? "", Classes = e.Classes.Select(x => x.Text?.Trim() ?? "").ToArray() }).ToList()); win.Close(); }
            catch (Exception ex) { error.Text = ex.Message; }
        }; buttons.Children.Add(save);
        var cancel = new Button { Content = "Cancelar", IsCancel = true }; cancel.Click += (_, _) => win.Close(); buttons.Children.Add(cancel); win.Content = layout;
        win.Opened += (_, _) => entries.FirstOrDefault().Start?.Focus();
        return win;
    }
    private async Task ChangeClass()
    {
        var selected = Selected(); if (selected.Count != 1) { await Inform("Cambiar clase", "Selecciona un solo archivo."); return; }
        var item = selected[0]; var choices = ScheduleRules.Choices(item.Timestamp, config.Schedule);
        if (choices.Count < 2) { await Inform("Cambiar clase", "Este archivo no coincide con dos clases del horario."); return; }
        var list = new ComboBox { ItemsSource = new[] { "Automática (clase anterior)" }.Concat(choices.Select(x => x[0] + ": " + x[1])).ToList(), SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        var saved = config.Assignment(item); if (saved is not null) list.SelectedIndex = choices.FindIndex(x => x.SequenceEqual(saved)) + 1;
        if (await Confirm("Cambiar clase", Path.GetFileName(item.FullPath), list, action: "Guardar")) { config.SetAssignment(item, list.SelectedIndex > 0 ? choices[list.SelectedIndex - 1] : null); await Refresh(true); }
    }
}
