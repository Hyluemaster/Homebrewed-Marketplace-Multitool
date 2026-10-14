using System;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace Homebrewed_Marketplace_Multitool.Models
{
	/// <summary>Creates connections to the application's MySQL database.</summary>
	public sealed class Database
	{
		private readonly string _connectionString;

		public Database(string connectionString)
		{
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentException("A connection string is required.", nameof(connectionString));

			_connectionString = connectionString;
		}

		/// <summary>Creates a new unopened MySQL connection.</summary>
		public MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);

		/// <summary>Opens a connection. The caller is responsible for disposing it.</summary>
		public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
		{
			var connection = CreateConnection();
			try
			{
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				return connection;
			}
			catch
			{
				await connection.DisposeAsync().ConfigureAwait(false);
				throw;
			}
		}
	}
}
