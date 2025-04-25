using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolarTQR.Models
{
    public class User
    {
        public int Id { get; set; }              // Unik bruker-ID
        public string Username { get; set; }     // Brukernavn
        public string Password { get; set; }     // Passord (NB: Ikke lagre plaintext IRL!)
        public string FullName { get; set; }     // Fullt navn
        public string Role { get; set; }         // Rolle (f.eks. Admin, Kontroller)

        // Eventuelt mer:
        // public string Email { get; set; }
        // public bool IsActive { get; set; }
    }
}
