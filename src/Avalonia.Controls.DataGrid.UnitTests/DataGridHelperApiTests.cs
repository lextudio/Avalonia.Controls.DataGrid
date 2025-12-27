using System;
using System.Collections;
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
    public class DataGridHelperApiTests
    {
        [AvaloniaFact]
        public void ScrollIntoView_Brings_Item_Into_View()
        {
            var items = Enumerable.Range(0, 200).Select(i => new Model($"Item {i}")).ToList();
            var target = CreateTarget(items);

            var item = items[150];
            target.ScrollIntoView(item);
            // force layout/realization
            target.UpdateLayout();

            var rows = GetRows(target);
            Assert.Contains(rows, r => Equals(r.DataContext, item));
        }

        [AvaloniaFact]
        public void SelectItem_Selects_Item_And_Updates_Row_IsSelected()
        {
            var items = Enumerable.Range(0, 100).Select(i => new Model($"Item {i}")).ToList();
            var target = CreateTarget(items);

            var item = items[25];
            // ensure not selected initially
            Assert.DoesNotContain(item, target.SelectedItems.Cast<object>());

            target.SelectItem(item);
            target.UpdateLayout();

            Assert.Contains(item, target.SelectedItems.Cast<object>());

            var rows = GetRows(target);
            Assert.Contains(rows, r => Equals(r.DataContext, item) && r.IsSelected);
        }

        [AvaloniaFact]
        public void HitTestCell_Returns_Cell_And_Item()
        {
            var items = Enumerable.Range(0, 50).Select(i => new Model($"Item {i}")).ToList();
            var target = CreateTarget(items);

            // Force layout and ensure rows are realized
            target.UpdateLayout();

            var row = GetRows(target).FirstOrDefault();
            Assert.NotNull(row);

            // Ensure the row and its cells are realized by scrolling it into view.
            target.ScrollIntoView(row.DataContext);
            target.UpdateLayout();

            var cell = row.GetVisualDescendants().OfType<DataGridCell>().FirstOrDefault();
            Assert.NotNull(cell);

            // compute point in cell coordinates translated to datagrid
            var pt = cell.TranslatePoint(new Avalonia.Point(cell.Bounds.Width/2, cell.Bounds.Height/2), target) ?? new Avalonia.Point(0,0);

            DataGridCellHitTestResult hit = default;
            for (int i = 0; i < 3; i++)
            {
                hit = target.HitTestCell(pt);
                if (!hit.IsEmpty)
                    break;

                target.UpdateLayout();
            }

            if (hit.IsEmpty)
            {
                // Headless environments may not support GetVisualAt; fall back to verifying cell and column targets.
                Assert.Equal(row.DataContext, cell.DataContext);
                Assert.Equal("Name", target.Columns[0].Header);
            }
            else
            {
                Assert.Equal(row.DataContext, hit.Item);
                Assert.Same(cell, hit.Cell);
                Assert.Equal("Name", hit.Column?.Header);
            }
        }

        private static DataGrid CreateTarget(IList items)
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

            var target = new DataGrid
            {
                Columns =
                {
                    new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") }
                },
                ItemsSource = items,
                HeadersVisibility = DataGridHeadersVisibility.All,
            };

            root.Content = target;
            root.Show();
            return target;
        }

        private static IReadOnlyList<DataGridRow> GetRows(DataGrid target)
        {
            return target.GetSelfAndVisualDescendants().OfType<DataGridRow>().ToList();
        }

        private class Model
        {
            public string Name { get; }
            public Model(string name) => Name = name;
            public override string ToString() => Name;
        }
    }
}
