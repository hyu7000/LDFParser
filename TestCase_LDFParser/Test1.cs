using LDFParser;

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
    }
}
