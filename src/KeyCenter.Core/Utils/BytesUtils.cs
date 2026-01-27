namespace KeyCenter.Core.Utils;

public class BytesUtils
{
    //逆转字节顺序
    public static byte[] ReveseByteOrder(byte[] data)
    {
        byte[] newData = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            newData[data.Length - i - 1] = data[i];
        }
        return newData;
    }

    //得到大端字节序的字节数组
    public static long GetInt64FromBytesWithBigEndian(byte[] buffer)
    {
        if (BitConverter.IsLittleEndian)
            buffer = ReveseByteOrder(buffer);
        return BitConverter.ToInt64(buffer, 0);
    }

    //得到大端字节序的字节数组
    public static byte[] GetBytesWithBigEndian(long number)
    {
        byte[] data = BitConverter.GetBytes(number);
        if (BitConverter.IsLittleEndian)
            data = ReveseByteOrder(data);
        return data;
    }

    //从16进制字节字符串转换为字节数组
    public static byte[] GetBytesFromByteString(string data)
    {
        if (data.Length % 2 != 0)
            throw new FormatException("字符个数非2的倍数！");
        byte[] buffer = new byte[data.Length / 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Byte.Parse(data.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
        }
        return buffer;
    }
}
