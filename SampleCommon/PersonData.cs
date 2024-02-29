using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommon
{
    public class PersonDataRequest
    {
        public int PersonId { get; set; }
    }

    public class PersonDataResponse
    {
        public int PersonId { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
    }
}
