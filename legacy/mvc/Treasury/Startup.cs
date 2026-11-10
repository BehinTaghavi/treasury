using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CSD.Treasury.UI.Startup))]
namespace CSD.Treasury.UI
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
