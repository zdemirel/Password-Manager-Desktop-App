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
    public partial class LoginScreen : Form
    {
        public LoginScreen()
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

                    string createTableQuery = "CREATE TABLE IF NOT EXISTS Login (login_user TEXT, login_pass TEXT)";
                    using (var command = new SQLiteCommand(createTableQuery, connection))
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
            login_pass = EncryptString(txtPassword.Text, login_user);

            string folderPath = @"C:\passmanager";
            string databasePath = Path.Combine(folderPath, "passwords.db");

            using (var connection = new SQLiteConnection($"Data Source={databasePath};Version=3;"))
            {
                connection.Open();

                string selectQuery = "SELECT COUNT(*) FROM Login WHERE login_user=@login_user AND login_pass=@login_pass";

                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@login_user", login_user);
                    command.Parameters.AddWithValue("@login_pass", login_pass);

                    int count = Convert.ToInt32(command.ExecuteScalar());

                    if (count == 1)
                    {
                        SelecScreen selecScreen = new SelecScreen(login_user);
                        selecScreen.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("Kullanıcı adı ya da şifre yanlış.");
                    }
                }
            }

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
            txtUsername.Text = string.Empty;
            txtPassword.Text = string.Empty;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateAccount createAccount = new CreateAccount();
            createAccount.Show();
            this.Hide();
        }
    }
}
