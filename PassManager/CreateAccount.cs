using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace PassManager
{
    public partial class CreateAccount : Form
    {
        public CreateAccount()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                string folderPath = @"C:\passmanager";
                string databasePath = Path.Combine(folderPath, "passwords.db");

                // Gizli klasörü oluşturur
                if (!Directory.Exists(folderPath))
                {
                    DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath);
                    directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                }

                // Dosyayı oluşturur (eğer zaten mevcut değilse)
                if (!File.Exists(databasePath))
                {
                    using (FileStream fs = File.Create(databasePath)) { }
                    File.SetAttributes(databasePath, File.GetAttributes(databasePath) | FileAttributes.Hidden);
                }

                using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
                {
                    connection.Open();

                    string createTableQuery = "CREATE TABLE IF NOT EXISTS Login (login_user NVARCHAR NOT NULL, login_pass NVARCHAR NOT NULL)";
                    using (var command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string createTableQuery2 = "CREATE TABLE IF NOT EXISTS Passwords (login_user NVARCHAR NOT NULL, website NVARCHAR NOT NULL, username NVARCHAR NOT NULL, password NVARCHAR NOT NULL)";
                    using (var command = new SQLiteCommand(createTableQuery2, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                // Dosyanın gizli özelliğini ayarlar
                File.SetAttributes(databasePath, File.GetAttributes(databasePath) | FileAttributes.Hidden);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string login_user = txtUsername.Text;
            string login_pass = txtPassword.Text;

            if (string.IsNullOrEmpty(login_user) || string.IsNullOrEmpty(login_pass))
            {
                MessageBox.Show("Lütfen kullanıcı adı ve şifre alanlarını doldurunuz.");
                return;
            }

            login_pass = EncryptString(txtPassword.Text, login_user);

            string folderPath = @"C:\passmanager";
            string databasePath = Path.Combine(folderPath, "passwords.db");

            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();

                // Kullanıcı adının veritabanında var olup olmadığını kontrol et
                string selectQuery = "SELECT COUNT(*) FROM Login WHERE login_user=@login_user";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@login_user", login_user);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count > 0)
                    {
                        MessageBox.Show("Bu kullanıcı adı zaten mevcut. Lütfen farklı bir kullanıcı adı seçin.");
                        ClearFields();
                        return;
                    }
                }

                // Kullanıcı adı mevcut değilse kayıt işlemini gerçekleştir
                string insertQuery = "INSERT INTO Login (login_user, login_pass) VALUES (@login_user, @login_pass)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@login_user", login_user);
                    command.Parameters.AddWithValue("@login_pass", login_pass);
                    command.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Kullanıcı başarıyla oluşturuldu.");
            ClearFields();

            LoginScreen loginScreen = new LoginScreen();
            loginScreen.Show();
            this.Hide();
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
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoginScreen loginScreen = new LoginScreen();
            loginScreen.Show();
            this.Hide();
        }
    }
}
