using SI.Component.Models;
using System.Text;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    private byte[] GetPropertyInputsBuf(IList<StockDevControl.Models.PropertyItem> PropertyItems, bool ShowLog, ref string errorMsg)
    {
        List<byte> all_bytes = [];
        foreach (var prop in PropertyItems)
        {
            var ansi_value = _krEncoder.GetBytes(prop.Value);
            int nRemByteCount = 0;
            if (prop.N > 0)
                nRemByteCount = prop.N - ansi_value.Length;
            if (nRemByteCount < 0)
            {
                if (ShowLog)
                    errorMsg = $"{prop.Name}값은 {prop.N}과 같거나 작아야 합니다.";
                return Array.Empty<byte>();
            }

            all_bytes.AddRange(ansi_value);
            if (nRemByteCount > 0)
                all_bytes.AddRange(SpaceArray(nRemByteCount));
        }

        return all_bytes.ToArray();

    }

    private static byte[] SpaceArray(int length)
    {
        byte[] bytes = new byte[length];
        for (int i = 0; i < length; i++)
        {
            bytes[i] = (byte)' ';
        }
        return bytes;
    }

    private static string SpaceText(int length) => _krEncoder.GetString(SpaceArray(length));

    private static byte[] MakeFixedBuf(string text, int byteLength)
    {
        var ansi_value = _krEncoder.GetBytes(text);

        byte[] buf = new byte[byteLength];
        for (int i = 0; i < byteLength; i++)
        {
            if (i < ansi_value.Length)
                buf[i] = ansi_value[i];
            else buf[i] = (byte)' ';
        }
        return buf;
    }

    private static string MakeFixedText(string text, int CharLength)
    {
        if (text.Length == CharLength) return text;
        if (text.Length > CharLength) return text.Substring(0, CharLength);

        return text + SpaceText(CharLength - text.Length);
    }

    private static string GetFidNames(int Count)
    {
        StringBuilder stringBuilder = new();

        for (int i = 0; i < Count; i++)
        {
            _ = stringBuilder.AppendFormat("{0:d3}", i);
        }

        return stringBuilder.ToString();
    }

    private static ChartRound GetChartRoundFromString(string value)
    {
        return value switch
        {
            "일" => ChartRound.일,
            "분" => ChartRound.분,
            "틱" => ChartRound.틱,
            _ => ChartRound.일,
        };
    }
}

