using StockDevControl.StockModels;

namespace SI_DevCenter.ViewModels;

internal partial class MainViewModel
{
    private void ParsingAccountInfo(string strData)
    {
        _axHDFCommAgent.AccountInfos.Clear();

        byte[] byData = _krEncoder.GetBytes(strData);
        int nOffset = 0;

        int.TryParse(_krEncoder.GetString(byData, nOffset, 5), out int nAccCount);
        nOffset += 5;
        for (int i = 0; i < nAccCount; i++)
        {
            string 계좌번호 = _krEncoder.GetString(byData, nOffset, 11).Trim(); nOffset += 11;
            string 계좌명 = _krEncoder.GetString(byData, nOffset, 30).Trim(); nOffset += 30;
            string _계좌구분 = _krEncoder.GetString(byData, nOffset, 1).Trim(); nOffset += 1;
            AccountInfo.KIND 계좌구분 = _계좌구분 switch
            {
                "1" => AccountInfo.KIND.해외,
                "2" => AccountInfo.KIND.FX,
                "9" => AccountInfo.KIND.국내,
                _ => AccountInfo.KIND.UNKNOWN
            };
            _axHDFCommAgent.AccountInfos.Add(new(계좌번호, 계좌명, 계좌구분));
        }

        UpdateServerType();
    }

}

