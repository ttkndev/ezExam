using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace ezExam.App.ViewModels.Base
{
    /// <summary>
    /// ICommand đơn giản dùng trong MVVM.
    /// Hỗ trợ cả sync và async command.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<object?, Task> _executeAsync;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        /// <summary>Dùng cho action đồng bộ</summary>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            : this(p => { execute(p); return Task.CompletedTask; }, canExecute) { }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
            => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try { await _executeAsync(parameter); }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
