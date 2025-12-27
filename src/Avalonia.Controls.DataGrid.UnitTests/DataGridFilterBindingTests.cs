using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Xunit;

namespace Avalonia.Controls.DataGridTests
{
    public class DataGridFilterBindingTests
    {
        [AvaloniaFact]
        public void ColumnFilter_Propagates_To_Bound_TextBox()
        {
            var items = new List<Model>
            {
                new Model { Name = "apple" },
                new Model { Name = "banana" },
                new Model { Name = "apricot" },
                new Model { Name = "cherry" }
            };

            var dg = CreateGrid(items);

            var col = dg.Columns[0] as DataGridTextColumn;

            var textBox = new TextBox();
            textBox.Bind(TextBox.TextProperty, new Binding("FilterValue") { Source = col, Mode = BindingMode.TwoWay });

            // Change source and ensure UI updates
            col.FilterValue = "ap";
            dg.OnColumnFilterChanged(col);
            dg.UpdateLayout();

            Assert.Equal("ap", textBox.Text);

            var view = dg.DataConnection.CollectionView;
            var filtered = view.Cast<Model>().ToList();
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, m => Assert.Contains("ap", m.Name));
        }

        [AvaloniaFact]
        public void Bound_TextBox_Updates_ColumnFilter_TwoWay()
        {
            var items = new List<Model>
            {
                new Model { Name = "apple" },
                new Model { Name = "banana" },
                new Model { Name = "apricot" },
                new Model { Name = "cherry" }
            };

            var dg = CreateGrid(items);

            var col = dg.Columns[0] as DataGridTextColumn;

            var textBox = new TextBox();
            textBox.Bind(TextBox.TextProperty, new Binding("FilterValue") { Source = col, Mode = BindingMode.TwoWay });

            // Change UI and ensure source updates
            textBox.Text = "ban";
            Assert.Equal("ban", col.FilterValue as string);

            dg.OnColumnFilterChanged(col);
            dg.UpdateLayout();

            var view = dg.DataConnection.CollectionView;
            var filtered = view.Cast<Model>().ToList();
            Assert.Equal(1, filtered.Count);
            Assert.Contains("ban", filtered[0].Name);

            // Clear UI filter and expect all items
            textBox.Text = string.Empty;
            Assert.Equal(string.Empty, col.FilterValue as string);
            dg.OnColumnFilterChanged(col);
            dg.UpdateLayout();

            var all = dg.DataConnection.CollectionView.Cast<Model>().ToList();
            Assert.Equal(4, all.Count);
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
                    new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") }
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
        }
    }
}
