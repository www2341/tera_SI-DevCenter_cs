namespace SI_DevCenter.Helpers
{
    internal interface IAppRegistry
    {
        public T GetValue<T>(string SectionName, string KeyName, T DefValue);
        public bool SetValue<T>(string SectionName, string KeyName, T Value);
    }
}
