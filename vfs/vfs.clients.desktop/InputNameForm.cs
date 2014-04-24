using System;
using System.Windows.Forms;

namespace vfs.clients.desktop
{
    public partial class InputNameForm : Form
    {
        public string Title { private get; set; }
        public string Description { private get; set; }

        public string Result { get; private set; }

        public InputNameForm()
        {
            InitializeComponent();
            if (Title != null)
                this.Text = Title;

            if (Description != null)
                this.textLabel.Text = Description;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            ok();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            close();
        }

        private void inputTextBox_KeyDown(object sender, KeyEventArgs e)
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
            this.Result = inputTextBox.Text;
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
