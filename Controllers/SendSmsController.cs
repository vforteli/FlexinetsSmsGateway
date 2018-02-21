using Flexinets.Core.Communication.Sms;
using FlexinetsSmsGateway.Models;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlexinetsSmsGateway.Controllers
{
    [Authorize(Roles = "Sms")]
    public class SendSmsController : Controller
    {
        private readonly ISmsGateway _smsGateway;
        private readonly ILog _log = LogManager.GetLogger(typeof(SendSmsController));


        public SendSmsController(ISmsGateway smsGateway)
        {
            _smsGateway = smsGateway;
        }


        public IActionResult Post([FromBody]SendSmsModel sms)
        {
            var id = _smsGateway.SendSmsAsync(sms.Message, sms.To);
            _log.Info($"Sent sms with id {id} to {sms.To}");
            return Ok(id);
        }
    }
}
