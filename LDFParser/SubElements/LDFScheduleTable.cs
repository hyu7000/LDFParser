using IF_LDFParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDFParser.SubElements
{
    public class LDFScheduleTable : ILdfScheduleTable
    {
        public string Name { get; set; } = "";

        //         Frame 이름, 시간(ms)
        public List<(string, uint)> Schedule { get; set; } = new List<(string, uint)> ();
    }
}
