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
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;
        public ByteType ByteType { get; set; } = ByteType.Unsigned;
        public double Scale { get; set; }
        public double Offset { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
    }
}
