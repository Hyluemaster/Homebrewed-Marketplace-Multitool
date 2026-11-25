using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Homebrewed_Marketplace_Multitool.Models.Items;
using Isopoh.Cryptography.Argon2;
using MySqlConnector;

namespace Homebrewed_Marketplace_Multitool.Models.Services
{
    public class UserActions
    {
        private const int SaltLength = 16;
        private const int HashLength = 32;

        public Database DB = new Database(
            "server=localhost;" +
            "user=root;" +
            "password=;" +
            "database=homebrewed-marketplace_general-db"
        );

        public User? CurrentUser { get; private set; }

        public async Task<bool> Register(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);

            if (!IsValidPassword(password))
            {
                return false;
            }

            var passwordHash = HashPassword(password);
            await using var connection = await DB.OpenConnectionAsync(cancellationToken);
            await using var command = new MySqlCommand(
                "INSERT INTO students (Username, Password) VALUES (@username, @password)",
                connection
            );
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", passwordHash);

            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        public async Task<User?> LogIn(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);
            ArgumentNullException.ThrowIfNull(password);
            CurrentUser = null;

            await using var connection = await DB.OpenConnectionAsync(cancellationToken);
            await using var command = new MySqlCommand(
                "SELECT UID, Username, Nickname, Password, `Group` " +
                "FROM students WHERE Username = @username LIMIT 1",
                connection
            );
            command.Parameters.AddWithValue("@username", username);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var storedHash = reader.GetString("Password");
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                if (!Argon2.Verify(storedHash, passwordBytes))
                {
                    return null;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }

            CurrentUser = new User(
                Convert.ToUInt64(reader["UID"]),
                reader.GetString("Nickname"),
                reader.GetString("Username"),
                reader.GetString("Group")
            );
            return CurrentUser;
        }

        private static bool IsValidPassword(string password) =>
            password is not null
            && password.Length is >= 3 and <= 30
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character => char.IsPunctuation(character) || char.IsSymbol(character));

        private static string HashPassword(string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            try
            {
                var config = new Argon2Config
                {
                    Type = Argon2Type.HybridAddressing,
                    Version = Argon2Version.Nineteen,
                    TimeCost = 3,
                    MemoryCost = 65536,
                    Lanes = 4,
                    Threads = 4,
                    Password = passwordBytes,
                    Salt = RandomNumberGenerator.GetBytes(SaltLength),
                    Secret = Array.Empty<byte>(),
                    AssociatedData = Array.Empty<byte>(),
                    HashLength = HashLength
                };

                using var hash = new Argon2(config).Hash();
                return config.EncodeString(hash.Buffer);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }
}
