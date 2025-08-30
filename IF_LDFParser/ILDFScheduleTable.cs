using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IF_LDFParser
{
    public interface ILDFScheduleTable
    {
        string Name { get; set; }

        // Frame 이름, 시간(ms)
        List<(string, uint)> Schedule { get; set; }
    }
}
