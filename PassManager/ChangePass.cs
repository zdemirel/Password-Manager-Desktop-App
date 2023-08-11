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
using System.Security.Cryptography;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace PassManager
{
    public partial class ChangePass : Form
    {
        private string login_user;
        public ChangePass(string login_user)
        {
            InitializeComponent();
            this.login_user = login_user;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
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
                            MessageBox.Show("Yanlış veya eksik bilgi.");
                            ClearFields();
                            return;
                        }
                    }
                    string new_password = GeneratePassword();
                    txtNewPassword.Text = new_password;
                    txtNewPassword.UseSystemPasswordChar = false; // Şifrenin görünmesini sağlar

                    new_password = EncryptString(txtNewPassword.Text, username);

                    string updateQuery = "UPDATE Passwords SET password=@new_password WHERE login_user=@login_user AND website=@website AND username=@username";
                    using (var command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@login_user", login_user);
                        command.Parameters.AddWithValue("@website", website);
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@new_password", new_password);

                        MessageBox.Show("Şifre başarıyla değiştirildi.");
                        command.ExecuteNonQuery();
                        ClearFields();
                    }
                    
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string GeneratePassword()
        {

            try
            {
                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";

                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] data = new byte[4];
                    rng.GetBytes(data);

                    int seed = BitConverter.ToInt32(data, 0);
                    Random random = new Random(seed);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < 12; i++)
                    {
                        int index = random.Next(chars.Length);
                        sb.Append(chars[index]);
                    }

                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return string.Empty;
            }
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
            txtNewPassword.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SelecScreen selecScreen = new SelecScreen(login_user);
            selecScreen.Show();
            this.Hide();
        }
    }
}
