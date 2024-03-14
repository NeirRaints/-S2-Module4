using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigBoarsSession2
{
    public class PersonLocations
    {
        public string PersonCode { get; set; }
        public string PersonRole { get; set; }
        public int LastSecurityPointNumber { get; set; }
        public string LastSecurityPointDirection { get; set; }
        public DateTime LastSecurityPointTime { get; set; }
    }
}
