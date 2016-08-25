using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace WebServerHoster
{
    public class SimpleResponse
    {

        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
            #region extension to MIME type list
            {".asf", "video/x-ms-asf"},
            {".asx", "video/x-ms-asf"},
            {".avi", "video/x-msvideo"},
            {".bin", "application/octet-stream"},
            {".cco", "application/x-cocoa"},
            {".crt", "application/x-x509-ca-cert"},
            {".css", "text/css"},
            {".deb", "application/octet-stream"},
            {".der", "application/x-x509-ca-cert"},
            {".dll", "application/octet-stream"},
            {".dmg", "application/octet-stream"},
            {".ear", "application/java-archive"},
            {".eot", "application/octet-stream"},
            {".exe", "application/octet-stream"},
            {".flv", "video/x-flv"},
            {".gif", "image/gif"},
            {".hqx", "application/mac-binhex40"},
            {".htc", "text/x-component"},
            {".htm", "text/html"},
            {".html", "text/html"},
            {".ico", "image/x-icon"},
            {".img", "application/octet-stream"},
            {".iso", "application/octet-stream"},
            {".jar", "application/java-archive"},
            {".jardiff", "application/x-java-archive-diff"},
            {".jng", "image/x-jng"},
            {".jnlp", "application/x-java-jnlp-file"},
            {".jpeg", "image/jpeg"},
            {".jpg", "image/jpeg"},
            {".js", "application/x-javascript"},
            {".mml", "text/mathml"},
            {".mng", "video/x-mng"},
            {".mov", "video/quicktime"},
            {".mp3", "audio/mpeg"},
            {".mpeg", "video/mpeg"},
            {".mpg", "video/mpeg"},
            {".msi", "application/octet-stream"},
            {".msm", "application/octet-stream"},
            {".msp", "application/octet-stream"},
            {".pdb", "application/x-pilot"},
            {".pdf", "application/pdf"},
            {".pem", "application/x-x509-ca-cert"},
            {".pl", "application/x-perl"},
            {".pm", "application/x-perl"},
            {".png", "image/png"},
            {".prc", "application/x-pilot"},
            {".ra", "audio/x-realaudio"},
            {".rar", "application/x-rar-compressed"},
            {".rpm", "application/x-redhat-package-manager"},
            {".rss", "text/xml"},
            {".run", "application/x-makeself"},
            {".sea", "application/x-sea"},
            {".shtml", "text/html"},
            {".sit", "application/x-stuffit"},
            {".swf", "application/x-shockwave-flash"},
            {".tcl", "application/x-tcl"},
            {".tk", "application/x-tcl"},
            {".txt", "text/plain"},
            {".war", "application/java-archive"},
            {".wbmp", "image/vnd.wap.wbmp"},
            {".wmv", "video/x-ms-wmv"},
            {".xml", "text/xml"},
            {".xpi", "application/x-xpinstall"},
            {".zip", "application/zip"}
            #endregion
        };

        private bool _closed = false;
        public SimpleResponse(HttpListenerResponse res)
        {
            UnderlyingResponse = res;
        }

        public HttpListenerResponse UnderlyingResponse { get; }

        public void SendString(string data)
        {
            if (_closed) throw new SimpleHttpServerException("You can only send the response once");
            try
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                UnderlyingResponse.ContentType = "text/plain";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                UnderlyingResponse.AddHeader("Date", DateTime.Now.ToString("r"));
                UnderlyingResponse.OutputStream.Write(bytes, 0, bytes.Length);
                UnderlyingResponse.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception)
            {
                UnderlyingResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
            finally
            {
                UnderlyingResponse.OutputStream.Close();
                _closed = true;
            }
        }

        public void SendFile(string filepath)
        {
            if (_closed) throw new SimpleHttpServerException("You can only send the response once");
            try
            {
                Stream input = new FileStream(filepath, FileMode.Open);
                //Adding permanent http response headers
                string mime;
                UnderlyingResponse.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filepath), out mime) ? mime : "application/octet-stream";
                UnderlyingResponse.ContentLength64 = input.Length;
                UnderlyingResponse.AddHeader("Date", DateTime.Now.ToString("r"));
                UnderlyingResponse.AddHeader("Last-Modified", File.GetLastWriteTime(filepath).ToString("r"));

                byte[] buffer = input.Length < 16000 ? new byte[input.Length] : new byte[1024 * 16];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    UnderlyingResponse.OutputStream.Write(buffer, 0, nbytes);
                input.Close();

                UnderlyingResponse.StatusCode = (int)HttpStatusCode.OK;
                UnderlyingResponse.OutputStream.Flush();
            }
            catch (Exception)
            {
                UnderlyingResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                UnderlyingResponse.OutputStream.Close();
                _closed = true;
            }
        }
    }
}