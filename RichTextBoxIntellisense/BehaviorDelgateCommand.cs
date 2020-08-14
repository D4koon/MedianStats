using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace Behaviorlibrary
{
    internal class BehaviorDelegateCommand : ICommand
    {
        // Specify the keys and mouse actions that invoke the command. 
        public Key GestureKey { get; set; }
        public ModifierKeys GestureModifier { get; set; }
        public MouseAction MouseGesture { get; set; }
        public string InputGestureText { get; set; }
        Action<object> _executeDelegate;
        Func<object, bool> _canExecuteDelegate;
        public BehaviorDelegateCommand(Action<object> executeDelegate)
            : this(executeDelegate, null){}
        public BehaviorDelegateCommand(Action<object> executeDelegate, Func<object, bool> canExecuteDelegate)
        {
            //Contract.Requires<ArgumentNullException>(executeDelegate == null);
            _executeDelegate = executeDelegate;
            _canExecuteDelegate = canExecuteDelegate;
        }
        public void Execute(object parameter)
        {
            _executeDelegate(parameter);
        }
        public bool CanExecute(object parameter)
        {
            return _canExecuteDelegate(parameter);
        }
        [SuppressMessage("Microsoft.Contracts", "CS0067", Justification = "The event 'BehaviorDelegateCommand.CanExecuteChanged' is never used.")]
        public event EventHandler CanExecuteChanged;
    }
}