namespace IF_LDFParser
{
    public enum ByteOrder
    {
        BigEndian = 0,
        LittleEndian = 1,
    }

    public enum ByteType
    {
        Unsigned,
        Signed,
    }

    public enum LinChecksumModel
    {
        Classic,
        Enhanced
    }
}
