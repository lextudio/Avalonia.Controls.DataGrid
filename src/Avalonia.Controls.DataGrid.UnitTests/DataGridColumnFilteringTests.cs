using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests
{
    public class DataGridColumnFilteringTests
    {
        [AvaloniaFact]
        public void ColumnFilter_TextContains_Filters_Items()
        {
            var items = new List<Model>();
            items.AddRange(Enumerable.Range(0, 10).Select(i => new Model { Name = i % 2 == 0 ? $"even-{i}" : $"odd-{i}", Value = i }));

            var dg = CreateGrid(items);

            // enable column filters (default true) and apply filter on first column
            var col = dg.Columns[0];
            col.FilterValue = "even";

            // force grid to apply filters
            dg.OnColumnFilterChanged(null);

            // The collection view should only contain items with 'even' in Name
            var view = dg.DataConnection.CollectionView as Avalonia.Collections.DataGridCollectionView;
            Assert.NotNull(view);
            var filtered = view.Cast<object>().ToList();
            Assert.All(filtered, item => Assert.Contains("even", item.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        [AvaloniaFact]
        public void ColumnFilter_NumericComparison_Works()
        {
            var items = Enumerable.Range(0, 20).Select(i => new Model { Name = $"Item {i}", Value = i }).ToList();
            var dg = CreateGrid(items);

            var valueCol = dg.Columns.OfType<DataGridBoundColumn>().FirstOrDefault(c => c.Binding is Binding b && b.Path == "Value");
            Assert.NotNull(valueCol);

            // Test greater-than operator
            valueCol.FilterValue = ">=10";
            dg.OnColumnFilterChanged(null);

            var view = dg.DataConnection.CollectionView as Avalonia.Collections.DataGridCollectionView;
            Assert.NotNull(view);
            var filtered = view.Cast<Model>().ToList();
            Assert.All(filtered, m => Assert.True(m.Value >= 10));
        }

        private static DataGrid CreateGrid(IList<Model> items)
        {
            var root = new Window
            {
                Width = 400,
                Height = 300,
                Styles =
                {
                    new StyleInclude((Uri?)null)
                    {
                        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                    },
                }
            };

            var dg = new DataGrid
            {
                Columns =
                {
                    new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") },
                    new DataGridTextColumn { Header = "Value", Binding = new Binding("Value") }
                },
                ItemsSource = items,
                HeadersVisibility = DataGridHeadersVisibility.All,
            };

            root.Content = dg;
            root.Show();
            dg.UpdateLayout();
            return dg;
        }

        private class Model
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public override string ToString() => Name ?? base.ToString();
        }
    }
}
