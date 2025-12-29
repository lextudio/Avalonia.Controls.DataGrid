// Minimal public cell info struct to mirror WPF's DataGridCellInfo API.
// Needed by consumers (e.g., ILSpy) that expect DataGrid.CurrentCell to expose
// the current item/column and a validity check.
using System;

namespace Avalonia.Controls
{
    public readonly struct DataGridCellInfo : IEquatable<DataGridCellInfo>
    {
        public DataGridCellInfo(object? item, DataGridColumn? column)
        {
            Item = item;
            Column = column;
        }

        public object? Item { get; }
        public DataGridColumn? Column { get; }

        public bool IsValid => Item != null && Column != null;

        public override bool Equals(object? obj) => obj is DataGridCellInfo other && Equals(other);

        public bool Equals(DataGridCellInfo other) => Equals(Item, other.Item) && Equals(Column, other.Column);

        public override int GetHashCode() => HashCode.Combine(Item, Column);

        public static bool operator ==(DataGridCellInfo left, DataGridCellInfo right) => left.Equals(right);
        public static bool operator !=(DataGridCellInfo left, DataGridCellInfo right) => !left.Equals(right);
    }
}
