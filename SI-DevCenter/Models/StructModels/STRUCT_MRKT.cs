namespace SI_DevCenter.Models.StructModels;

internal class STRUCT_MRKT
{
    public enum Kind
    {
        상품군,
        거래소코드,
        시장구분코드,
        상품명영문,
        상품명한글,
        RESERVED
    }
    public static int[] PerSize =
    [
        20,
        5,
        3,
        50,
        50,
        12
    ];

    public static int[] PerIndex;
    static STRUCT_MRKT()
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
