using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IF_LDFParser
{
    public interface ILinParser
    {
        List<string> GetAllNodeName();
        List<ILdfLinFrame> GetAllLinFrame();
        List<ILdfLinFrame> GetLinFrame(string nodeName);
        List<ILdfScheduleTable> GetScheduleTables();
    }
}
