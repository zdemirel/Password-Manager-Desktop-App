using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;

namespace PassManager
{
    public partial class DelUser : Form
    {
        private string login_user;
        public DelUser(string login_user)
        {
            InitializeComponent();
            this.login_user = login_user;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string website = txtWebsite.Text;
            string username = txtUsername.Text;
            string password = txtPassword.Text;
            password = EncryptString(txtPassword.Text, username);

            string folderPath = @"C:\passmanager";
            string databasePath = Path.Combine(folderPath, "passwords.db");

            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();

                string selectQuery = "SELECT COUNT(*) FROM Passwords WHERE login_user=@login_user AND website=@website AND username=@username AND password=@password";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@login_user", login_user);
                    command.Parameters.AddWithValue("@website", website);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 0)
                    {
                        MessageBox.Show("Kullanıcı mevcut değil.");
                        ClearFields();
                        return;
                    }
                }

                string deleteQuery = "DELETE FROM Passwords WHERE login_user=@login_user AND website=@website AND username=@username";
                using (var command = new SQLiteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@login_user", login_user);
                    command.Parameters.AddWithValue("@website", website);
                    command.Parameters.AddWithValue("@username", username);
                    command.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Kullanıcı başarıyla silindi.");
            ClearFields();
        }

        private string EncryptString(string plainText, string username)
        {

            string salt = GenerateSalt(username);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;
                aesAlg.Mode = CipherMode.CFB;
                aesAlg.Padding = PaddingMode.PKCS7;

                byte[] keyBytes = new Rfc2898DeriveBytes(salt, Encoding.UTF8.GetBytes(salt), 10000).GetBytes(32);
                byte[] iv = new byte[aesAlg.BlockSize / 8];

                using (ICryptoTransform encryptor = aesAlg.CreateEncryptor(keyBytes, iv))
                {
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                            csEncrypt.Write(plainTextBytes, 0, plainTextBytes.Length);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

        }

        private static string GenerateSalt(string input)
        {
            string salt = input.Length > 8 ? input.Substring(input.Length - 8) : input.PadRight(8, '0');
            return salt;
        }
        private void ClearFields()
        {
            txtWebsite.Text = string.Empty;
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SelecScreen selecScreen = new SelecScreen(login_user);
            selecScreen.Show();
            this.Hide();
        }
    }
}
