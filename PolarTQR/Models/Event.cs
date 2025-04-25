using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolarTQR.Models
{
    public class Event
    {
        public int Id { get; set; }          // Unik ID for eventet
        public string Name { get; set; }     // Navn på eventet
        public string Location { get; set; } // Hvor det skjer
        public DateTime Date { get; set; }   // Når det skjer
        public string Description { get; set; } // Litt info om eventet

        // Eksempelvis kan vi legge til flere felt som:
        // public bool IsActive { get; set; }
    }
}
