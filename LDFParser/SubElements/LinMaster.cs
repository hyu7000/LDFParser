using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDFParser.SubElements
{
    public class LinMaster
    {
        public string Name { get; set; } = "";
        public int TimeBase { get; set; } = 0; // 단위: ms
        public int Jitter { get; set; } = 0;   // 단위: ms
    }
}
