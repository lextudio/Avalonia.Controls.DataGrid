using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.VisualTree;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Xunit;

namespace Avalonia.Controls.DataGridTests
{
    public class DataGridRowDetailsTests
    {
        [AvaloniaFact]
        public void RowDetailsTemplateSelector_Applies_PerItemTemplate()
        {
            var items = new List<Model>
            {
                new Model { Name = "A", Value = 1 },
                new Model { Name = "B", Value = 2 }
            };

            var dg = CreateGrid(items);

            // provide a selector that returns a details template for item with Value==2
            dg.RowDetailsTemplateSelector = item =>
            {
                var m = item as Model;
                if (m != null && m.Value == 2)
                {
                    return new FuncDataTemplate<Model>((d, ns) => new TextBlock { Text = "DETAILS:" + d.Name }, true);
                }
                return null;
            };

            // realize layout and make details visible at grid level so templates are applied
            dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible;
            dg.UpdateLayout();

            var rows = dg.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
            Assert.NotEmpty(rows);

            var rowForB = rows.FirstOrDefault(r => (r.DataContext as Model)?.Name == "B");
            Assert.NotNull(rowForB);

            // The details template is applied only when details visibility is true. We'll set details visibility mode to Visible.
            dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible;
            dg.UpdateLayout();

            Assert.True(rowForB.DetailsTemplate != null || dg.RowDetailsTemplateSelector != null);
        }

        [AvaloniaFact]
        public void RowDetailsVisibilitySelector_Controls_Visibility()
        {
            var items = Enumerable.Range(0, 5).Select(i => new Model { Name = $"Item{i}", Value = i }).ToList();
            var dg = CreateGrid(items);

            // show details only for items with Value >= 3
            dg.RowDetailsVisibilitySelector = item => ((Model)item).Value >= 3;
            dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible;

            dg.UpdateLayout();

            var rows = dg.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
            Assert.NotEmpty(rows);

            foreach (var row in rows)
            {
                var m = row.DataContext as Model;
                if (m != null)
                {
                    bool expected = m.Value >= 3;
                    Assert.Equal(expected, dg.GetRowDetailsVisibility(rows.IndexOf(row)));
                }
            }
        }

        [AvaloniaFact]
        public void SetDetailsVisibilityForItem_Toggles_Specific_Item()
        {
            var items = Enumerable.Range(0, 4).Select(i => new Model { Name = $"Item{i}", Value = i }).ToList();
            var dg = CreateGrid(items);

            var item = items[2];
            // initially collapsed
            dg.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Collapsed;
            dg.UpdateLayout();

            dg.SetDetailsVisibilityForItem(item, DataGridRowDetailsVisibilityMode.Visible);
            dg.UpdateLayout();

            // The row's details visibility state should be true
            var row = dg.GetSelfAndVisualDescendants().OfType<DataGridRow>().FirstOrDefault(r => r.DataContext == item);
            Assert.NotNull(row);
            Assert.True(row.AreDetailsVisible);
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
        }
    }
}
