// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Avalonia.Controls
{
    /// <summary>
    /// Simple filter kind hints for column filters. Avalonia fork extension.
    /// </summary>
    public enum DataGridFilterKind
    {
        Text = 0,
        Hex = 1,
        Flags = 2,
        Regex = 3,
        Numeric = 4
    }
}
