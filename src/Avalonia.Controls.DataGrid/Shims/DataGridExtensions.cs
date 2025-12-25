using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;

// Minimal shims for WPF DataGridExtensions API used by ILSpy. Not a full implementation.
namespace DataGridExtensions
{
    public static class DataGridFilter
    {
        public static readonly AttachedProperty<bool> IsAutoFilterEnabledProperty =
            AvaloniaProperty.RegisterAttached<DataGrid, bool>("IsAutoFilterEnabled", typeof(DataGridFilter), false);

        public static readonly AttachedProperty<object?> ContentFilterFactoryProperty =
            AvaloniaProperty.RegisterAttached<DataGrid, object?>("ContentFilterFactory", typeof(DataGridFilter), null);

        private static readonly ConcurrentDictionary<DataGrid, Filter> Filters = new();

        public static void SetIsAutoFilterEnabled(DataGrid grid, bool value)
        {
            grid.SetValue(IsAutoFilterEnabledProperty, value);
       }

        public static bool GetIsAutoFilterEnabled(DataGrid grid) => grid.GetValue(IsAutoFilterEnabledProperty);

        public static void SetContentFilterFactory(DataGrid grid, object? factory) =>
            grid.SetValue(ContentFilterFactoryProperty, factory);

        public static object? GetContentFilterFactory(DataGrid grid) => grid.GetValue(ContentFilterFactoryProperty);

        public static Filter GetFilter(DataGrid grid) => Filters.GetOrAdd(grid, g => new Filter(g));

        public sealed class Filter
        {
            private readonly DataGrid _grid;
            internal Filter(DataGrid grid) => _grid = grid;

            public void Clear()
            {
                foreach (var col in _grid.Columns)
                    col.FilterValue = null;
                if (_grid.CollectionView is { } view && view.CanFilter)
                {
                    view.Refresh();
                }
            }
        }
    }

    // Placeholder factories for compatibility; no-op in Avalonia shim.
    public sealed class RegexContentFilterFactory
    {
    }

    public static class DataGridColumnExtensions
    {
        /// <summary>
        /// WPF DataGridExtensions helper: assign a filter control template to a column.
        /// </summary>
        public static void SetTemplate(this DataGridColumn column, object? template)
        {
            switch (template)
            {
                case null:
                    column.FilterControlTemplate = null;
                    return;
                case IDataTemplate dataTemplate:
                    column.FilterControlTemplate = dataTemplate;
                    return;
                case ControlTemplate controlTemplate:
                    // For ControlTemplate, wrap it as a data template that builds a ContentControl with the template applied
                    column.FilterControlTemplate = new FuncDataTemplate<object>((data, _) =>
                    {
                        var contentControl = new ContentControl
                        {
                            DataContext = data,
                            Template = controlTemplate
                        };
                        return contentControl;
                    });
                    return;
                case ITemplate<Control?> controlTemplate:
                    column.FilterControlTemplate = new FuncDataTemplate<object>((data, _) =>
                    {
                        var control = controlTemplate.Build();
                        if (control != null)
                            control.DataContext = data;
                        return control;
                    });
                    return;
                default:
                    // Best-effort fallback: treat any object as a single control instance.
                    column.FilterControlTemplate = new FuncDataTemplate<object>((_, __) => template as Control);
                    return;
            }
        }
    }
}
