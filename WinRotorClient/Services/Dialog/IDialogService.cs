using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WinRotor.Models;

public interface IDialogService

{

    Task<bool> ShowConfirmDialog(string title, string message, string confirmText = "OK", string cancelText = "Cancel");

    Task<bool> ShowConfirmMapDialog(string title, string message, TimeSpan estimatedTime ,string confirmText = "OK", string cancelText = "Cancel");

    // Returns File Title and File Path
    Task<(bool, string, string)> ShowAddFilePathDialog(string title, string confirmText = "OK", string cancelText = "Cancel");

    Task<(bool Confirmed, FileItem SelectedFile)> ShowFilesListDialog(ObservableCollection<FileItem> files);

    Task ShowInfoDialog(string title, string message);

}