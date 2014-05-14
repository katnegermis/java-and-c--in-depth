using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vfs.synchronizer.server
{
    class JCDSynchronizerDatabase
    {

        private const string dbName = "jcdSynchronizer.db";

        private SQLiteConnection connection;

        public JCDSynchronizerDatabase()
        {
            openDbConnection();
        }

        /// <summary>
        /// Opens a new Connection to the database.
        /// Creates the tables if they do not exist already.
        /// </summary>
        private void openDbConnection()
        {
            connection = new SQLiteConnection();
            connection.ConnectionString = "Data Source=" + dbName;
            connection.Open();

            createTablesIfNotExisting();
        }

        /// <summary>
        /// Creates the SQLite tables if they do not yet exist.
        /// </summary>
        internal void createTablesIfNotExisting()
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    // Users
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Users ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE NOT NULL, password TEXT NOT NULL );";
                    command.ExecuteNonQuery();

                    // VFS
                    command.CommandText = "CREATE TABLE IF NOT EXISTS VFS ( id INTEGER NOT NULL PRIMARY KEY, initPath TEXT UNIQUE NOT NULL, currentPath TEST UNIQUE NOT NULL, user_id INTEGER NOT NULL, FOREIGN KEY(user_id) REFERENCES Users(id) );";
                    command.ExecuteNonQuery();

                    // Files
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Files ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, vfsPath TEXT NOT NULL, vfs_id INTEGER NOT NULL, FOREIGN KEY(vfs_id) REFERENCES VFS(id) );";
                    command.ExecuteNonQuery();

                    // Changes
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Changes ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, event_type INTEGER NOT NULL, dataPath TEXT, file_id INTEGER NOT NULL, FOREIGN KEY(file_id) REFERENCES Files(id) );";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Tries to register a User with the given name and password in the DB.
        /// </summary>
        /// <param name="name">The user's name.</param>
        /// <param name="password">The user's password</param>
        /// <returns>The id of the new user, -1 otherwise.</returns>
        internal long Register(string name, string password)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "INSERT INTO Users (name, password) VALUES(@name, @password);";
                    command.Parameters.Add("@name", System.Data.DbType.String, name.Length).Value = name;
                    command.Parameters.Add("@password", System.Data.DbType.String, password.Length).Value = password;

                    if (command.ExecuteNonQuery() > 0)
                    {
                        command.CommandText = "SELECT last_insert_rowid();";
                        var result = command.ExecuteScalar();
                        return result != null ? (long)result : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return -1;
        }

        /// <summary>
        /// Tries to log in a user with the given name and password.
        /// </summary>
        /// <param name="name">The user's name.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>The id of the logged in user, -1 otherwise.</returns>
        internal long Login(string name, string password)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT id FROM Users WHERE name = @name AND password = @password;";
                    command.Parameters.Add("@name", System.Data.DbType.String, name.Length).Value = name;
                    command.Parameters.Add("@password", System.Data.DbType.String, password.Length).Value = password;

                    var result = command.ExecuteScalar();
                    return result != null ? (long)result : -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        internal List<Tuple<long, string>> ListVFSes(long userId)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT id, initPath FROM VFS WHERE user_id = @userId;";
                    command.Parameters.Add("@userId", System.Data.DbType.Int64).Value = userId;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = Convert.ToInt64(reader["id"]);
                            string path = Convert.ToString(reader["initPath"]);
                            string name = Path.GetFileName(path.Remove(path.Length - 5));
                            //TODO: put into some object to then give back directly or serialize
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }


        /// <summary>
        /// Tries to add a new VFS of the given user to the DB.
        /// The newly created unique VFS-id is then returned if added successfully.
        /// </summary>
        /// <param name="vfsName">The name of the VFS file.</param>
        /// <param name="userId">Id of the user.</param>
        /// <param name="data">The actual vfs data that is to be stored.</param>
        /// <returns>The unique id of the VFS if added successfully, null otherwise.</returns>
        internal string AddVFS(string vfsName, long userId, byte[] data)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    long vfsId = Convert.ToInt64((vfsName + DateTime.Now.Ticks).GetHashCode());
                    string hfsPath = vfsId + @"/";
                    string initPath = hfsPath + vfsName + ".init";
                    string currentPath = hfsPath + vfsName + ".curr";
                    //TODO store to disk as init and current in own folder (vfsId)

                    command.CommandText = "INSERT INTO VFS (id, user_id, initPath, currentPath) VALUES(@id, @userId, @initPath, @currentPath);";
                    command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;
                    command.Parameters.Add("@userId", System.Data.DbType.Int64).Value = userId;
                    command.Parameters.Add("@initPath", System.Data.DbType.String, initPath.Length).Value = initPath;
                    command.Parameters.Add("@currentPath", System.Data.DbType.String, currentPath.Length).Value = currentPath;

                    command.ExecuteNonQuery();

                    return vfsId.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Tries to delete the given VFS from the database.
        /// </summary>
        /// <param name="vfsId">Id of the VFS to delete.</param>
        /// <param name="userID">Id of the user that wants to delete the VFS.</param>
        /// <returns>True if done successfully, false otherwise.</returns>
        internal bool DeleteVFS(long vfsId)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "DELETE FROM VFS WHERE id = @id;";
                    command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;

                    command.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// Retrieves the given vfs and returns its data and version id.
        /// </summary>
        /// <param name="vfsId">Id of the VFS.</param>
        /// <returns>Tuple with id and data.</returns>
        internal Tuple<long, byte[]> RetrieveVFS(long vfsId)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    //TODO implement
                    command.CommandText = "SELECT V.currentPath, F.id FROM VFS AS V JOIN Files AS F JOIN Changes AS C WHERE id = @id;";
                    command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;

                    command.ExecuteScalar();

                    return new Tuple<long, byte[]>(123L, new byte[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Tries to add a new change to a vfs file.
        /// </summary>
        /// <param name="eventType">The type of the change.</param>
        /// <param name="vfsId">The id of the vfs where a change happaned.</param>
        /// <param name="vfsPath">The path of the file that has been changed.</param>
        /// <param name="data">The change data.</param>
        /// <returns>The newly created version id if successfully created, null otherwise.</returns>
        internal string AddChange(int eventType, long vfsId, string vfsPath, byte[] data)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT id FROM Files WHERE vfs_id = @vfsId AND vfsPath = @vfsPath;";
                    command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfsId;
                    command.Parameters.Add("@vfsPath", System.Data.DbType.String, vfsPath.Length).Value = vfsPath;

                    var result = command.ExecuteScalar();
                    long fileId = result != null ? (long)result : -1;

                    if (fileId < 0)
                    {
                        command.CommandText = "INSERT INTO Files (vfsPath, vfs_id) VALUES(@vfsPath, @vfsId);";
                        command.Parameters.Add("@vfsPath", System.Data.DbType.String, vfsPath.Length).Value = vfsPath;
                        command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfsId;

                        command.ExecuteNonQuery();

                        command.CommandText = "SELECT last_insert_rowid();";

                        fileId = (long)command.ExecuteScalar();
                    }

                    if (data == null)
                    {
                        command.CommandText = "INSERT INTO Changes (event_type, file_id) VALUES(@event_type, @fileId);";
                        command.Parameters.Add("@event_type", System.Data.DbType.Int32).Value = eventType;
                        command.Parameters.Add("@fileId", System.Data.DbType.Int64).Value = fileId;
                    }
                    else
                    {
                        //TODO store the data to disk
                        var dataPath = @"vfsId/" + fileId + DateTime.Now.Ticks;

                        command.CommandText = "INSERT INTO Changes (event_type, dataPath, file_id) VALUES(@event_type, @dataPath, @fileId);";
                        command.Parameters.Add("@event_type", System.Data.DbType.Int32).Value = eventType;
                        command.Parameters.Add("@dataPath", System.Data.DbType.String, dataPath.Length).Value = dataPath;
                        command.Parameters.Add("@fileId", System.Data.DbType.Int64).Value = fileId;

                    }
                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid();";

                    long versionId = (long)command.ExecuteScalar();

                    return versionId.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="vfsId"></param>
        /// <param name="lastVersionId"></param>
        /// <returns></returns>
        internal string RetrieveChanges(long vfsId, long lastVersionId)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT F.vfsPath, C.id, C.event_type, C.dataPath " +
                           " FROM Files AS F JOIN Changes AS C" +
                           " WHERE F.vfs_id = @vfsId AND F.id = C.file_id AND C.id > @versionId" +
                           " ORDER BY(C.id);";
                    command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfsId;
                    command.Parameters.Add("@versionId", System.Data.DbType.Int64).Value = lastVersionId;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = Convert.ToInt64(reader["id"]);
                            int event_type = Convert.ToInt32(reader["event_type"]);
                            string vfsPath = Convert.ToString(reader["vfsPath"]);
                            string dataPath = Convert.ToString(reader["dataPath"]);
                            //TODO: put into some object to then give back directly or serialize
                        }
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Closes the db connection.
        /// </summary>
        internal void closeDbConnection()
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
        }
    }
}
