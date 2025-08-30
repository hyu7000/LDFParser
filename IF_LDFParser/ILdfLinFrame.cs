using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IF_LDFParser
{
    public interface ILdfLinFrame
    {
        byte ID { get; set; }                // LIN Frame ID (0~63)
        string Name { get; set; }            // Frame 이름
        string Publisher { get; set; }       // Response Publisher Node
        List<string> Subscribers { get; set; } // Subscriber Node들
        byte ByteLength { get; set; }        // 데이터 길이 (0~8)
        ushort CycleTime { get; set; }       // 주기(ms), 스케줄 테이블 기준
        LinChecksumModel Checksum { get; set; } // 체크섬 모델
        List<ILdfLinSignal> GetSignals();       // 포함된 Signal 리스트        
    }
}
