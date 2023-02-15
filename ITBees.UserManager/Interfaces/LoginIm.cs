﻿using System.ComponentModel.DataAnnotations;

namespace ITBees.UserManager.Interfaces
{
    /// <summary>
    /// Login input model
    /// </summary>
    public class LoginIm
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}