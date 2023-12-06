namespace SI_DevCenter.Models
{
    internal enum REQKIND_MASTER
    {
        주문,
        조회,
        실시간,
    }
    internal enum REQKIND_MAIN
    {
        국내,
        해외,
        FX,
        공통,
    }
    internal enum REQKIND_SUB
    {
        TR,
        FID,
        시세,
        주문,
        None,
    }
    internal enum REQKIND_FUNC
    {
        CommJumunSvr,
        CommRqData,
        CommFIDRqData,
        CommSetJumunChe,
        CommSetBroad,
        None,
    }
}
