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
using System.Xml;
using System.Xml.Linq;

namespace vfs.clients.desktop
{

    public partial class SynchroManagerForm : Form
    {

        public VFSSession session { get; set; }

        private const string PATH = @"vfs.xml";

        private List<VFSEntry> vfsList = new List<VFSEntry>();

        public SynchroManagerForm()
        {
            InitializeComponent();
        }

        private void SynchroManagerForm_Shown(object sender, EventArgs e)
        {

        }

        private void SynchroManagerForm_Load(object sender, EventArgs e)
        {
            if (session == null)
                connectButton.Enabled = false;
            else if (!session.IsLoggedIn)
                connectButton.Text = "Connect";
            else
            {
                connectButton.Text = "Disconnect";

                if (!File.Exists(PATH))
                    createNewFile();
                else
                {
                    loadVFS();
                    retrieveFromServer();
                }

                populateListView();
            }
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (session.IsLoggedIn)
            {
                storeVFS();
                session.LogOut();
                vfsListView.Enabled = false;
                connectButton.Text = "Connect";
            }
            else
            {
                if (makeLogin())
                {
                    vfsListView.Enabled = true;
                    connectButton.Text = "Disconnect";
                    retrieveFromServer();
                }
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (session != null && session.IsLoggedIn)
                storeVFS();

            this.Close();
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Add VFS";
            openFileDialog.Filter = "VFS File|*.vfs|All Files|*.*";
            openFileDialog.DefaultExt = ".vfs";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var files = openFileDialog.FileNames;
                foreach (var file in files)
                {
                    var vfs = new VFSEntry();
                    vfs.Path = file;
                    vfs.Name = Path.GetFileName(file);
                    vfs.ID = (file + DateTime.Now.Ticks).GetHashCode();

                    vfsList.Add(vfs);
                }
                populateListView();
            }
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Download VFS";
            saveFileDialog.Filter = "VFS File|*.vfs|All Files|*.*";
            saveFileDialog.DefaultExt = ".vfs";
            saveFileDialog.AddExtension = true;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.CheckFileExists = false;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = saveFileDialog.FileName;

                //TODO download the vfs from the server
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = vfsListView.FocusedItem;
            if (item != null)
            {
                makeDelete(item);
            }
        }

        private void vfsListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = vfsListView.GetItemAt(e.X, e.Y);
            if (item != null && item.ImageIndex == 1)
            {
                searchForPath(item);
            }
        }

        private void vfsListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var item = vfsListView.FocusedItem;
                if (item != null && item.ImageIndex == 1)
                {
                    searchForPath(item);
                }
            }
            else if (e.KeyCode == Keys.Delete)
            {
                var item = vfsListView.FocusedItem;
                if (item != null)
                {
                    makeDelete(item);
                }
            }
        }


        private void vfsListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null)
                return;

            var item = vfsListView.Items[e.Item];
            var index = searchVFSListIndexOfItem(item);
            vfsList[index].Name = e.Label;
        }

        private void populateListView()
        {
            try
            {
                vfsListView.Items.Clear();
                ListViewItem item = null;
                ListViewItem.ListViewSubItem[] subItems;

                foreach (var vfsEntry in vfsList)
                {
                    if (File.Exists(vfsEntry.Path))
                        item = new ListViewItem(vfsEntry.Name, 0);
                    else
                        item = new ListViewItem(vfsEntry.Name, 1);
                    item.Tag = vfsEntry.ID;
                    subItems = new ListViewItem.ListViewSubItem[] { new ListViewItem.ListViewSubItem(item, vfsEntry.Path) };

                    item.SubItems.AddRange(subItems);
                    vfsListView.Items.Add(item);
                }
                vfsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void searchForPath(ListViewItem item)
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Title = "Search VFS";
            openFileDialog.Filter = "VFS File|*.vfs|All Files|*.*";
            openFileDialog.DefaultExt = ".vfs";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var file = openFileDialog.FileName;

                var index = searchVFSListIndexOfItem(item);
                if (index > 0)
                {
                    vfsList[index].Path = file;

                    populateListView();
                }
            }
        }

        private int searchVFSListIndexOfItem(ListViewItem item)
        {
            for (int i = 0; i < vfsList.Count; i++)
            {
                var vfsElem = vfsList[i];
                if (vfsElem.ID == Convert.ToInt32(item.Tag))
                    return i;
            }
            return -1;
        }

        private void makeDelete(ListViewItem item)
        {
            var index = searchVFSListIndexOfItem(item);
            if (index > 0)
            {
                vfsList.RemoveAt(index);
                populateListView();
            }
        }


        private bool makeLogin()
        {
            var form = new LoginForm();
            form.session = session;
            form.ShowDialog(this);

            return form.DialogResult == DialogResult.OK;
        }

        private void retrieveFromServer()
        {
            //TODO implement
        }

        #region XML methods

        private void createNewFile()
        {
            try
            {
                using (XmlWriter writer = XmlWriter.Create(PATH))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Users");

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadVFS()
        {
            try
            {
                XDocument xmlDoc = XDocument.Load(PATH);

                vfsList = (from vfs in xmlDoc.Descendants("Users").Descendants("User").Descendants("VFS")
                           where vfs.Parent.Attribute("UserName").Value == session.UserName
                           select new VFSEntry
                           {
                               Name = vfs.Element("Name").Value,
                               Path = vfs.Element("Path").Value,
                               ID = Convert.ToInt32(vfs.Element("ID").Value)
                           }).ToList<VFSEntry>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void storeVFS()
        {
            try
            {
                XDocument xmlDoc = XDocument.Load(PATH);

                var users = from element in xmlDoc.Descendants("Users").Descendants("User")
                            where element.Attribute("UserName").Value == session.UserName
                            select element;

                XElement user = null;
                if (users.Count() > 0)
                {
                    user = users.ElementAt(0);
                    user.Descendants("VFS").Remove();
                }
                else
                {
                    user = new XElement("User", new XAttribute("UserName", session.UserName));
                    xmlDoc.Element("Users").Add(user);
                }

                foreach (var vfs in vfsList)
                {
                    XElement elem = new XElement("VFS",
                        new XElement("Name", vfs.Name),
                        new XElement("Path", vfs.Path),
                        new XElement("ID", vfs.ID)
                    );
                    user.Add(elem);
                }

                xmlDoc.Save(PATH);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

    }

    class VFSEntry
    {
        public string Name;
        public string Path;
        public int ID;
    }

}
