using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Egzaminas_I.K_
{
    public partial class NewEntry : Window
    {
        private string _username;
        private const string encryptionKey = "SecretKey";

        public NewEntry(string username)
        {
            InitializeComponent();
            _username = username;
        }

        private void ReturnPage_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string appName = app.Text;
            string password = appPass.Password;

            if (GeneratePass.IsChecked == true)
            {
                password = GenerateRandomPassword();
            }

            string encryptedPassword = EncryptPassword(password, encryptionKey);

            string connectionString = "Server=127.0.0.1;Database=users;Uid=root;Pwd=;";
            string query = "INSERT INTO passEntries (username, website_name, password, creation_time) VALUES (@username, @website_name, @password, @creation_time)";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", _username);
                cmd.Parameters.AddWithValue("@website_name", appName);
                cmd.Parameters.AddWithValue("@password", encryptedPassword);
                cmd.Parameters.AddWithValue("@creation_time", DateTime.Now);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("New entry added successfully!");
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private string EncryptPassword(string password, string key)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                Array.Resize(ref keyBytes, aes.Key.Length);
                aes.Key = keyBytes;
                aes.GenerateIV();
                byte[] iv = aes.IV;
                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                {
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
                    byte[] result = new byte[iv.Length + encryptedBytes.Length];
                    iv.CopyTo(result, 0);
                    encryptedBytes.CopyTo(result, iv.Length);
                    return Convert.ToBase64String(result);
                }
            }
        }

        private string GenerateRandomPassword(int length = 8)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder result = new StringBuilder();
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    result.Append(validChars[(int)(num % (uint)validChars.Length)]);
                }
            }
            return result.ToString();
        }
    }
}
