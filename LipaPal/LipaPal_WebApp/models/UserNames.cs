using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;



namespace LipaPal_WebApp.Models
{
    public class UserNames
    {
        public int KycId { get; set; }
        public int UserId { get; set; }        
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }
}