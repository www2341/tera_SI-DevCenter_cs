namespace SI_DevCenter.Models.StructModels;

internal class STRUCT_0065
{
    public enum Kind
    {
        Data구분,
        종목코드,
        체결시간,
        사인,
        전일대비,
        등락율,
        현재가,
        시가,
        고가,
        저가,
        체결구분,
        체결량,
        누적거래량,
        거래량전일대비,
        거래량등락율,
        누적거래대금,
        매도누적체결량,
        매도누적체결건수,
        매수누적체결량,
        매수누적체결건수,
        체결강도,
        매도호가1,
        매수호가1,
        미결제약정수량,
        미결제약직전대비,
        미결제약정전일대비,
        KOSPI200지수,
        이론가,
        괴리도,
        괴리율,
        시장BASIS,
        이론BASIS,
        장운영정보,
        전일동시간대거래량,
    }
    public static int[] PerSize =
    [
        4,
        8,
        6,
        1,
        6,
        6,
        6,
        6,
        6,
        6,
        1,
        6,
        12,
        12,
        8,
        12,
        12,
        8,
        12,
        8,
        9,
        6,
        6,
        8,
        8,
        8,
        6,
        6,
        6,
        6,
        6,
        6,
        2,
        12
    ];

    public static int[] PerIndex;
    static STRUCT_0065()
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
