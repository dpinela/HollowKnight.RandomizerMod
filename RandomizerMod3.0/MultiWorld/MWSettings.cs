using SereCore;

namespace RandomizerMod.MultiWorld
{
    public class MWSettings : BaseSettings
    {
        public string URL
        {
            get => GetString("127.0.0.1");
            set => SetString(value);
        }

        public int Port
        {
            get => GetInt(38281);
            set => SetInt(value);
        }

        public string UserName
        {
            get => GetString("Lazy_Person");
            set => SetString(value);
        }

        public string Token
        {
            get => GetString("");
            set => SetString(value);
        }
    }
}
