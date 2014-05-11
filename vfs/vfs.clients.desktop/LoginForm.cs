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
        public LoginForm()
        {
            InitializeComponent();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            string userName = NameLabel.Text;
            string password = pwLabel.Text;

            //TODO make login

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void registerButton_Click(object sender, EventArgs e)
        {
            string userName = NameLabel.Text;
            string password = pwLabel.Text;

            //TODO make register

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
