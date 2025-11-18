using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wcs.Monitor.Models
{
    public class EquipmentStatusDto
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;   // Idle, Running, Fault 등
        public string? AlarmCode { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
