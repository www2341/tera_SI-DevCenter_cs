namespace SI_DevCenter.Models.StructModels;

internal class STRUCT_JMCODE
{
    public enum Kind
    {
        종목코드,
        거래소,
        품목인덱스코드,
        품목코드,
        거래소번호,
        소수점정보,
        소수점정보2,
        계약크기,
        TickSize,
        TickValue,
        거래승수,
        진법,
        Full종목명,
        Full종목명한글,
        최근월물,
        거래가능여부,
        신규거래제한일,
        최초거래일,
        최종거래일,
        만기일,
        잔존일수,
        호가방식,
        상하한폭비율,
        기준가,
        상한가,
        하한가,
        신규주문증거금,
        유지증거금,
        결제통화코드,
        BaseCrcCd,
        CounterCrcCd,
        PipCost,
        매수이자,
        매도이자,
        RoundLots,
        진법자리수,
        decimalchiper,
        전일거래량,
    }
    public static int[] PerSize =
    [
        32,
        5,
        4,
        5,
        5,
        5,
        5,
        20,
        20,
        20,
        20,
        10,
        32,
        32,
        1,
        1,
        8,
        8,
        8,
        8,
        4,
        30,
        6,
        20,
        20,
        20,
        20,
        20,
        3,
        3,
        3,
        20,
        20,
        20,
        6,
        10,
        5,
        10,
    ];

    public static int[] PerIndex;
    static STRUCT_JMCODE()
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
