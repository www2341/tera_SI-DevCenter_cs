namespace SI_DevCenter.Models.StructModels;

internal class STRUCT_0082
{
    public enum Kind
    {
        Data구분,
        종목코드,
        체결시간,
        시가,
        고가,
        저가,
        체결가,
        전일대비구분,
        전일대비,
        전일대비등락율,
        직전대비구분,
        체결량,
        체결구분,
        누적거래량,
        누적거래대금,
        상승거래량,
        하락거래량,
        상승건수,
        하락건수,
        매도호가,
        매수호가,
        매도잔량,
        매수잔량,
        거래번호,
        진법변환전체결가,
        영업일,
        종목코드1,
        기준체결시간,
        국내일자,
        전일대비거래량등락율,
    }
    public static int[] PerSize =
    [
        4,
        32,
        6,
        10,
        10,
        10,
        10,
        1,
        10,
        6,
        1,
        6,
        1,
        10,
        12,
        10,
        10,
        6,
        6,
        10,
        10,
        8,
        8,
        8,
        15,
        8,
        32,
        6,
        8,
        6
    ];

    public static int[] PerIndex;
    static STRUCT_0082()
    {
        PerIndex = new int[PerSize.Length];
        int Sum = 0;
        for (int i = 0; i < PerSize.Length; i++)
        {
            PerIndex[i] = Sum;
            Sum += PerSize[i];
        }
    }
}
