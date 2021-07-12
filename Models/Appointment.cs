using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot.Models
{
    public class Appointment
    {
        public string purpose { get; set; }
        public string Date { get; set; }
        public string studentId { get; set; }
        public string professor { get; set; }
    }
}
