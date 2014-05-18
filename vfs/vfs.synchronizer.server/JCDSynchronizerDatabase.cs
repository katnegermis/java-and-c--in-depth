using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vfs.synchronizer.common;

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
            CreateDefaultUser("user", "password");
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
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Changes ( id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, event_type INTEGER NOT NULL, dataPath TEXT UNIQUE NOT NULL, file_id INTEGER NOT NULL, FOREIGN KEY(file_id) REFERENCES Files(id) );";
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void CreateDefaultUser(string username, string password) {
            if (!(Login(username, password) > 0)) {
                Register(username, password);
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

                    var list = new List<Tuple<long, string>>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = Convert.ToInt64(reader["id"]);
                            string path = Convert.ToString(reader["initPath"]);
                            string name = Path.GetFileName(path.Remove(path.Length - 5));
                            list.Add(new Tuple<long, string>(id, name));
                        }
                    }
                    return list;
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
                    string hfsDir = GetVFSStoragePath(vfsId);
                    
                    var storageNames = GetVFSStorageNames(vfsName);
                    string initPath = storageNames.Item1;
                    string currentPath = storageNames.Item2;

                    writeToFile(currentPath, data);
                    writeToFile(initPath, data);

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
                    command.CommandText = "SELECT currentPath FROM VFS WHERE id = @id;";
                    command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;

                    var path = command.ExecuteScalar().ToString();

                    command.CommandText = "SELECT COUNT(C.id) FROM Files AS F JOIN Changes AS C WHERE F.vfs_id = @id;";
                    command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;

                    long versionId = 0;
                    if ((long)command.ExecuteScalar() > 0)
                    {

                        command.CommandText = "SELECT MAX(C.id) FROM Files AS F JOIN Changes AS C WHERE F.vfs_id = @id;";
                        command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;

                        versionId = (long)command.ExecuteScalar();
                    }
                    var data = readFromFile(path);

                    return new Tuple<long, byte[]>(versionId, data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Adds the creation of a new file to the changes of the given VFS.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS the file has been added to.</param>
        /// <param name="vfsPath">The path of the file in the VFS.</param>
        /// <param name="isFolder">Whether the file is a folder or not.</param>
        /// <returns>The new version id if the change was successfully added, null otherwise.</returns>
        internal string AddFile(long vfsId, string vfsPath, long size, bool isFolder)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    long fileId = addFileRow(vfsId, vfsPath);
                    if (fileId == -1)
                    {
                        fileId = getFileId(vfsId, vfsPath);
                    }

                    if (fileId > 0)
                    {
                        var changeData = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Added, vfsPath, size, isFolder);
                        var versionId = addChange((int)JCDSynchronizationEventType.Added, fileId, changeData);

                        var vfs = getCurrentVFSPath(vfsId);
                        if (vfs == null)
                            throw new Exception(String.Format("Could not find the current VFS file path of {0}", vfsId));

                        JCDSynchronizerChangeExecutor.Execute(vfs, (int)JCDSynchronizationEventType.Added, changeData);

                        return versionId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Adds the delete of a file to the changes of the given VFS.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS the file has been deleted from.</param>
        /// <param name="vfsPath">The path of the file in the VFS.</param>
        /// <returns>The new version id if the change was successfully added, null otherwise.</returns>
        internal string DeleteFile(long vfsId, string vfsPath)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    var fileId = getFileId(vfsId, vfsPath);
                    if (fileId < 0)
                        fileId = addFileRow(vfsId, vfsPath);

                    if (fileId > 0)
                    {
                        var changeData = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Deleted, vfsPath);
                        var versionId = addChange((int)JCDSynchronizationEventType.Deleted, fileId, changeData);

                        var vfs = getCurrentVFSPath(vfsId);
                        if (vfs == null)
                            throw new Exception(String.Format("Could not find the current VFS file path of {0}", vfsId));

                        JCDSynchronizerChangeExecutor.Execute(vfs, (int)JCDSynchronizationEventType.Deleted, changeData);

                        return versionId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Adds the moving of a file to the changes of the given VFS.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS.</param>
        /// <param name="oldPath">The path of the file that has been moved.</param>
        /// <param name="newPath">The new path of the file.</param>
        /// <returns>The new version id if the change was successfully added, null otherwise.</returns>
        internal string MoveFile(long vfsId, string oldPath, string newPath)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    var fileId = getFileId(vfsId, oldPath);
                    if (fileId < 0)
                        fileId = addFileRow(vfsId, oldPath);

                    if (fileId > 0)
                    {
                        var changeData = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Moved, oldPath, newPath);
                        var versionId = addChange((int)JCDSynchronizationEventType.Moved, fileId, changeData);

                        var vfs = getCurrentVFSPath(vfsId);
                        if (vfs == null)
                            throw new Exception(String.Format("Could not find the current VFS file path of {0}", vfsId));

                        JCDSynchronizerChangeExecutor.Execute(vfs, (int)JCDSynchronizationEventType.Moved, changeData);

                        return versionId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Adds the modification of a file to the changes of the given VFS.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS.</param>
        /// <param name="vfsPath">The path of the file that has been modified.</param>
        /// <param name="offset">The offset of the change.</param>
        /// <param name="data">The new data at the given offset.</param>
        /// <returns>The new version id if the change was successfully added, null otherwise.</returns>
        internal string ModifyFile(long vfsId, string vfsPath, long offset, byte[] data)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    var fileId = getFileId(vfsId, vfsPath);
                    if (fileId < 0)
                        fileId = addFileRow(vfsId, vfsPath);

                    if (fileId > 0)
                    {
                        var changeData = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Modified, vfsPath, offset, data);
                        var versionId = addChange((int)JCDSynchronizationEventType.Modified, fileId, changeData);

                        var vfs = getCurrentVFSPath(vfsId);
                        if (vfs == null)
                            throw new Exception(String.Format("Could not find the current VFS file path of {0}", vfsId));

                        JCDSynchronizerChangeExecutor.Execute(vfs, (int)JCDSynchronizationEventType.Modified, changeData);

                        return versionId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Adds the resizing of a file to the changes of the given VFS.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS.</param>
        /// <param name="vfsPath">The path of the file that has been resized.</param>
        /// <param name="newSize">The new size of the file.</param>
        /// <returns>The new version id if the change was successfully added, null otherwise.</returns>
        internal string ResizeFile(long vfsId, string vfsPath, long newSize)
        {
            try
            {
                using (var command = new SQLiteCommand(connection))
                {
                    var fileId = getFileId(vfsId, vfsPath);
                    if (fileId < 0)
                        fileId = addFileRow(vfsId, vfsPath);

                    if (fileId > 0)
                    {
                        var changeData = JCDSynchronizerSerialization.Serialize(JCDSynchronizationEventType.Resized, vfsPath, newSize);
                        var versionId = addChange((int)JCDSynchronizationEventType.Resized, fileId, changeData);

                        var vfs = getCurrentVFSPath(vfsId);
                        if (vfs == null)
                            throw new Exception(String.Format("Could not find the current VFS file path of {0}", vfsId));

                        JCDSynchronizerChangeExecutor.Execute(vfs, (int)JCDSynchronizationEventType.Resized, changeData);

                        return versionId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        /// <summary>
        /// Tries to add a file row into the File table.
        /// </summary>
        /// <param name="vfsId">ID of the VFs.</param>
        /// <param name="vfsPath">Path of the file to add.</param>
        /// <returns>The new file's ID if successfully added, -1 otherwise.</returns>
        private long addFileRow(long vfsId, string vfsPath)
        {
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "INSERT INTO Files (vfsPath, vfs_id) VALUES(@vfsPath, @vfsId);";
                command.Parameters.Add("@vfsPath", System.Data.DbType.String, vfsPath.Length).Value = vfsPath;
                command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfsId;

                var res = command.ExecuteNonQuery();

                long fileId = -1;
                if (res > 0)
                {
                    command.CommandText = "SELECT last_insert_rowid();";
                    fileId = (long)command.ExecuteScalar();
                }

                return fileId;
            }
        }

        /// <summary>
        /// Tries to add a new change to a vfs file.
        /// </summary>
        /// <param name="eventType">The type of the change.</param>
        /// <param name="fileId">The id of the file that has been changed.</param>
        /// <param name="data">The change data.</param>
        /// <returns>The newly created version id if successfully created, null otherwise.</returns>
        private string addChange(int eventType, long fileId, byte[] data)
        {
            using (var command = new SQLiteCommand(connection))
            {
                var dir = "vfsId";
                var dataPath = Path.Combine(dir, (fileId + DateTime.Now.Ticks).ToString());
                if (!(Directory.Exists(dir))) {
                    Directory.CreateDirectory(dir);
                }
                writeToFile(dataPath, data);

                command.CommandText = "INSERT INTO Changes (event_type, dataPath, file_id) VALUES(@event_type, @dataPath, @fileId);";
                command.Parameters.Add("@event_type", System.Data.DbType.Int32).Value = eventType;
                command.Parameters.Add("@dataPath", System.Data.DbType.String, dataPath.Length).Value = dataPath;
                command.Parameters.Add("@fileId", System.Data.DbType.Int64).Value = fileId;

                command.ExecuteNonQuery();

                command.CommandText = "SELECT last_insert_rowid();";
                long versionId = (long)command.ExecuteScalar();

                return versionId.ToString();
            }
        }

        /// <summary>
        /// Tries to add a new change to a vfs file without storing data.
        /// </summary>
        /// <param name="eventType">The type of the change.</param>
        /// <param name="vfsPath">The id of the file that has been changed.</param>
        /// <returns>The newly created version id if successfully created, null otherwise.</returns>
        /* private string addChangeWithoutData(int eventType, long fileId)
         {
             using (var command = new SQLiteCommand(connection))
             {
                 command.CommandText = "INSERT INTO Changes (event_type, file_id) VALUES(@event_type, @fileId);";
                 command.Parameters.Add("@event_type", System.Data.DbType.Int32).Value = eventType;
                 command.Parameters.Add("@fileId", System.Data.DbType.Int64).Value = fileId;

                 command.ExecuteNonQuery();

                 command.CommandText = "SELECT last_insert_rowid();";
                 long versionId = (long)command.ExecuteScalar();

                 return versionId.ToString();
             }
         }*/

        /// <summary>
        /// Tries to retrieve the file ID of the file with the given vfsPath in the VFS with the given ID.
        /// </summary>
        /// <param name="vfsId">ID of the VFS.</param>
        /// <param name="vfsPath">Path of the file in the VFS.</param>
        /// <returns>The ID if found, -1 otherwise.</returns>
        private long getFileId(long vfsId, string vfsPath)
        {
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "SELECT id FROM Files WHERE vfs_id = @vfsId AND vfsPath = @vfsPath;";
                command.Parameters.Add("@vfsId", System.Data.DbType.Int64).Value = vfsId;
                command.Parameters.Add("@vfsPath", System.Data.DbType.String, vfsPath.Length).Value = vfsPath;

                var result = command.ExecuteScalar();
                return result != null ? (long)result : -1;
            }
        }

        /// <summary>
        /// Tries to retrieve the path of the current VFS file of the VFS with the given ID.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS.</param>
        /// <returns>The path of the current VFS file.</returns>
        private string getCurrentVFSPath(long vfsId)
        {
            using (var command = new SQLiteCommand(connection))
            {
                command.CommandText = "SELECT currentPath FROM VFS WHERE id = @id;";
                command.Parameters.Add("@id", System.Data.DbType.Int64).Value = vfsId;

                var result = command.ExecuteScalar();
                return result != null ? result.ToString() : null;
            }
        }


        /// <summary>
        /// Retrieve the changes for the given vfs that happened after the given version.
        /// The result is a Tuple with the new highest version ID and a list with the changes.
        /// Each change is a Tuple with the int of the JCDSynchronizationEventType and a byte[] 
        /// that comes from the JCDSynchronizerSerialization.
        /// </summary>
        /// <param name="vfsId">The ID of the VFS.</param>
        /// <param name="lastVersionId">The Version ID from which on the changes are to be retrieved.</param>
        /// <returns>Tuple with the new version ID and a list of event IDs and event data.</returns>
        internal Tuple<long, List<Tuple<int, byte[]>>> RetrieveChanges(long vfsId, long lastVersionId)
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
                        var list = new List<Tuple<int, byte[]>>();
                        long id = -1;
                        while (reader.Read())
                        {
                            id = Convert.ToInt64(reader["id"]);
                            int eventType = Convert.ToInt32(reader["event_type"]);
                            string dataPath = Convert.ToString(reader["dataPath"]);
                            var data = readFromFile(dataPath);

                            list.Add(new Tuple<int, byte[]>(eventType, data));
                        }
                        if (id > 0)
                            return new Tuple<long, List<Tuple<int, byte[]>>>(id, list);
                    }
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
        internal void CloseDbConnection()
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
            }
        }

        /// <summary>
        /// Creates a new file with the given path and writes the data to it.
        /// </summary>
        /// <param name="path">To write the data to.</param>
        /// <param name="data">To write into the new file.</param>
        private void writeToFile(string path, byte[] data)
        {
            if (File.Exists(path))
                throw new Exception(String.Format("File '{0}' already exists!", path));

            using (var fileStream = new FileStream(path, FileMode.CreateNew))
            using (var writer = new BinaryWriter(fileStream))
                writer.Write(data);
        }

        /// <summary>
        /// Reads and returns the bytes in the given file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <returns>The bytes.</returns>
        private byte[] readFromFile(string path)
        {
            if (!File.Exists(path))
                throw new Exception(String.Format("File '{0}' not exists!", path));

            using (var fileStream = new FileStream(path, FileMode.Open))
            using (var reader = new BinaryReader(fileStream))
                return reader.ReadBytes(Convert.ToInt32(fileStream.Length));
        }

        private string GetVFSStoragePath(long vfsId) {
            var path = vfsId.ToString();
            if (!(Directory.Exists(path))) {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private Tuple<string, string> GetVFSStorageNames(string vfsName) {
            return Tuple.Create(vfsName + ".init", vfsName + ".curr");
        }
    }
}
