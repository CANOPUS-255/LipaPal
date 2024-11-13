using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LipaPal_WebApp.Models;

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
        public IActionResult CompleteKYC([FromBody] KYCRequest kycRequest)
        {
            if (IsPhoneNumberTaken(kycRequest.PersonalDetails.PhoneNumber) ||
                IsBankAccountTaken(kycRequest.PersonalDetails.BankAccount) ||
                IsDocumentNumberTaken(kycRequest.PersonalDetails.DocumentNumber))
            {
                return BadRequest("Some details are already registered.");
            }

            if (SaveKYCToDatabase(kycRequest.PersonalDetails, kycRequest.UserNames))
                return Ok("KYC Verification Process is successful!");

            return StatusCode(500, "Error during KYC verification.");
        }

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

        private bool SaveKYCToDatabase(PersonalDetails personalDetails, UserNames userNames)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                string query1 = @"
                    INSERT INTO personaldetail (user_id, phone_number, bank_account, document_type, document_number, profile_photo, is_verified)
                    VALUES (@user_id, @phone_number, @bank_account, @document_type, @document_number, @profile_photo, @is_verified) RETURNING kyc_id";

                using var cmd1 = new NpgsqlCommand(query1, connection);
                cmd1.Parameters.AddWithValue("user_id", personalDetails.UserId);
                cmd1.Parameters.AddWithValue("phone_number", personalDetails.PhoneNumber);
                cmd1.Parameters.AddWithValue("bank_account", personalDetails.BankAccount);
                cmd1.Parameters.AddWithValue("document_type", personalDetails.DocumentType);
                cmd1.Parameters.AddWithValue("document_number", personalDetails.DocumentNumber);
                cmd1.Parameters.AddWithValue("profile_photo", personalDetails.ProfilePhoto);
                cmd1.Parameters.AddWithValue("is_verified", personalDetails.IsVerified);
                int kycId = (int)cmd1.ExecuteScalar(); // Get the generated kyc_id

                string query2 = @"
                    INSERT INTO usernames (kyc_id, user_id, first_name, middle_name, last_name)
                    VALUES (@kyc_id, @user_id, @first_name, @middle_name, @last_name)";

                using var cmd2 = new NpgsqlCommand(query2, connection);
                cmd2.Parameters.AddWithValue("kyc_id", kycId);
                cmd2.Parameters.AddWithValue("user_id", userNames.UserId);
                cmd2.Parameters.AddWithValue("first_name", userNames.FirstName);
                cmd2.Parameters.AddWithValue("middle_name", userNames.MiddleName);
                cmd2.Parameters.AddWithValue("last_name", userNames.LastName);

                cmd2.ExecuteNonQuery(); // Execute the second insert command
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

    public class KYCRequest
    {
        public PersonalDetails PersonalDetails { get; set; }
        public UserNames UserNames { get; set; }
    }
}
