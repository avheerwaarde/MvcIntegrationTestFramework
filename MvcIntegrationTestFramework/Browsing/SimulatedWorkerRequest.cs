using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Linq;
using System.Text.RegularExpressions;

namespace MvcIntegrationTestFramework.Browsing
{
    internal class SimulatedWorkerRequest : SimpleWorkerRequest
    {
        private HttpCookieCollection cookies;
        private readonly string httpVerbName;
        private readonly NameValueCollection formValues;
        private readonly NameValueCollection headers;
        private Stream file;
        private readonly string boundary;
        private string contentLength;
        private byte[] requestData;

        public SimulatedWorkerRequest(string page, string query, TextWriter output, HttpCookieCollection cookies, string httpVerbName, NameValueCollection formValues, NameValueCollection headers)
            : base(page, query, output)
        {
            this.cookies = cookies;
            this.httpVerbName = httpVerbName;
            this.formValues = formValues;
            this.headers = headers;
            this.contentLength = "0";
            this.file = new MemoryStream();

            string boundary = "boundary=";
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    if (headers[key].Contains(boundary))
                    {
                        this.boundary = headers[key].Substring(headers[key].IndexOf(boundary) + boundary.Length);
                    }
                }
            }
        }

        public override string GetHttpVerbName()
        {
            return httpVerbName;
        }

        public override string GetKnownRequestHeader(int index)
        {
            // Override "Content-Type" header for POST requests, otherwise ASP.NET won't read the Form collection
            //if (index == 12)
            //    if (string.Equals(httpVerbName, "post", StringComparison.OrdinalIgnoreCase))
            //        return "application/x-www-form-urlencoded";

            switch (index) {
                case 0x19:
                    return MakeCookieHeader();
                default:
                    if (headers == null)
                        return null;
                    return headers[GetKnownRequestHeaderName(index)];
            }
        }

        public override string GetUnknownRequestHeader(string name)
        {
            if(headers == null)
                return null;
            return headers[name];
        }

        public override string[][] GetUnknownRequestHeaders()
        {
            if (headers == null)
                return null;
            var unknownHeaders = from key in headers.Keys.Cast<string>()
                                 let knownRequestHeaderIndex = GetKnownRequestHeaderIndex(key)
                                 where knownRequestHeaderIndex < 0
                                 select new[] { key, headers[key] };
            return unknownHeaders.ToArray();
        }

        public override byte[] GetPreloadedEntityBody()
        {
            if(formValues == null)
                return base.GetPreloadedEntityBody();

            string fileName = "filename=";
            foreach (string key in formValues.Keys)
            {
                if (formValues[key].Contains(fileName)){
                   return GetPreloadedEntityBodyFileUpload();
                }
            }

            var sb = new StringBuilder();
            foreach (string key in formValues)
                sb.AppendFormat("{0}={1}&", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(formValues[key]));
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] GetPreloadedEntityBodyFileUpload()
        {
            //string logMessages = "------------------------------------------------\r\n"; 
            //logMessages += "Executing GetPreloadedEntityBody in SimulatedWorkerRequest\r\n"; 
            
            if (formValues == null) 
                return base.GetPreloadedEntityBody(); 
            
            //memory stream to hold the response body 
            MemoryStream memStream = new MemoryStream();

            //logMessages += "Establishing the boundary: \r\n--" + this.boundary + "\r\n";

            string beginBoundary = "\r\n--" + this.boundary + "\r\n";

            byte[] beginBoundaryBytes = System.Text.Encoding.ASCII.GetBytes(beginBoundary);

            //first, write out all the form fields into boundaries
            string formDataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            
            string fileName = "filename=";
            string fileParam = "";

            foreach (string key in formValues.Keys)
            {
                if (!formValues[key].Contains(fileName))
                {
                    memStream.Write(beginBoundaryBytes, 0, beginBoundaryBytes.Length);
                    string formItem = String.Format(formDataTemplate, key, formValues[key]);
                    
                    //logMessages += "Adding a form item----> " + formItem + "\r\n";
                    
                    byte[] formItemBytes = Encoding.UTF8.GetBytes(formItem);
                    memStream.Write(formItemBytes, 0, formItemBytes.Length);
                }
                else
                {
                    //this is the file parameter
                    fileParam = key;
                }
            }

            //write a boundary prior to the file data
            memStream.Write(beginBoundaryBytes, 0, beginBoundaryBytes.Length);
            string[] fileInfo = formValues[fileParam].Split(';');
            string header = String.Format("Content-Disposition: form-data; name=\"{0}\"; {1}\r\n{2}\r\n\r\n", fileParam, fileInfo[0], fileInfo[1]);
            
            //logMessages += "File Part Header----> " + header + "\r\n";
            
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);
            memStream.Write(headerBytes, 0, headerBytes.Length);

            //write the file into the memory stream in 4kb chunks
            byte[] fBuffer = new byte[4096];
            int bytesRead = 0;
            
            StreamWriter writer = new StreamWriter(this.file);
            writer.Write(fileInfo[2]);
            writer.Flush();
            this.file.Position = 0;

            using (this.file)
            {
                //read the file data into a buffer in a chunk
                //logMessages += "Can the file be read? --> " + this.file.CanRead + "\r\n";

                while ((bytesRead = this.file.Read(fBuffer, 0, fBuffer.Length)) != 0)
                {
                    //write the chunk onto the memory stream
                    //logMessages += "Read " + bytesRead + " bytes onto the memory stream\r\n";
                    memStream.Write(fBuffer, 0, bytesRead);
                }
            }

            //set up the ending boundary
            byte[] endingBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + this.boundary + "--\r\n");
            memStream.Write(endingBoundaryBytes, 0, endingBoundaryBytes.Length);

            //read the request body from the memory stream to a byte array
            //logMessages += "Memstream length (content length) = " + memStream.Length + "\r\n";
            byte[] requestBody = new byte[memStream.Length];

            //memstream.Read(request_body, 0, (int)memstream.Length);
            requestBody = memStream.ToArray();
            memStream.Close();

            //logMessages += "Request Body (byte array) length: " + requestBody.Length + "\r\n";

            //save the content length
            this.contentLength = Convert.ToString(requestBody.Length);
            
            //logMessages += "\r\n--------------------------------------------------------------\r\n";
            //logMessages += "\r\n\tRequest Body:\r\n\r\n";

            //FileStream output = new FileStream("C:\\temp\\output.txt", FileMode.Create);
            
            //byte[] messages = Encoding.UTF8.GetBytes(logMessages);
            //output.Write(messages, 0, messages.Length);
            //output.Write(requestBody, 0, requestBody.Length);

            //logMessages = "\r\n\r\nRequest Body (byte array) length: " + requestBody.Length + "\r\n";
            //byte[] messages2 = Encoding.UTF8.GetBytes(logMessages);
            //output.Write(messages2, 0, messages2.Length);
            //output.Close();

            this.requestData = new byte[requestBody.Length];
            this.requestData = requestBody;

            return requestBody;
        }

        private string MakeCookieHeader()
        {
            if((cookies == null) || (cookies.Count == 0))
                return null;
            var sb = new StringBuilder();
            foreach (string cookieName in cookies)
                sb.AppendFormat("{0}={1};", cookieName, cookies[cookieName].Value);
            return sb.ToString();
        }
    }
}