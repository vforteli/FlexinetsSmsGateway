using Flexinets.Common;
using Flexinets.Core.Communication.Sms;
using Flexinets.Core.Database.Models;
using FlexinetsSmsGateway.Models;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio.AspNet.Core;

namespace FlexinetsSmsGateway.Controllers
{
    public class TwilioIncomingController : Controller
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(TwilioIncomingController));
        private readonly FlexinetsContext _flexnetsContext;
        private readonly ISmsGateway _smsGateway;
        private readonly ServiceBusConnectionStringBuilder _serviceBusConnectionString;
        private readonly String _echoNumber;


        public TwilioIncomingController(ISmsGateway smsGateway, FlexinetsContext flexinetsContext, IConfiguration configuration)
        {
            _flexnetsContext = flexinetsContext;
            _smsGateway = smsGateway;
            _serviceBusConnectionString = new ServiceBusConnectionStringBuilder(configuration.GetConnectionString("ServiceBusConnectionString"));
            _echoNumber = configuration["Twilio:echonumber"];
        }


        [AllowAnonymous]
        [HttpPost("api/TwilioIncoming")]
        public async Task<IActionResult> TwilioIncoming()
        {
            var dictionary = Request.Form.ToDictionary(o => o.Key, o => o.Value.ToString());
            var messageAuthenticator = Request.Headers.SingleOrDefault(o => o.Key == "X-Twilio-Signature").Value.FirstOrDefault();
            if (!_smsGateway.ValidateMessage(Request.GetDisplayUrl(), dictionary, messageAuthenticator))
            {
                _log.Warn("Invalid message authenticator, check twilio logs");
                return BadRequest("Invalid message authenticator");
            }

            // cannot use frombody because then the parameters are not available for validating message authenticator
            var model = new TwilioIncomingSmsModel
            {
                AccountSid = dictionary.SingleOrDefault(o => o.Key == "AccountSid").Value,
                Body = dictionary.SingleOrDefault(o => o.Key == "Body").Value,
                From = dictionary.SingleOrDefault(o => o.Key == "From").Value,
                To = dictionary.SingleOrDefault(o => o.Key == "To").Value,
                MessageSid = dictionary.SingleOrDefault(o => o.Key == "MessageSid").Value,
            };

            _log.Info($"Incoming sms {model.MessageSid} from {model.From} to {model.To}");

            try
            {
                model.From = Phonenumber.Parse(model.From);
                model.To = Phonenumber.Parse(model.To);

                _flexnetsContext.SmsLogEntries.Add(new SmsLogEntries
                {
                    EventTimestamp = DateTime.UtcNow,
                    DestinationAddress = model.To,
                    Orgaddr = model.From,
                    Text = model.Body,
                    Smsid = model.MessageSid,
                    Inbound = true,
                    Status = "RECEIVED",
                    SmsTimestamp = DateTime.UtcNow
                });
                await _flexnetsContext.SaveChangesAsync();


                _log.Info($"Saved incoming sms with id {model.MessageSid}");
                _log.Info($"Incoming SMS from {model.From} to {model.To} : {model.Body}");

                if (model.Body.ToUpper() == "A" || model.Body.ToUpper() == "\"A\"" || model.Body.ToUpper() == "\'A\'")
                {
                    _log.Info("Handling limit extension");
                    var queueClient = new QueueClient(_serviceBusConnectionString);
                    await queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes(model.From)));
                }
                if (model.Body == "echo?" && model.From == _echoNumber)
                {
                    _log.Info("Sending echo reply");
                    await _smsGateway.SendSmsAsync("echo!", model.From);
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                _log.Warn($"Ignoring duplicate sms delivery report for id {model.MessageSid}");
            }
            catch (Exception ex)
            {
                _log.Error("Failed receiving incoming sms", ex);
                return BadRequest(ex);
            }

            return new TwiMLResult();
        }
    }
}