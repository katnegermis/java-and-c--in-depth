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
    public partial class SearchResultForm : Form
    {

        private DirectoryEntry[] results = new DirectoryEntry[0];

        private DirectoryEntry selectedEntry;

        public SearchResultForm()
        {
            InitializeComponent();
        }

        public void SetSearchDetailsBeforeShow(DirectoryEntry[] searchResults, string searchText)
        {
            this.results = searchResults;
            searchStringLabel.Text = String.Format("Search results for: \"{0}\"", searchText);
        }

        public DirectoryEntry getSelectedEntry()
        {
            return selectedEntry;
        }

        private void SearchResultForm_Shown(object sender, EventArgs e)
        {
            populateListView();
        }

        private void populateListView()
        {
            searchListView.Items.Clear();
            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems;

            foreach (var dirEntry in results)
            {
                //item = new ListViewItem(path, 1);
                //searchListView.Items.Add(item);

                if (dirEntry.IsFolder)
                {
                    item = new ListViewItem(dirEntry.Name, 0);
                    subItems = new ListViewItem.ListViewSubItem[] 
                        { new ListViewItem.ListViewSubItem(item, dirEntry.Path),
                          new ListViewItem.ListViewSubItem(item, "Directory")};
                    //new ListViewItem.ListViewSubItem(item, dirEntry.Size.ToString())};

                    item.SubItems.AddRange(subItems);
                    item.Tag = dirEntry;
                    searchListView.Items.Add(item);
                }
                else
                {
                    item = new ListViewItem(dirEntry.Name, 1);
                    subItems = new ListViewItem.ListViewSubItem[] 
                        { new ListViewItem.ListViewSubItem(item, dirEntry.Path), 
                          new ListViewItem.ListViewSubItem(item, "File"),
                          new ListViewItem.ListViewSubItem(item, dirEntry.Size.ToString())};

                    item.SubItems.AddRange(subItems);
                    item.Tag = dirEntry;
                    searchListView.Items.Add(item);
                }

            }
            searchListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            ok();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            close();
        }

        private void searchListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ok();
        }

        private void searchListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                ok();
            else if (e.KeyCode == Keys.Escape)
                close();
        }

        private void ok()
        {
            if (searchListView.SelectedIndices.Count <= 0)
                close();
            else
            {
                selectedEntry = (DirectoryEntry)searchListView.SelectedItems[0].Tag;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void close()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

    }
}
