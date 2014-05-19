using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using vfs.synchronizer.client;

namespace vfs.clients.desktop
{
    public partial class LoginForm : Form
    {

        private string userName;
        private string password;
        private List<Tuple<long, string>> listVFS;

        public Tuple<string, string> GetLoginCredentials
        {
            get
            {
                return new Tuple<string, string>(userName, password);
            }
        }

        public List<Tuple<long, string>> GetVFSList
        {
            get
            {
                return listVFS;
            }
        }

        public bool forListVFS { private get; set; }

        public VFSSession session { private get; set; }

        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            if (forListVFS)
            {
                registerButton.Visible = false;
                var pt = new Point(48, 3);
                loginButton.Location = pt;
            }
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            userName = nameTextBox.Text;
            password = textBox1.Text;

            if (forListVFS)
            {
                try
                {
                    listVFS = JCDVFSSynchronizer.ListVFSes(userName, password);
                    this.DialogResult = DialogResult.OK;
                }
                catch (vfs.exceptions.VFSSynchronizationServerException ex)
                {
                    MessageBox.Show(this, "Retrieving the List of VFS files failed!\n" + ex.Message, "Retrieve List", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.Cancel;
                }
                this.Close();
            }
            else
            {
                if (makeLogin(userName, password))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void registerButton_Click(object sender, EventArgs e)
        {
            try
            {
                string userName = nameTextBox.Text;
                string password = textBox1.Text;

                JCDVFSSynchronizer.Register(userName, password);

                if (makeLogin(userName, password))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch
            {
                MessageBox.Show(this, "Could not register successfully.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private bool makeLogin(string userName, string password)
        {
            try
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
            }
            catch (vfs.exceptions.VFSSynchronizationServerException e)
            {
                MessageBox.Show(this, "Could not login successfully.\n" + e.Message, "Login", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return false;
        }
    }
}
