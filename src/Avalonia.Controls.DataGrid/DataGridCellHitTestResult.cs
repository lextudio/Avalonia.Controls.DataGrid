// Copyright (c) 2024. Avalonia fork extension.

namespace Avalonia.Controls
{
    /// <summary>
    /// Result of a cell hit test on a DataGrid.
    /// </summary>
    internal readonly struct DataGridCellHitTestResult
    {
        public DataGridCellHitTestResult(DataGridCell cell, object item, DataGridColumn column)
        {
            Cell = cell;
            Item = item;
            Column = column;
        }

        public DataGridCell? Cell { get; }

        public object? Item { get; }

        public DataGridColumn? Column { get; }

        public bool IsEmpty => Cell == null;
    }
}
