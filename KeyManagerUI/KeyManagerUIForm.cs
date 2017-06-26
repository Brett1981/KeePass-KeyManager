﻿using KeePassLib.Cryptography;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace KeyManagerUI
{
    public partial class KeyManagerUIForm : Form
    {
        private byte[] fileBytes;
        private Certmanager cert_mgmt = new Certmanager();
        public KeyManagerUIForm()
        {
            InitializeComponent();
        }

        // May some security risk - access the key 
        public byte[] priKey
        {
            get {
                if (fileBytes.Length > 0 || fileBytes != null)
                    return cert_mgmt.DecryptMsg(fileBytes);
                else
                    return null;
            }
        }

        private void load_key_Click(object sender, EventArgs e)
        {

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;
                try
                {
                    cert_mgmt.applied_certs = new X509Certificate2Collection();
                    string key_path = openFileDialog1.FileName;
                    // read the file
                    fileBytes = File.ReadAllBytes(file);
                    this.Text = "Loaded key: " + key_path;
                    cert_mgmt.getRecipient(fileBytes);
                    addToList(cert_mgmt.applied_certs);
                }
                catch (IOException)
                {
                    MessageBox.Show("Can't read file!");
                }
            }
        }

        private void change_click(object sender, EventArgs e)
        {
            string certs = "";

            X509Certificate2Collection tempcollection = new X509Certificate2Collection();
            foreach (X509Certificate2 applied_cert in cert_mgmt.applied_certs)
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.Checked && item.Text == applied_cert.SubjectName.Name)
                    {
                        tempcollection.Add(applied_cert);
                        certs += applied_cert.SubjectName.Name + "\n";
                    }
                }
            }

            DialogResult priv_deci = new DialogResult();
            bool priv_key = cert_mgmt.checkIfPrivKeyExists(tempcollection);
            if (!priv_key)
            {
                priv_deci = MessageBox.Show("No private key found in certificate collection!\nClick OK to proceed! But after that you can't open the file anymore!!\nClick Cancel to stop! No changes will made!!", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Stop);
            }
            if (priv_deci == DialogResult.OK || priv_key)
            {
                cert_mgmt.applied_certs = tempcollection;

                DialogResult decision = MessageBox.Show("The following certificates will be used for encryption:\n\n" + certs, "Encryption", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (decision == DialogResult.Yes)
                {
                    DialogResult save_dialog = saveFileDialog1.ShowDialog();
                    if (save_dialog == DialogResult.OK)
                    {
                        if (fileBytes.Length > 1)
                        {
                            File.WriteAllBytes(saveFileDialog1.FileName, cert_mgmt.EncryptMsg(cert_mgmt.DecryptMsg(fileBytes), cert_mgmt.applied_certs));
                            MessageBox.Show("New key saved!", "Key saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }

        }

        private void addToList(X509Certificate2Collection tolist)
        {
            listView1.Items.Clear();
            foreach (X509Certificate2 cert in tolist)
            {
                ListViewItem item = new ListViewItem(cert.SubjectName.Name);
                item.Checked = true;
                listView1.Items.Add(item);
            }
            foreach (string serial in cert_mgmt.not_applied_certs)
            {
                ListViewItem item = new ListViewItem(serial);
                item.Checked = false;
                item.Font = new System.Drawing.Font("Segoe UI", 8, System.Drawing.FontStyle.Italic);
                item.BackColor = System.Drawing.Color.LightGray;

                listView1.Items.Add(item);
            }
        }

        private void import_key_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog2.FileName;
                try
                {
                    X509Certificate2 file_cert = new X509Certificate2();
                    file_cert.Import(file);
                    listView1.Items.Add(new ListViewItem(file_cert.SubjectName.Name));
                    cert_mgmt.applied_certs.Add(file_cert);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Can't read cert file!\n" + ex);
                }
            }
        }

        private void exit_button_click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void load_store_btn_Click(object sender, EventArgs e)
        {
            cert_mgmt.addFromStore();
            addToList(cert_mgmt.applied_certs);
        }

        private void new_key_Click(object sender, EventArgs e)
        {
            MessageBox.Show("You have to select at least on certificate for init. encryption!", "Key Manager UI");
            CryptoRandom rnd = CryptoRandom.Instance;
            // Override
            cert_mgmt.applied_certs = new X509Certificate2Collection();
            this.Text = "New Key";
            cert_mgmt.addFromStore();
            addToList(cert_mgmt.applied_certs);
            if (cert_mgmt.applied_certs.Count > 0)
            {
                fileBytes = cert_mgmt.EncryptMsg(rnd.GetRandomBytes(256), cert_mgmt.applied_certs);
            }
        }

        private void KeyManagerUIForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (Form.ModifierKeys == Keys.None && keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            Regex re = new Regex(@"\A\b[0-9A-F]+\b\Z");
            if (e.Item.Checked && re.Match(e.Item.Text).Success)
            {
                e.Item.Checked = false;
            }
        }
    }
}