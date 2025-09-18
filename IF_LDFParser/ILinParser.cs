using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IF_LDFParser
{
    public interface ILinParser
    {
        string GetMasterName();
        uint GetBaudRate();
        List<string> GetAllNodeName();
        List<ILdfLinFrame> GetAllLinFrame();
        List<ILdfLinFrame> GetLinFrame(string nodeName);
        List<ILdfScheduleTable> GetScheduleTables();
    }
}
