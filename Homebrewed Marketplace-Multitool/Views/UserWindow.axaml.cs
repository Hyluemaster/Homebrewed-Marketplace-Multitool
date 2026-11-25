using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Homebrewed_Marketplace_Multitool.Models.Items;
using Homebrewed_Marketplace_Multitool.Models.Services;
using MySqlConnector;

namespace Homebrewed_Marketplace_Multitool.Views;

public partial class UserWindow : Window
{
    private readonly UserActions _userActions = new();

    public User? AuthenticatedUser => _userActions.CurrentUser;

    public UserWindow()
    {
        InitializeComponent();
    }

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        var username = LoginUsernameBox.Text?.Trim() ?? string.Empty;
        var password = LoginPasswordBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || password.Length == 0)
        {
            StatusText.Text = "Enter your username and password.";
            return;
        }

        SetBusy(true);
        try
        {
            var user = await _userActions.LogIn(username, password);
            StatusText.Text = user is null
                ? "The username or password is incorrect."
                : $"Signed in as {user.Username}.";
        }
        catch (MySqlException exception)
        {
            Console.Error.WriteLine(exception);
            StatusText.Text = "Could not reach the account database. Please try again.";
        }
        finally
        {
            LoginPasswordBox.Clear();
            SetBusy(false);
        }
    }

    private async void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        var username = RegisterUsernameBox.Text?.Trim() ?? string.Empty;
        var password = RegisterPasswordBox.Text ?? string.Empty;
        var confirmation = ConfirmPasswordBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            StatusText.Text = "Enter a username.";
            return;
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            StatusText.Text = "The passwords do not match.";
            return;
        }

        SetBusy(true);
        try
        {
            if (!await _userActions.Register(username, password))
            {
                StatusText.Text = "Use 3–30 characters with uppercase, lowercase, a number, and a symbol.";
                return;
            }

            LoginUsernameBox.Text = username;
            AuthTabs.SelectedIndex = 0;
            StatusText.Text = "Account created. You can now log in.";
        }
        catch (MySqlException exception)
        {
            Console.Error.WriteLine(exception);
            StatusText.Text = "Could not create the account. Check the username and database, then try again.";
        }
        finally
        {
            RegisterPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        AuthTabs.IsEnabled = !isBusy;
    }
}
