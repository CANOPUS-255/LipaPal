using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace RegistrationApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("User Registration For LipaPal");
            var registeredUser = RegisterUser();

            if (registeredUser != null)
            {
                Console.WriteLine("\nRegistration successful. Please complete KYC for verification.");
                if (CompleteKYC(registeredUser.UserId))
                {
                    Console.WriteLine("KYC Verification Process is successful!");
                }
                else
                {
                    Console.WriteLine("Error during KYC verification.");
                }
            }
            else
            {
                Console.WriteLine("Error encountered while registering.");
            }
        }

        // Registers a new user
        static User RegisterUser()
        {
            Console.Write("Enter your username: ");
            string username = Console.ReadLine();
            Console.Write("Enter your email: ");
            string email = Console.ReadLine();
            Console.Write("Enter your password: ");
            string password = Console.ReadLine();

            if (!IsValidUsername(username) || IsUsernameTaken(username))
            {
                Console.WriteLine("Username is invalid or already taken.");
                return null;
            }
            if (!IsValidEmail(email) || IsEmailTaken(email))
            {
                Console.WriteLine("Email is invalid or already registered.");
                return null;
            }
            if (!IsValidPassword(password))
            {
                Console.WriteLine("Password does not meet strength requirements.");
                return null;
            }

            byte[] passwordHash = HashPassword(password);
            var newUser = new User { Username = username, Email = email };

            return SaveUserToDatabase(newUser, passwordHash) ? newUser : null;
        }

        // Completes KYC verification
        static bool CompleteKYC(int userId)
        {
            Console.Write("Enter your First Name: ");
            string Fname = Console.ReadLine();
            Console.Write("Enter your Middle Name: ");
            string Mname = Console.ReadLine();
            Console.Write("Enter your Last Name: ");
            string Lname = Console.ReadLine();
            Console.Write("Enter your Phone number: ");
            string phoneNumber = Console.ReadLine();
            Console.Write("Enter your Bank Account Number: ");
            string bankAccount = Console.ReadLine();
            Console.Write("Enter your Verification Document Type: ");
            string documentType = Console.ReadLine();
            Console.Write("Enter your Document Number: ");
            string documentNumber = Console.ReadLine();

            // Checking for existing details
            if (IsPhoneNumberTaken(phoneNumber) || IsBankAccountTaken(bankAccount) || IsDocumentNumberTaken(documentNumber))
            {
                Console.WriteLine("Some details are already registered.");
                return false;
            }

            try
            {
                using var connection = Database.GetConnection();
                connection.Open();

                // Insert into personaldetail table and retrieve kyc_id
                string query1 = @"
            INSERT INTO personaldetail (user_id, phone_number, bank_account, document_type, document_number)
            VALUES (@user_id, @phone_number, @bank_account, @document_type, @document_number)
            RETURNING kyc_id";

                using var cmd1 = new NpgsqlCommand(query1, connection);
                cmd1.Parameters.AddWithValue("user_id", userId);
                cmd1.Parameters.AddWithValue("phone_number", phoneNumber);
                cmd1.Parameters.AddWithValue("bank_account", bankAccount);
                cmd1.Parameters.AddWithValue("document_type", documentType);
                cmd1.Parameters.AddWithValue("document_number", documentNumber);

                int kycId = (int)cmd1.ExecuteScalar(); // Get the generated kyc_id

                // Insert into usernames table with retrieved kyc_id
                string query2 = @"
            INSERT INTO usernames (user_id, kyc_id, first_name, middle_name, last_name)
            VALUES (@user_id, @kyc_id, @first_name, @middle_name, @last_name)";

                using var cmd2 = new NpgsqlCommand(query2, connection);
                cmd2.Parameters.AddWithValue("user_id", userId);
                cmd2.Parameters.AddWithValue("kyc_id", kycId);
                cmd2.Parameters.AddWithValue("first_name", Fname);
                cmd2.Parameters.AddWithValue("middle_name", Mname);
                cmd2.Parameters.AddWithValue("last_name", Lname);

                cmd2.ExecuteNonQuery(); // Execute the second insert command

                return true; // If both inserts succeed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        // Helper Methods
        static bool IsUsernameTaken(string username) => CheckIfExists("users", "username", username);
        static bool IsEmailTaken(string email) => CheckIfExists("users", "email", email);
        static bool IsPhoneNumberTaken(string phoneNumber) => CheckIfExists("personaldetail", "phone_number", phoneNumber);
        static bool IsBankAccountTaken(string bankAccount) => CheckIfExists("personaldetail", "bank_account", bankAccount);
        static bool IsDocumentNumberTaken(string documentNumber) => CheckIfExists("personaldetail", "document_number", documentNumber);

        // Check if a value exists in the database
        static bool CheckIfExists(string table, string column, string value)
        {
            using var connection = Database.GetConnection();
            connection.Open();
            string query = $"SELECT COUNT(1) FROM {table} WHERE {column} = @value";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("value", value);
            return (long)cmd.ExecuteScalar() > 0;
        }

        // Validations
        static bool IsValidUsername(string username) => username.Length > 2;
        static bool IsValidEmail(string email) => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        static bool IsValidPassword(string password) => password.Length >= 8 && Regex.IsMatch(password, @"[A-Z]") && Regex.IsMatch(password, @"[a-z]") && Regex.IsMatch(password, @"\d");

        // Hash password
        static byte[] HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        // Save user to database
        static bool SaveUserToDatabase(User user, byte[] passwordHash)
        {
            try
            {
                using var connection = Database.GetConnection();
                connection.Open();
                string query = "INSERT INTO users (username, email, password_hash) VALUES (@username, @Email, @PasswordHash) RETURNING user_id";
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("username", user.Username);
                cmd.Parameters.AddWithValue("Email", user.Email);
                cmd.Parameters.AddWithValue("PasswordHash", passwordHash);
                user.UserId = (int)cmd.ExecuteScalar(); // Assign generated user_id
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
    }

    // User model
    class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    // PersonalDetails model
    class PersonalDetails
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string BankAccount { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
    }

    //taking full names for our user
    class UserNames
    {
        public int userId { get; set; }
        public int KycId { get; set; }
        public string Fname { get; set; }
        public string Mname { get; set; }
        public string Lname { get; set; }
    }

    // Database connection helper
    static class Database
    {
        private const string ConnectionString = "Host=localhost; username=vendorlink_user; password=123; Database=Lipapal_db";

        public static NpgsqlConnection GetConnection() => new NpgsqlConnection(ConnectionString);
    }
}