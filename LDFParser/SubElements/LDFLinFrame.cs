using IF_LDFParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDFParser.SubElements
{
    public class LDFLinFrame : ILdfLinFrame
    {
        public byte ID { get; set; }                // LIN Frame ID (0~63)
        public string Name { get; set; } = "";
        public string Publisher { get; set; } = "";
        public List<string> Subscribers { get; set; } = new List<string>();
        public byte ByteLength { get; set; }        // 데이터 길이 (0~8)
        public ushort CycleTime { get; set; }       // 주기(ms), 스케줄 테이블 기준
        public LinChecksumModel Checksum { get; set; } // 체크섬 모델

        public Dictionary<string, ILdfLinSignal> Signals { get; set; } = new Dictionary<string, ILdfLinSignal>();
        public List<ILdfLinSignal> GetSignals()
        {
            List<ILdfLinSignal> list = new List<ILdfLinSignal>();

            foreach (var signal in Signals.Values)
            { 
                list.Add(signal);
            }

            return list;
        }
        public void AddSignal(ILdfLinSignal signal)
        {
            Signals.Add(signal.SignalName, signal);
        }
    }
}
