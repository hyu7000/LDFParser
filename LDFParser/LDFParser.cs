using IF_LDFParser;
using LDFParser.SubElements;
using System.Numerics;
using System.Text;

namespace LDFParser
{
    public class LDFParser : ILinParser
    {
        private sealed class EncodingType
        {
            public double Min, Max, Scale, Offset;
            public string Unit = "";
        }

        private readonly string _filePath;

        private Dictionary<string, LDFLinFrame> _linFramesDict = new Dictionary<string, LDFLinFrame>();
        private Dictionary<string, LDFLinSignal> _linSignalsDict = new Dictionary<string, LDFLinSignal>();

        private LinMaster _linMaster = new LinMaster();
        private List<string> _slaves = new List<string>();

        private Dictionary<string, LDFScheduleTable> _scheduleTable = new Dictionary<string, LDFScheduleTable>();

        private readonly Dictionary<string, EncodingType> _encodingTypes = new();

        private string _protocol_version = "";
        private string _language_version = "";
        private uint _baudRate = 0;

        public LDFParser(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Invalid : {filePath}");
            }

            _filePath = filePath;

            Parse();
        }

        private void Parse()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = GetFileEncoding(_filePath);

            // 파일의 모든 라인 읽기
            string[] lines = File.ReadAllLines(_filePath, encoding);

            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("LIN_protocol_version"))
                    {
                        string value = GetValue(line);
                        _protocol_version = value;
                    }
                    else if (line.StartsWith("LIN_language_version"))
                    {
                        string value = GetValue(line);
                        _language_version = value;
                    }
                    else if (line.StartsWith("LIN_speed"))
                    {
                        string value = GetValue(line);
                        _baudRate = GetBaudRate(value);
                    }
                    else if (line.StartsWith("Nodes"))
                    {
                        i = ParseNodes(lines, i); 
                    }
                    else if (line.StartsWith("Signals"))
                    {
                        i = ParseSignals(lines, i);
                    }
                    else if (line.StartsWith("Diagnostic_signals"))
                    {
                        // To do: Add code when Diagnostic is needed
                    }
                    else if (line.StartsWith("Frames"))
                    {
                        i = ParseFrames(lines, i);
                    }
                    else if (line.StartsWith("Diagnostic_frames"))
                    {
                        // To do: Add code when Diagnostic is needed
                    }
                    else if (line.StartsWith("Node_attributes"))
                    {
                        // To do: Add code when Diagnostic is needed
                    }
                    else if (line.StartsWith("Schedule_tables"))
                    {
                        i = ParseScheduleTables(lines, i);
                    }
                    else if (line.StartsWith("Signal_encoding_types"))
                    {
                        i = ParseSignalEncodingTypes(lines, i);
                    }
                    else if (line.StartsWith("Signal_representation"))
                    {
                        i = ParseSignalRepresentation(lines, i);
                    }
                }

            }
            catch (Exception e)
            {

            }
        }

        private int ParseNodes(string[] lines, int startIndex)
        {
            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                else if (line.StartsWith("Master:"))
                {
                    // Master: LinMaster, 10 ms, 0.1 ms ;
                    string raw = line.Substring("Master:".Length).Trim().TrimEnd(';');
                    string[] parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 0)
                        _linMaster.Name = parts[0].Trim();

                    if (parts.Length > 1)
                        _linMaster.TimeBase = ParseMs(parts[1]);  // "10 ms" → 10

                    if (parts.Length > 2)
                        _linMaster.Jitter = ParseMs(parts[2]);    // "0.1 ms" → 0 (소수점 버림)
                }
                else if (line.StartsWith("Slaves:"))
                {
                    string raw = line.Substring("Slaves:".Length).Trim().TrimEnd(';');
                    string[] slaves = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in slaves)
                        _slaves.Add(s.Trim());
                }
                else if (line.StartsWith("}"))
                {
                    return i; // 블록 끝 → 호출한 쪽 for 루프가 } 다음 라인부터 진행
                }
            }
            return startIndex;
        }

        private int ParseSignals(string[] lines, int startIndex)
        {
            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("}"))
                {
                    return i; // 블록 종료
                }

                // Signal 파싱
                // 예) "Eop_Target_Spd_RPM: 8, 0, LinMaster, Eop ;"
                string[] parts = line.Split(':');
                if (parts.Length < 2) continue;

                string signalName = parts[0].Trim();
                string rightPart = parts[1].Trim().TrimEnd(';');

                string[] tokens = rightPart.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 4) continue;

                // Length, InitialValue, Publisher, Subscriber
                uint length = uint.Parse(tokens[0].Trim());
                uint initVal = uint.Parse(tokens[1].Trim()); // 필요하면 Signal 클래스에 추가
                string publisher = tokens[2].Trim();
                string subscriber = tokens[3].Trim();

                var signal = new LDFLinSignal
                {
                    SignalName = signalName,
                    Length = length,
                    StartBit = 0,           // LDF Signals 블록에는 위치 정보 없음 (Frame에서 정의됨)
                    ByteOrder = ByteOrder.LittleEndian,
                    ByteType = ByteType.Unsigned,
                    Scale = 1.0,
                    Offset = 0.0,
                    MinValue = 0,
                    MaxValue = Math.Pow(2, length) - 1
                };

                // Dictionary에 저장
                _linSignalsDict[signal.SignalName] = signal;
            }

            return startIndex;
        }

        #region ParseFrame
        private int ParseFrames(string[] lines, int startIndex)
        {
            LDFLinFrame? current = null;

            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // -------------------------------------------
                // '}' 처리: 프레임 종료 vs Frames 블록 종료 구분
                // -------------------------------------------
                if (line == "}")
                {
                    if (current != null)
                    {
                        // 프레임 하나 닫힘
                        _linFramesDict.Add(current.Name, current);
                        current = null;
                        continue;                 // ❗다음 라인 계속 파싱
                    }
                    else
                    {
                        // current == null 이면 Frames 블록 자체가 끝난 것
                        return i;
                    }
                }

                // 프레임 헤더 시작: ex) COMMAND_EOP: 39, LinMaster, 8 {
                if (line.Contains(":") && line.EndsWith("{"))
                {
                    // 혹시 이전 프레임이 열린 채였다면 먼저 등록
                    if (current != null)
                    {
                        _linFramesDict.Add(current.Name, current);
                        current = null;
                    }
                    current = ParseFrameHeader(line);
                    continue;
                }

                // 프레임 내부 신호 매핑: ex) Eop_Target_Spd_RPM, 24 ;
                if (current != null && line.EndsWith(";"))
                {
                    var sm = ParseFrameSignalMap(line);
                    if (sm != null)
                    {
                        if (_linSignalsDict.TryGetValue(sm.Value.SignalName, out var sigMeta))
                        {
                            sigMeta.StartBit = sm.Value.StartBit;

                            current.AddSignal(sigMeta);
                        }
                        else
                        {
                            current.AddSignal(new LDFLinSignal
                            {
                                SignalName = sm.Value.SignalName,
                                StartBit = sm.Value.StartBit,
                                Length = 0
                            });
                        }
                    }
                    continue;
                }
            }

            return startIndex; // 비정상 종료 대비
        }

        private (string SignalName, uint StartBit)? ParseFrameSignalMap(string line)
        {
            // "Eop_Torque, 50 ;" → ("Eop_Torque", 50)
            string trimmed = line.Trim().TrimEnd(';').Trim();
            var parts = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            string sigName = parts[0].Trim();
            if (!uint.TryParse(parts[1].Trim(), out uint startBit)) return null;

            return (sigName, startBit);
        }

        private LDFLinFrame ParseFrameHeader(string line)
        {
            // "STATUS_EOP: 58, Eop, 8 {" 형태
            // 좌측 name / 우측 "id, publisher, dlc {" 분리
            var parts = line.Split(':', 2);
            string name = parts[0].Trim();

            string right = parts[1].Trim();
            if (right.EndsWith("{")) right = right.Substring(0, right.Length - 1).Trim(); // '{' 제거
            if (right.EndsWith(";")) right = right.Substring(0, right.Length - 1).Trim();

            var tokens = right.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3) throw new FormatException($"Frame header parse error: {line}");

            // ID: 10진 또는 0xNN
            byte id = ParseLinId(tokens[0].Trim());
            string publisher = tokens[1].Trim();
            byte dlc = byte.Parse(tokens[2].Trim());

            LinChecksumModel checksum = LinChecksumModel.Classic;
            if(_protocol_version.Contains("2.1"))
            {
                checksum = LinChecksumModel.Enhanced;
            }
            else if (_protocol_version.Contains("2.0"))
            {
                checksum = LinChecksumModel.Enhanced;
            }

            return new LDFLinFrame
            {
                Name = name,
                ID = id,
                Publisher = publisher,
                ByteLength = dlc,
                Checksum = checksum
            };
        }

        private byte ParseLinId(string s)
        {
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToByte(s, 16);
            }
            return Convert.ToByte(s);
        }
        #endregion

        #region ParseScheduleTable
        private int ParseScheduleTables(string[] lines, int startIndex)
        {
            LDFScheduleTable? currentTable = null;

            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 바깥 Schedule_tables 블록 종료
                if (line == "}")
                {
                    // 열려 있던 테이블이 남아 있으면 등록
                    if (currentTable != null)
                    {                        
                        _scheduleTable.Add(currentTable.Name, currentTable);
                        currentTable = null;
                    }
                    return i;
                }

                // 테이블 시작:  e.g.  BusActiveNomal {
                if (line.EndsWith("{") && !line.Contains(":"))
                {
                    // 이전 테이블 닫기
                    if (currentTable != null)
                    {
                        _scheduleTable.Add(currentTable.Name, currentTable);
                        currentTable = null;
                    }

                    string name = line.Substring(0, line.Length - 1).Trim(); // '{' 제거
                    currentTable = new LDFScheduleTable { Name = name };
                    continue;
                }

                // 테이블 종료
                if (line == "}" && currentTable != null)
                {
                    _scheduleTable.Add(currentTable.Name, currentTable);
                    currentTable = null;
                    continue;
                }

                // 슬롯 라인: "<FrameName> delay <N> ms ;"
                if (currentTable != null && line.EndsWith(";"))
                {
                    var slot = ParseScheduleSlot(line);
                    if (slot != null)
                    {
                        currentTable.Schedule.Add(slot.Value);
                    }
                    continue;
                }
            }

            return startIndex; // 비정상 종료 대비
        }

        private (string FrameName, uint DelayMs)? ParseScheduleSlot(string line)
        {
            // "COMMAND_EOP delay 50 ms ;" -> ("COMMAND_EOP", 50)
            string s = line.Trim().TrimEnd(';').Trim();

            int idx = s.IndexOf(" delay ", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            string frameName = s.Substring(0, idx).Trim();
            string right = s.Substring(idx + " delay ".Length).Trim();  // "50 ms" 또는 "50"

            // 끝의 "ms" 제거
            if (right.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
                right = right.Substring(0, right.Length - 2).Trim();

            // 소수 가능 → 반올림
            if (!double.TryParse(right, out double ms))
                return null;

            uint delay = (uint)Math.Max(0, Math.Round(ms));
            return (frameName, delay);
        }
        #endregion

        #region ParseSignalEncodingType
        private int ParseSignalEncodingTypes(string[] lines, int startIndex)
        {
            string? currentType = null;

            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Signal_encoding_types 블록 종료
                if (line == "}" && currentType == null)
                    return i;

                // 타입 블록 시작: e.g. Eop_Current_DC_t {
                if (line.EndsWith("{"))
                {
                    currentType = line.Substring(0, line.Length - 1).Trim();
                    _encodingTypes[currentType] = new EncodingType();
                    continue;
                }

                // 타입 블록 종료
                if (line == "}" && currentType != null)
                {
                    currentType = null;
                    continue;
                }

                // physical_value, min, max, scale, offset, "unit" ;
                if (currentType != null && line.StartsWith("physical_value", StringComparison.OrdinalIgnoreCase))
                {
                    string s = line.Trim().TrimEnd(';').Trim();

                    // "physical_value," 제거
                    int idx = s.IndexOf(',');
                    if (idx < 0) continue;
                    s = s.Substring(idx + 1).Trim();

                    // min, max, scale, offset, "unit"
                    int lastComma = s.LastIndexOf(',');
                    if (lastComma < 0) continue;

                    string left = s.Substring(0, lastComma).Trim();
                    string unitPart = s.Substring(lastComma + 1).Trim();

                    string[] nums = left.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (nums.Length < 4) continue;

                    if (double.TryParse(nums[0].Trim(), out double min) &&
                        double.TryParse(nums[1].Trim(), out double max) &&
                        double.TryParse(nums[2].Trim(), out double scale) &&
                        double.TryParse(nums[3].Trim(), out double offset))
                    {
                        _encodingTypes[currentType].Min = min;
                        _encodingTypes[currentType].Max = max;
                        _encodingTypes[currentType].Scale = scale;
                        _encodingTypes[currentType].Offset = offset;
                        _encodingTypes[currentType].Unit = unitPart.Trim().Trim('"');
                    }
                }
            }

            return startIndex; // 비정상 종료 시
        }

        #endregion

        #region ParseSignalRepresentation
        private int ParseSignalRepresentation(string[] lines, int startIndex)
        {
            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line == "}") // 블록 종료
                    return i;

                // 예) Eop_Current_DC_t: Eop_Current_DC ;
                if (line.EndsWith(";") && line.Contains(':'))
                {
                    string s = line.TrimEnd(';').Trim();
                    var parts = s.Split(':', 2);
                    if (parts.Length < 2) continue;

                    string typeName = parts[0].Trim();
                    string right = parts[1].Trim();

                    // 시그널이 쉼표로 여러 개일 수도 있다고 가정
                    var sigNames = right.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(x => x.Trim());

                    if (!_encodingTypes.TryGetValue(typeName, out var enc))
                        continue; // 타입을 못 찾으면 스킵(또는 로깅)

                    foreach (var sigName in sigNames)
                    {
                        if (_linSignalsDict.TryGetValue(sigName, out var sig))
                        {
                            sig.MinValue = enc.Min;
                            sig.MaxValue = enc.Max;
                            sig.Scale = enc.Scale;
                            sig.Offset = enc.Offset;
                            sig.Unit = enc.Unit;
                        }
                        else
                        {
                            // 시그널이 아직 없는 경우: 필요 시 새로 만들거나 로깅
                            // _linSignalsDict[sigName] = new LDFLinSignal { SignalName = sigName, ... };
                        }
                    }
                }
            }

            return startIndex;
        }

        #endregion

        #region Common
        private int ParseMs(string value)
        {
            // "10 ms" or "0.1 ms"
            string numPart = value.ToLower().Replace("ms", "").Trim();

            // 소수점도 들어올 수 있음 → double로 먼저 파싱
            if (double.TryParse(numPart, out double ms))
            {
                return (int)Math.Round(ms); // 정수 밀리초로 변환
            }

            return 0;
        }

        private string GetValue(string line)
        {
            // "LIN_protocol_version = "2.1";"
            string[] parts = line.Split('=');
            if (parts.Length < 2) return "";

            string raw = parts[1].Trim();

            // 끝의 세미콜론 제거
            if (raw.EndsWith(";"))
                raw = raw.Substring(0, raw.Length - 1);

            // 따옴표 제거
            raw = raw.Trim().Trim('"');

            return raw;
        }

        private uint GetBaudRate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            string lower = value.ToLower().Trim();

            // "19.2 kbps", "19.2k"
            if (lower.EndsWith("kbps") || lower.EndsWith("k"))
            {
                string numPart = new string(value.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                if (double.TryParse(numPart, out double kbps))
                    return (uint)(kbps * 1000);
            }

            // "19200 bps"
            if (lower.EndsWith("bps"))
            {
                string numPart = new string(value.TakeWhile(c => char.IsDigit(c)).ToArray());
                if (uint.TryParse(numPart, out uint bps))
                    return bps;
            }

            // "19200"
            if (uint.TryParse(value, out uint pure))
                return pure;

            return 0;
        }

        private Encoding GetFileEncoding(string filePath)
        {
            using (var reader = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (reader.Length >= 2)
                {
                    byte[] bom = new byte[3];
                    reader.Read(bom, 0, 2);

                    // UTF-16 Little Endian
                    if (bom[0] == 0xFF && bom[1] == 0xFE)
                        return Encoding.Unicode;

                    // UTF-16 Big Endian
                    if (bom[0] == 0xFE && bom[1] == 0xFF)
                        return Encoding.BigEndianUnicode;

                    // UTF-8 BOM
                    if (reader.Length >= 3)
                    {
                        reader.Read(bom, 2, 1);
                        if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                            return Encoding.UTF8;
                    }
                }

                // 기본적으로 EUC-KR 반환
                return Encoding.GetEncoding("EUC-KR");
            }
        }
        #endregion


        #region Interface
        public string GetMasterName()
        {
            return _linMaster.Name;
        }
        public uint GetBaudRate()
        {
            return _baudRate;
        }
        public List<string> GetAllNodeName()
        {
            List<string> node = new List<string>();

            node.Add(_linMaster.Name);
            node.AddRange(_slaves);

            return node;
        }
        public List<ILdfLinFrame> GetAllLinFrame()
        {
            List<ILdfLinFrame> linFrames = new List<ILdfLinFrame>();

            foreach (var frame in _linFramesDict)
            {
                linFrames.Add(frame.Value);
            }

            return linFrames;
        }
        public List<ILdfLinFrame> GetLinFrame(string nodeName)
        {
            if(
                (_linMaster.Name != nodeName) &&
                (_slaves.Contains(nodeName) == false)
            ){
                throw new Exception("Slave Name is invalid");
            }

            List<ILdfLinFrame> linFrames = new List<ILdfLinFrame>();

            foreach (var frame in _linFramesDict)
            {
                if(frame.Value.Publisher == nodeName)
                {
                    linFrames.Add(frame.Value);
                }
            }

            return linFrames;
        }
        public List<ILdfScheduleTable> GetScheduleTables()
        {
            List<ILdfScheduleTable> scheduleTables = new List<ILdfScheduleTable>();

            foreach(var table in _scheduleTable)
            {
                scheduleTables.Add(table.Value);
            }

            return scheduleTables;
        }
        #endregion


        public string TestDebug()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"Protocol Version : {_protocol_version}\n");
            sb.Append($"Language Version : {_language_version}\n");
            sb.Append($"BaudRate : {_baudRate}\n");
            sb.Append($"\n");

            sb.Append($"Master : {_linMaster.Name}\n");
            foreach (var slave in _slaves)
            {
                sb.Append($"Slave : {slave}\n");
            }
            sb.Append($"\n");            

            foreach(var frame in _linFramesDict)
            {
                sb.Append($"Frame : {frame.Value.Name}, ID : {frame.Value.ID}\n");
            }
            sb.Append($"\n");

            foreach (var data in _scheduleTable)
            {
                sb.Append($"Table : {data.Value.Name}\n");

                foreach(var schedule in data.Value.Schedule)
                {
                    sb.Append($"-Frame : {schedule.Item1}, time : {schedule.Item2}\n");
                }
            }
            sb.Append($"\n");

            foreach (var type in _encodingTypes)
            {
                sb.Append($"Name : {type.Key}\n");
                sb.Append($"-min : {type.Value.Min}\n");
                sb.Append($"-max : {type.Value.Max}\n");
                sb.Append($"-offset : {type.Value.Offset}\n");
                sb.Append($"-scale : {type.Value.Scale}\n");
                sb.Append($"-Unit : {type.Value.Unit}\n");
            }
            sb.Append($"\n");

            foreach (var signal in _linSignalsDict)
            {
                sb.Append($"Signal : {signal.Value.SignalName}\n");
                sb.Append($"-min : {signal.Value.MinValue}\n");
                sb.Append($"-max : {signal.Value.MaxValue}\n");
                sb.Append($"-offset : {signal.Value.Offset}\n");
                sb.Append($"-scale : {signal.Value.Scale}\n");
                sb.Append($"-StartBit : {signal.Value.StartBit}\n");
                sb.Append($"-Length : {signal.Value.Length}\n");
                sb.Append($"-Unit : {signal.Value.Unit}\n");
            }
            sb.Append($"\n");

            return sb.ToString();
        }
    }
}
