using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Homebrewed_Marketplace_Multitool.Models;
using Homebrewed_Marketplace_Multitool.Models.Items;
using MySqlConnector;

namespace Homebrewed_Marketplace_Multitool.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly Library _library;
    private BookShelfItemViewModel? _selectedBook;
    private string _previewText = "Select a book from the shelf to preview it.";
    private string _statusMessage = "Loading books...";

    public ObservableCollection<BookShelfItemViewModel> Books { get; } = new();

    public BookShelfItemViewModel? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (SetProperty(ref _selectedBook, value))
            {
                UpdatePreview();
            }
        }
    }

    public string PreviewText
    {
        get => _previewText;
        private set => SetProperty(ref _previewText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public LibraryViewModel()
    {
        _library = new Library();
        try
        {
            _library.LoadBooks();
            RefreshBooks();
            StatusMessage = Books.Count == 0
                ? "The shelf is empty. Add a file to get started."
                : $"{Books.Count} book(s) loaded.";
        }
        catch (MySqlException exception)
        {
            Console.Error.WriteLine(exception);
            StatusMessage = "Could not load books. Check the MySQL connection and books table.";
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine(exception);
            StatusMessage = "The books table has an unsupported file_content value. Check the file_content column type.";
        }
    }

    [RelayCommand]
    public void AddBook(Book book)
    {
        var result = _library.AddBook(book);
        if (result[0] == "OK")
        {
            RefreshBooks();
            SelectedBook = Books.FirstOrDefault(item => item.Book.Id == book.Id);
            StatusMessage = $"Added {book.Name}.";
        }
        else
        {
            Console.Error.WriteLine(result[1]);
            StatusMessage = result[1];
        }
    }

    [RelayCommand]
    public void UpdateBook(Book book)
    {
        var result = _library.UpdateBook(book);
        if (result[0] == "OK")
        {
            RefreshBooks();
            SelectedBook = Books.FirstOrDefault(item => item.Book.Id == book.Id);
            StatusMessage = $"Updated {book.Name}.";
        }
        else
        {
            Console.Error.WriteLine(result[1]);
            StatusMessage = result[1];
        }
    }

    [RelayCommand]
    public void RemoveBook(Book book)
    {
        var result = _library.RemoveBook(book.Id);
        if (result[0] == "OK")
        {
            RefreshBooks();
            StatusMessage = $"Removed {book.Name}.";
        }
        else
        {
            Console.Error.WriteLine(result[1]);
            StatusMessage = result[1];
        }
    }

    [RelayCommand]
    public void GetBook(ulong id)
    {
        SelectedBook = Books.FirstOrDefault(item => item.Book.Id == id);
    }

    [RelayCommand]
    public void GetAllBooks()
    {
        RefreshBooks();
    }

    [RelayCommand]
    public void SearchBooks(string query)
    {
        RefreshBooks(string.IsNullOrWhiteSpace(query)
            ? _library.GetAllBooks()
            : _library.SearchBooks(query));
    }

    public string GetColourCode(Book book) => _library.GetColourCode(book);

    public void ReportImportError(string message)
    {
        StatusMessage = message;
    }

    private void RefreshBooks(IEnumerable<Book>? source = null)
    {
        var selectedId = SelectedBook?.Book.Id;
        Books.Clear();
        foreach (var book in source ?? _library.GetAllBooks())
        {
            Books.Add(new BookShelfItemViewModel(book, GetColourCode(book)));
        }

        SelectedBook = Books.FirstOrDefault(item => item.Book.Id == selectedId)
            ?? Books.FirstOrDefault();
    }

    private void UpdatePreview()
    {
        if (SelectedBook is null)
        {
            PreviewText = "Select a book from the shelf to preview it.";
            return;
        }

        try
        {
            PreviewText = BookDocumentReader.Read(SelectedBook.Book);
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine(exception);
            PreviewText = $"Could not read this file: {exception.Message}";
        }
        catch (XmlException exception)
        {
            Console.Error.WriteLine(exception);
            PreviewText = $"Could not read this document: {exception.Message}";
        }
    }
}

public sealed class BookShelfItemViewModel
{
    public Book Book { get; }
    public IBrush CoverBrush { get; }
    public IBrush TextBrush { get; }

    public BookShelfItemViewModel(Book book, string colourCode)
    {
        Book = book;
        CoverBrush = Brush.Parse(colourCode);

        var colour = Color.Parse(colourCode);
        var brightness = (0.299 * colour.R + 0.587 * colour.G + 0.114 * colour.B) / 255;
        TextBrush = brightness > 0.55 ? Brushes.Black : Brushes.White;
    }
}
