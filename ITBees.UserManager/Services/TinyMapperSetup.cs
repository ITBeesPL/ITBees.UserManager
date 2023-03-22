using ITBees.FAS.ApiInterfaces.MyAccounts;
using ITBees.Models.MyAccount;
using Nelibur.ObjectMapper;

namespace ITBees.UserManager.Services
{
    public class TinyMapperSetup
    {
        public static void ConfigureMappings()
        {
            var m = new MyAccount();

            TinyMapper.Bind<MyAccount, MyAccountVm>(config =>
            {
                config.Bind(x => x.Companies, dst => dst.Companies);
                config.Bind(x => x.Language.Code, dst => dst.Language);
            });
        }
    }
}