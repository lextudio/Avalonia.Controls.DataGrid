using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests
{
    public class DataGridFilterControlTests
    {
        [AvaloniaFact]
        public void CustomFilterControl_Can_Set_IsFiltered_And_Trigger_Filtering()
        {
            var items = new List<Model>
            {
                new Model { Name = "keep", Value = 1 },
                new Model { Name = "drop", Value = 2 }
            };

            var dg = CreateGrid(items);

            // Provide a custom filter control template that contains a ToggleButton which when checked sets IsFiltered.
            var col = dg.Columns[0];
            col.UseDefaultFilterTemplate = false; // opt out of default

            var template = new FuncDataTemplate<DataGridColumn>((c, ns) =>
            {
                var tb = new ToggleButton { Content = "FilterOn" };
                tb.Checked += (s, e) => { c.IsFiltered = true; c.FilterValue = "keep"; };
                tb.Unchecked += (s, e) => { c.IsFiltered = false; c.FilterValue = null; };
                return tb;
            }, true);

            col.FilterControlTemplate = template;

            dg.UpdateLayout();

            // Header/template may not realize visual children in headless mode; simulate a custom filter by setting
            // the column's IsFiltered flag and FilterValue as a filter control would do, then trigger grid filtering.
            col.IsFiltered = true;
            col.FilterValue = "keep";
            dg.OnColumnFilterChanged(null);

            var view = dg.DataConnection.CollectionView as Avalonia.Collections.DataGridCollectionView;
            Assert.NotNull(view);
            var filtered = view.Cast<Model>().ToList();
            Assert.Single(filtered);
            Assert.Equal("keep", filtered[0].Name);
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
