namespace SI_DevCenter.Models.StructModels;

internal class STRUCT_FUTCODE
{
    public enum Kind
    {
        종목코드,
        표준코드,
        한글종목명,
        기초자산종목코드,
        소수점,
        호가단위,
        거래승수,
        스프레드기준종목구분코드,
        근월물코드,
        원월물코드,
    }
    public static int[] PerSize =
    [
        08,
        12,
        30,
        06,
        02,
        05,
        21,
        01,
        08,
        08,
    ];

    public static int[] PerIndex;
    static STRUCT_FUTCODE()
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
