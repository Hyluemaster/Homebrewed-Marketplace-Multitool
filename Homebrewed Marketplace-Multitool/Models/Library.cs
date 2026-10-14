using System;
using System.Collections.Generic;
using Homebrewed_Marketplace_Multitool.Models.Items;
using Database = Homebrewed_Marketplace_Multitool.Models.Database;

namespace Homebrewed_Marketplace_Multitool.Models
{
    public class Library
    {
        public Database DB = new Database(
            "server=localhost;" +
            "user=root;" +
            "password=;" +
            "database=homebrewed_marketplace"
        );
        public List<Book> Books { get; set; } = new List<Book>();

        public Library()
        {
            // Loads all documents from the database into the library.
            var command = new MySqlConnector.MySqlCommand(
                "SELECT * FROM books",
                DB.CreateConnection()
            );
            var response = command.ExecuteReader();
            while (response.Read())
            {
                var id = response.GetInt32("id");
                var name = response.GetString("name");
                var content = response.GetString("content");
                Books.Add(new Book(id, name, content));
            }
        }

        public List<string> AddBook(Book book)
        {
            // Adds a new book to the library and the database.
            Books.Add(book);
            var command = new MySqlConnector.MySqlCommand(
                "INSERT INTO books (name, content) VALUES (@name, @content)",
                DB.CreateConnection()
            );
            command.Parameters.AddWithValue("@name", book.Name);
            command.Parameters.AddWithValue("@content", book.Content);
            try
            {
                command.ExecuteNonQuery();
                return ["OK", ""];
            }
            catch (Exception ex)
            {
                return ["Error", $"Error occurred while adding {book.Name}: {ex.Message}"];
            }
        }

        public List<string> UpdateBook(Book book)
        {
            // Updates an existing book in the library and the database.
            var existingBook = Books.Find(b => b.Id == book.Id);
            if (existingBook != null)
            {
                existingBook.Name = book.Name;
                existingBook.Content = book.Content;
                var command = new MySqlConnector.MySqlCommand(
                    "UPDATE books SET name = @name, content = @content WHERE id = @id",
                    DB.CreateConnection()
                );
                command.Parameters.AddWithValue("@id", book.Id);
                command.Parameters.AddWithValue("@name", book.Name);
                command.Parameters.AddWithValue("@content", book.Content);
                try
                {
                    command.ExecuteNonQuery();
                    return ["OK", ""];
                }
                catch (Exception ex)
                {
                    return ["Error", $"Error occurred while updating {book.Name}: {ex.Message}"];
                }
            }
            else
            {
                return ["Error", "Book not found."];
            }
        }

        public List<string> RemoveBook(int id)
        {
            // Removes a book from the library and the database.
            var book = Books.Find(b => b.Id == id);
            if (book != null)
            {
                Books.Remove(book);
                var command = new MySqlConnector.MySqlCommand(
                    "DELETE FROM books WHERE id = @id",
                    DB.CreateConnection()
                );
                command.Parameters.AddWithValue("@id", id);
                try
                {
                    command.ExecuteNonQuery();
                    return ["OK", ""];
                }
                catch (Exception ex)
                {
                    return ["Error", $"Error occurred while removing {book.Name}: {ex.Message}"];
                }
            }
            return ["Error", "Book not found."];
        }

        public Book? GetBook(int id)
        {
            // Retrieves a book from the library by its ID.
            return Books.Find(b => b.Id == id);
        }

        public List<Book> GetAllBooks()
        {
            // Retrieves all books from the library.
            return Books;
        }

        public List<Book> SearchBooks(string query)
        {
            // Searches for books in the library by name.
            return Books.FindAll(b => b.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        public string GetColourCode(Book book)
        {
            var temp = book.Name.Split('.');
            if (temp.Length > 1)
            {
                var extension = temp[1];
                return extension switch
                {
                    "txt" => "#FFFFFF", // White for TXT
                    "pdf" => "#FE0000", // Red for PDF
                    "doc" => "#2775e1", // Blue for DOC
                    "docx" => "#29538F", // Dark Blue for DOCX
                    "ppt" => "#f44720", // Orange for PPT
                    "pptx" => "#C9634B", // Light Brown for PPTX
                    "xls" => "#14d169", // Light Green for XLS
                    "xlsx" => "#1F7246", // Dark Green for XLSX
                    "accdb" => "#A13638", // Dark Red for ACCDB
                    "zip" => "#F0E68C", // Yellow for ZIP
                    _ => "#b017c8", // Purple for any other Extension.
                    // (Mostly to catch Files like .py, .cs, .php, .html, etc.)
                };
            }
            return "#808080"; // Default color for files with no extensions.
        }
    }
}
