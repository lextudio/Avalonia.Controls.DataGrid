// Copyright (c) 2024 Avalonia.DataGrid fork extensions.

using System;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Event args for DataGrid context menu requests.
    /// </summary>
    public sealed class DataGridContextMenuEventArgs : EventArgs
    {
        public DataGridContextMenuEventArgs(object? item, DataGridColumn? column, object? originalSource)
        {
            Item = item;
            Column = column;
            OriginalSource = originalSource;
        }

        /// <summary>
        /// Gets the item associated with the hit cell, if any.
        /// </summary>
        public object? Item { get; }

        /// <summary>
        /// Gets the column that was hit, if any.
        /// </summary>
        public DataGridColumn? Column { get; }

        /// <summary>
        /// Set a flyout to display as the context menu. If set, the grid will show this and mark the event handled.
        /// </summary>
        public FlyoutBase? Flyout { get; set; }

        /// <summary>
        /// The original source control/object that triggered the context menu request, if available.
        /// </summary>
        public object? OriginalSource { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the event was handled.
        /// </summary>
        public bool Handled {
            get => Flyout != null;
            set => Flyout = null;
        }
    }
}
