using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace Behaviorlibrary
{
	public class MyIntellisenseBehavior : Behavior<RichTextBox>
	{
		#region private Field
		// main items
		Popup _container;
		protected Selector _Child;
		protected Panel _Panel;
		//intellisense core
		int _filterIndex = 0;
		object _selectedValue;
		List<string> _filterList;
		//intellisense core for insert
		TextPointer _insertStart { get; set; }
		TextPointer _insertEnd;
		// Context menu
		Control _intellisenseContextMenu;
		Separator _sep;
		BehaviorDelegateCommand _open;
		InputBinding _ib_open;
		Image _icon;
		#endregion
		#region ctor
		public MyIntellisenseBehavior()
		{
			_container = null;
			_Child = null;
			_selectedValue = null;
			_insertStart = null;
			_insertEnd = null;
			_filterList = new List<string>();
			_icon = new Image();
			_icon.BeginInit();
			_icon.Source = new BitmapImage(new Uri(@"/Behaviorlibrary;component/brackets_icon16.png", UriKind.RelativeOrAbsolute));
			_icon.EndInit();
			_open = new BehaviorDelegateCommand(OnOpen, CanOnOpen)
			 {
				 // internal using
				 GestureKey = Key.J,
				 GestureModifier = ModifierKeys.Control,
				 MouseGesture = MouseAction.LeftClick,
				 InputGestureText = "Ctrl+J"
			 };
		}
		#endregion
		#region Command Open
		protected virtual void OnOpen(object parameter)
		{
			// all procedure opening
			this.IsOpen = true;
		}
		protected virtual bool CanOnOpen(object parameter)
		{
			return !this.IsOpen;
		}
		#endregion
		#region Property Size
		private double _height = 100;
		public double Height
		{
			get { return _height; }
			set
			{
				if (value < 50) return;
				_height = value;
			}
		}

		private double _width = 150;
		public double Width
		{
			get { return _width; }
			set
			{
				if (value < 50) return;
				_width = value;
			}
		}
		#endregion
		#region DependencyProperty ItemsSource
		// Attribute for Blend Expression 
		[CustomPropertyValueEditorAttribute(System.Windows.Interactivity.CustomPropertyValueEditor.PropertyBinding)]
		public IEnumerable<object> ItemsSource
		{
			get { return (IEnumerable<object>)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}
		// Using a DependencyProperty as the backing store for ItemsSource.  This enables binding.
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(IEnumerable<object>), typeof(MyIntellisenseBehavior), new UIPropertyMetadata(new List<object>()));
		#endregion
		#region DependencyProperty SelectedItem
		public object SelectedItem
		{
			get { return (object)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}
		// Using a DependencyProperty as the backing store for SelectedItem.  This binding, etc...
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register("SelectedItem", typeof(object), typeof(MyIntellisenseBehavior), new UIPropertyMetadata(new object(), onSelectedItemChanged));

		private static void onSelectedItemChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			// In this version not implementation
			//RichTextBoxIntellisense control = (RichTextBoxIntellisense)obj;
			//RoutedPropertyChangedEventArgs<object> e = new RoutedPropertyChangedEventArgs<object>(
			//    (object)args.OldValue, (object)args.NewValue, SelectedItemChangedEvent);
			//control.OnSelectedItemChanged(e);
		}
		#endregion
		#region DependencyProperty SelectedIndex
		public int SelectedIndex
		{
			get { return (int)GetValue(SelectedIndexProperty); }
			set { SetValue(SelectedIndexProperty, value); }
		}
		// Using a DependencyProperty as the backing store for SelectedIndex.  This enables binding.
		public static readonly DependencyProperty SelectedIndexProperty =
			DependencyProperty.Register("SelectedIndex", typeof(int), typeof(MyIntellisenseBehavior), new UIPropertyMetadata(-1));
		#endregion
		#region DependencyProperty IsOpen
		[CustomPropertyValueEditorAttribute(System.Windows.Interactivity.CustomPropertyValueEditor.PropertyBinding)]
		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty, value); }
		}
		// Using a DependencyProperty as the backing store for IsOpen. This enables binding.
		public static readonly DependencyProperty IsOpenProperty =
			DependencyProperty.Register("IsOpen", typeof(bool), typeof(MyIntellisenseBehavior),
			new UIPropertyMetadata(false,
				new PropertyChangedCallback(onIsOpenChanged),
				new CoerceValueCallback(coerceValue)));
		private static object coerceValue(DependencyObject element, object value)
		{
			bool newValue = (bool)value;
			return newValue;
		}
		private static void onIsOpenChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			MyIntellisenseBehavior control = (MyIntellisenseBehavior)obj;
			RoutedPropertyChangedEventArgs<bool> e = new RoutedPropertyChangedEventArgs<bool>(
				(bool)args.OldValue, (bool)args.NewValue, IsOpenChangedEvent);
			control.OnIsOpenChanged(e);
		}
		#endregion
		#region RoutedEvent SelectedItemChanged
		/// <summary>
		/// Identifies the SelectedItemChanged routed event.
		/// </summary>
		public static readonly RoutedEvent SelectedItemChangedEvent = EventManager.RegisterRoutedEvent(
			"SelectedItemChanged", RoutingStrategy.Bubble,
			typeof(RoutedPropertyChangedEventHandler<bool>), typeof(MyIntellisenseBehavior));
		/// <summary>
		/// Occurs when the SelectedItemChanged property changes.
		/// </summary>
		public event RoutedPropertyChangedEventHandler<object> SelectedItemChanged;
		protected virtual void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
		{
			// changes from the user ignores in this version  

			RoutedPropertyChangedEventHandler<object> handler = SelectedItemChanged;
			if (handler != null)
			{
				handler(this, new RoutedPropertyChangedEventArgs<object>(
					null, // old value
					_selectedValue, // new value
					SelectedItemChangedEvent // SelectedItemChanged routed event
					));
			}
		}
		#endregion
		#region RoutedEvent IsOpenChanged and PreviewIsOpenChanged
		/// <summary>
		/// Identifies the IsOpenChanged routed event.
		/// </summary>
		public static readonly RoutedEvent IsOpenChangedEvent = EventManager.RegisterRoutedEvent(
			"IsOpenChanged", RoutingStrategy.Bubble,
			typeof(RoutedPropertyChangedEventHandler<bool>), typeof(MyIntellisenseBehavior));
		/// <summary>
		/// Identifies the PreviewIsOpenChanged routed event.
		/// </summary>
		public static readonly RoutedEvent PreviewIsOpenChangedEvent = EventManager.RegisterRoutedEvent(
			"PreviewIsOpenChanged", RoutingStrategy.Tunnel,
			typeof(RoutedPropertyChangedEventHandler<bool>), typeof(MyIntellisenseBehavior));
		/// <summary>
		/// Occurs when the PreviewIsOpen property changes.
		/// </summary>
		public event RoutedPropertyChangedEventHandler<bool> PreviewIsOpenChanged;
		/// <summary>
		/// Occurs when the IsOpen property changes.
		/// </summary>
		public event RoutedPropertyChangedEventHandler<bool> IsOpenChanged;
		/// <summary>
		/// Raises the IsOpenChanged event.
		/// </summary>
		/// <param name="args">Arguments associated with the IsOpenChanged event.</param>
		protected virtual void OnIsOpenChanged(RoutedPropertyChangedEventArgs<bool> args)
		{
			if (args.NewValue && this._container == null)
			{
				if (KeysOpened == null) throw new ArgumentNullException();
				this.StartIntellisense();
				_insertStart = this.AssociatedObject.CaretPosition;
				// IMPORTANT: This is needed for when the rightclick menue is used:
				// Because the TextPointer remembers the position by selecting a character and then if the insert-postion was before or after the character we need to be carefull.
				// Example: text is "12"
				// I want to select between 1 and 2 but that can have two outcomes
				// If i click to the left side (more on the 1) then i get the TextPointer with properties 1 and formward-direction
				// If i click to the right side (more on the 2) then i get the TextPointer with properties 2 and backward-direction
				// The problem is then when we get a TextPointer with properties 2 and backward because now when new charachters are typed it will move with the new text and when a TextRange is created with that it will always be empty
				if (_insertStart.LogicalDirection == LogicalDirection.Forward) {
					_insertStart = this.AssociatedObject.CaretPosition.GetPositionAtOffset(-1);
				}
				_insertEnd = this.AssociatedObject.CaretPosition;
				this.CoreIntellisense();
			}

			if (!args.NewValue)
			{
				this._container.IsOpen = false;
				this._container = null;
			}
			RoutedPropertyChangedEventHandler<bool> handler = IsOpenChanged;
			if (handler != null)
			{
				handler(this, args);
			}
			RoutedPropertyChangedEventHandler<bool> previewHandler = PreviewIsOpenChanged;
			if (previewHandler != null)
			{
				previewHandler(this, args);
			}
		}
		#endregion
		#region DependencyProperty KeysOpened
		public IEnumerable<Key> KeysOpened
		{
			get { return (IEnumerable<Key>)GetValue(KeysOpenedProperty); }
			set { SetValue(KeysOpenedProperty, value); }
		}
		public static readonly DependencyProperty KeysOpenedProperty =
			DependencyProperty.Register("KeysOpened", typeof(IEnumerable<Key>), typeof(MyIntellisenseBehavior), new UIPropertyMetadata(new List<Key> { Key.Oem4 }));
		#endregion
		#region DependencyProperty KeysClosed
		public IEnumerable<Key> KeysClosed
		{
			get { return (IEnumerable<Key>)GetValue(KeysClosedProperty); }
			set { SetValue(KeysClosedProperty, value); }
		}
		public static readonly DependencyProperty KeysClosedProperty =
			DependencyProperty.Register("KeysClosed", typeof(IEnumerable<Key>), typeof(MyIntellisenseBehavior), new UIPropertyMetadata(new List<Key>() { Key.Escape }));
		#endregion
		#region DependencyProperty KeysReturned
		public IEnumerable<Key> KeysReturned
		{
			get { return (IEnumerable<Key>)GetValue(KeysReturnedProperty); }
			set { SetValue(KeysReturnedProperty, value); }
		}
		public static readonly DependencyProperty KeysReturnedProperty =
			DependencyProperty.Register("KeysReturned", typeof(IEnumerable<Key>), typeof(MyIntellisenseBehavior), new UIPropertyMetadata(new List<Key>() { Key.Tab, Key.Return }));
		#endregion
		#region Core Intellisense
		protected override void OnAttached()
		{
			base.OnAttached();
			// KeyDown is overidde in TextBox and has limited functionality used PreviewKeyDown
			this.AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
			this.AssociatedObject.PreviewKeyUp += AssociatedObject_PreviewKeyUp;
			this.AssociatedObject.LostFocus += AssociatedObject_LostFocus;
			// Here attaches ContextMenu
			this.AssociatedObject.ContextMenuOpening += new ContextMenuEventHandler(associatedObject_ContextMenuOpening);
			this.AssociatedObject.ContextMenuClosing += new ContextMenuEventHandler(associatedObject_ContextMenuClosing);
			setGesture();
		}
		private void closeAncestorTopHandler()
		{
			 GetAncestorTop<Window>(this.AssociatedObject).Closing += new System.ComponentModel.CancelEventHandler(topWindow_Closing);
		}
		void topWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			EndIntellisense();
		}
		private void setGesture()
		{
			_ib_open = new InputBinding(
				_open,
				new KeyGesture(
					_open.GestureKey,
					_open.GestureModifier));
			this.AssociatedObject.InputBindings.Add(_ib_open);
		}
		void associatedObject_ContextMenuClosing(object sender, ContextMenuEventArgs e)
		{
			// removing all intellisense items
			this.AssociatedObject.ContextMenu.Items.Remove(_intellisenseContextMenu);
			_intellisenseContextMenu = null;
			this.AssociatedObject.ContextMenu.Items.Remove(_sep);
		}
		void associatedObject_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			if (_intellisenseContextMenu == null)
			{
				MenuItem menuItem = new MenuItem()
				{
					Header = "Intellisense",
					InputGestureText = this._open.InputGestureText,
					Command = this._open,
					Icon = _icon
				};

				// first separator
				this.AssociatedObject.ContextMenu.Items.Add(_sep = new Separator());
				// temp pointer for removing
				_intellisenseContextMenu = menuItem;
				// now item
				this.AssociatedObject.ContextMenu.Items.Add(_intellisenseContextMenu);
			}
		}
		void AssociatedObject_LostFocus(object sender, RoutedEventArgs e)
		{
			// _no exist
			if (_Child == null)
			{
				this.IsOpen = false;
				return;
			}
			// Where is focus
			var m_wind = GetAncestorTop<Window>(this.AssociatedObject);
			IInputElement m_inputElem = FocusManager.GetFocusedElement(m_wind);

			// If selected mouse or other touch device
			if (typeof(ListBoxItem) == m_inputElem.GetType())
			{
				var m_testAnces = GetAncestorTop<ListBox>(m_inputElem as ListBoxItem);
				if (_Child.Equals(m_testAnces)) return;
			};
			// is exist
			if (!_Child.Equals(m_inputElem))
				this.IsOpen = false;
		}

		// handle key
		void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			// The Focus for the ListBox in popup
			if ((e.Key == Key.Down || e.Key == Key.Up) && _container != null) {
				this._Child.Focus();
			}
		}

		// handle key
		[SuppressMessage("Microsoft.Contracts", "CC1015", MessageId = "if (_container == null) return", Justification = "Return statement found in contract section.")]
		[SuppressMessage("Microsoft.Contracts", "CC1057", MessageId = "KeyEventArgs e", Justification = "Has custom parameter validation but assembly mode is not set to support this. It will be treated as Requires<E>.")]
		void AssociatedObject_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			// CC1057 = "Has custom parameter validation but assembly mode is not set to support this.
			// It will be treated as Requires<E>."
			//Contract.Requires(e.Key != Key.None);
			Key m_key = e.Key;
			// Initialization popup
			if (KeysOpened == null) throw new ArgumentNullException("KeysOpened");
			// Init intellisense if an open-key was pressed
			if (_container == null && (this.KeysOpened.Count() >= 0 && this.KeysOpened.Any(k => k == m_key))) {
				StartIntellisense();
			}
			// CC1015 = "Return statement found in contract section."
			if (_container == null) { 
				this.EndIntellisense(); 
				return;
			}

			// Check for key to abort intellisense
			if (KeysClosed == null) throw new ArgumentNullException("KeysClosed");
			if (this.KeysClosed.Count() >= 1 && this.KeysClosed.Any(k => k == m_key)) this.EndIntellisense();// Closed intellisense

			// Check for key to insert selected intellisense entry
			if (KeysReturned == null) throw new ArgumentNullException("KeysReturned");
			if (_container != null && !this.KeysReturned.Any(k => k == m_key)) {
				CoreIntellisense();
			}

			// to selecting
			if (this.KeysReturned.Count() >= 1 && this.KeysReturned.Any(k => k == m_key)) InsertValue();  // Key to selecting
			// contract (KeysReturned == null) at lines
			Contract.EndContractBlock();
		}

		// Get the typed words since start and filter the list of suggestions
		void CoreIntellisense()
		{
			// start position to inserting tag
			if (_insertStart == null) _insertStart = this.AssociatedObject.CaretPosition.GetPositionAtOffset(-1, LogicalDirection.Backward);
			// ending selection to reverse at new run
			_insertEnd = this.AssociatedObject.CaretPosition;
			string m_textFilter = new TextRange(_insertStart, _insertEnd).Text;
			// this is Hard Code
			// TODO: trim from list KeysOpened and KeysClosed
			string m_newFlter = m_textFilter.Trim(new char[] { '[', '{', ' ', ']', '}' });
			IEnumerable<object> m_newFilter = null;
			// set filterList to visable
			_Child.ItemsSource = this._filterList;
			// asynch
			this.Dispatcher.BeginInvoke(
				new Action(() =>
				{
					if (!String.IsNullOrWhiteSpace(m_newFlter))
					{
						//update filterList
						m_newFilter = this._filterList.Where(s => s.ToLower().StartsWith(m_newFlter.ToLower()));
						// set filterList to visable
						if (m_newFilter != null && m_newFilter.Count() > 0) _Child.ItemsSource = m_newFilter;
						// actual index in list
						_Child.SelectedIndex = this._filterIndex;
					}
				}));
			// actual index in list
			_Child.SelectedIndex = this._filterIndex;
		}

		// Preparation for the main work - only setting
		void StartIntellisense()
		{
			closeAncestorTopHandler();
			// creating instantion visual
			InitUIElements();
			_container = new Popup();
			((IAddChild)_container).AddChild(_Panel);
			// set functionality child - attach events
			FunctionalityDefaultChild();
			// layout popup
			_container.Placement = PlacementMode.Custom;
			_container.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(placePopup);
			_container.PlacementTarget = this.AssociatedObject;
			// default setting data source for list to display
			if (this.ItemsSource != null)
			{
				this._filterList = this.ItemsSource.Cast<string>().OrderBy(s => s).ToList();
			}
			if (!this.IsOpen) this.IsOpen = true;
			// appears popup
			_container.IsOpen = true;
		}

		// Add handle to list selector
		private void FunctionalityDefaultChild()
		{
			// PreviewMouseDoubleClick because MouseDoubleClick not is for me using
			_Child.PreviewMouseDoubleClick += (s_child, e_child) =>
			{
				_insertStart = this.AssociatedObject.CaretPosition;
				InsertValue();
			};
			// no coments
			_Child.SelectionChanged += (s_child, e_child) => setSelectedValue();
			// as above MouseDoubleClick
			_Child.PreviewKeyDown += (s_child, e_child) =>
			{
				switch (e_child.Key)
				{
					default:
						this.AssociatedObject.Focus();
						this.IsOpen = false;
						break;
					case Key.Down:
						break;
					case Key.Up:
						break;
					case Key.Tab:
						InsertValue();
						e_child.Handled = true;
						this.AssociatedObject.Focus();
						break;
				}
			};
			// correct size
			if (_width >= 0 || double.IsNaN(_width))
				_Child.Width = _width;
			if (_height >= 0 || double.IsNaN(_height))
				_Child.Height = _height;
		}

		/// <summary>
		/// In a class that inherits from this class overriding this method, you can change some graphics. 
		/// The panel inside the popup, and selector.
		/// </summary>
		protected virtual void InitUIElements()
		{
			_Panel = new StackPanel();
			_Child = new ListBox();
			_Panel.Children.Add(_Child);
		}
		// Inserting selected tip
		void InsertValue()
		{
			this.AssociatedObject.CaretPosition = _insertStart;
			this.AssociatedObject.Selection.Select(_insertStart, _insertEnd);
			this.AssociatedObject.Selection.Text = String.Empty;
			setSelectedValue();
			this.AssociatedObject.CaretPosition.InsertTextInRun(String.Format("{0}", _selectedValue));
			this.AssociatedObject.Focus();
			EndIntellisense();
		}
		// Actualize values selecting 
		void setSelectedValue()
		{
			// selected index
			_filterIndex = _Child.SelectedIndex;
			this.SelectedIndex = _filterIndex;
			_selectedValue = _Child.SelectedItem;
			this.SelectedItem = _selectedValue;
			// this system ignore args
			this.OnSelectedItemChanged(null);
		}
		// Clearing to a new work
		void EndIntellisense()
		{
			this._filterIndex = 0;
			this._filterList = null;
			this._insertEnd = null;
			this._insertStart = null;
			this.IsOpen = false;
			_Child = null;
			_Panel = null;
			_container = null;
			GetAncestorTop<Window>(this.AssociatedObject).Closing -= new System.ComponentModel.CancelEventHandler(topWindow_Closing);
		}
		/// <summary>
		/// The method of uncoupling occurs during the primary class. A good place to clean up.
		/// </summary>
		protected override void OnDetaching()
		{
			this.AssociatedObject.PreviewKeyDown -= new KeyEventHandler(AssociatedObject_PreviewKeyDown);
			this.AssociatedObject.LostFocus -= new RoutedEventHandler(AssociatedObject_LostFocus);
			this.AssociatedObject.PreviewKeyUp -= new KeyEventHandler(AssociatedObject_PreviewKeyUp);
			this.AssociatedObject.ContextMenuOpening -= new ContextMenuEventHandler(associatedObject_ContextMenuOpening);
			this.AssociatedObject.ContextMenuClosing -= new ContextMenuEventHandler(associatedObject_ContextMenuClosing);
			this.AssociatedObject.InputBindings.Remove(_ib_open);
			EndIntellisense();
			base.OnDetaching();

		}
		CustomPopupPlacement[] placePopup(Size popupSize, Size targetSize, Point offset)
		{
			double xOffset = (double)this.AssociatedObject.CaretPosition.GetCharacterRect(LogicalDirection.Forward).Right;
			double yOffset = (double)this.AssociatedObject.CaretPosition.GetCharacterRect(LogicalDirection.Forward).Bottom;

			CustomPopupPlacement placementSubstitution = new CustomPopupPlacement(new Point(xOffset, -this.Height), PopupPrimaryAxis.Vertical);
			CustomPopupPlacement placementDefault = new CustomPopupPlacement(new Point(xOffset, yOffset), PopupPrimaryAxis.Horizontal);

			return new CustomPopupPlacement[] { placementDefault, placementSubstitution };
		}
		#endregion //Core
		#region Helpers
		/// <summary>
		/// No comments
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="childObject"></param>
		/// <returns></returns>
		internal static T GetAncestorTop<T>(System.Windows.DependencyObject childObject) where T : DependencyObject
		{
			var obj = System.Windows.Media.VisualTreeHelper.GetParent(childObject);
			T wind = obj as T;
			if (wind == null)
			{
				wind = GetAncestorTop<T>(obj);
			}
			return wind as T;
		}
		#endregion //Helpers

	}
}
