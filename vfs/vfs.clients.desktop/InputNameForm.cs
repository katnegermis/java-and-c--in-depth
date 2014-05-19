using System;
using System.Windows.Forms;

namespace vfs.clients.desktop
{
    public partial class InputNameForm : Form
    {

        private string result;
        public string GetResult
        {
            get
            {
                return result;
            }
        }

        public InputNameForm()
        {
            InitializeComponent();
        }

        public void SetTitleAndDescription(string title, string description)
        {
            if (title != null)
                this.Text = title;

            if (description != null)
                this.textLabel.Text = description;
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
            this.result = inputTextBox.Text;
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
