using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LipaPal_WebApp.Models;  // Add this to import the User class

namespace LipaPal_WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly string _connectionString = "Host=localhost;Username=vendorlink_user;Password=123;Database=Lipapal_db";

        [HttpPost("register")]
        public IActionResult Register([FromBody] User user)
        {
            if (!IsValidUsername(user.Username) || IsUsernameTaken(user.Username))
                return BadRequest("Invalid or already taken username.");

            if (!IsValidEmail(user.Email) || IsEmailTaken(user.Email))
                return BadRequest("Invalid or already registered email.");

            byte[] passwordHash = HashPassword(user.PasswordHash);

            if (SaveUserToDatabase(user, passwordHash))
                return Ok(new { message = "User registered successfully.", userId = user.UserId });

            return StatusCode(500, "Error saving user to the database.");
        }

        [HttpPost("kyc")]
        public IActionResult CompleteKYC([FromBody] PersonalDetails personalDetails)
        {
            if (IsPhoneNumberTaken(personalDetails.PhoneNumber) ||
                IsBankAccountTaken(personalDetails.BankAccount) ||
                IsDocumentNumberTaken(personalDetails.DocumentNumber))
            {
                return BadRequest("Some details are already registered.");
            }

            if (SaveKYCToDatabase(personalDetails))
                return Ok("KYC Verification Process is successful!");

            return StatusCode(500, "Error during KYC verification.");
        }

        // Helper methods for database operations
        private bool SaveUserToDatabase(User user, byte[] passwordHash)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                string query = "INSERT INTO users (username, email, password_hash) VALUES (@username, @Email, @PasswordHash) RETURNING user_id";
                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("username", user.Username);
                cmd.Parameters.AddWithValue("Email", user.Email);
                cmd.Parameters.AddWithValue("PasswordHash", passwordHash);
                user.UserId = (int)cmd.ExecuteScalar();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool SaveKYCToDatabase(PersonalDetails personalDetails)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    INSERT INTO personaldetail (user_id, phone_number, bank_account, document_type, document_number)
                    VALUES (@user_id, @phone_number, @bank_account, @document_type, @document_number)";

                using var cmd = new NpgsqlCommand(query, connection);
                cmd.Parameters.AddWithValue("user_id", personalDetails.UserId);
                cmd.Parameters.AddWithValue("phone_number", personalDetails.PhoneNumber);
                cmd.Parameters.AddWithValue("bank_account", personalDetails.BankAccount);
                cmd.Parameters.AddWithValue("document_type", personalDetails.DocumentType);
                cmd.Parameters.AddWithValue("document_number", personalDetails.DocumentNumber);

                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsUsernameTaken(string username) => CheckIfExists("users", "username", username);
        private bool IsEmailTaken(string email) => CheckIfExists("users", "email", email);
        private bool IsPhoneNumberTaken(string phoneNumber) => CheckIfExists("personaldetail", "phone_number", phoneNumber);
        private bool IsBankAccountTaken(string bankAccount) => CheckIfExists("personaldetail", "bank_account", bankAccount);
        private bool IsDocumentNumberTaken(string documentNumber) => CheckIfExists("personaldetail", "document_number", documentNumber);

        private bool CheckIfExists(string table, string column, string value)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            string query = $"SELECT COUNT(1) FROM {table} WHERE {column} = @value";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("value", value);

            return (long)cmd.ExecuteScalar() > 0;
        }

        private byte[] HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private bool IsValidUsername(string username) => username.Length > 2;
        private bool IsValidEmail(string email) => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
