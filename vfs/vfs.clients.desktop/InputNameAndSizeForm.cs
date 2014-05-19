using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace vfs.clients.desktop
{
    public partial class InputNameAndSizeForm : Form
    {
        public string NameResult { get; private set; }
        public ulong SizeResult { get; private set; }

        public InputNameAndSizeForm()
        {
            InitializeComponent();
        }

        public void SetSpecialText(string title, string nameText, string sizeText)
        {
            if (title != null)
                this.Text = title;

            if (nameText != null)
                this.nameLabel.Text = nameText;

            if (sizeText != null)
                this.sizeLabel.Text = sizeText;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            ok();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            close();
        }

        private void nameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                close();
        }

        private void sizeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ok();
            else if (e.KeyCode == Keys.Escape)
                close();
        }

        private void okButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ok();
            else if (e.KeyCode == Keys.Escape)
                close();
        }

        private void cancelButton_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                close();
        }

        private void ok()
        {
            try
            {
                this.NameResult = nameTextBox.Text;
                this.SizeResult = Convert.ToUInt64(sizeTextBox.Text);
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
    }
}
