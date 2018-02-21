using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FlexinetsSmsGateway.Models
{
    /// <summary>
    /// https://www.twilio.com/docs/api/twiml/sms/twilio_request
    /// </summary>
    public class TwilioIncomingSmsModel
    {
        /// <summary>
        /// A 34 character unique identifier for the message. May be used to later retrieve this message from the REST API.
        /// </summary>
        public String MessageSid
        {
            get;
            set;
        }

        /// <summary>
        /// The 34 character id of the Account this message is associated with.
        /// </summary>
        public String AccountSid
        {
            get;
            set;
        }

        /// <summary>
        /// The phone number that sent this message.
        /// </summary>
        public String From
        {
            get;
            set;
        }

        /// <summary>
        /// The phone number of the recipient.
        /// </summary>
        public String To
        {
            get;
            set;
        }

        /// <summary>
        /// The text body of the message. Up to 1600 characters long.
        /// </summary>
        public String Body
        {
            get;
            set;
        }
    }

}