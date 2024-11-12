using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;



namespace LipaPal_WebApp.Models
{
    public class UserNames
    {
        public int UserId { get; set; }
        public int KycId { get; set; }
        public string Fname { get; set; }
        public string Mname { get; set; }
        public string Lname { get; set; }
    }
}