using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WebServerHoster
{
    /// <summary>
    /// 
    /// </summary>
    public class SimpleResponse
    {
        protected static readonly IDictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
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

        protected bool _closed = false;
        public SimpleResponse(HttpListenerResponse res)
        {
            UnderlyingResponse = res;
        }

        public HttpListenerResponse UnderlyingResponse { get; }

        /// <summary>
        /// Sends data as text
        /// </summary>
        /// <param name="data">The text data to send</param>
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

        /// <summary>
        /// Sends file as response
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        public void SendFile(string filepath, string mime = null)
        {
            if (_closed) throw new SimpleHttpServerException("You can only send the response once");
            try
            {
                Stream input = new FileStream(filepath, FileMode.Open);
                //Adding permanent http response headers
                if (mime == null) UnderlyingResponse.ContentType = MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime) ? mime : "application/octet-stream";
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

        /// <summary>
        /// "Renders" an .ecs file, which is an html file extension, used for dynamic content. 
        /// All tags in parameters will be replaced in the page
        /// </summary>
        /// <param name="pagesIndesEcs">The path of the .ecs file</param>
        /// <param name="parameters">The parameter object used when replacing data</param>
        public void RenderPage(string pagesIndesEcs, RenderParams parameters)
        {
            if (_closed) throw new SimpleHttpServerException("You can only send the response once");
            try
            {
                var pageData = File.ReadAllText(pagesIndesEcs, Encoding.UTF8);
                var sb = new StringBuilder(pageData);
                foreach (var parPair in parameters)
                {
                    sb.Replace(parPair.Key, parPair.Value);
                }
                pageData = sb.ToString();
                UnderlyingResponse.ContentType = "text/html";
                UnderlyingResponse.ContentLength64 = pageData.Length;
                UnderlyingResponse.AddHeader("Date", DateTime.Now.ToString("r"));
                UnderlyingResponse.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
                var pageBytes = Encoding.UTF8.GetBytes(pageData);
                UnderlyingResponse.OutputStream.Write(pageBytes, 0, pageBytes.Length);
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