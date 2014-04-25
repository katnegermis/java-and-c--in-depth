using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace vfs.clients.desktop
{

    public partial class MainForm : Form
    {
        /// <summary>
        /// The current VFS Session with the mounted VFS.
        /// </summary>
        VFSSession session;

        /// <summary>
        /// The name of the currently mounted VFS.
        /// </summary>
        string vfsName;

        /// <summary>
        /// The Sorter class for the ListView Columns.
        /// </summary>
        private ListViewColumnSorter lvwColumnSorter;

        /// <summary>
        /// Bool indicating whether a drag and drop has left the ListView or not.
        /// </summary>
        private bool draggedOutside = false;

        public MainForm()
        {
            InitializeComponent();

            lvwColumnSorter = new ListViewColumnSorter();
            this.directoryListView.ListViewItemSorter = lvwColumnSorter;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (session != null)
                makeVFSClose();
        }



        #region Button Clicks

        private void CreateButton_Click(object sender, EventArgs e)
        {
            makeVFSCreate();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            makeVFSOpen();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            makeVFSDelete();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            makeVFSClose();
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            makeMoveBack();
        }

        private void searchOptionButton_Click(object sender, EventArgs e)
        {
            Button btnSender = (Button)sender;
            Point ptLowerLeft = new Point(0, btnSender.Height);
            ptLowerLeft = btnSender.PointToScreen(ptLowerLeft);
            searchMenuStrip.Show(ptLowerLeft);
        }

        #endregion

        #region ListView ToolStripMenu Click

        private void createDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeCreateDir();
        }

        private void createFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeCreateFile();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeRename();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeCopy();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeCut();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makePaste();
        }

        private void importFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeFileImport();
        }

        private void importFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeFolderImport();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeExport();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeDelete();
        }

        private void directoryListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.directoryListView.Sort();
        }

        #endregion

        #region Search ToolStripMenu Click and Opening

        private void searchMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            setChecksSearchMenuStrip();
        }

        private void currentFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SearchLocation = SearchLocation.Folder;
            setChecksSearchLocation(SearchLocation.Folder);
            searchTextBox.Focus();
        }

        private void currentSubfoldersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SearchLocation = SearchLocation.SubFolder;
            setChecksSearchLocation(SearchLocation.SubFolder);
            searchTextBox.Focus();
        }

        private void everywhereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SearchLocation = SearchLocation.Everywhere;
            setChecksSearchLocation(SearchLocation.Everywhere);
            searchTextBox.Focus();
        }

        private void sensitiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SearchCaseSensitive = true;
            setChecksSearchCaseSensitivity(true);
            searchTextBox.Focus();
        }

        private void insensitiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            session.SearchCaseSensitive = false;
            setChecksSearchCaseSensitivity(false);
            searchTextBox.Focus();
        }

        #endregion

        #region Drag and Drop

        private void directoryListView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            var count = this.directoryListView.SelectedIndices.Count;
            if (count > 0)
            {
                var selectedNames = new string[count];
                for (int i = 0; i < count; i++)
                {
                    selectedNames[i] = directoryListView.SelectedItems[i].Text;
                }

                if (e.Button == MouseButtons.Left)
                {
                    draggedOutside = false;
                    var effect = DoDragDrop(selectedNames, DragDropEffects.Move);
                    if (effect == DragDropEffects.None && draggedOutside)
                        makeExport();
                }
                else if (e.Button == MouseButtons.Right)
                {
                    draggedOutside = false;
                    var effect = DoDragDrop(selectedNames, DragDropEffects.Copy);
                    if (effect == DragDropEffects.None && draggedOutside)
                        makeExport();
                }

            }

        }

        private void directoryListView_DragEnter(object sender, DragEventArgs e)
        {
            draggedOutside = false;
            if (e.Data.GetDataPresent(typeof(string[])))
                e.Effect = e.AllowedEffect;
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = e.AllowedEffect;
            else
                e.Effect = DragDropEffects.None;
        }

        private void directoryListView_DragLeave(object sender, EventArgs e)
        {
            draggedOutside = true;
        }


        private void directoryListView_DragOver(object sender, DragEventArgs e)
        {
            var pos = directoryListView.PointToClient(new Point(e.X, e.Y));
            var item = directoryListView.GetItemAt(pos.X, pos.Y);
            if (item != null && item.GetType() == typeof(ListViewItem) && item.SubItems[1].Text == "Directory")
            {
                item.Focused = true;
                directoryListView.Focus();
            }

        }

        private void directoryListView_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var pos = directoryListView.PointToClient(new Point(e.X, e.Y));
                var hit = directoryListView.HitTest(pos);

                if (e.Data.GetDataPresent(typeof(string[])))
                {
                    if (hit.Item != null && hit.Item.Text != null && hit.Item.SubItems[1].Text == "Directory")
                    {
                        var droppedNames = e.Data.GetData(typeof(string[])) as string[];

                        if (e.Effect == DragDropEffects.Copy)
                            session.DragDrop(droppedNames, hit.Item.Text, false);
                        else if (e.Effect == DragDropEffects.Move)
                        {
                            var count = session.DragDrop(droppedNames, hit.Item.Text, true);
                            if (count > 0)
                                updateForm();
                        }
                    }
                }
                else if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string targetDir = session.CurrentDir;
                    if (hit.Item != null && hit.Item.Text != null && hit.Item.SubItems[1].Text == "Directory")
                        targetDir += hit.Item.Text + "/";

                    Array arr = (Array)e.Data.GetData(DataFormats.FileDrop);
                    if (arr != null && arr.Length > 0)
                    {
                        var files = new string[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                            files[i] = arr.GetValue(i).ToString();

                        makeImport(files, targetDir);

                        this.Activate();
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Key and Mouse Clicks

        private void directoryListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var item = directoryListView.FocusedItem;
                if (item != null && item.SubItems[1].Text == "Directory")
                {
                    makeMoveInto(item.Text, false);
                }
            }
            else if (e.KeyCode == Keys.Back)
            {
                makeMoveBack();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                makeDelete();
            }
            else if (e.KeyCode == Keys.C && e.Modifiers == Keys.Control)
            {
                makeCopy();
            }
            else if (e.KeyCode == Keys.X && e.Modifiers == Keys.Control)
            {
                makeCut();
            }
            else if (e.KeyCode == Keys.V && e.Modifiers == Keys.Control)
            {
                makePaste();
            }
            else if (e.KeyCode == Keys.A && e.Modifiers == Keys.Control)
            {
                foreach (ListViewItem item in directoryListView.Items)
                    item.Selected = true;
            }
        }

        private void directoryListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (directoryListView.FocusedItem.Bounds.Contains(e.Location))
            {
                var item = directoryListView.FocusedItem;
                if (item != null && item.SubItems[1].Text == "Directory")
                {
                    makeMoveInto(item.Text, false);
                }
            }
        }

        private void pathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var path = pathTextBox.Text.Trim();
                makeMoveInto(path, true);
            }
        }

        private void backButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                makeMoveBack();
            }
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var searchString = searchTextBox.Text.Trim();
                if (searchString != "")
                    makeSearch(searchString);
            }
        }

        #endregion

        #region Form Methods

        /// <summary>
        /// Method to be called for calling the necessary Form update methods.
        /// </summary>
        private void updateForm()
        {
            updateVFSLabel();
            populateListView();
        }

        /// <summary>
        /// Updates the VFS label with the name and space informations of the mounted VFS.
        /// </summary>
        private void updateVFSLabel()
        {
            try
            {
                vfsLabel.Text = String.Format("Mounted VFS: {0} (Space Free: {1}, Occupied: {2})", vfsName, session.FreeSpace.ToString(), session.OccupiedSpace.ToString());
            }
            catch (Exception)
            {
                vfsLabel.Text = "VFS mounted..";
            }
        }

        /// <summary>
        /// Method to be called when a VFS has been mounted to enable and disable the according elements.
        /// </summary>
        private void ChangeFormToMounted()
        {
            closeButton.Enabled = true;
            searchTextBox.Enabled = true;
            searchOptionButton.Enabled = true;
            vfsLabel.Enabled = true;

            directoryListView.Enabled = true;
            backButton.Enabled = true;
            pathTextBox.Enabled = true;
            pathTextBox.Text = session.CurrentDir;

            createButton.Enabled = false;
            deleteButton.Enabled = false;
            openButton.Enabled = false;
        }

        /// <summary>
        /// Method to be called when a VFS has been unmounted to enable and disable the according Form elements.
        /// </summary>
        private void ChangeFormToNotMounted()
        {
            createButton.Enabled = true;
            deleteButton.Enabled = true;
            openButton.Enabled = true;

            closeButton.Enabled = false;
            searchTextBox.Enabled = false;
            searchOptionButton.Enabled = false;
            directoryListView.Enabled = false;
            pathTextBox.Enabled = false;
            pathTextBox.Text = "";
            backButton.Enabled = false;
            vfsLabel.Enabled = false;

            foreach (ListViewItem item in directoryListView.Items)
                item.Remove();

            vfsLabel.Text = "No VFS mounted..";
        }

        /// <summary>
        /// Retrieves the elements in the current directory of the VFS in the active Session and puts them into the ListView.
        /// </summary>
        private void populateListView()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                directoryListView.Items.Clear();
                ListViewItem item = null;
                ListViewItem.ListViewSubItem[] subItems;
                var dirList = session.ListCurrentDirectory();

                foreach (var dirEntry in dirList)
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

        /// <summary>
        /// Returns the names of the selected ListViewItems.
        /// </summary>
        /// <returns>Array with the names.</returns>
        private string[] getSelectedListViewItemTexts()
        {
            var items = directoryListView.SelectedItems;
            string[] names = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                names[i] = items[i].Text;

            return names;
        }

        private void setChecksSearchMenuStrip()
        {
            if (session == null)
                return;

            setChecksSearchLocation(session.SearchLocation);
            setChecksSearchCaseSensitivity(session.SearchCaseSensitive);
        }


        private void setChecksSearchLocation(SearchLocation searchLocation)
        {
            switch (searchLocation)
            {
                case SearchLocation.Folder:
                    currentFolderToolStripMenuItem.Checked = true;
                    currentSubfoldersToolStripMenuItem.Checked = false;
                    everywhereToolStripMenuItem.Checked = false;
                    break;
                case SearchLocation.SubFolder:
                    currentFolderToolStripMenuItem.Checked = false;
                    currentSubfoldersToolStripMenuItem.Checked = true;
                    everywhereToolStripMenuItem.Checked = false;
                    break;
                case SearchLocation.Everywhere:
                    currentFolderToolStripMenuItem.Checked = false;
                    currentSubfoldersToolStripMenuItem.Checked = false;
                    everywhereToolStripMenuItem.Checked = true;
                    break;
                default:
                    break;
            }
        }


        private void setChecksSearchCaseSensitivity(bool searchCaseSensitive)
        {
            if (searchCaseSensitive)
            {
                sensitiveToolStripMenuItem.Checked = true;
                insensitiveToolStripMenuItem.Checked = false;
            }
            else
            {
                sensitiveToolStripMenuItem.Checked = false;
                insensitiveToolStripMenuItem.Checked = true;
            }
        }

        #endregion

        #region General Methods, especially on the VFS Session

        /// <summary>
        /// Shows the CreateForm as Dialog to let the user enter the necessary values.
        /// Then calls the method to make the actual VFS creation.
        /// </summary>
        private void makeVFSCreate()
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
                        VFSSession.CreateVFS(file, size);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Shows a OpenFileDialog to let the user choose the file with the VFS to open.
        /// Then calls the method to open and when that's successful the VFSSession is created.
        /// </summary>
        private void makeVFSOpen()
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
                    session = VFSSession.OpenVFS(file);
                    if (session != null)
                    {
                        vfsName = (new FileInfo(file)).Name;

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

        /// <summary>
        /// Shows a OpenFileDialog to let the user choose the VFS file to delete.
        /// Then makes the call to the delete method.
        /// </summary>
        private void makeVFSDelete()
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
                    VFSSession.DeleteVFS(file);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Closes the session, which closes the open VFS file.
        /// </summary>
        private void makeVFSClose()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                session.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            session = null;
            vfsName = null;

            ChangeFormToNotMounted();
        }

        /// <summary>
        /// Moves into the given directory in the active Session's VFS.
        /// If completePath is set to true, the directory path is used as given.
        /// If set to false, the directory is assumed to be relative (in the current directory).
        /// The Form is updated if the current directory has been changes.
        /// </summary>
        /// <param name="directory">Directory to move into.</param>
        /// <param name="completePath">If set to true, the path is assumed to be absolute, otherwise it is assumed to be relative.</param>
        private void makeMoveInto(string directory, bool completePath)
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                if (session.MoveInto(directory, completePath))
                {
                    updateForm();
                    pathTextBox.Text = session.CurrentDir;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Tries to move one directory up from the current directory in the VFS mounted in the active Session.
        /// The form is updated if the current directory has been changed.
        /// </summary>
        private void makeMoveBack()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                if (session.MoveBack())
                {
                    updateForm();
                    pathTextBox.Text = session.CurrentDir;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows the InputNameForm to retrieve the new directory name from the user.
        /// Then the Session is called to create the directory in the current directory.
        /// The Form is updated afterwards in case no Exception is thrown.
        /// </summary>
        private void makeCreateDir()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                using (var form = new InputNameForm())
                {
                    form.Title = "Create Directory";
                    form.Description = "Enter the name of the new directory.";

                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var dirName = form.Result;

                        session.CreateDir(dirName);
                        updateForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows the InputNameAnsSizeForm to retrieve the new file name and size from the user.
        /// Then the Session is called to create the file in the current directory.
        /// The Form is updated afterwards in case no Exception is thrown. 
        /// </summary>
        private void makeCreateFile()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                using (var form = new InputNameAndSizeForm())
                {
                    form.Title = "Create File";
                    form.TextName = "Name and extension of the new file.";
                    form.TextSize = "Size of the new file.";

                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var fileName = form.NameResult;
                        var size = form.SizeResult;

                        session.CreateFile(fileName, size);
                        updateForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows the InputNameForm to retrieve the new name from the user.
        /// Then the Session is called to make the rename of the file or directory in the current directory.
        /// The Form is updated afterwards in case no Exception is thrown.
        /// </summary>
        private void makeRename()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var item = directoryListView.FocusedItem;
                var oldName = item.Text;

                using (var form = new InputNameForm())
                {
                    form.Title = "Rename";
                    form.Description = "Enter the name of the new directory.";

                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        var newName = form.Result;

                        session.Rename(oldName, newName);
                        updateForm();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Retrieves the names of the selected Items in the ListView, which are about to be copied.
        /// These are then given to the Session to be put into the clipboard.
        /// </summary>
        private void makeCopy()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var names = getSelectedListViewItemTexts();
                session.Copy(names);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Retrieves the names of the selected Items in the ListView, which are about to be cut.
        /// These are then given to the Session to be put into the clipboard. 
        /// </summary>
        private void makeCut()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var names = getSelectedListViewItemTexts();
                session.Cut(names);

                //TODO show item with greyed out icon
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Calls the Session to paste the entries in the clipboard into the current directory.
        /// If the number of copied entries is >0 the Form is updated, a MessageBox is shown otherwise.
        /// </summary>
        private void makePaste()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                session.Paste();
                updateForm();
                /*if (session.Paste() > 0)
                    updateForm();
                else
                    MessageBox.Show(String.Format("There was nothing to paste to \"{0}\"", session.CurrentDir), "Paste done", MessageBoxButtons.OK, MessageBoxIcon.Information);*/
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Retrieves the selected ListViewItems in the current directory and calls the Session to delete them.
        /// Updates the form if more then 0 elements have been deleted.
        /// </summary>
        private void makeDelete()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var names = getSelectedListViewItemTexts();

                if (session.Delete(names) > 0)
                    updateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows an OpenFileFialog with multiselect so that the user can select files and directories to import.
        /// The Session object is then called to import the given files/dirs into the current directory of the open VFS.
        /// The Form is updated if more than 0 files/dirs have been imported. 
        /// </summary>
        private void makeFileImport()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var openFileDialog = new OpenFileDialog();

                openFileDialog.Title = "Import File";
                openFileDialog.Filter = "All Files|*.*";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var files = openFileDialog.FileNames;
                    int count = session.Import(files, session.CurrentDir);
                    updateForm();

                    if (count > 0)
                        MessageBox.Show(String.Format("Imported \"{0}\" files/directories.", count), "Import done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows a FolderBrowserDialog to let the user select a folder to import.
        /// The session object is then called to import the folder into the current directory of the open VFS.
        /// The Form is updated if the folder has been imported.
        /// </summary>
        private void makeFolderImport()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var folderBrowserDialog = new FolderBrowserDialog();

                folderBrowserDialog.Description = "Select the folder to import.";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    //if (session.Import(new string[] { folderBrowserDialog.SelectedPath }, session.CurrentDir) > 0)
                    int count = session.Import(new string[] { folderBrowserDialog.SelectedPath }, session.CurrentDir);
                    updateForm();

                    if (count > 0)
                        MessageBox.Show(String.Format("Imported \"{0}\" files/directories.", count), "Import done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Imports the given files to the target directory by calling the Session object.
        /// </summary>
        /// <param name="files">Files/dirs to import.</param>
        /// <param name="targetDir">Dir to import to.</param>
        private void makeImport(string[] files, string targetDir)
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                int count = session.Import(files, targetDir);
                updateForm();

                if (count > 0)
                    MessageBox.Show(String.Format("Imported \"{0}\" files/directories.", count), "Import done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows a FolderBrowserDialog to let the user select the destination folder for the export.
        /// The selected ListViewItems in the current directory are then exported there.
        /// A MessageBox is then shown with the number of exported elements.
        /// </summary>
        private void makeExport()
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                var names = getSelectedListViewItemTexts();
                if (names.Length == 0)
                    return;

                var folderBrowserDialog = new FolderBrowserDialog();

                folderBrowserDialog.Description = "Select the directory to export to";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var target = folderBrowserDialog.SelectedPath;

                    int count = session.Export(names, target);
                    MessageBox.Show(String.Format("Exported \"{0}\" files/directories successfully to \"{1}\".", count, target), "Export done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Uses the Session object to search for the given string with the set options.
        /// </summary>
        /// <param name="searchString">String to search for.</param>
        private void makeSearch(string searchString)
        {
            try
            {
                if (session == null)
                    throw new Exception("No VFS mounted!");

                string[] found = session.Search(searchString);
                if (found.Length > 0)
                {
                    var form = new SearchResultForm();
                    form.SearchString = searchString;
                    form.SearchResultPaths = found;

                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        if (form.SelectedPath != "")
                            session.MoveInto(form.SelectedPath, true);
                    }
                }
                else
                    MessageBox.Show(String.Format("No file with name \"{0}\" found.", searchString), "File not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

    }
}
