using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Egzaminas_I.K_
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int maxPasswordLength = 5;
        private Stopwatch stopwatch;
        private volatile string foundPassword = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void homePage_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTextBox.Text;
            string password = passwordTextBox.Password;

            string connectionString = "Server=127.0.0.1;Database=users;Uid=root;Pwd=;";
            string hashedPassword = HashPassword(password);

            string query = "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", hashedPassword);

                    try
                    {
                        connection.Open();
                        int count = Convert.ToInt32(command.ExecuteScalar());

                        if (count > 0)
                        {
                            HomePage homePage = new HomePage(username);
                            homePage.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Invalid username or password.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred: {ex.Message}");
                    }
                }
            }
        }

        private void signUp_Click(object sender, RoutedEventArgs e)
        {
            SignUp signUpWindow = new SignUp();
            signUpWindow.Show();
        }

        private async void Brute_Force_Click(object sender, RoutedEventArgs e)
        {
            string username = usernameTextBox.Text;

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a username.");
                return;
            }

            string hash = GetStoredPasswordHash(username);
            if (hash == null)
            {
                MessageBox.Show("Username not found.");
                return;
            }

            stopwatch = Stopwatch.StartNew();
            int maxThreads = (int)threadsSlider.Value;
            string crackedPassword = await Task.Run(() => BruteForce(hash, maxThreads));
            stopwatch.Stop();

            if (crackedPassword != null)
            {
                MessageBox.Show($"Password found: {crackedPassword}\nElapsed Time: {stopwatch.Elapsed}");
            }
            else
            {
                MessageBox.Show("Password not found within the given constraints.");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashedBytes.Length; i++)
                {
                    builder.Append(hashedBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GetStoredPasswordHash(string username)
        {
            string hash = null;
            string connectionString = "Server=127.0.0.1;Database=users;Uid=root;Pwd=;";
            string query = "SELECT password FROM users WHERE username = @username";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", username);

                try
                {
                    connection.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        hash = reader.GetString("password");
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            return hash;
        }

        private string BruteForce(string targetHash, int maxThreads)
        {
            foundPassword = null;
            Thread[] threads = new Thread[maxThreads];

            for (int i = 0; i < maxThreads; i++)
            {
                threads[i] = new Thread(() => BruteForceThread(targetHash));
                threads[i].Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            return foundPassword;
        }

        private void BruteForceThread(string targetHash)
        {
            for (int length = 1; length <= maxPasswordLength; length++)
            {
                string result = BruteForceRecursive("", targetHash, length);
                if (result != null)
                {
                    foundPassword = result;
                    break;
                }
            }
        }

        private string BruteForceRecursive(string current, string targetHash, int maxLength)
        {
            if (current.Length == maxLength)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    currentAttemptTextBlock.Text = $"Current Attempt: {current}\nElapsed Time: {stopwatch.Elapsed}";
                });

                if (HashPassword(current) == targetHash)
                {
                    return current;
                }
                return null;
            }

            if (foundPassword != null)
            {
                return foundPassword;
            }

            foreach (char c in charset)
            {
                string result = BruteForceRecursive(current + c, targetHash, maxLength);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void ChangePass_Click(object sender, RoutedEventArgs e)
        {
            ChangePassword changePassword = new ChangePassword();
            changePassword.Show();
        }
    }
}
