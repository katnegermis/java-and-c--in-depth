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
    public partial class LoginForm : Form
    {

        public VFSSession session { get; set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            string userName = nameTextBox.Text;
            string password = textBox1.Text;

            if (makeLogin(userName, password))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void registerButton_Click(object sender, EventArgs e)
        {
            string userName = nameTextBox.Text;
            string password = textBox1.Text;

            //TODO make register
            session.Register(userName, password);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool makeLogin(string userName, string password)
        {
            if (session.LogIn(userName, password))
            {
                return true;
            }
            else
            {
                var result = MessageBox.Show(this, "Could not login successfully.", "Login", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Retry)
                    makeLogin(userName, password);
            }
            return false;
        }


    }
}
