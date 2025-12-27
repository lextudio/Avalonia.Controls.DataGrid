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
    public class DataGridFilterUiBindingTests
    {
        [AvaloniaFact]
        public void DefaultTextFilterTemplate_Binds_TextBox_To_FilterValue()
        {
            var items = new List<Model> { new Model { Name = "apple" }, new Model { Name = "banana" }, new Model { Name = "apricot" }, new Model { Name = "cherry" } };

            var dg = CreateGrid(items);

            var col = dg.Columns[0] as DataGridTextColumn;

            // Simulate default filter TextBox by creating one and binding its Text to the column's FilterValue
            var textBox = new TextBox();
            textBox.Bind(TextBox.TextProperty, new Binding("FilterValue") { Source = col, Mode = BindingMode.TwoWay });

            // Change TextBox.Text and ensure FilterValue updates
            textBox.Text = "ap";
            Assert.Equal("ap", col.FilterValue as string);

            // Now apply filter via public change handler
            dg.OnColumnFilterChanged(col);
            dg.UpdateLayout();

            var view = dg.DataConnection.CollectionView;
            var filtered = view.Cast<Model>().ToList();
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, m => Assert.Contains("ap", m.Name));
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
