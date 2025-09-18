using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IF_LDFParser
{
    public interface ILdfLinSignal
    {
        String SignalName { get; set; }
        String Unit { get; set; }
        uint StartBit { get; set; }
        uint Length { get; set; }
        ByteOrder ByteOrder { get; set; }
        ByteType ByteType { get; set; }
        double Scale { get; set; }
        double Offset { get; set; }
        double MinValue { get; set; }
        double MaxValue { get; set; }
    }
}
