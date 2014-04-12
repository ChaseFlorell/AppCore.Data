﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;

namespace FutureState.AppCore.Data.Sqlite.Android
{
    public class DbProvider : SqliteDbProviderBase
    {
        private readonly IDbConnectionProvider _connectionProvider;
        private readonly string _sqliteDatabasePath;
        public DbProvider(string databaseName)
        {

            DatabaseName = databaseName;
            _sqliteDatabasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), databaseName);
            _connectionProvider = new DbConnectionProvider(_sqliteDatabasePath);
        }

        public override bool CheckIfDatabaseExists()
        {
            var exists = File.Exists(_sqliteDatabasePath);
            return exists;
        }

        public override void CreateDatabase()
        {
            SqliteConnection.CreateFile(_sqliteDatabasePath);
        }

        public override void DropDatabase()
        {
            File.Delete(_sqliteDatabasePath);
        }

        public override bool CheckIfTableExists(string tableName)
        {
            var exists = ExecuteScalar<int>(string.Format(CheckTableExists, tableName)) == 1;
            return exists;
        }

        #region ExecuteReader

        public override TResult ExecuteReader<TResult>(string commandText, Func<IDbReader, TResult> readerMapper)
        {
            return ExecuteReader(commandText, new Dictionary<string, object>(), readerMapper);
        }

        public override TResult ExecuteReader<TResult>(string commandText, IDictionary<string, object> parameters, Func<IDbReader, TResult> readerMapper)
        {
            using (var connection = _connectionProvider.GetOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    EnableForeignKeys(command);
                    command.CommandText = commandText;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(new SqliteParameter(parameter.Key, parameter.Value ?? DBNull.Value));
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        var r = new DbReader(reader);
                        return readerMapper(r);
                    }
                }
            }
        }

        #endregion

        #region ExecuteNonQuery

        public override void ExecuteNonQuery(string commandText)
        {
            ExecuteNonQuery(commandText, new Dictionary<string, object>());
        }

        public override void ExecuteNonQuery(string commandText, IDictionary<string, object> parameters)
        {
            using (var connection = _connectionProvider.GetOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    EnableForeignKeys(command);
                    command.CommandText = commandText;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(new SqliteParameter(parameter.Key, parameter.Value ?? DBNull.Value));
                    }

                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region ExecuteScalar

        public override TKey ExecuteScalar<TKey>(string commandText)
        {
            return ExecuteScalar<TKey>(commandText, new Dictionary<string, object>());
        }

        public override TKey ExecuteScalar<TKey>(string commandText, IDictionary<string, object> parameters)
        {
            using (var connection = _connectionProvider.GetOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    EnableForeignKeys(command);
                    command.CommandText = commandText;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.Add(new SqliteParameter(parameter.Key, parameter.Value ?? DBNull.Value));
                    }

                    var result = command.ExecuteScalar();

                    if (typeof(TKey) == typeof(Guid))
                        return (TKey)(object)new Guid((byte[])result);
                    if (typeof(TKey) == typeof(int))
                        return (TKey)(object)(result == null ? 0 : 1);

                    return (TKey)result;
                }
            }
        }

        #endregion

        private static void EnableForeignKeys(SqliteCommand command)
        {
            // todo: make this configurable.
            command.CommandText = "PRAGMA foreign_keys=ON";
            command.ExecuteNonQuery();
        }
    }
}