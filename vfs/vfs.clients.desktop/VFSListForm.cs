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
using vfs.synchronizer.client;

namespace vfs.clients.desktop
{
    public partial class VFSListForm : Form
    {

        private string username;
        private string pw;

        private List<Tuple<long, string>> list;

        public VFSListForm()
        {
            InitializeComponent();
        }

        private void VFSListForm_Load(object sender, EventArgs e)
        {
            populateListView();
        }

        public void prepareToBeShown(string loggedInUsername, string loggedInPw, List<Tuple<long, string>> vfsList)
        {
            this.username = loggedInUsername;
            this.pw = loggedInPw;
            this.list = vfsList;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void serverVFSListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (serverVFSListView.FocusedItem.Bounds.Contains(e.Location))
            {
                var item = serverVFSListView.FocusedItem;
                makeDownload((long)item.Tag);
            }
        }

        private void serverVFSListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && serverVFSListView.SelectedItems.Count > 0)
            {
                var item = serverVFSListView.SelectedItems[0];
                if (item != null)
                    makeDownload((long)item.Tag);
            }
        }

        private void populateListView()
        {
            try
            {
                serverVFSListView.Items.Clear();
                ListViewItem item = null;

                foreach (var vfsEntry in list)
                {
                    item = new ListViewItem(vfsEntry.Item2, 0);
                    item.Tag = vfsEntry.Item1;

                    serverVFSListView.Items.Add(item);
                }
                serverVFSListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void makeDownload(long vfsId)
        {
            try
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
                    if (File.Exists(file))
                        throw new Exception("File already exising!");

                    var reply = JCDVFSSynchronizer.RetrieveVFS(username, pw, vfsId);
                    //long versionId = (long)reply.Data[0];
                    var data = (byte[])reply.Data[1];

                    using (var fileStream = new FileStream(file, FileMode.CreateNew))
                    using (var writer = new BinaryWriter(fileStream))
                        writer.Write(data);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }





    }
}
