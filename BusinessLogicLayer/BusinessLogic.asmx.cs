using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Net;
using System.Text;
using System.IO;
using System.Net.Mail;
using System.Xml;

namespace BusinessLogicLayer
{
    /// <summary>
    /// Summary description for BusinessLogic
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class BusinessLogic : System.Web.Services.WebService
    {

        List<String> ZoneForTypeA = new List<string>()
        {
            "A1",
            "A2",
            "B1",
            "B2",
            "C1",
            "D2",
            "D3",
        };
        List<String> ZoneForTypeB = new List<string>()
        {
            "A1",
            "A2",
            "B1",
            "B2",
            "C1",
            "D3",
        };


        [WebMethod]
        public string GetLastLocation(String RFID)
        {

            var Url = @"http://localhost:52663/api/PassengerAreaAccesses/LastAccess/" + RFID.ToString();
            Uri serviceURL = new Uri(Url);

            HttpWebRequest ServiceRequest = (HttpWebRequest)WebRequest.Create(serviceURL);
            ServiceRequest.Method = "Get";
            HttpWebResponse serviceResponse;
            try
            {
                //receive the http response.
                serviceResponse = (HttpWebResponse)ServiceRequest.GetResponse();
            }
            catch
            {
                return "No Success Response Received (Not Found)";

            }

            var ZoneId = serviceResponse.GetResponseHeader("ZoneId");
            var RFID_Id = serviceResponse.GetResponseHeader("RFID");
            var DateTime = serviceResponse.GetResponseHeader("LastAccessTime");

            return "ZoneId: " + ZoneId + ", RFID : " + RFID + ", LastAccess : " + DateTime;
        }


        [WebMethod]
        public string GetAccess(String RFID, String ZoneId)
        {

            var Url = @"http://localhost:52663/api/PassengerDetails/ByRfid/" + RFID.ToString();
            Uri serviceURL = new Uri(Url);

            HttpWebRequest ServiceRequest = (HttpWebRequest)WebRequest.Create(serviceURL);
            ServiceRequest.Method = "Get";
            HttpWebResponse serviceResponse;
            try
            {
                //receive the http response.
                serviceResponse = (HttpWebResponse)ServiceRequest.GetResponse();
            }
            catch
            {
                return "No Success Response Received";
            }

            var passengerType = serviceResponse.GetResponseHeader("PassengerType");
            var PNR = serviceResponse.GetResponseHeader("PNR");
            var name = serviceResponse.GetResponseHeader("Name");
            string tracking = serviceResponse.GetResponseHeader("Tracking").ToString();
            String access = "Denied";

            if (passengerType.Equals("A"))
            {
                if (ZoneForTypeA.Contains(ZoneId))
                {
                    access = "Sucess";
                }

            }
            else if (passengerType.Equals("B"))
            {
                if (ZoneForTypeB.Contains(ZoneId))
                {
                    access = "Sucess";
                }
            }

            if (tracking.Equals("True"))
            {
                access = "Denied";
            }

            if (access.Equals("Sucess"))
            {

                var bytes = Encoding.ASCII.GetBytes("");
                var putUrl = @"http://localhost:52663/api/PassengerAreaAccesses/put/" + RFID.ToString() + "/" + ZoneId.ToString();
                using (WebClient client = new WebClient())
                {
                    byte[] response = client.UploadData(putUrl, "PUT", bytes);
                    string result = Encoding.ASCII.GetString(response);
                }

            }

            return access;
        }




        [WebMethod]
        public string GetLuggageStatus(string rfid)
        {
            var uri = "http://localhost:52663/GetLuggageStage/" + rfid;
            HttpWebRequest serviceRequest = (HttpWebRequest)WebRequest.Create(uri);
            serviceRequest.Method = "GET";
            HttpWebResponse serviceResponse;
            try
            {
                //receive the http response.
                serviceResponse = (HttpWebResponse)serviceRequest.GetResponse();
            }
            catch
            {
                return "No Success Response Received";
            }

            string answer = "Your luggage status: ";
            string stage = serviceResponse.GetResponseHeader("LuggageStage").ToString();
            answer += stage + ". ";
            if (stage.Equals("AtConveyorBelt"))
            {
                var uri_belt = "http://localhost:52663/GetBeltInfo/" + rfid;
                HttpWebRequest serviceRequest_belt = (HttpWebRequest)WebRequest.Create(uri_belt);
                serviceRequest.Method = "GET";
                HttpWebResponse serviceResponse_belt;
                try
                {
                    //receive the http response.
                    serviceResponse_belt = (HttpWebResponse)serviceRequest_belt.GetResponse();
                }
                catch
                {
                    return "No Success Response Received";
                }

                answer += "Your luggage is available at: " + serviceResponse_belt.GetResponseHeader("BeltInfo").ToString();
            }

            return answer;

        }



        [WebMethod]
        public string UpdateLuggageLocation(string lugg_rfid, string zone_id)
        {
            var uri = "http://localhost:52663/UpdateLuggageLocation/" + lugg_rfid + "/" + zone_id;
            HttpWebRequest serviceRequest = (HttpWebRequest)WebRequest.Create(uri);
            serviceRequest.Method = "PUT";
            serviceRequest.ContentLength = 0;
            HttpWebResponse serviceResponse;
            try
            {
                //receive the http response.
                serviceResponse = (HttpWebResponse)serviceRequest.GetResponse();
            }
            catch
            {
                return "No Success Response Received";
            }
            if (serviceResponse.StatusCode != HttpStatusCode.OK)
            {
                return "Bad request, something is wrong in the luggage tag update";
            }


            string email = serviceResponse.GetResponseHeader("PassengerEmail").ToString();
            string stage = serviceResponse.GetResponseHeader("LuggageStatus").ToString();

            string answer = sendEmail(email, stage);
            //string answer = "Email ID: "+ serviceResponse.GetResponseHeader("PassengerEmail").ToString();

            return answer;
        }



        [WebMethod]
        public  String GetFlightDetails(string rfid)
        {
            var uri = "http://localhost:52663/GetFlightDetails/" + rfid;
            HttpWebRequest serviceRequest = (HttpWebRequest)WebRequest.Create(uri);
            serviceRequest.Method = "GET";
            serviceRequest.Accept = "application/json";
            HttpWebResponse serviceResponse;
            try
            {
                //receive the http response.
                serviceResponse = (HttpWebResponse)serviceRequest.GetResponse();
            }
            catch
            {
                return "No Success Response Received";
            }

            Stream receiveStream = serviceResponse.GetResponseStream();
            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader readStream = new StreamReader(receiveStream, encode, true);
            string serviceResult = readStream.ReadToEnd();

            return serviceResult;

        }


        private string sendEmail(string email, string stage)
        {
            // This address must be verified with Amazon SES.
            String FROM = "rajeevsuri040989@gmail.com";
            String FROMNAME = "AirportAuthoritySecurity";

            // this address must be verified.
            String TO = "pbhumkar3@gmail.com";

            // Replace smtp_username with your Amazon SES SMTP user name.
            String SMTP_USERNAME = "AKIAJS6KKMDZVJQ64HCQ";

            // Replace smtp_password with your Amazon SES SMTP user name.
            String SMTP_PASSWORD = "BE8RMVpXu//pjJmjV0KElPaOKP+v7tDpQVdEYj62aOvZ";


            // If you're using Amazon SES in a region other than US West (Oregon), 
            // replace email-smtp.us-west-2.amazonaws.com with the Amazon SES SMTP  
            // endpoint in the appropriate AWS Region.
            String HOST = "email-smtp.us-west-2.amazonaws.com";

            // The port you will connect to on the Amazon SES SMTP endpoint. We
            // are choosing port 587 because we will use STARTTLS to encrypt
            // the connection.
            int PORT = 587;

            // The subject line of the email
            String SUBJECT =
                "Your Luggage Status [From Airport Authorities]";

            // The body of the email
            String BODY =
                "<p> Your luggage status is " + stage + ".</p>";

            // Create and build a new MailMessage object
            MailMessage message = new MailMessage();
            message.IsBodyHtml = true;
            message.From = new MailAddress(FROM, FROMNAME);
            message.To.Add(new MailAddress(TO));
            message.Subject = SUBJECT;
            message.Body = BODY;
            // Comment or delete the next line if you are not using a configuration set
            //message.Headers.Add("X-SES-CONFIGURATION-SET", CONFIGSET);

            using (var client = new System.Net.Mail.SmtpClient(HOST, PORT))
            {
                // Pass SMTP credentials
                client.Credentials =
                    new NetworkCredential(SMTP_USERNAME, SMTP_PASSWORD);

                // Enable SSL encryption
                client.EnableSsl = true;

                // Try to send the message. Show status in console.
                try
                {
                    //Console.WriteLine("Attempting to send email...");
                    client.Send(message);
                    //Console.WriteLine("Email sent!");
                }
                catch (Exception ex)

                {
                    return "some error" + ex.Message;
                    //Console.WriteLine("The email was not sent.");
                    //Console.WriteLine("Error message: " + ex.Message);
                }
                //Console.ReadKey();
            }


            return "email was sent";




        }
















    }
}
