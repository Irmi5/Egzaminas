using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Egzaminas_I.K_
{
    public partial class HomePage : Window
    {
        private string _username;
        private ObservableCollection<PasswordEntry> _passwordEntries;
        private const string encryptionKey = "SecretKey";

        public HomePage(string username)
        {
            InitializeComponent();
            _username = username;
            UpdateWelcomeText();
            _passwordEntries = new ObservableCollection<PasswordEntry>();
            PasswordEntriesListBox.ItemsSource = _passwordEntries;
            LoadPasswordEntries();
        }

        private void UpdateWelcomeText()
        {
            WelcomeTextBlock.Text = $"Welcome, {_username}!";
        }

        private void LoadPasswordEntries()
        {
            string connectionString = "Server=127.0.0.1;Database=users;Uid=root;Pwd=;";
            string query = "SELECT id, website_name, password, creation_time FROM passEntries WHERE username = @username";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", _username);

                try
                {
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();
                    _passwordEntries.Clear();

                    while (reader.Read())
                    {
                        string decryptedPassword = DecryptPassword(reader.GetString("password"), encryptionKey);
                        _passwordEntries.Add(new PasswordEntry
                        {
                            ID = reader.GetInt32("id"),
                            WebsiteName = reader.GetString("website_name"),
                            Password = decryptedPassword,
                            CreationTime = reader.GetDateTime("creation_time")
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private string DecryptPassword(string encryptedPassword, string key)
        {
            byte[] fullCipher = Convert.FromBase64String(encryptedPassword);
            using (Aes aes = Aes.Create())
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                Array.Resize(ref keyBytes, aes.Key.Length);
                aes.Key = keyBytes;

                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] cipher = new byte[fullCipher.Length - iv.Length];

                Array.Copy(fullCipher, iv, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private void NewEntryButton_Click(object sender, RoutedEventArgs e)
        {
            NewEntry newEntryWindow = new NewEntry(_username);
            newEntryWindow.ShowDialog();
            LoadPasswordEntries();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = (int)button.Tag;
            PasswordEntry entry = _passwordEntries.FirstOrDefault(pe => pe.ID == id);
            if (entry != null)
            {
                EditEntry editEntryWindow = new EditEntry(entry);
                editEntryWindow.ShowDialog();
                LoadPasswordEntries();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = (int)button.Tag;

            string connectionString = "Server=127.0.0.1;Database=users;Uid=root;Pwd=;";
            string query = "DELETE FROM passEntries WHERE id = @id";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    LoadPasswordEntries();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        public class PasswordEntry
        {
            public int ID { get; set; }
            public string WebsiteName { get; set; }
            public string Password { get; set; }
            public DateTime CreationTime { get; set; }

            public override string ToString()
            {
                return $"Website: {WebsiteName}, Password: {Password}, Created: {CreationTime}";
            }
        }
    }
}