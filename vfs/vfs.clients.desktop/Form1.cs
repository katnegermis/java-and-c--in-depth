using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using vfs.core;

namespace vfs.clients.desktop
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// The currently mounted VFS.
        /// If none is mounted, then null.
        /// </summary>
        JCDVFS mountedVFS;

        /// <summary>
        /// The name of the currently mounted VFS.
        /// </summary>
        string vfsName;

        /// <summary>
        /// The current directory we are in.
        /// </summary>
        string currentDir;

        /// <summary>
        /// The pathes of files and directories that have been chosen to be copied or cut
        /// </summary>
        string[] copyCutPathes;

        /// <summary>
        /// Boolean that indicates what the copy/cut mode is.
        /// Cut if true, just copy otherwise.
        /// </summary>
        bool cutNotCopy;


        public Form1()
        {
            InitializeComponent();
        }

        #region Button Clicks

        private void CreateButton_Click(object sender, EventArgs e)
        {
            using (var form = new CreateForm())
            {
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    var file = form.file;
                    var size = form.size;
                    try
                    {
                        JCDVFS.Create(file, size).Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Open VFS";
            openFileDialog.Filter = "VFS File|*.vfs|All Files|*.*";
            openFileDialog.DefaultExt = ".vfs";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = openFileDialog.FileName;
                try
                {
                    var jcdvfs = JCDVFS.Open(file);
                    if (jcdvfs != null)
                    {
                        mountedVFS = jcdvfs;
                        vfsName = (new FileInfo(file)).Name;
                        currentDir = jcdvfs.GetCurrentDirectory();

                        ChangeFormToMounted();
                        updateForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Delete VFS";
            openFileDialog.Filter = "VFS File|*.vfs|All Files|*.*";
            openFileDialog.DefaultExt = ".vfs";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = openFileDialog.FileName;
                try
                {
                    JCDVFS.Delete(file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                mountedVFS.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            mountedVFS = null;
            currentDir = null;
            vfsName = null;
            ChangeFormToNotMounted();
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            var path = parentDirOf(currentDir);
            if (path != currentDir)
                moveIntoDirectory(path);
        }

        #endregion

        #region ToolStrip Menu Click

        private void createDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                using (var form = new InputNameForm())
                {
                    form.title = "Create Directory";
                    form.text = "Enter the name of the new directory.";

                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var dirName = form.result;

                        mountedVFS.CreateDirectory(Helpers.PathCombine(currentDir, dirName), false);
                        updateForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void createFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                using (var form = new InputNameAndSizeForm())
                {
                    form.title = "Create File";
                    form.textName = "Name and extension of the new file.";
                    form.textSize = "Size of the new file.";

                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var fileName = form.nameResult;
                        var size = form.sizeResult;

                        mountedVFS.CreateFile(Helpers.PathCombine(currentDir, fileName), size, false);
                        updateForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                var item = directoryListView.FocusedItem;
                var oldName = item.Text;

                using (var form = new InputNameForm())
                {
                    form.title = "Rename";
                    form.text = "Enter the name of the new directory.";

                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var newName = form.result;
                        mountedVFS.RenameFile(Helpers.PathCombine(currentDir, oldName), newName);
                        updateForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = directoryListView.SelectedItems;
            string[] pathes = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

            copy(pathes);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = directoryListView.SelectedItems;
            string[] pathes = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

            cut(pathes);
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            paste();
        }

        private void importFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                var openFileDialog = new OpenFileDialog();

                openFileDialog.Title = "Import File";
                openFileDialog.Filter = "All Files|*.*";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var files = openFileDialog.FileNames;
                    foreach (var file in files)
                    {
                        var name = new FileInfo(file).Name;
                        mountedVFS.ImportFile(file, Helpers.PathCombine(currentDir, name));
                    }
                    updateForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void importFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                var folderBrowserDialog = new FolderBrowserDialog();

                folderBrowserDialog.Description = "Select the folder to import.";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var path = folderBrowserDialog.SelectedPath;
                    var name = new FileInfo(path).Name;

                    mountedVFS.ImportFile(path, Helpers.PathCombine(currentDir, name));
                    updateForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                var items = directoryListView.SelectedItems;
                if (items.Count == 0)
                    return;

                string[] pathes = new string[items.Count];
                for (int i = 0; i < items.Count; i++)
                    pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

                var folderBrowserDialog = new FolderBrowserDialog();

                folderBrowserDialog.Description = "Select the directory to export to";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var target = folderBrowserDialog.SelectedPath;

                    int count = 0;
                    foreach(string path in pathes)
                    {
                        try
                        {
                            var file = Helpers.PathCombine(currentDir, path);
                            mountedVFS.ExportFile(file, target);
                        }
                        catch(Exception)
                        {
                            //log or just ignore
                        }
                        count++;
                    }
                    
                    MessageBox.Show(String.Format("Exported \"{0}\" files/directories successfully to \"{1}\".", count, target), "Export done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var items = directoryListView.SelectedItems;
            string[] pathes = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

            delete(pathes);
        }

        #endregion

        #region Key and Mouse Clicks

        private void directoryListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (mountedVFS != null)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    var item = directoryListView.FocusedItem;
                    if (item != null && item.SubItems[1].Text == "Directory")
                    {
                        var newDir = (Helpers.PathCombine(currentDir, item.Text) + '/');
                        moveIntoDirectory(newDir);
                    }
                }
                else if (e.KeyCode == Keys.Back)
                {
                    var path = parentDirOf(currentDir);
                    if (path != currentDir)
                        moveIntoDirectory(path);
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    var items = directoryListView.SelectedItems;
                    string[] pathes = new string[items.Count];
                    for (int i = 0; i < items.Count; i++)
                        pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

                    delete(pathes);
                }
                else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
                {
                    var items = directoryListView.SelectedItems;
                    string[] pathes = new string[items.Count];
                    for (int i = 0; i < items.Count; i++)
                        pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

                    copy(pathes);
                }
                else if (e.KeyCode == Keys.X && e.Modifiers == Keys.Control)
                {
                    var items = directoryListView.SelectedItems;
                    string[] pathes = new string[items.Count];
                    for (int i = 0; i < items.Count; i++)
                        pathes[i] = Helpers.PathCombine(currentDir, items[i].Text);

                    cut(pathes);
                }
                else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
                {
                    paste();
                }
                else if (e.KeyCode == Keys.A && e.Modifiers == Keys.Control)
                {
                    foreach (ListViewItem item in directoryListView.Items)
                        item.Selected = true;
                }
            }
        }

        private void directoryListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (directoryListView.FocusedItem.Bounds.Contains(e.Location))
            {
                var item = directoryListView.FocusedItem;
                if (item != null && item.SubItems[1].Text == "Directory")
                {
                    var newDir = (Helpers.PathCombine(currentDir, item.Text) + '/');
                    moveIntoDirectory(newDir);
                }
            }
        }

        private void pathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var path = pathTextBox.Text.Trim();
                moveIntoDirectory(path);
            }
            else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
            {
                //TODO implement
            }
            else if (e.KeyCode == Keys.X && e.Modifiers == Keys.Control)
            {
                //TODO implement
            }
            else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                //TODO implement
            }
        }

        private void backButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var path = parentDirOf(currentDir);
                if (path != currentDir)
                    moveIntoDirectory(path);
            }
        }

        #endregion

        #region Form Methods

        private void updateForm()
        {
            updateVFSLabel();
            populateListView();
        }

        private void updateVFSLabel()
        {
            try
            {
                vfsLabel.Text = String.Format("Mounted VFS: {0} (Space Free: {1}, Occupied: {2})", vfsName, mountedVFS.FreeSpace().ToString(), mountedVFS.OccupiedSpace().ToString());
            }
            catch (Exception)
            {
                vfsLabel.Text = "VFS mounted..";
            }
        }

        private void ChangeFormToMounted()
        {
            closeButton.Enabled = true;
            searchTextBox.Enabled = true;
            searchTypeComboBox.Enabled = true;
            vfsLabel.Enabled = true;

            directoryListView.Enabled = true;
            backButton.Enabled = true;
            pathTextBox.Enabled = true;
            pathTextBox.Text = currentDir;

            createButton.Enabled = false;
            deleteButton.Enabled = false;
            openButton.Enabled = false;
        }

        private void ChangeFormToNotMounted()
        {
            createButton.Enabled = true;
            deleteButton.Enabled = true;
            openButton.Enabled = true;

            closeButton.Enabled = false;
            searchTextBox.Enabled = false;
            searchTypeComboBox.Enabled = false;
            directoryListView.Enabled = false;
            pathTextBox.Enabled = false;
            pathTextBox.Text = "";
            backButton.Enabled = false;
            vfsLabel.Enabled = false;

            foreach (ListViewItem item in directoryListView.Items)
                item.Remove();

            vfsLabel.Text = "No VFS mounted..";
        }

        private void populateListView()
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                directoryListView.Items.Clear();
                ListViewItem item = null;
                ListViewItem.ListViewSubItem[] subItems;
                var dirList = mountedVFS.ListDirectory(currentDir);

                foreach (JCDDirEntry dirEntry in dirList)
                {
                    if (dirEntry.IsFolder)
                    {
                        item = new ListViewItem(dirEntry.Name, 0);
                        subItems = new ListViewItem.ListViewSubItem[] { new ListViewItem.ListViewSubItem(item, "Directory") };
                        //new ListViewItem.ListViewSubItem(item, dirEntry.Size.ToString())};

                        item.SubItems.AddRange(subItems);
                        directoryListView.Items.Add(item);
                    }
                    else
                    {
                        item = new ListViewItem(dirEntry.Name, 1);
                        subItems = new ListViewItem.ListViewSubItem[] 
                        {new ListViewItem.ListViewSubItem(item, "File"), 
                         new ListViewItem.ListViewSubItem(item, dirEntry.Size.ToString())};

                        item.SubItems.AddRange(subItems);
                        directoryListView.Items.Add(item);
                    }
                }

                directoryListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Methods that carry out VFS operations

        private void copy(string[] pathes)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                cutNotCopy = false;
                copyCutPathes = new string[pathes.Length];
                Array.Copy(pathes, copyCutPathes, pathes.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cut(string[] pathes)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                cutNotCopy = true;
                copyCutPathes = new string[pathes.Length];
                Array.Copy(pathes, copyCutPathes, pathes.Length);

                //TODO show item with greyed out icon
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void paste()
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                int count = 0;
                foreach (string path in copyCutPathes)
                {
                    try
                    {
                        var name = new FileInfo(path).Name;
                        mountedVFS.CopyFile(path, Helpers.PathCombine(currentDir, name));
                        if (cutNotCopy)
                            mountedVFS.DeleteFile(path, true);
                        count++;
                    }
                    catch (Exception)
                    {
                        //Log or just ignore
                    }
                }
                updateForm();
                if (count == 0)
                MessageBox.Show(String.Format("There was nothing to paste to \"{1}\"", count, currentDir), "Paste done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void delete(string[] pathes)
        {
            try
            {
                if (mountedVFS == null)
                    throw new Exception("No VFS mounted!");

                foreach (string path in pathes)
                    mountedVFS.DeleteFile(path, true);

                updateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void moveIntoDirectory(string dir)
        {
            try
            {
                mountedVFS.SetCurrentDirectory(dir);
                currentDir = dir;
                updateForm();
                pathTextBox.Text = dir;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Other, especially logical, Methods

        private string parentDirOf(string path)
        {
            string result;

            if (Helpers.TrimLastSlash(path) == "")
                return path;

            result = Helpers.PathGetDirectoryName(Helpers.TrimLastSlash(path));

            return result;
        }

        #endregion

    }
}
