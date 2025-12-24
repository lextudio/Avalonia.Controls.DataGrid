// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an individual <see cref="T:Avalonia.Controls.DataGrid" /> column header.
    /// </summary>
    [PseudoClasses(":dragIndicator", ":pressed", ":sortascending", ":sortdescending")]
#if !DATAGRID_INTERNAL
    public
#endif
    class DataGridColumnHeader : ContentControl
    {
        private enum DragMode
        {
            None = 0,
            MouseDown = 1,
            Drag = 2,
            Resize = 3,
            Reorder = 4
        }

        private const int DATAGRIDCOLUMNHEADER_resizeRegionWidth = 5;
        private const int DATAGRIDCOLUMNHEADER_columnsDragTreshold = 5;

        private bool _areHandlersSuspended;
        private static DragMode _dragMode;
        private static Point? _lastMousePositionHeaders;
        private static Cursor _originalCursor;
        private static double _originalHorizontalOffset;
        private static double _originalWidth;
        private bool _desiredSeparatorVisibility = true;
        private static Point? _dragStart;
        private static DataGridColumn _dragColumn;
        private static double _frozenColumnsWidth;
        private static Lazy<Cursor> _resizeCursor = new Lazy<Cursor>(() => new Cursor(StandardCursorType.SizeWestEast));
        
        private Popup _filterPopup;
        private IInputElement _previousFocusedElement;
        private ContentPresenter _popupCustomPresenter;
        private TextBox _popupDefaultBox;
        private ToggleButton _filterToggleButton;
        private Control _inlineTextHost;
        private Control _inlineHexHost;
        private TextBox _inlineTextBox;
        private TextBox _inlineHexTextBox;
        private bool _inlineTextHasFocus;
        private bool _inlineHexHasFocus;
        private Control _filterArea;
        private bool _isPointerOverFilterIcon;
        private bool _isPointerOverInlineText;
        private bool _isPointerOverInlineHex;
        private bool _isPointerOverFilterArea;

        public static readonly StyledProperty<IBrush> SeparatorBrushProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, IBrush>(nameof(SeparatorBrush));

        public IBrush SeparatorBrush
        {
            get { return GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        public static readonly StyledProperty<bool> AreSeparatorsVisibleProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(
                nameof(AreSeparatorsVisible),
                defaultValue: true);

        // Inline filter row removed; no DataGrid-level IsFilterRowVisible property.

        public static readonly StyledProperty<string> FilterValueProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, string>(nameof(FilterValue));

        public static readonly StyledProperty<string> FilterHintProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, string>(nameof(FilterHint));

        public static readonly StyledProperty<IDataTemplate> FilterControlTemplateProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, IDataTemplate>(nameof(FilterControlTemplate));

        public static readonly StyledProperty<bool> HasFilterProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(HasFilter));

        public static readonly StyledProperty<bool> HasCustomFilterTemplateProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(HasCustomFilterTemplate));

        public static readonly StyledProperty<bool> HasDefaultFilterTemplateProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(HasDefaultFilterTemplate));

        public static readonly StyledProperty<bool> VisibleFilterButtonProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(VisibleFilterButton));

        public static readonly StyledProperty<DataGridFilterKind> FilterKindProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, DataGridFilterKind>(nameof(FilterKind), defaultValue: DataGridFilterKind.Text);

        public static readonly StyledProperty<bool> ShowInlineTextFilterProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(ShowInlineTextFilter));

        public static readonly StyledProperty<bool> ShowInlineHexFilterProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(ShowInlineHexFilter));

        public static readonly StyledProperty<bool> InlineTextFilterVisibleProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(InlineTextFilterVisible));

        public static readonly StyledProperty<bool> InlineHexFilterVisibleProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(InlineHexFilterVisible));

        public static readonly StyledProperty<bool> IsFilterPopupOpenProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(nameof(IsFilterPopupOpen));


        public bool AreSeparatorsVisible
        {
            get { return GetValue(AreSeparatorsVisibleProperty); }
            set { SetValue(AreSeparatorsVisibleProperty, value); }
        }

        // Inline filter row removed; no IsFilterRowVisible on header.

        public string FilterValue
        {
            get => GetValue(FilterValueProperty);
            set => SetValue(FilterValueProperty, value);
        }

        public string FilterHint
        {
            get => GetValue(FilterHintProperty);
            set => SetValue(FilterHintProperty, value);
        }

        public IDataTemplate FilterControlTemplate
        {
            get => GetValue(FilterControlTemplateProperty);
            set => SetValue(FilterControlTemplateProperty, value);
        }

        public bool HasFilter
        {
            get => GetValue(HasFilterProperty);
            set => SetValue(HasFilterProperty, value);
        }

        public bool HasCustomFilterTemplate
        {
            get => GetValue(HasCustomFilterTemplateProperty);
            set => SetValue(HasCustomFilterTemplateProperty, value);
        }

        public bool HasDefaultFilterTemplate
        {
            get => GetValue(HasDefaultFilterTemplateProperty);
            set => SetValue(HasDefaultFilterTemplateProperty, value);
        }

        public bool VisibleFilterButton
        {
            get => GetValue(VisibleFilterButtonProperty);
            set => SetValue(VisibleFilterButtonProperty, value);
        }

        public DataGridFilterKind FilterKind
        {
            get => GetValue(FilterKindProperty);
            set => SetValue(FilterKindProperty, value);
        }

        public bool ShowInlineTextFilter
        {
            get => GetValue(ShowInlineTextFilterProperty);
            set => SetValue(ShowInlineTextFilterProperty, value);
        }

        public bool ShowInlineHexFilter
        {
            get => GetValue(ShowInlineHexFilterProperty);
            set => SetValue(ShowInlineHexFilterProperty, value);
        }

        public bool InlineTextFilterVisible
        {
            get => GetValue(InlineTextFilterVisibleProperty);
            set => SetValue(InlineTextFilterVisibleProperty, value);
        }

        public bool InlineHexFilterVisible
        {
            get => GetValue(InlineHexFilterVisibleProperty);
            set => SetValue(InlineHexFilterVisibleProperty, value);
        }

        public bool IsFilterPopupOpen
        {
            get => GetValue(IsFilterPopupOpenProperty);
            set => SetValue(IsFilterPopupOpenProperty, value);
        }

        static DataGridColumnHeader()
        {
            AreSeparatorsVisibleProperty.Changed.AddClassHandler<DataGridColumnHeader>((x, e) => x.OnAreSeparatorsVisibleChanged(e));
            FilterControlTemplateProperty.Changed.AddClassHandler<DataGridColumnHeader>((x, e) => x.UpdateFilterTemplateVisibility());
            // Inline filter row removed; do not register IsFilterRowVisible changed handler.
            IsFilterPopupOpenProperty.Changed.AddClassHandler<DataGridColumnHeader>((x, e) => x.OnIsFilterPopupOpenChanged(e));
            FilterKindProperty.Changed.AddClassHandler<DataGridColumnHeader>((x, e) => x.UpdateInlineState());
            PressedMixin.Attach<DataGridColumnHeader>();
            IsTabStopProperty.OverrideDefaultValue<DataGridColumnHeader>(false);
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideDefaultValue<DataGridColumnHeader>(IsOffscreenBehavior.FromClip);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.Primitives.DataGridColumnHeader" /> class.
        /// </summary>
        //TODO Implement
        public DataGridColumnHeader()
        {
            PointerPressed += DataGridColumnHeader_PointerPressed;
            PointerReleased += DataGridColumnHeader_PointerReleased;
            PointerMoved += DataGridColumnHeader_PointerMoved;
            PointerEntered += DataGridColumnHeader_PointerEntered;
            PointerExited += DataGridColumnHeader_PointerExited;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (_filterToggleButton != null)
            {
                _filterToggleButton.PointerEntered -= FilterButton_PointerEntered;
                _filterToggleButton.PointerExited -= FilterButton_PointerExited;
            }
            if (_inlineTextHost != null)
            {
                _inlineTextHost.PointerEntered -= InlineTextHost_PointerEntered;
                _inlineTextHost.PointerExited -= InlineTextHost_PointerExited;
            }
            if (_inlineHexHost != null)
            {
                _inlineHexHost.PointerEntered -= InlineHexHost_PointerEntered;
                _inlineHexHost.PointerExited -= InlineHexHost_PointerExited;
            }

            _filterPopup = e.NameScope.Find<Popup>("PART_FilterPopup");
            _filterToggleButton = e.NameScope.Find<ToggleButton>("PART_FilterButton");
            _inlineTextHost = e.NameScope.Find<Border>("PART_InlineTextHost");
            _inlineHexHost = e.NameScope.Find<Border>("PART_InlineHexHost");
            _filterArea = e.NameScope.Find<Border>("PART_FilterArea");
            var popupCustom = e.NameScope.Find<ContentPresenter>("PART_PopupCustom");
            var popupDefault = e.NameScope.Find<TextBox>("PART_PopupDefault");

            if (_filterToggleButton != null)
            {
                _filterToggleButton.PointerEntered += FilterButton_PointerEntered;
                _filterToggleButton.PointerExited += FilterButton_PointerExited;
            }

            if (_inlineTextHost != null)
            {
                _inlineTextHost.PointerEntered += InlineTextHost_PointerEntered;
                _inlineTextHost.PointerExited += InlineTextHost_PointerExited;
                // remove previous handlers if any
                if (_inlineTextBox != null)
                {
                    _inlineTextBox.GotFocus -= InlineTextBox_GotFocus;
                    _inlineTextBox.LostFocus -= InlineTextBox_LostFocus;
                    _inlineTextBox = null;
                }
                // find the inline TextBox and attach focus handlers so focus keeps the inline visible
                var foundTb = FindFirstOfTypeIn(_inlineTextHost as Control, typeof(TextBox)) as TextBox;
                if (foundTb != null)
                {
                    _inlineTextBox = foundTb;
                    _inlineTextBox.GotFocus += InlineTextBox_GotFocus;
                    _inlineTextBox.LostFocus += InlineTextBox_LostFocus;
                }
            }

            if (_inlineHexHost != null)
            {
                _inlineHexHost.PointerEntered += InlineHexHost_PointerEntered;
                _inlineHexHost.PointerExited += InlineHexHost_PointerExited;
                if (_inlineHexTextBox != null)
                {
                    _inlineHexTextBox.GotFocus -= InlineHexBox_GotFocus;
                    _inlineHexTextBox.LostFocus -= InlineHexBox_LostFocus;
                    _inlineHexTextBox = null;
                }
                var foundHex = FindFirstOfTypeIn(_inlineHexHost as Control, typeof(TextBox)) as TextBox;
                if (foundHex != null)
                {
                    _inlineHexTextBox = foundHex;
                    _inlineHexTextBox.GotFocus += InlineHexBox_GotFocus;
                    _inlineHexTextBox.LostFocus += InlineHexBox_LostFocus;
                }
            }

            if (_filterArea != null)
            {
                // Use the unified filter area to control inline visibility to prevent pointer event races
                _filterArea.PointerEntered += FilterArea_PointerEntered;
                _filterArea.PointerExited += FilterArea_PointerExited;
            }

            if (popupCustom != null)
            {
                popupCustom.ContentTemplate = FilterControlTemplate;
                popupCustom.Content = OwningColumn ?? (object)this;
            }

            if (popupDefault != null)
            {
                popupDefault.Bind(TextBox.TextProperty, new Binding("FilterValue") { Source = this, Mode = BindingMode.TwoWay });
                popupDefault.Bind(TextBox.WatermarkProperty, new Binding("FilterHint") { Source = this });
            }

            _popupCustomPresenter = popupCustom;
            _popupDefaultBox = popupDefault;

            if (_filterPopup != null)
            {
                _filterPopup.IsOpen = IsFilterPopupOpen;
                _filterPopup.Opened -= FilterPopup_Opened;
                _filterPopup.Closed -= FilterPopup_Closed;
                _filterPopup.Opened += FilterPopup_Opened;
                _filterPopup.Closed += FilterPopup_Closed;
            }
            UpdateFilterTemplateVisibility();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == FilterControlTemplateProperty)
            {
                UpdateFilterTemplateVisibility();
            }
        }

        private void UpdateFilterTemplateVisibility()
        {
            // Inline filter row is removed; popup uses the available templates.
            HasCustomFilterTemplate = FilterControlTemplate != null;
            // Provide the default textbox-based filter in the popup unless a column explicitly opts out.
            HasDefaultFilterTemplate = true;
            HasFilter = HasCustomFilterTemplate || HasDefaultFilterTemplate;
            UpdateInlineState();

            // popup presenters are handled via PART_PopupCustom / PART_PopupDefault

            if (_filterPopup != null)
            {
                _filterPopup.IsOpen = IsFilterPopupOpen;
            }
        }

        private void UpdateInlineState()
        {
            bool hasDefaultFilter = HasDefaultFilterTemplate && !HasCustomFilterTemplate;
            bool isHex = FilterKind == DataGridFilterKind.Hex;
            SetValueNoCallback(ShowInlineTextFilterProperty, hasDefaultFilter && !isHex);
            SetValueNoCallback(ShowInlineHexFilterProperty, hasDefaultFilter && isHex);
            VisibleFilterButton = HasFilter;
            SetValueNoCallback(InlineTextFilterVisibleProperty, false);
            SetValueNoCallback(InlineHexFilterVisibleProperty, false);
            UpdateInlineVisibility();
        }

        private void UpdateInlineVisibility()
        {
            // Prefer the filter area hovered state which encompasses icon and inline hosts
            bool over = _isPointerOverFilterArea || _isPointerOverFilterIcon;
            // Keep inline visible if filter has content or the control has focus or is hovered
            bool hasValue = !string.IsNullOrEmpty(FilterValue);
            bool showText = ShowInlineTextFilter && (hasValue || over || _isPointerOverInlineText || _inlineTextHasFocus);
            bool showHex = ShowInlineHexFilter && (hasValue || over || _isPointerOverInlineHex || _inlineHexHasFocus);
            bool prevText = InlineTextFilterVisible;
            SetValueNoCallback(InlineTextFilterVisibleProperty, showText);
            SetValueNoCallback(InlineHexFilterVisibleProperty, showHex);
            if (showText && !prevText)
            {
                // Try to focus the inline TextBox when it becomes visible
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (_inlineTextHost is Visual v)
                        {
                            // find first TextBox descendant
                            var tb = FindFirstOfTypeIn(v as Control, typeof(TextBox)) as IInputElement;
                            tb?.Focus();
                        }
                    }
                    catch { }
                });
            }
            // Hide the filter toggle while an inline filter control is visible
            bool anyInlineVisible = showText || showHex;
            SetValueNoCallback(VisibleFilterButtonProperty, HasFilter && !anyInlineVisible);
        }

        private void FilterButton_PointerEntered(object sender, PointerEventArgs e)
        {
            _isPointerOverFilterIcon = true;
            UpdateInlineVisibility();
        }

        private void FilterButton_PointerExited(object sender, PointerEventArgs e)
        {
            _isPointerOverFilterIcon = false;
            UpdateInlineVisibility();
        }

        private void FilterArea_PointerEntered(object sender, PointerEventArgs e)
        {
            _isPointerOverFilterArea = true;
            UpdateInlineVisibility();
        }

        private void FilterArea_PointerExited(object sender, PointerEventArgs e)
        {
            _isPointerOverFilterArea = false;
            UpdateInlineVisibility();
        }

        private void InlineTextBox_GotFocus(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _inlineTextHasFocus = true;
            UpdateInlineVisibility();
        }

        private void InlineTextBox_LostFocus(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _inlineTextHasFocus = false;
            UpdateInlineVisibility();
        }

        private void InlineHexBox_GotFocus(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _inlineHexHasFocus = true;
            UpdateInlineVisibility();
        }

        private void InlineHexBox_LostFocus(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _inlineHexHasFocus = false;
            UpdateInlineVisibility();
        }

        private void InlineTextHost_PointerEntered(object sender, PointerEventArgs e)
        {
            _isPointerOverInlineText = true;
            UpdateInlineVisibility();
        }

        private void InlineTextHost_PointerExited(object sender, PointerEventArgs e)
        {
            _isPointerOverInlineText = false;
            UpdateInlineVisibility();
        }

        private void InlineHexHost_PointerEntered(object sender, PointerEventArgs e)
        {
            _isPointerOverInlineHex = true;
            UpdateInlineVisibility();
        }

        private void InlineHexHost_PointerExited(object sender, PointerEventArgs e)
        {
            _isPointerOverInlineHex = false;
            UpdateInlineVisibility();
        }

        private static void OnIsFilterPopupOpenChanged(DataGridColumnHeader header, AvaloniaPropertyChangedEventArgs e)
        {
            // This static handler proxies to instance method
            header.OnIsFilterPopupOpenChanged(e);
        }

        private void OnIsFilterPopupOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_filterPopup == null)
                return;

            bool isOpen = (bool)e.NewValue;
            // Keep popup IsOpen in sync
            if (_filterPopup.IsOpen != isOpen)
            {
                _filterPopup.IsOpen = isOpen;
            }

            if (isOpen)
            {
                // store previous focus (use Avalonia FocusManager)
                _previousFocusedElement = FocusManager.GetFocusManager(this)?.GetFocusedElement() as IInputElement;
                // try to focus the first focusable control in popup
                FocusFirstElementInPopup();
            }
            else
            {
                // restore focus back to previous element (usually the toggle button)
                if (_previousFocusedElement != null)
                {
                    try
                    {
                        _previousFocusedElement.Focus();
                    }
                    catch { }
                }
                _previousFocusedElement = null;
            }
        }

        private void FilterPopup_Opened(object sender, EventArgs e)
        {
            // If the popup opens (e.g., via ToggleButton), set our property
            SetValueNoCallback(IsFilterPopupOpenProperty, true);
            FocusFirstElementInPopup();

            // attach key handler to popup child for Escape/Enter
            if (_filterPopup?.Child is Control childControl)
            {
                childControl.KeyDown -= PopupChild_KeyDown;
                childControl.KeyDown += PopupChild_KeyDown;
            }
        }

        private void FilterPopup_Closed(object sender, EventArgs e)
        {
            SetValueNoCallback(IsFilterPopupOpenProperty, false);
            // restore focus
            if (_previousFocusedElement != null)
            {
                try { _previousFocusedElement.Focus(); } catch { }
            }
            if (_filterPopup?.Child is Control childControl2)
            {
                childControl2.KeyDown -= PopupChild_KeyDown;
            }
        }

        private void FocusFirstElementInPopup()
        {
            // prefer custom presenter content, then default textbox
            try
            {
                IInputElement toFocus = null;
                // prefer TextBox inside custom presenter
                if (_popupCustomPresenter != null && _popupCustomPresenter.Content is Control custom)
                {
                    toFocus = FindFirstOfTypeIn(custom, typeof(TextBox)) ?? FindFirstOfTypeIn(custom, typeof(Button)) ?? FindFirstFocusable(custom);
                }
                // fallback to popup default textbox
                if (toFocus == null && _popupDefaultBox != null)
                {
                    toFocus = _popupDefaultBox;
                }

                if (toFocus != null)
                {
                    // schedule focus on UI thread to ensure popup visuals are ready
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        try { toFocus.Focus(); } catch { }
                    });
                }
            }
            catch { }
        }

        private IInputElement FindFirstFocusable(Control root)
        {
            if (root == null) return null;
            if (root.Focusable) return root;
            foreach (var child in root.GetVisualChildren())
            {
                if (child is Control cc)
                {
                    var r = FindFirstFocusable(cc);
                    if (r != null) return r;
                }
            }
            return null;
        }

        private IInputElement FindFirstOfTypeIn(Control root, Type type)
        {
            if (root == null) return null;
            if (type.IsInstanceOfType(root) && root.Focusable) return root;
            foreach (var child in root.GetVisualChildren())
            {
                if (child is Control cc)
                {
                    var r = FindFirstOfTypeIn(cc, type);
                    if (r != null) return r;
                }
            }
            return null;
        }

        private void PopupChild_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Enter)
            {
                // close popup and restore focus
                try
                {
                    SetValueNoCallback(IsFilterPopupOpenProperty, false);
                    if (_filterPopup != null)
                        _filterPopup.IsOpen = false;
                }
                catch { }
                e.Handled = true;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DataGridColumnHeaderAutomationPeer(this);
        }

        private void OnAreSeparatorsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_areHandlersSuspended)
            {
                _desiredSeparatorVisibility = (bool)e.NewValue;
                if (OwningGrid != null)
                {
                    UpdateSeparatorVisibility(OwningGrid.ColumnsInternal.LastVisibleColumn);
                }
                else
                {
                    UpdateSeparatorVisibility(null);
                }
            }
        }

        internal DataGridColumn OwningColumn
        {
            get;
            set;
        }
        internal DataGrid OwningGrid => OwningColumn?.OwningGrid;

        internal int ColumnIndex
        {
            get
            {
                if (OwningColumn == null)
                {
                    return -1;
                }
                return OwningColumn.Index;
            }
        }

        internal ListSortDirection? CurrentSortingState
        {
            get;
            private set;
        }

        private bool IsMouseOver
        {
            get;
            set;
        }

        private bool IsPressed
        {
            get;
            set;
        }

        private void SetValueNoCallback<T>(AvaloniaProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
        {
            _areHandlersSuspended = true;
            try
            {
                SetValue(property, value, priority);
            }
            finally
            {
                _areHandlersSuspended = false;
            }
        }

        internal void UpdatePseudoClasses()
        {
            CurrentSortingState = null;
            if (OwningGrid != null
                && OwningGrid.DataConnection != null
                && OwningGrid.DataConnection.AllowSort)
            {
                var sort = OwningColumn.GetSortDescription();
                if (sort != null)
                {
                    CurrentSortingState = sort.Direction;
                }
            }

            PseudoClasses.Set(":sortascending",
                CurrentSortingState == ListSortDirection.Ascending);
            PseudoClasses.Set(":sortdescending",
                CurrentSortingState == ListSortDirection.Descending);
        }

        internal void UpdateSeparatorVisibility(DataGridColumn lastVisibleColumn)
        {
            bool newVisibility = _desiredSeparatorVisibility;

            // Collapse separator for the last column if there is no filler column
            if (OwningColumn != null &&
                OwningGrid != null &&
                _desiredSeparatorVisibility &&
                OwningColumn == lastVisibleColumn &&
                !OwningGrid.ColumnsInternal.FillerColumn.IsActive)
            {
                newVisibility = false;
            }

            // Update the public property if it has changed
            if (AreSeparatorsVisible != newVisibility)
            {
                SetValueNoCallback(AreSeparatorsVisibleProperty, newVisibility);
            }
        }

        public event EventHandler<KeyModifiers> LeftClick;

        internal void OnMouseLeftButtonUp_Click(KeyModifiers keyModifiers, ref bool handled)
        {
            LeftClick?.Invoke(this, keyModifiers);

            // completed a click without dragging, so we're sorting
            InvokeProcessSort(keyModifiers);
            handled = true;
        }

        internal void InvokeProcessSort(KeyModifiers keyModifiers, ListSortDirection? forcedDirection = null)
        {
            Debug.Assert(OwningGrid != null);
            if (OwningGrid.WaitForLostFocus(() => InvokeProcessSort(keyModifiers, forcedDirection)))
            {
                return;
            }
            if (OwningGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true))
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => ProcessSort(keyModifiers, forcedDirection));
            }
        }

        //TODO GroupSorting
        internal void ProcessSort(KeyModifiers keyModifiers, ListSortDirection? forcedDirection = null)
        {
            // if we can sort:
            //  - AllowUserToSortColumns and CanSort are true, and
            //  - OwningColumn is bound
            // then try to sort
            if (OwningColumn != null
                && OwningGrid != null
                && OwningGrid.EditingRow == null
                && OwningColumn != OwningGrid.ColumnsInternal.FillerColumn
                && OwningGrid.CanUserSortColumns
                && OwningColumn.CanUserSort)
            {
                var ea = new DataGridColumnEventArgs(OwningColumn);
                OwningGrid.OnColumnSorting(ea);

                if (!ea.Handled && OwningGrid.DataConnection.AllowSort && OwningGrid.DataConnection.SortDescriptions != null)
                {
                    // - DataConnection.AllowSort is true, and
                    // - SortDescriptionsCollection exists, and
                    // - the column's data type is comparable

                    DataGrid owningGrid = OwningGrid;
                    DataGridSortDescription newSort;

                    KeyboardHelper.GetMetaKeyState(this, keyModifiers, out bool ctrl, out bool shift);

                    DataGridSortDescription sort = OwningColumn.GetSortDescription();
                    IDataGridCollectionView collectionView = owningGrid.DataConnection.CollectionView;
                    Debug.Assert(collectionView != null);

                    using (collectionView.DeferRefresh())
                    {
                        // if shift is held down, we multi-sort, therefore if it isn't, we'll clear the sorts beforehand
                        if (!shift || owningGrid.DataConnection.SortDescriptions.Count == 0)
                        {
                            owningGrid.DataConnection.SortDescriptions.Clear();
                        }

                        // if ctrl is held down, we only clear the sort directions
                        if (!ctrl)
                        {
                            if (sort != null)
                            {
                                if (forcedDirection == null || sort.Direction != forcedDirection)
                                {
                                    newSort = sort.SwitchSortDirection();
                                }
                                else
                                {
                                    newSort = sort;
                                }

                                // changing direction should not affect sort order, so we replace this column's
                                // sort description instead of just adding it to the end of the collection
                                int oldIndex = owningGrid.DataConnection.SortDescriptions.IndexOf(sort);
                                if (oldIndex >= 0)
                                {
                                    owningGrid.DataConnection.SortDescriptions.Remove(sort);
                                    owningGrid.DataConnection.SortDescriptions.Insert(oldIndex, newSort);
                                }
                                else
                                {
                                    owningGrid.DataConnection.SortDescriptions.Add(newSort);
                                }
                            }
                            else if (OwningColumn.CustomSortComparer != null)
                            {
                                newSort = forcedDirection != null ?
                                    DataGridSortDescription.FromComparer(OwningColumn.CustomSortComparer, forcedDirection.Value) :
                                    DataGridSortDescription.FromComparer(OwningColumn.CustomSortComparer);


                                owningGrid.DataConnection.SortDescriptions.Add(newSort);
                            }
                            else
                            {
                                string propertyName = OwningColumn.GetSortPropertyName();
                                // no-opt if we couldn't find a property to sort on
                                if (string.IsNullOrEmpty(propertyName))
                                {
                                    return;
                                }

                                newSort = DataGridSortDescription.FromPath(propertyName, culture: collectionView.Culture);
                                if (forcedDirection != null && newSort.Direction != forcedDirection)
                                {
                                    newSort = newSort.SwitchSortDirection();
                                }

                                owningGrid.DataConnection.SortDescriptions.Add(newSort);
                            }
                        }
                    }
                }
            }
        }

        private bool CanReorderColumn(DataGridColumn column)
        {
            return OwningGrid.CanUserReorderColumns
                && !(column is DataGridFillerColumn)
                && (column.CanUserReorderInternal.HasValue && column.CanUserReorderInternal.Value || !column.CanUserReorderInternal.HasValue);
        }

        /// <summary>
        /// Determines whether a column can be resized by dragging the border of its header.  If star sizing
        /// is being used, there are special conditions that can prevent a column from being resized:
        /// 1. The column is the last visible column.
        /// 2. All columns are constrained by either their maximum or minimum values.
        /// </summary>
        /// <param name="column">Column to check.</param>
        /// <returns>Whether or not the column can be resized by dragging its header.</returns>
        private static bool CanResizeColumn(DataGridColumn column)
        {
            if (column.OwningGrid != null && column.OwningGrid.ColumnsInternal != null && column.OwningGrid.UsesStarSizing &&
                (column.OwningGrid.ColumnsInternal.LastVisibleColumn == column || !MathUtilities.AreClose(column.OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth, column.OwningGrid.CellsWidth)))
            {
                return false;
            }
            return column.ActualCanUserResize;
        }

        private static bool TrySetResizeColumn(DataGridColumn column)
        {
            // If datagrid.CanUserResizeColumns == false, then the column can still override it
            if (CanResizeColumn(column))
            {
                _dragColumn = column;

                _dragMode = DragMode.Resize;

                return true;
            }
            return false;
        }

        //TODO DragDrop

        internal void OnMouseLeftButtonDown(ref bool handled, PointerEventArgs args, Point mousePosition)
        {
            IsPressed = true;

            if (OwningGrid != null && OwningGrid.ColumnHeaders != null)
            {
                _dragMode = DragMode.MouseDown;
                _frozenColumnsWidth = OwningGrid.ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth();
                _lastMousePositionHeaders = this.Translate(OwningGrid.ColumnHeaders, mousePosition);

                double distanceFromLeft = mousePosition.X;
                double distanceFromRight = Bounds.Width - distanceFromLeft;
                DataGridColumn currentColumn = OwningColumn;
                DataGridColumn previousColumn = null;
                if (!(OwningColumn is DataGridFillerColumn))
                {
                    previousColumn = OwningGrid.ColumnsInternal.GetPreviousVisibleNonFillerColumn(currentColumn);
                }

                if (_dragMode == DragMode.MouseDown && _dragColumn == null && (distanceFromRight <= DATAGRIDCOLUMNHEADER_resizeRegionWidth))
                {
                    handled = TrySetResizeColumn(currentColumn);
                }
                else if (_dragMode == DragMode.MouseDown && _dragColumn == null && distanceFromLeft <= DATAGRIDCOLUMNHEADER_resizeRegionWidth && previousColumn != null)
                {
                    handled = TrySetResizeColumn(previousColumn);
                }

                if (_dragMode == DragMode.Resize && _dragColumn != null)
                {
                    _dragStart = _lastMousePositionHeaders;
                    _originalWidth = _dragColumn.ActualWidth;
                    _originalHorizontalOffset = OwningGrid.HorizontalOffset;

                    handled = true;
                }
            }
        }

        //TODO DragEvents
        //TODO MouseCapture
        internal void OnMouseLeftButtonUp(ref bool handled, PointerEventArgs args, Point mousePosition, Point mousePositionHeaders)
        {
            IsPressed = false;

            if (OwningGrid != null && OwningGrid.ColumnHeaders != null)
            {
                if (_dragMode == DragMode.MouseDown)
                {
                    OnMouseLeftButtonUp_Click(args.KeyModifiers, ref handled);
                }
                else if (_dragMode == DragMode.Reorder)
                {
                    // Find header we're hovering over
                    int targetIndex = GetReorderingTargetDisplayIndex(mousePositionHeaders);

                    if (((!OwningColumn.IsFrozen && targetIndex >= OwningGrid.FrozenColumnCount)
                          || (OwningColumn.IsFrozen && targetIndex < OwningGrid.FrozenColumnCount)))
                    {
                        OwningColumn.DisplayIndex = targetIndex;

                        DataGridColumnEventArgs ea = new DataGridColumnEventArgs(OwningColumn);
                        OwningGrid.OnColumnReordered(ea);
                    }
                }

                SetDragCursor(mousePosition);

                // Variables that track drag mode states get reset in DataGridColumnHeader_LostMouseCapture
                args.Pointer.Capture(null);
                OnLostMouseCapture();
                _dragMode = DragMode.None;
                handled = true;
            }
        }

        //TODO DragEvents
        internal void OnMouseMove(PointerEventArgs args, Point mousePosition, Point mousePositionHeaders)
        {
            var handled = args.Handled;
            if (handled || OwningGrid == null || OwningGrid.ColumnHeaders == null)
            {
                return;
            }

            Debug.Assert(OwningGrid.Parent is InputElement);

            OnMouseMove_Resize(ref handled, mousePositionHeaders);

            OnMouseMove_Reorder(ref handled, mousePosition, mousePositionHeaders);

            SetDragCursor(mousePosition);
        }

        private void DataGridColumnHeader_PointerEntered(object sender, PointerEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            Point mousePosition = e.GetPosition(this);
            OnMouseEnter(mousePosition);
            UpdatePseudoClasses();
        }

        private void DataGridColumnHeader_PointerExited(object sender, PointerEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            OnMouseLeave();
            UpdatePseudoClasses();
        }

        private void DataGridColumnHeader_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            // If the pointer event originated from a TextBox (or from inside the filter area),
            // let that control handle the event so it can receive focus and typing.
            if (e.Source is TextBox)
            {
                return;
            }
            if (_filterArea != null && e.Source is Visual srcVisual)
            {
                var cur = srcVisual;
                while (cur != null)
                {
                    if (cur == (Visual)_filterArea)
                        return;
                    cur = cur.VisualParent as Visual;
                }
            }

            if (OwningColumn == null || e.Handled || !IsEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            Point mousePosition = e.GetPosition(this);
            bool handled = e.Handled;
            OnMouseLeftButtonDown(ref handled, e, mousePosition);
            e.Handled = handled;

            UpdatePseudoClasses();
        }

        private void DataGridColumnHeader_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            // Allow controls inside the filter area to receive the release event
            if (e.Source is TextBox)
            {
                return;
            }
            if (_filterArea != null && e.Source is Visual srcVisual)
            {
                var cur = srcVisual;
                while (cur != null)
                {
                    if (cur == (Visual)_filterArea)
                        return;
                    cur = cur.VisualParent as Visual;
                }
            }

            if (OwningColumn == null || e.Handled || !IsEnabled || e.InitialPressMouseButton != MouseButton.Left)
            {
                return;
            }

            Point mousePosition = e.GetPosition(this);
            Point mousePositionHeaders = e.GetPosition(OwningGrid.ColumnHeaders);
            bool handled = e.Handled;
            OnMouseLeftButtonUp(ref handled, e, mousePosition, mousePositionHeaders);
            e.Handled = handled;

            UpdatePseudoClasses();
        }

        private void DataGridColumnHeader_PointerMoved(object sender, PointerEventArgs e)
        {
            if (OwningGrid == null || !IsEnabled)
            {
                return;
            }

            Point mousePosition = e.GetPosition(this);
            Point mousePositionHeaders = e.GetPosition(OwningGrid.ColumnHeaders);

            OnMouseMove(e, mousePosition, mousePositionHeaders);
        }

        /// <summary>
        /// Returns the column against whose top-left the reordering caret should be positioned
        /// </summary>
        /// <param name="mousePositionHeaders">Mouse position within the ColumnHeadersPresenter</param>
        /// <param name="scroll">Whether or not to scroll horizontally when a column is dragged out of bounds</param>
        /// <param name="scrollAmount">If scroll is true, returns the horizontal amount that was scrolled</param>
        /// <returns></returns>
        private DataGridColumn GetReorderingTargetColumn(Point mousePositionHeaders, bool scroll, out double scrollAmount)
        {
            scrollAmount = 0;
            double leftEdge = OwningGrid.ColumnsInternal.RowGroupSpacerColumn.IsRepresented ? OwningGrid.ColumnsInternal.RowGroupSpacerColumn.ActualWidth : 0;
            double rightEdge = OwningGrid.CellsWidth;
            if (OwningColumn.IsFrozen)
            {
                rightEdge = Math.Min(rightEdge, _frozenColumnsWidth);
            }
            else if (OwningGrid.FrozenColumnCount > 0)
            {
                leftEdge = _frozenColumnsWidth;
            }

            if (mousePositionHeaders.X < leftEdge)
            {
                if (scroll &&
                    OwningGrid.HorizontalScrollBar != null &&
                    OwningGrid.HorizontalScrollBar.IsVisible &&
                    OwningGrid.HorizontalScrollBar.Value > 0)
                {
                    double newVal = mousePositionHeaders.X - leftEdge;
                    scrollAmount = Math.Min(newVal, OwningGrid.HorizontalScrollBar.Value);
                    OwningGrid.UpdateHorizontalOffset(scrollAmount + OwningGrid.HorizontalScrollBar.Value);
                }
                mousePositionHeaders = mousePositionHeaders.WithX(leftEdge);
            }
            else if (mousePositionHeaders.X >= rightEdge)
            {
                if (scroll &&
                    OwningGrid.HorizontalScrollBar != null &&
                    OwningGrid.HorizontalScrollBar.IsVisible &&
                    OwningGrid.HorizontalScrollBar.Value < OwningGrid.HorizontalScrollBar.Maximum)
                {
                    double newVal = mousePositionHeaders.X - rightEdge;
                    scrollAmount = Math.Min(newVal, OwningGrid.HorizontalScrollBar.Maximum - OwningGrid.HorizontalScrollBar.Value);
                    OwningGrid.UpdateHorizontalOffset(scrollAmount + OwningGrid.HorizontalScrollBar.Value);
                }
                mousePositionHeaders = mousePositionHeaders.WithX(rightEdge - 1);
            }

            foreach (DataGridColumn column in OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                Point mousePosition = OwningGrid.ColumnHeaders.Translate(column.HeaderCell, mousePositionHeaders);
                double columnMiddle = column.HeaderCell.Bounds.Width / 2;
                if (mousePosition.X >= 0 && mousePosition.X <= columnMiddle)
                {
                    return column;
                }
                else if (mousePosition.X > columnMiddle && mousePosition.X < column.HeaderCell.Bounds.Width)
                {
                    return OwningGrid.ColumnsInternal.GetNextVisibleColumn(column);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the display index to set the column to
        /// </summary>
        /// <param name="mousePositionHeaders">Mouse position relative to the column headers presenter</param>
        /// <returns></returns>
        private int GetReorderingTargetDisplayIndex(Point mousePositionHeaders)
        {
            DataGridColumn targetColumn = GetReorderingTargetColumn(mousePositionHeaders, false /*scroll*/, out double scrollAmount);
            if (targetColumn != null)
            {
                return targetColumn.DisplayIndex > OwningColumn.DisplayIndex ? targetColumn.DisplayIndex - 1 : targetColumn.DisplayIndex;
            }
            else
            {
                return OwningGrid.Columns.Count - 1;
            }
        }

        /// <summary>
        /// Returns true if the mouse is
        /// - to the left of the element, or within the left half of the element
        /// and
        /// - within the vertical range of the element, or ignoreVertical == true
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <param name="element"></param>
        /// <param name="ignoreVertical"></param>
        /// <returns></returns>
        private bool IsReorderTargeted(Point mousePosition, Control element, bool ignoreVertical)
        {
            Point position = this.Translate(element, mousePosition);

            return (position.X < 0 || (position.X >= 0 && position.X <= element.Bounds.Width / 2))
                && (ignoreVertical || (position.Y >= 0 && position.Y <= element.Bounds.Height));
        }

        /// <summary>
        /// Resets the static DataGridColumnHeader properties when a header loses mouse capture
        /// </summary>
        private void OnLostMouseCapture()
        {
            // When we stop interacting with the column headers, we need to reset the drag mode
            // and close any popups if they are open.

            if (_dragColumn != null && _dragColumn.HeaderCell != null)
            {
                _dragColumn.HeaderCell.Cursor = _originalCursor;
            }
            _dragMode = DragMode.None;
            _dragColumn = null;
            _dragStart = null;
            _lastMousePositionHeaders = null;

            if (OwningGrid != null && OwningGrid.ColumnHeaders != null)
            {
                OwningGrid.ColumnHeaders.DragColumn = null;
                OwningGrid.ColumnHeaders.DragIndicator = null;
                OwningGrid.ColumnHeaders.DropLocationIndicator = null;
            }
        }

        /// <summary>
        /// Sets up the DataGridColumnHeader for the MouseEnter event
        /// </summary>
        /// <param name="mousePosition">mouse position relative to the DataGridColumnHeader</param>
        private void OnMouseEnter(Point mousePosition)
        {
            IsMouseOver = true;
            SetDragCursor(mousePosition);
        }

        /// <summary>
        /// Sets up the DataGridColumnHeader for the MouseLeave event
        /// </summary>
        private void OnMouseLeave()
        {
            IsMouseOver = false;
        }

        private void OnMouseMove_BeginReorder(Point mousePosition)
        {
            var dragIndicator = new DataGridColumnHeader
            {
                OwningColumn = OwningColumn,
                IsEnabled = false,
                Content = GetDragIndicatorContent(Content, ContentTemplate)
            };
            if (OwningGrid.ColumnHeaderTheme is { } columnHeaderTheme)
            {
                dragIndicator.SetValue(ThemeProperty, columnHeaderTheme, BindingPriority.Template);
            }

            dragIndicator.PseudoClasses.Add(":dragIndicator");

            Control dropLocationIndicator = OwningGrid.DropLocationIndicatorTemplate?.Build();

            // If the user didn't style the dropLocationIndicator's Height, default to the column header's height
            if (dropLocationIndicator != null && double.IsNaN(dropLocationIndicator.Height) && dropLocationIndicator is Control element)
            {
                element.Height = Bounds.Height;
            }

            // pass the caret's data template to the user for modification
            DataGridColumnReorderingEventArgs columnReorderingEventArgs = new DataGridColumnReorderingEventArgs(OwningColumn)
            {
                DropLocationIndicator = dropLocationIndicator,
                DragIndicator = dragIndicator
            };
            OwningGrid.OnColumnReordering(columnReorderingEventArgs);
            if (columnReorderingEventArgs.Cancel)
            {
                return;
            }

            // The user didn't cancel, so prepare for the reorder
            _dragColumn = OwningColumn;
            _dragMode = DragMode.Reorder;
            _dragStart = mousePosition;

            // Display the reordering thumb
            OwningGrid.ColumnHeaders.DragColumn = OwningColumn;
            OwningGrid.ColumnHeaders.DragIndicator = columnReorderingEventArgs.DragIndicator;
            OwningGrid.ColumnHeaders.DropLocationIndicator = columnReorderingEventArgs.DropLocationIndicator;

            // If the user didn't style the dragIndicator's Width, default it to the column header's width
            if (double.IsNaN(dragIndicator.Width))
            {
                dragIndicator.Width = Bounds.Width;
            }
        }

#nullable enable

        private object? GetDragIndicatorContent(object? content, IDataTemplate? dataTemplate)
        {
            if (content is ContentControl icc)
            {
                content = icc.Content;
            }

            if (content is Control control)
            {
                if (VisualRoot == null) return content;
                control.Measure(Size.Infinity);
                var rect = new Rectangle()
                {
                    Width = control.DesiredSize.Width,
                    Height = control.DesiredSize.Height,
                    Fill = new VisualBrush
                    {
                        Visual = control, Stretch = Stretch.None, AlignmentX = AlignmentX.Left,
                    }
                };
                return rect;
            }

            if (dataTemplate is not null)
            {
                return dataTemplate.Build(content);
            }
            return content;
        }

#nullable disable

        //TODO DragEvents
        private void OnMouseMove_Reorder(ref bool handled, Point mousePosition, Point mousePositionHeaders)
        {
            if (handled)
            {
                return;
            }

            //handle entry into reorder mode
            if (_dragMode == DragMode.MouseDown && _dragColumn == null && _lastMousePositionHeaders != null)
            {
                var distanceFromInitial = (Vector)(mousePositionHeaders - _lastMousePositionHeaders);
                if (distanceFromInitial.Length > DATAGRIDCOLUMNHEADER_columnsDragTreshold)
                {
                    handled = CanReorderColumn(OwningColumn);

                    if (handled)
                    {
                        OnMouseMove_BeginReorder(mousePosition);
                    }
                }
            }

            //handle reorder mode (eg, positioning of the popup)
            if (_dragMode == DragMode.Reorder && OwningGrid.ColumnHeaders.DragIndicator != null)
            {
                // Find header we're hovering over
                DataGridColumn targetColumn = GetReorderingTargetColumn(mousePositionHeaders, !OwningColumn.IsFrozen /*scroll*/, out double scrollAmount);

                OwningGrid.ColumnHeaders.DragIndicatorOffset = mousePosition.X - _dragStart.Value.X + scrollAmount;
                OwningGrid.ColumnHeaders.InvalidateArrange();

                if (OwningGrid.ColumnHeaders.DropLocationIndicator != null)
                {
                    Point targetPosition = new Point(0, 0);
                    if (targetColumn == null || targetColumn == OwningGrid.ColumnsInternal.FillerColumn || targetColumn.IsFrozen != OwningColumn.IsFrozen)
                    {
                        targetColumn =
                            OwningGrid.ColumnsInternal.GetLastColumn(
                                isVisible: true,
                                isFrozen: OwningColumn.IsFrozen,
                                isReadOnly: null);
                        targetPosition = targetColumn.HeaderCell.Translate(OwningGrid.ColumnHeaders, targetPosition);

                        targetPosition = targetPosition.WithX(targetPosition.X + targetColumn.ActualWidth);
                    }
                    else
                    {
                        targetPosition = targetColumn.HeaderCell.Translate(OwningGrid.ColumnHeaders, targetPosition);
                    }
                    OwningGrid.ColumnHeaders.DropLocationIndicatorOffset = targetPosition.X - scrollAmount;
                }

                handled = true;
            }
        }

        private void OnMouseMove_Resize(ref bool handled, Point mousePositionHeaders)
        {
            if (handled)
            {
                return;
            }

            if (_dragMode == DragMode.Resize && _dragColumn != null && _dragStart.HasValue)
            {
                // resize column

                double mouseDelta = mousePositionHeaders.X - _dragStart.Value.X;
                double desiredWidth = _originalWidth + mouseDelta;

                desiredWidth = Math.Max(_dragColumn.ActualMinWidth, Math.Min(_dragColumn.ActualMaxWidth, desiredWidth));
                _dragColumn.Resize(_dragColumn.Width,
                    new(_dragColumn.Width.Value, _dragColumn.Width.UnitType, _dragColumn.Width.DesiredValue, desiredWidth),
                    true);

                OwningGrid.UpdateHorizontalOffset(_originalHorizontalOffset);

                handled = true;
            }
        }

        private void SetDragCursor(Point mousePosition)
        {
            if (_dragMode != DragMode.None || OwningGrid == null || OwningColumn == null)
            {
                return;
            }

            // set mouse if we can resize column

            double distanceFromLeft = mousePosition.X;
            double distanceFromRight = Bounds.Width - distanceFromLeft;
            DataGridColumn currentColumn = OwningColumn;
            DataGridColumn previousColumn = null;

            if (!(OwningColumn is DataGridFillerColumn))
            {
                previousColumn = OwningGrid.ColumnsInternal.GetPreviousVisibleNonFillerColumn(currentColumn);
            }

            if ((distanceFromRight <= DATAGRIDCOLUMNHEADER_resizeRegionWidth && currentColumn != null && CanResizeColumn(currentColumn)) ||
                (distanceFromLeft <= DATAGRIDCOLUMNHEADER_resizeRegionWidth && previousColumn != null && CanResizeColumn(previousColumn)))
            {
                var resizeCursor = _resizeCursor.Value;
                if (Cursor != resizeCursor)
                {
                    _originalCursor = Cursor;
                    Cursor = resizeCursor;
                }
            }
            else
            {
                Cursor = _originalCursor;
            }
        }

    }

}
