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
using vfs.clients.desktop.exceptions;

namespace vfs.clients.desktop
{
    public partial class CreateForm : Form
    {
        private string file;
        private ulong size;

        public Tuple<string, ulong> GetFileDetails
        {
            get
            {
                return new Tuple<string, ulong>(file, size);
            }
        }

        public CreateForm()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            ok();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            close();
        }

        private void fileDirTextBox_Click(object sender, EventArgs e)
        {
            showSaveFileDialog();
        }

        private void fileDirTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                close();
                e.Handled = true;
            }
            else
            {
                showSaveFileDialog();
                e.Handled = true;
            }

        }

        private void sizeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ok();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                close();
                e.Handled = true;
            }
        }

        private void okButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ok();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                close();
                e.Handled = true;
            }
        }

        private void cancelButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                close();
                e.Handled = true;
            }
        }

        private void ok()
        {
            try
            {
                size = Convert.ToUInt64(sizeTextBox.Text);
                var info = new FileInfo(file);
                if (!info.Directory.Exists)
                    throw new InvalidPathException("Invalid Path");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void close()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void showSaveFileDialog()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Create VFS";
            saveFileDialog.Filter = "VFS File|*.vfs|All Files|*.*";
            saveFileDialog.DefaultExt = ".vfs";
            saveFileDialog.AddExtension = true;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.CheckFileExists = false;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                file = saveFileDialog.FileName;
                fileDirTextBox.Text = file;
            }
        }
    }
}
