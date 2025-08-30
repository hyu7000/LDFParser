using IF_LDFParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDFParser.SubElements
{
    public class LDFLinSignal : ILdfLinSignal
    {
        public String SignalName { get; set; } = "";
        public String Unit { get; set; } = "";
        public uint StartBit { get; set; }
        public uint Length { get; set; }
        public ByteOrder byteOrder { get; set; } = ByteOrder.LittleEndian;
        public ByteType byteType { get; set; } = ByteType.Unsigned;
        public double scale { get; set; }
        public double offset { get; set; }
        public double minValue { get; set; }
        public double maxValue { get; set; }
    }
}
