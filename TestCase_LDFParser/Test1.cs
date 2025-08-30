using IF_LDFParser;
using LDFParser;
using System.Xml.Linq;

namespace TestCase_LDFParser
{
    [TestClass]
    public sealed class Test1
    {
        private LDFParser.LDFParser? _parser = null;

        [TestInitialize] // 각 테스트 메서드 실행 전에 호출되는 초기화 메서드
        public void Setup()
        {
            string relativePath = @"MCU_EOP1_LIN_Controller.ldf";
            string basePath = AppDomain.CurrentDomain.BaseDirectory; // 실행 파일 기준 경로

            // 절대 경로 생성
            string absolutePath = Path.Combine(basePath, relativePath);

            _parser = new LDFParser.LDFParser(absolutePath);
        }

        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine(_parser.TestDebug());
        }

        [TestMethod]
        public void CheckNodes()
        {
            if(_parser == null) return;

            bool result = false;

            List<string> node = _parser.GetAllNodeName();

            if (
                (node.Count == 2) &&
                (node[0] == "LinMaster") &&
                (node[1] == "Eop")
            )
            {
                result = true;
            }

            Assert.IsTrue(result, $"노드가 예상과 다름. Count={node.Count}, 값=[{string.Join(",", node)}]");
        }

        [TestMethod]
        public void CheckFrames()
        {
            if (_parser == null) return;

            bool result = false;

            List<ILdfLinFrame> frames = _parser.GetAllLinFrame();

            if (
                (frames.Count == 2) &&
                (frames[0].Name == "COMMAND_EOP") &&
                (frames[1].Name == "STATUS_EOP")
            )
            {
                result = true;
            }

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CheckScheduleTable()
        {
            if (_parser == null) return;

            bool result = false;

            List<ILdfScheduleTable> table = _parser.GetScheduleTables();

            if (
                (table.Count == 1) &&
                (table[0].Name == "BusActiveNomal")
            )
            {
                result = true;
            }

            Assert.IsTrue(result);
        }
    }
}
