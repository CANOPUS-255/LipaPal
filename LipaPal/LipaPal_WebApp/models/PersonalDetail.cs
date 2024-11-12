using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;



namespace LipaPal_WebApp.Models
{
    public class PersonalDetails
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string BankAccount { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
    }
}

