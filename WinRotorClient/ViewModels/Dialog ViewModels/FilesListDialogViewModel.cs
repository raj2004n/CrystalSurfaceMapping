using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinRotor.Models;

namespace WinRotor.ViewModels;

public partial class FilesListDialogViewModel : DialogViewModel
{
    public ObservableCollection<FileItem> FileItems { get; set; }
    public Window? DialogWindow { get; set; }
    
    [ObservableProperty] private FileItem _selectedFile;
    [ObservableProperty] private bool _confirmed;

    public FilesListDialogViewModel(ObservableCollection<FileItem> fileItems)
    {
        FileItems = fileItems;
    }
    
    public FilesListDialogViewModel(){}
    
    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        Close();
        DialogWindow?.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
        Close();
        DialogWindow?.Close();
    }
    
    [RelayCommand]
    private async Task DeleteFile(FileItem file)
    {
        if (file == null) return;

        // Remove from the observable collection
        FileItems.Remove(file);

        // Remove from the JSON file
        await RemoveFromJsonFile(file);
    }

    private async Task RemoveFromJsonFile(FileItem fileToRemove)
    {
        try
        {
            // Construct the full path to the JSON file
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appFolder = Path.Combine(documentsFolder, "WinRotor");
            string jsonFilePath = Path.Combine(appFolder, "saved_paths.json");
        
            // Ensure the directory exists
            Directory.CreateDirectory(appFolder);

            // Read existing JSON data
            List<FileItem> fileItems;
            if (File.Exists(jsonFilePath))
            {
                var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
                fileItems = JsonSerializer.Deserialize<List<FileItem>>(jsonContent) ?? new List<FileItem>();
            }
            else
            {
                fileItems = new List<FileItem>();
            }

            // Remove the item that matches both Title and FilePath
            var itemToRemove = fileItems.FirstOrDefault(f => 
                f.Title == fileToRemove.Title && f.FilePath == fileToRemove.FilePath);
        
            if (itemToRemove != null)
            {
                fileItems.Remove(itemToRemove);
            
                // Save the updated list back to JSON
                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(fileItems, options);
                await File.WriteAllTextAsync(jsonFilePath, updatedJson);
            }
        }
        catch (Exception ex)
        {
            // Handle any errors (you might want to show a message to the user)
            Console.WriteLine($"Error removing file from JSON: {ex.Message}");
            // Consider showing a dialog or notification to the user
        }
    }
}