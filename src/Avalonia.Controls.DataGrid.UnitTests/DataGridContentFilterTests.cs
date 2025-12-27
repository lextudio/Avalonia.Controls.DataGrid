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
    public class DataGridContentFilterTests
    {
        [AvaloniaFact]
        public void ContentFilter_IsMatch_IsUsed_Instead_Of_TextSearch()
        {
            var items = new List<Model>
            {
                new Model { Name = "keep-me", Value = 1 },
                new Model { Name = "skip-me", Value = 2 },
            };

            var dg = CreateGrid(items);

            // provide a content filter that matches only items with Value == 1
            var col = dg.Columns[0];
            col.ContentFilter = new SimpleFilter();

            // set a filter value that would normally match both via textual contains
            col.FilterValue = "me";
            dg.OnColumnFilterChanged(null);

            var view = dg.DataConnection.CollectionView as Avalonia.Collections.DataGridCollectionView;
            Assert.NotNull(view);

            var filtered = view.Cast<Model>().ToList();
            Assert.Single(filtered);
            Assert.Equal(1, filtered[0].Value);
        }

        private class SimpleFilter
        {
            public bool IsMatch(object obj)
            {
                if (obj is string s)
                {
                    return s.IndexOf("keep", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                // when passed the data item, check Value property
                var prop = obj.GetType().GetProperty("Value");
                if (prop != null)
                {
                    var val = prop.GetValue(obj);
                    if (val is int iv)
                        return iv == 1;
                }
                return false;
            }
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
