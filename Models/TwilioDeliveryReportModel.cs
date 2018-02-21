using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlexinetsSmsGateway.Models
{
    public class TwilioDeliveryReportModel
    {
        public String MessageSid
        {
            get;
            set;
        }
        public String SmsStatus
        {
            get;
            set;
        }
    }
}