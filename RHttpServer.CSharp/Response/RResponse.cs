using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using RHttpServer.Plugins;
using RPlugin.RHttpServer;
using IJsonConverter = RHttpServer.Plugins.IJsonConverter;
using IPageRenderer = RHttpServer.Plugins.IPageRenderer;
using RPluginCollection = RHttpServer.Plugins.RPluginCollection;

namespace RHttpServer.Response
{
    /// <summary>
    ///     Class representing the reponse to a clients request
    /// </summary>
    public class RResponse
    {
        protected static readonly IDictionary<string, string> MimeTypes =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
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
                {".json", "text/json"},
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

        internal RResponse(HttpListenerResponse res, RPluginCollection rPluginCollection)
        {
            UnderlyingResponse = res;
            Plugins = rPluginCollection;
        }

        protected bool Closed;

        public RPluginCollection Plugins { get; }


        /// <summary>
        ///     The underlying HttpListenerResponse
        ///     This implementation of RResponse is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerResponse UnderlyingResponse { get; }

        public void AddHeader(string fieldName, string fieldValue)
        {
            UnderlyingResponse.AddHeader(fieldName, fieldValue);
        }

        /// <summary>
        ///     Redirects the client to a given path or url
        /// </summary>
        /// <param name="redirectPath">The path or url to redirect to</param>
        public void Redirect(string redirectPath)
        {
            UnderlyingResponse.Redirect(redirectPath);
            UnderlyingResponse.Close();
        }

        /// <summary>
        ///     Sends data as text
        /// </summary>
        /// <param name="data">The text data to send</param>
        /// <param name="status">The status code for the response</param>
        public void SendString(string data, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            var bytes = Encoding.UTF8.GetBytes(data);
            try
            {
                UnderlyingResponse.ContentType = "text/plain";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                UnderlyingResponse.OutputStream.Write(bytes, 0, bytes.Length);
                UnderlyingResponse.AddHeader("X-Powered-By", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.StatusCode = (int) status;
            }
            catch (Exception)
            {
                UnderlyingResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
            finally
            {
                UnderlyingResponse.OutputStream.Close();
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends object serialized to text using the current IJsonConverter plugin
        /// </summary>
        /// <param name="data">The object to be serialized and send</param>
        /// <param name="status">The status code for the response</param>
        public void SendJson(object data, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            var bytes = Encoding.UTF8.GetBytes(Plugins.Use<IJsonConverter>().Serialize(data));
            try
            {
                UnderlyingResponse.ContentType = "application/json";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                UnderlyingResponse.OutputStream.Write(bytes, 0, bytes.Length);
                UnderlyingResponse.StatusCode = (int) status;
            }
            catch (Exception)
            {
                UnderlyingResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
            finally
            {
                UnderlyingResponse.OutputStream.Close();
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends file as response and requests the data to be displayed in-browser if possible
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        /// <param name="status">The status code for the response</param>
        public void SendFile(string filepath, string mime = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            using (Stream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    if (mime == null)
                        UnderlyingResponse.ContentType = MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime)
                            ? mime
                            : "application/octet-stream";
                    var len = input.Length;
                    UnderlyingResponse.ContentLength64 = len;
                    UnderlyingResponse.AddHeader("Date", DateTime.Now.ToString("r"));
                    UnderlyingResponse.AddHeader("Last-Modified", File.GetLastWriteTime(filepath).ToString("r"));
                    UnderlyingResponse.AddHeader("Content-disposition", "inline; filename=" + Path.GetFileName(filepath));

                    var buffer = len < 0x10000 ? new byte[len] : new byte[0x10000];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        UnderlyingResponse.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    UnderlyingResponse.StatusCode = (int) status;
                    UnderlyingResponse.OutputStream.Flush();
                }
                catch (Exception)
                {
                    UnderlyingResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
                finally
                {
                    UnderlyingResponse.OutputStream.Close();
                    UnderlyingResponse.Close();
                    Closed = true;
                }
            }
        }

        /// <summary>
        ///     Sends file as response and requests the data to be downloaded as an attachment
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        /// <param name="status">The status code for the response</param>
        public void Download(string filepath, string mime = null, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            using (var input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    if (mime == null)
                        UnderlyingResponse.ContentType = MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime)
                            ? mime
                            : "application/octet-stream";
                    var len = input.Length;
                    UnderlyingResponse.ContentLength64 = len;
                    UnderlyingResponse.AddHeader("Date", DateTime.Now.ToString("r"));
                    UnderlyingResponse.AddHeader("Last-Modified", File.GetLastWriteTime(filepath).ToString("r"));
                    UnderlyingResponse.AddHeader("Content-disposition",
                        "attachment; filename=" + Path.GetFileName(filepath));

                    var buffer = len < 0x10000 ? new byte[len] : new byte[0x10000];
                    int nbytes;
                    while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        UnderlyingResponse.OutputStream.Write(buffer, 0, nbytes);
                    input.Close();

                    UnderlyingResponse.StatusCode = (int) status;
                    UnderlyingResponse.OutputStream.Flush();
                }
                catch (Exception)
                {
                    UnderlyingResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
                }
                finally
                {
                    UnderlyingResponse.OutputStream.Close();
                    UnderlyingResponse.Close();
                    Closed = true;
                }
            }
        }

        /// <summary>
        ///     Renders a page file using the current IPageRenderer plugin
        /// </summary>
        /// <param name="pagefilepath">The path of the file to be rendered</param>
        /// <param name="parameters">The parameter collection used when replacing data</param>
        /// <param name="status">The status code for the response</param>
        public void RenderPage(string pagefilepath, RenderParams parameters, HttpStatusCode status = HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            var data = Encoding.UTF8.GetBytes(Plugins.Use<IPageRenderer>().Render(pagefilepath, parameters));
            try
            {
                UnderlyingResponse.ContentType = "text/html";
                UnderlyingResponse.ContentLength64 = data.Length;
                UnderlyingResponse.OutputStream.Write(data, 0, data.Length);
                UnderlyingResponse.StatusCode = (int) status;
                UnderlyingResponse.OutputStream.Flush();
            }
            catch (Exception)
            {
                UnderlyingResponse.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
            finally
            {
                UnderlyingResponse.OutputStream.Close();
                UnderlyingResponse.Close();
                Closed = true;
            }
        }
    }
}