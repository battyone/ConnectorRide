using System.Threading.Tasks;
using System.Web.Http;
using Knapcode.ConnectorRide.Web.Services;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Web.Controllers
{
    public class CommandsController : ApiController
    {
        public async Task<UploadResult> UpdateSchedulesAsync()
        {
            return await new CommandsService().UpdateSchedulesAsync();
        }
    }
}
