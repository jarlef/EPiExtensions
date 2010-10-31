using EPiServer.PlugIn;

namespace JarleF.EPiElmah.Admin
{
    [GuiPlugIn(DisplayName = "Error Log", Area = PlugInArea.AdminMenu, Url = "~/console/cms/admin/elmah.axd")]
    public partial class ElmahPlugin : EPiServer.UserControlBase
    {
        public void Test()
        {
            
            
        }
    }
}
