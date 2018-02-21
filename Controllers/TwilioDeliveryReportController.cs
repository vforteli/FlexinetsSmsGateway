using Flexinets.Core.Communication.Sms;
using Flexinets.Core.Database.Models;
using FlexinetsSmsGateway.Models;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Twilio.AspNet.Core;

namespace FlexinetsSmsGateway.Controllers
{
    public class TwilioDeliveryReportController : Controller
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(TwilioDeliveryReportController));
        private readonly FlexinetsContext _flexinetsContext;
        private readonly ISmsGateway _smsGateway;


        public TwilioDeliveryReportController(ISmsGateway smsGateway, FlexinetsContext flexinetsContext)
        {
            _flexinetsContext = flexinetsContext;
            _smsGateway = smsGateway;
        }


        [AllowAnonymous]
        [HttpPost("api/TwilioDeliveryReport")]
        public async Task<IActionResult> TwilioDeliveryReport(TwilioDeliveryReportModel deliveryReport)
        {
            try
            {
                _log.Debug($"Received delivery report for sms {deliveryReport.MessageSid} with status {deliveryReport.SmsStatus}");
                var message = await _smsGateway.GetSmsReportAsync(deliveryReport.MessageSid);

                _flexinetsContext.SmsLogEntries.Add(new SmsLogEntries
                {
                    EventTimestamp = DateTime.UtcNow,
                    DestinationAddress = message.To.ToString(),
                    Orgaddr = message.From.ToString(),
                    Text = message.Body,
                    Smsid = message.Sid,
                    Inbound = false,
                    Status = deliveryReport.SmsStatus,
                    SmsTimestamp = message.DateSent
                });
                await _flexinetsContext.SaveChangesAsync();

                _log.Debug($"Saved sms with id {message.Sid}");
                if (deliveryReport.SmsStatus == "sent")
                {
                    _log.Info($"Sms with id {message.Sid} from {message.From} to {message.To} : {message.Body}");
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                _log.Warn($"Ignoring duplicate sms delivery report for id {deliveryReport.MessageSid}");
            }
            catch (Exception ex)
            {
                _log.Error("Failed receiving delivery report", ex);
                return BadRequest(ex);
            }

            return new TwiMLResult();
        }
    }
}