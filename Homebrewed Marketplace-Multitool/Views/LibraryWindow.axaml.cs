using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Homebrewed_Marketplace_Multitool.Models.Items;
using Homebrewed_Marketplace_Multitool.ViewModels;

namespace Homebrewed_Marketplace_Multitool.Views;

public partial class LibraryWindow : Window
{
    public LibraryWindow()
    {
        InitializeComponent();
        DataContext = new LibraryViewModel();
    }

    private async void OnAddBookClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not LibraryViewModel viewModel)
        {
            return;
        }

        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Add a book to the library",
                AllowMultiple = false
            });
            var file = files.FirstOrDefault();
            if (file is null)
            {
                return;
            }

            await using var input = await file.OpenReadAsync();
            using var contents = new MemoryStream();
            await input.CopyToAsync(contents);
            viewModel.AddBook(new Book(0, file.Name, contents.ToArray(), ownerId: 0));
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine(exception);
            viewModel.ReportImportError($"Could not read the selected file: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.Error.WriteLine(exception);
            viewModel.ReportImportError("Access to the selected file was denied.");
        }
    }
}
