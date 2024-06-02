using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Egzaminas_I.K_.HomePage;

namespace Egzaminas_I.K_
{
    /// <summary>
    /// Interaction logic for EditEntry.xaml
    /// </summary>
    public partial class EditEntry : Window
    {
        private PasswordEntry _passwordEntry;
        private const string encryptionKey = "SecretKey";

        public EditEntry(PasswordEntry passwordEntry)
        {
            InitializeComponent();
            _passwordEntry = passwordEntry;
            appPassChange.Password = _passwordEntry.Password;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = appPassChange.Password;

            if (GeneratePassChange.IsChecked == true)
            {
                newPassword = GenerateRandomPassword();
            }

            string encryptedPassword = EncryptPassword(newPassword, encryptionKey);

            string connectionString = "Server=127.0.0.1;Database=users;Uid=root;Pwd=;";
            string query = "UPDATE passEntries SET password = @password WHERE id = @id";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@password", encryptedPassword);
                cmd.Parameters.AddWithValue("@id", _passwordEntry.ID);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Password updated successfully.");
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void ReturnPage_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
