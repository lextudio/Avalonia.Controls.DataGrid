using System;
using Avalonia;
using Avalonia.Controls.Templates;

namespace System.Windows.Controls
{
    /// <summary>
    /// Minimal WPF DataTemplateSelector shim for Avalonia DataGrid consumers.
    /// Provides implicit conversion to Func&lt;object, IDataTemplate&gt; used by RowDetailsTemplateSelector.
    /// </summary>
    public abstract class DataTemplateSelector
    {
        public virtual IDataTemplate SelectTemplate(object item, AvaloniaObject container) => null;

        public static implicit operator Func<object, IDataTemplate>? (DataTemplateSelector selector) =>
            selector == null ? null : new Func<object, IDataTemplate>(obj => selector.SelectTemplate(obj, null));
    }
}
