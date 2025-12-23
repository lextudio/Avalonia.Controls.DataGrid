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

namespace DataGridSample
{
    public partial class DataGridPage : UserControl
    {
        public DataGridPage()
        {
            this.InitializeComponent();

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
    }
}
