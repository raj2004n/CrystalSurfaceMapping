using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using WinRotor.Models;
using WinRotor.ViewModels;
using WinRotor.Views;

namespace WinRotor.Services.Dialog
{
    public class DialogService : IDialogService
    {
        private readonly Func<TopLevel?> _topLevelProvider;

        public DialogService(Func<TopLevel?> topLevelProvider)
        {
            _topLevelProvider = topLevelProvider;
        }

        public async Task<bool> ShowConfirmDialog(
            string title,
            string message,
            string confirmText = "OK",
            string cancelText = "Cancel")
        {
            var viewModel = new ConfirmDialogViewModel(title, message)
            {
                ConfirmText = confirmText,
                CancelText = cancelText
            };

            var dialog = new DialogWindow
            {
                Content = new ConfirmDialogView { DataContext = viewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                CanResize = false
            };

            // Store reference for direct closure
            viewModel.DialogWindow = dialog;

            var topLevel = _topLevelProvider();
            if (topLevel is Window ownerWindow)
            {
                await dialog.ShowDialog(ownerWindow);
            }
            else
            {
                dialog.Show();
            }

            await viewModel.WaitAsync();
            return viewModel.Confirmed;
        }

        public async Task<bool> ShowConfirmMapDialog(
            string title,
            string message,
            TimeSpan estimatedTime,
            string confirmText = "OK",
            string cancelText = "Cancel")
        {
            var viewModel = new ConfirmMapDialogViewModel(title, message, estimatedTime)
            {
                ConfirmText = confirmText,
                CancelText = cancelText
            };

            var dialog = new DialogWindow
            {
                Content = new ConfirmMapDialogView { DataContext = viewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                CanResize = false
            };

            // Store reference for direct closure
            viewModel.DialogWindow = dialog;

            var topLevel = _topLevelProvider();
            if (topLevel is Window ownerWindow)
            {
                await dialog.ShowDialog(ownerWindow);
            }
            else
            {
                dialog.Show();
            }

            await viewModel.WaitAsync();
            return viewModel.Confirmed;
        }

        public async Task ShowInfoDialog(string title, string message)
        {
            var viewModel = new InfoDialogViewModel(title, message);

            var dialog = new DialogWindow
            {
                Content = new InfoDialogView { DataContext = viewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                CanResize = false
            };

            // Store reference for direct closure
            viewModel.DialogWindow = dialog;

            var topLevel = _topLevelProvider();
            if (topLevel is Window ownerWindow)
            {
                await dialog.ShowDialog(ownerWindow);
            }
            else
            {
                dialog.Show();
            }

            await viewModel.WaitAsync();
        }

        public async Task<(bool, string, string)> ShowAddFilePathDialog(
            string title,
            string confirmText = "OK",
            string cancelText = "Cancel")
        {
            // Returns File Title and File Path
            var viewModel = new AddFilePathDialogViewModel
            {
                Title = title,
                ConfirmText = confirmText,
                CancelText = cancelText
            };

            var dialog = new DialogWindow
            {
                Content = new AddFilePathDialogView { DataContext = viewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                CanResize = false
            };

            // Store reference for direct closure
            viewModel.DialogWindow = dialog;

            var topLevel = _topLevelProvider();
            if (topLevel is Window ownerWindow)
            {
                await dialog.ShowDialog(ownerWindow);
            }
            else
            {
                dialog.Show();
            }

            await viewModel.WaitAsync();
            return (viewModel.Confirmed, viewModel.FileTitle, viewModel.FilePath);
        }

        public async Task<(bool Confirmed, FileItem SelectedFile)> ShowFilesListDialog(ObservableCollection<FileItem> files)
        {
            var viewModel = new FilesListDialogViewModel(files);

            var dialog = new DialogWindow
            {
                Content = new FilesListDialogView { DataContext = viewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                CanResize = false
            };

            viewModel.DialogWindow = dialog;

            var topLevel = _topLevelProvider();
            if (topLevel is Window ownerWindow)
            {
                await dialog.ShowDialog(ownerWindow);
            }
            else
            {
                dialog.Show();
            }

            await viewModel.WaitAsync();

            // Return whether a file was confirmed and the selected file
            return (viewModel.Confirmed, viewModel.SelectedFile);
        }
    }
}
