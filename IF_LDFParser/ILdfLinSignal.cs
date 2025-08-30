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
        ByteOrder byteOrder { get; set; }
        ByteType byteType { get; set; }
        double scale { get; set; }
        double offset { get; set; }
        double minValue { get; set; }
        double maxValue { get; set; }
    }
}
