using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace PassManager
{
    public partial class PassShow : Form
    {
        private string databasePath;
        private string login_user;
        public PassShow(string login_user)
        {
            InitializeComponent();

            string folderPath = @"C:\passmanager";
            databasePath = Path.Combine(folderPath, "passwords.db");

            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.Columns.Add("Username", 175);
            listView1.Columns.Add("Password", 175);

            this.login_user = login_user;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                listView1.BeginUpdate();
                listView1.Items.Clear();

                using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
                {
                    connection.Open();

                    string selectQuery = "SELECT username, password FROM Passwords WHERE (website=@website AND login_user=@login_user)";
                    string website = txtWebsite.Text;

                    using (var command = new SQLiteCommand(selectQuery, connection))
                    {
                        command.Parameters.AddWithValue("@website", website);
                        command.Parameters.AddWithValue("@login_user", login_user);

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string username = reader["username"].ToString();
                                    string password = reader["password"].ToString();

                                    ListViewItem item = new ListViewItem();
                                    item.Text = username; // Kullanıcı adını ilk sütuna ekler
                                    item.SubItems.Add(DecryptPassword(password, username));
                                    listView1.Items.Add(item);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Girilen web sitesine ait kayıt bulunamadı.");
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Veri yükleme işlemi başarısız.");
            }
            finally
            {
                listView1.EndUpdate();
            }
        }

        public string DecryptPassword(string encryptedPassword, string username)
        {
            string salt = GenerateSalt(username);
            byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;
                aesAlg.Mode = CipherMode.CFB;
                aesAlg.Padding = PaddingMode.PKCS7;

                byte[] keyBytes = new Rfc2898DeriveBytes(salt, saltBytes, 10000).GetBytes(32);
                byte[] iv = new byte[aesAlg.BlockSize / 8];
                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);

                using (ICryptoTransform decryptor = aesAlg.CreateDecryptor(keyBytes, iv))
                {
                    using (MemoryStream msDecrypt = new MemoryStream(encryptedBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        private static string GenerateSalt(string input)
        {
            string salt = input.Length > 8 ? input.Substring(input.Length - 8) : input.PadRight(8, '0');
            return salt;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SelecScreen selecScreen = new SelecScreen(login_user);
            selecScreen.Show();
            this.Hide();
        }
    }
}
