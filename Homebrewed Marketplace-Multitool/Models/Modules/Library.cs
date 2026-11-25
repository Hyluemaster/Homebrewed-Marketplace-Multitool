using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Homebrewed_Marketplace_Multitool.Models.Items;
using Homebrewed_Marketplace_Multitool.Models.Services;
using MySqlConnector;

namespace Homebrewed_Marketplace_Multitool.Models.Modules
{
    public class Library
    {
        public Database DB = new Database(
            "server=localhost;" +
            "user=root;" +
            "password=;" +
            "database=homebrewed-marketplace_general-db"
        );
        public List<Book> Books { get; private set; } = new List<Book>();

        public Library()
        {
        }

        public void LoadBooks()
        {
            var books = new List<Book>();
            using var connection = DB.CreateConnection();
            connection.Open();
            using var command = new MySqlCommand(
                "SELECT id, filename, file_content, owner FROM books",
                connection
            );
            using var response = command.ExecuteReader();
            while (response.Read())
            {
                var id = response.GetUInt64("id");
                var name = response.GetString("filename");
                var contentValue = response["file_content"];
                var content = contentValue switch
                {
                    byte[] bytes => bytes,
                    string text => Encoding.UTF8.GetBytes(text),
                    DBNull => null,
                    _ => throw new InvalidDataException(
                        $"The file_content for book '{name}' is not stored as binary or text data.")
                };
                books.Add(new Book(id, name, content, response.GetInt32("owner")));
            }

            Books = books;
        }

        public List<string> AddBook(Book book)
        {
            try
            {
                using var connection = DB.CreateConnection();
                connection.Open();
                using var command = new MySqlCommand(
                    "INSERT INTO books (filename, file_content, owner) " +
                    "VALUES (@filename, @file_content, @owner)",
                    connection
                );
                command.Parameters.AddWithValue("@filename", book.Name);
                command.Parameters.Add("@file_content", MySqlDbType.LongBlob).Value =
                    book.Content is null ? DBNull.Value : book.Content;
                command.Parameters.AddWithValue("@owner", book.OwnerId);
                command.ExecuteNonQuery();
                using var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
                book.Id = Convert.ToUInt64(idCommand.ExecuteScalar());
                Books.Add(book);
                return ["OK", ""];
            }
            catch (MySqlException ex)
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
                try
                {
                    using var connection = DB.CreateConnection();
                    connection.Open();
                    using var command = new MySqlCommand(
                        "UPDATE books SET filename = @filename, file_content = @file_content " +
                        "WHERE id = @id",
                        connection
                    );
                    command.Parameters.AddWithValue("@id", book.Id);
                    command.Parameters.AddWithValue("@filename", book.Name);
                    command.Parameters.Add("@file_content", MySqlDbType.LongBlob).Value =
                        book.Content is null ? DBNull.Value : book.Content;
                    command.ExecuteNonQuery();
                    existingBook.Name = book.Name;
                    existingBook.Content = book.Content;
                    return ["OK", ""];
                }
                catch (MySqlException ex)
                {
                    return ["Error", $"Error occurred while updating {book.Name}: {ex.Message}"];
                }
            }
            else
            {
                return ["Error", "Book not found."];
            }
        }

        public List<string> RemoveBook(ulong id)
        {
            // Removes a book from the library and the database.
            var book = Books.Find(b => b.Id == id);
            if (book != null)
            {
                try
                {
                    using var connection = DB.CreateConnection();
                    connection.Open();
                    using var command = new MySqlCommand(
                        "DELETE FROM books WHERE id = @id",
                        connection
                    );
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                    Books.Remove(book);
                    return ["OK", ""];
                }
                catch (MySqlException ex)
                {
                    return ["Error", $"Error occurred while removing {book.Name}: {ex.Message}"];
                }
            }
            return ["Error", "Book not found."];
        }

        public Book? GetBook(ulong id)
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
            var extension = Path.GetExtension(book.Name).TrimStart('.').ToLowerInvariant();
            if (extension.Length > 0)
            {
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
