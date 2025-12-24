using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Threading;
using DataGridSample.Models;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia;
using System;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Globalization;
using Avalonia.VisualTree;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using System.Text.RegularExpressions;

namespace DataGridSample
{
    public partial class DataGridPage : UserControl
    {
        public ICommand ShowCountryDetailsCommand { get; }
        private bool _ignoreFlagTextChange;
        private bool _ignoreFlagCheckChange;

        public DataGridPage()
        {
            this.InitializeComponent();
            ShowCountryDetailsCommand = new DelegateCommand<string?>(ShowCountryDetails);

            var dataGridSortDescription = DataGridSortDescription.FromPath(nameof(Country.Region), ListSortDirection.Ascending, new ReversedStringComparer());
            var collectionView1 = new DataGridCollectionView(Countries.All);
            collectionView1.SortDescriptions.Add(dataGridSortDescription);
            var dg1 = this.Get<DataGrid>("dataGrid1");
            dg1.IsReadOnly = true;
            dg1.Sorting += (s, a) =>
            {
                var binding = (a.Column as DataGridBoundColumn)?.Binding as Binding;

                if (binding?.Path is string property
                    && property == dataGridSortDescription.PropertyPath
                    && !collectionView1.SortDescriptions.Contains(dataGridSortDescription))
                {
                    collectionView1.SortDescriptions.Add(dataGridSortDescription);
                }
            };
            dg1.ItemsSource = collectionView1;
            dg1.RowDetailsTemplateSelector = CreateCountryDetailsTemplate();

            var showDetailsToggle = this.Get<CheckBox>("ShowDetailsToggle");
            var detailsThreshold = this.Get<NumericUpDown>("DetailsThreshold");
            void RebindVisibilitySelector()
            {
                var enabled = showDetailsToggle.IsChecked == true;
                dg1.RowDetailsVisibilityMode = enabled
                    ? DataGridRowDetailsVisibilityMode.Visible
                    : DataGridRowDetailsVisibilityMode.Collapsed;
                if (!enabled)
                {
                    dg1.RowDetailsVisibilitySelector = null;
                    return;
                }

                // Force property change even if the lambda would be reference-equal
                dg1.RowDetailsVisibilitySelector = null;
                dg1.RowDetailsVisibilitySelector = item =>
                {
                    if (item is Country c)
                    {
                        var threshold = detailsThreshold.Value;
                        if (threshold == null)
                            return false;
                        return c.GDP >= threshold.Value;
                    }
                    return false;
                };
            }
            showDetailsToggle.IsCheckedChanged += (_, __) => RebindVisibilitySelector();
            detailsThreshold.ValueChanged += (_, __) => RebindVisibilitySelector();
            RebindVisibilitySelector();

            var dg2 = this.Get<DataGrid>("dataGridGrouping");
            dg2.IsReadOnly = true;

            var collectionView2 = new DataGridCollectionView(Countries.All);
            collectionView2.GroupDescriptions.Add(new DataGridPathGroupDescription("Region"));

            dg2.ItemsSource = collectionView2;

            var dg3 = this.Get<DataGrid>("dataGridEdit");
            dg3.IsReadOnly = false;

            var list = new ObservableCollection<Person>
            {
                new Person { FirstName = "John", LastName = "Doe" , Age = 30},
                new Person { FirstName = "Elizabeth", LastName = "Thomas", IsBanned = true , Age = 40 },
                new Person { FirstName = "Zack", LastName = "Ward" , Age = 50 }
            };
            DataGrid3Source = list;

            var addButton = this.Get<Button>("btnAdd");
            addButton.Click += (a, b) => list.Add(new Person());

            var dgFilters = this.Get<DataGrid>("dataGridFilters");
            dgFilters.ItemsSource = BuildFilterDemoItems();
            dgFilters.AddHandler(InputElement.PointerPressedEvent, OnFiltersPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            dg1.ContextMenuOpening += OnGridContextMenuOpening;

            DataContext = this;
        }

        public IEnumerable<Person> DataGrid3Source { get; }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class ReversedStringComparer : IComparer<object>, IComparer
        {
            public int Compare(object? x, object? y)
            {
                if (x is string left && y is string right)
                {
                    var reversedLeft = new string(left.Reverse().ToArray());
                    var reversedRight = new string(right.Reverse().ToArray());
                    return reversedLeft.CompareTo(reversedRight);
                }

                return Comparer.Default.Compare(x, y);
            }
        }

        private void NumericUpDown_OnTemplateApplied(object sender, TemplateAppliedEventArgs e)
        {
            // We want to focus the TextBox of the NumericUpDown. To do so we search for this control when the template
            // is applied, but we postpone the action until the control is actually loaded. 
            if (e.NameScope.Find<TextBox>("PART_TextBox") is {} textBox)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }, DispatcherPriority.Loaded);
            }
        }

        private Func<object, IDataTemplate> CreateCountryDetailsTemplate()
        {
            return item =>
            {
                if (item is Country)
                {
                    return new FuncDataTemplate<Country>((country, _) =>
                    {
                        var stack = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Spacing = 4
                        };
                        stack.Children.Add(new TextBlock { Text = $"Region: {country.Region}" });
                        stack.Children.Add(new TextBlock { Text = $"Population: {country.Population:n0}" });
                        stack.Children.Add(new TextBlock { Text = $"GDP: {country.GDP:n0}" });
                        stack.Children.Add(new TextBlock { Text = $"Literacy: {country.LiteracyPercent:0.0}%"} );
                        return new Border
                        {
                            Background = Brushes.LightGray,
                            CornerRadius = new CornerRadius(4),
                            Padding = new Thickness(8),
                            Child = stack
                        };
                    }, true);
                }
                return null!;
            };
        }

        private IEnumerable<object> BuildFilterDemoItems()
        {
            var items = new List<object>();
            for (int i = 0; i < 25; i++)
            {
                var offset = i * 0x10;
                var flags = (i % 2 == 0 ? 0x1 : 0) | (i % 3 == 0 ? 0x4 : 0) | (i % 5 == 0 ? 0x8 : 0);
                items.Add(new
                {
                    Name = $"Item {i}",
                    Index = i,
                    OffsetHex = $"0x{offset:X}",
                    Flags = $"0x{flags:X}",
                    Description = $"Offset {offset} (0x{offset:X}), flags {flags} (0x{flags:X})"
                });
            }
            return items;
        }

        private void OnScrollFilterDemo(object? sender, RoutedEventArgs e)
        {
            var dg = this.Get<DataGrid>("dataGridFilters");
            var idx = (int)(this.Get<NumericUpDown>("FilterDemoIndex").Value ?? 0);
            var item = (dg.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Skip(idx).FirstOrDefault();
            if (item != null)
            {
                dg.ScrollIntoView(item);
            }
        }

        private void OnSelectFilterDemo(object? sender, RoutedEventArgs e)
        {
            var dg = this.Get<DataGrid>("dataGridFilters");
            var idx = (int)(this.Get<NumericUpDown>("FilterDemoIndex").Value ?? 0);
            var item = (dg.ItemsSource as System.Collections.IEnumerable)?.Cast<object>().Skip(idx).FirstOrDefault();
            if (item != null)
            {
                dg.SelectItem(item);
            }
        }

        private void OnFiltersPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not DataGrid dg)
                return;
            var point = e.GetPosition(dg);
            var hit = dg.HitTestCell(point);
            if (hit.IsEmpty)
            {
                this.Get<TextBlock>("HitTestInfo").Text = "Click a cell to see hit info.";
            }
            else
            {
                var col = hit.Column?.Header?.ToString() ?? "(no column)";
                var itemText = hit.Item?.ToString() ?? "(null)";
                this.Get<TextBlock>("HitTestInfo").Text = $"Cell: {col}, Item: {itemText}";
            }
        }

        private void OnFiltersPointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is not DataGrid dg)
                return;
            if (this.Get<CheckBox>("HoverTooltipToggle").IsChecked != true)
            {
                ToolTip.SetTip(dg, null);
                return;
            }
            var hit = dg.HitTestCell(e.GetPosition(dg));
            if (hit.IsEmpty || hit.Column == null)
            {
                ToolTip.SetTip(dg, null);
                return;
            }

            var header = hit.Column.Header?.ToString() ?? "(col)";
            var itemIndex = GetItemIndex(dg, hit.Item);
            var valueText = hit.Item?.ToString() ?? "(null)";
            var idxText = itemIndex >= 0 ? $"Row {itemIndex}" : "Row ?";
            ToolTip.SetTip(dg, $"{idxText}, {header}: {valueText}");
        }

        private void OnFiltersPointerExited(object? sender, PointerEventArgs e)
        {
            if (sender is DataGrid dg)
            {
                ToolTip.SetTip(dg, null);
            }
        }

        private int GetItemIndex(DataGrid dg, object? item)
        {
            if (item == null || dg.ItemsSource == null)
                return -1;
            int i = 0;
            foreach (var it in dg.ItemsSource.Cast<object>())
            {
                if (ReferenceEquals(it, item))
                    return i;
                i++;
            }
            return -1;
        }

        private void ShowCountryDetails(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            var dlg = new Window
            {
                Width = 320,
                Height = 200,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = $"You clicked: {name}", Classes = { "h3" } },
                        new TextBlock { Text = "This simulates a hyperlink action; hook into navigation here." }
                    }
                }
            };
            dlg.Show();
        }

        private void OnWpfThemeChanged(object? sender, RoutedEventArgs e)
        {
            var toggle = this.Get<CheckBox>("WpfThemeToggle");
            var enable = toggle.IsChecked == true;
            ApplyThemeClass(enable, "dataGrid1");
            ApplyThemeClass(enable, "dataGridFilters");
        }

        private void ApplyThemeClass(bool enable, string gridName)
        {
            var dg = this.Get<DataGrid>(gridName);
            const string cls = "wpf-theme";
            if (enable)
            {
                if (!dg.Classes.Contains(cls))
                    dg.Classes.Add(cls);
            }
            else
            {
                dg.Classes.Remove(cls);
            }
        }

        private void OnGridContextMenuOpening(object? sender, DataGridContextMenuEventArgs e)
        {
            var header = e.Column?.Header?.ToString() ?? "(no column)";
            var itemText = e.Item?.ToString() ?? "(no item)";
            var flyout = new MenuFlyout
            {
                Items =
                {
                    new MenuItem { Header = $"Column: {header}" , IsEnabled = false},
                    new MenuItem { Header = $"Item: {itemText}", IsEnabled = false},
                    new MenuItem
                    {
                        Header = "Show details",
                        Command = ShowCountryDetailsCommand,
                        CommandParameter = (e.Item as Country)?.Name ?? itemText
                    }
                }
            };
            e.Flyout = flyout;
        }

        private sealed class DelegateCommand<T> : ICommand
        {
            private readonly Action<T?> _execute;

            public DelegateCommand(Action<T?> execute)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                _execute((T?)parameter);
            }
        }

        private void OnOffsetFilterChanged(object? sender, TextChangedEventArgs e)
        {
            var raw = (sender as TextBox)?.Text;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (FindHeader(sender) is { } header)
                {
                    header.FilterValue = null;
                    var _hdrCol0 = header.Content as DataGridColumn;
                    if (_hdrCol0 != null)
                        _hdrCol0.IsFiltered = false;
                }
                else if (GetOffsetColumn() is { } column)
                {
                    column.FilterValue = null;
                    column.IsFiltered = false;
                }
                return;
            }

            // Treat typed offset as hexadecimal by default. If it doesn't start with 0x, prefix it.
            var trimmed = raw.Trim();
            var normalized = trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? trimmed : "0x" + trimmed;

            // Only set the column's FilterValue to keep the textbox input intact
            var col = GetOffsetColumn();
            if (col != null)
            {
                col.FilterValue = normalized;
                col.IsFiltered = !string.IsNullOrWhiteSpace(col.FilterValue);
            }
        }

        private void OnClearOffsetFilter(object? sender, RoutedEventArgs e)
        {
            if (FindHeader(sender) is { } header)
            {
                header.FilterValue = null;
                // clear textboxes inside template
                foreach (var tb in header.GetVisualDescendants().OfType<TextBox>())
                {
                    tb.Text = string.Empty;
                }
                var _hdrCol1 = header.Content as DataGridColumn;
                if (_hdrCol1 != null)
                    _hdrCol1.IsFiltered = false;
            }
            else if (GetOffsetColumn() is { } column)
            {
                column.FilterValue = null;
                column.IsFiltered = false;
            }
        }

        private void OnFlagsFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_ignoreFlagTextChange)
                return;
            var text = (sender as TextBox)?.Text ?? string.Empty;
            ApplyFlagsFilter(text, FindHeader(sender), syncCheckBoxes: true);
        }

        private void OnFlagCheckChanged(object? sender, RoutedEventArgs e)
        {
            if (_ignoreFlagCheckChange)
                return;

            var header = FindHeader(sender);
            var mask = GetFlagsMaskFromCheckBoxes(header);
            var text = mask == 0 ? string.Empty : $"0x{mask:X}";
            _ignoreFlagTextChange = true;
            if (sender is Control c)
            {
                var box = c.FindAncestorOfType<DataGridColumnHeader>()?
                    .GetVisualDescendants()
                    .OfType<TextBox>()
                    .FirstOrDefault();
                if (box != null)
                    box.Text = text;
            }
            _ignoreFlagTextChange = false;
            ApplyFlagsFilter(text, header, syncCheckBoxes: false);
        }

        private void OnFlagPresetChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb)
                return;
            var header = FindHeader(sender);
            var col = GetFlagsColumn();
            if (col == null)
                return;

            if (cb.SelectedItem is ComboBoxItem item && item.Tag is string tag && int.TryParse(tag, out var mask))
            {
                var text = $"0x{mask:X}";
                col.FilterValue = text;
                col.IsFiltered = !string.IsNullOrWhiteSpace(col.FilterValue);
                if (header != null)
                {
                    header.FilterValue = text;
                    var _hdrCol2 = header.Content as DataGridColumn;
                    if (_hdrCol2 != null)
                        _hdrCol2.IsFiltered = !string.IsNullOrWhiteSpace(header.FilterValue);
                    UpdateFlagCheckBoxes(header, mask);
                    var box = header.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
                    if (box != null)
                        box.Text = text;
                }
            }
        }

        private void OnFlagTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb)
                return;
            var header = FindHeader(sender);
            var col = GetFlagsColumn();
            if (col == null)
                return;

            // Adjust default preset based on type selection
            var tag = (cb.SelectedItem as ComboBoxItem)?.Tag as string;
            switch (tag)
            {
                case "LowerNybble":
                    SetFlagMask(header, col, 0x0F);
                    break;
                case "UpperNybble":
                    SetFlagMask(header, col, 0xF0);
                    break;
                case "FullByte":
                    SetFlagMask(header, col, 0xFF);
                    break;
                default:
                    break;
            }
        }

        private void SetFlagMask(DataGridColumnHeader? header, DataGridColumn col, int mask)
        {
            var text = $"0x{mask:X}";
            col.FilterValue = text;
            col.IsFiltered = !string.IsNullOrWhiteSpace(col.FilterValue);
            if (header != null)
            {
                header.FilterValue = text;
                    var _hdrCol3 = header.Content as DataGridColumn;
                    if (_hdrCol3 != null)
                        _hdrCol3.IsFiltered = !string.IsNullOrWhiteSpace(header.FilterValue);
                UpdateFlagCheckBoxes(header, mask);
                var box = header.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
                if (box != null)
                    box.Text = text;
                foreach (var preset in header.GetVisualDescendants().OfType<ComboBox>())
                {
                    if (preset.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == mask.ToString()) is { } match)
                    {
                        preset.SelectedItem = match;
                    }
                }
            }
        }

        private void OnClearFlagsFilter(object? sender, RoutedEventArgs e)
        {
            var header = FindHeader(sender);
            _ignoreFlagTextChange = true;
            if (header != null)
            {
                foreach (var tb in header.GetVisualDescendants().OfType<TextBox>())
                {
                    tb.Text = string.Empty;
                }
                UpdateFlagCheckBoxes(header, 0);
                foreach (var cb in header.GetVisualDescendants().OfType<ComboBox>())
                {
                    cb.SelectedIndex = -1;
                }
                var _hdrCol4 = header.Content as DataGridColumn;
                if (_hdrCol4 != null)
                    _hdrCol4.IsFiltered = false;
            }
            _ignoreFlagTextChange = false;
            ApplyFlagsFilter(string.Empty, header, syncCheckBoxes: true);
        }

        private void ApplyFlagsFilter(string text, DataGridColumnHeader? header, bool syncCheckBoxes)
        {
            if (header != null)
            {
                header.FilterValue = string.IsNullOrWhiteSpace(text) ? null : text;
                var _hdrCol5 = header.Content as DataGridColumn;
                if (_hdrCol5 != null)
                    _hdrCol5.IsFiltered = !string.IsNullOrWhiteSpace(header.FilterValue);
                if (syncCheckBoxes && TryParseMask(text, out var mask))
                {
                    UpdateFlagCheckBoxes(header, mask);
                }
                else if (syncCheckBoxes && string.IsNullOrWhiteSpace(text))
                {
                    UpdateFlagCheckBoxes(header, 0);
                }
                return;
            }

            var column = GetFlagsColumn();
            if (column == null)
                return;

            if (string.IsNullOrWhiteSpace(text))
            {
                column.FilterValue = null;
                column.IsFiltered = false;
                return;
            }

            column.FilterValue = text;
            column.IsFiltered = true;
        }

        private void UpdateFlagCheckBoxes(DataGridColumnHeader header, int mask)
        {
            _ignoreFlagCheckChange = true;
            foreach (var cb in header.GetVisualDescendants().OfType<CheckBox>())
            {
                if (cb.Tag is string tag && int.TryParse(tag, out var bit))
                {
                    cb.IsChecked = (mask & bit) == bit;
                }
                else if (cb.Tag is int bitValue)
                {
                    cb.IsChecked = (mask & bitValue) == bitValue;
                }
            }
            _ignoreFlagCheckChange = false;
        }

        private int GetFlagsMaskFromCheckBoxes(DataGridColumnHeader? header)
        {
            if (header == null)
                return 0;
            int mask = 0;
            foreach (var cb in header.GetVisualDescendants().OfType<CheckBox>())
            {
                if (cb.IsChecked == true && cb.Tag is string tag && int.TryParse(tag, out var bit))
                {
                    mask |= bit;
                }
                else if (cb.IsChecked == true && cb.Tag is int bitValue)
                {
                    mask |= bitValue;
                }
            }
            return mask;
        }

        private bool TryParseMask(string text, out int mask)
        {
            mask = 0;
            var trimmed = text?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return false;
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out mask);
            }
            return int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out mask);
        }

        private DataGridColumn? GetOffsetColumn() => GetFilterColumn(2);

        private DataGridColumn? GetFlagsColumn() => GetFilterColumn(3);

        private DataGridColumn? GetIndexColumn() => GetFilterColumn(1);

        private DataGridColumn? GetDescriptionColumn() => GetFilterColumn(4);

        private DataGridColumn? GetFilterColumn(int index)
        {
            var dg = this.Get<DataGrid>("dataGridFilters");
            if (index < 0 || index >= dg.Columns.Count)
                return null;
            return dg.Columns[index];
        }

        private DataGridColumnHeader? FindHeader(object? sender)
        {
            return (sender as Control)?.FindAncestorOfType<DataGridColumnHeader>();
        }

        private void OnRegexFilterChanged(object? sender, TextChangedEventArgs e)
        {
            var text = (sender as TextBox)?.Text;
            if (FindHeader(sender) is { } header)
            {
                header.FilterValue = string.IsNullOrWhiteSpace(text) ? null : text;
                var _hdrCol6 = header.Content as DataGridColumn;
                if (_hdrCol6 != null)
                    _hdrCol6.IsFiltered = !string.IsNullOrWhiteSpace(header.FilterValue);
                UpdateRegexStatus(header, text);
            }
            else if (GetDescriptionColumn() is { } column)
            {
                column.FilterValue = string.IsNullOrWhiteSpace(text) ? null : text;
                column.IsFiltered = !string.IsNullOrWhiteSpace(column.FilterValue);
            }
        }

        private void OnIndexFilterChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            var header = FindHeader(sender);
            var col = GetIndexColumn();
            if (col == null)
                return;

            var min = GetNumericUpDownValue(sender, "IndexMin");
            var max = GetNumericUpDownValue(sender, "IndexMax");

            string filter = null;
            if (min.HasValue && max.HasValue)
            {
                filter = $"{min.Value}..{max.Value}";
            }
            else if (min.HasValue)
            {
                filter = $">={min.Value}";
            }
            else if (max.HasValue)
            {
                filter = $"<={max.Value}";
            }

            col.FilterValue = filter;
            if (header != null)
            {
                header.FilterValue = filter;
                var _hdrCol8 = header.Content as DataGridColumn;
                if (_hdrCol8 != null)
                    _hdrCol8.IsFiltered = !string.IsNullOrWhiteSpace(header.FilterValue);
            }
        }

        private void OnClearIndexFilter(object? sender, RoutedEventArgs e)
        {
            var header = FindHeader(sender);
            var col = GetIndexColumn();
            if (col != null)
            {
                col.SetCurrentValue(DataGridColumn.FilterValueProperty, null);
                col.IsFiltered = false;
            }

            // reset controls inside the template
            if (header != null)
            {
                foreach (var num in header.GetVisualDescendants().OfType<NumericUpDown>())
                {
                    num.Value = null;
                }
            }
        }

        private int? GetNumericUpDownValue(object? sender, string name)
        {
            if (FindHeader(sender) is { } header)
            {
                var num = header.GetVisualDescendants().OfType<NumericUpDown>().FirstOrDefault(x => x.Name == name);
                return (int?)num?.Value;
            }
            return null;
        }

        private void UpdateRegexStatus(DataGridColumnHeader header, string? pattern)
        {
            var status = header.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "RegexStatus");
            if (status == null)
                return;
            if (string.IsNullOrWhiteSpace(pattern))
            {
                status.Text = "Enter a regex; invalid patterns fallback to contains.";
                status.Foreground = Brushes.Gray;
                return;
            }

            try
            {
                _ = new Regex(pattern);
                status.Text = "Regex OK";
                status.Foreground = Brushes.ForestGreen;
            }
            catch
            {
                status.Text = "Invalid regex (will use contains)";
                status.Foreground = Brushes.OrangeRed;
            }
        }
    }
}
