using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using RHttpServer.Logging;
using RHttpServer.Plugins;

namespace RHttpServer.Response
{
    /// <summary>
    ///     Class representing the reponse to a clients request
    ///     All 
    /// </summary>
    public class RResponse
    {
        private const int BufferSize = 0x1000;

        internal static readonly IDictionary<string, string> MimeTypes =
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
                {".mp4", "video/mp4"},
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

        /// <summary>
        ///     Whether this response has been closed
        /// </summary>
        protected bool Closed;

        /// <summary>
        ///     The plugins registered to the server
        /// </summary>
        public RPluginCollection Plugins { get; }


        /// <summary>
        ///     The underlying HttpListenerResponse <para/>
        ///     The implementation of RResponse is leaky, to avoid limiting you
        /// </summary>
        public HttpListenerResponse UnderlyingResponse { get; }

        /// <summary>
        ///     Add header item to response
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        public void AddHeader(string fieldName, string fieldValue)
        {
            if (Closed) throw new RHttpServerException("You cannot add a header after closing the request");
            UnderlyingResponse.AddHeader(fieldName, fieldValue);
        }

        /// <summary>
        ///     Redirects the client to a given path or url
        /// </summary>
        /// <param name="redirectPath">The path or url to redirect to</param>
        public void Redirect(string redirectPath)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            UnderlyingResponse.Redirect(redirectPath);
            UnderlyingResponse.Close();
            Closed = true;
        }

        /// <summary>
        ///     Sends data as text
        /// </summary>
        /// <param name="data">The text data to send</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="status">The status code for the response</param>
        public async void SendString(string data, string contentType = "text/plain",
            int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                var bytes = Encoding.UTF8.GetBytes(data);
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.ContentLength64 = bytes.Length;
                await UnderlyingResponse.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                await UnderlyingResponse.OutputStream.FlushAsync();
                UnderlyingResponse.OutputStream.Close();
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends data as bytes
        /// </summary>
        /// <param name="data">The text data to send</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="status">The status code for the response</param>
        public async void SendBytes(byte[] data, string contentType = "application/octet-stream",
            int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.ContentLength64 = data.Length;
                await UnderlyingResponse.OutputStream.WriteAsync(data, 0, data.Length);
                await UnderlyingResponse.OutputStream.FlushAsync();
                UnderlyingResponse.OutputStream.Close();
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends data from stream
        /// </summary>
        /// <param name="stream">Stream of data to send</param>
        /// <param name="length">Length of the content in the stream</param>
        /// <param name="gzipCompress">Whether the data should be compressed</param>
        /// <param name="contentType">The mime type of the content</param>
        /// <param name="status">The status code for the response</param>
        /// <exception cref="RHttpServerException"></exception>
        public async void SendFromStream(Stream stream, long length, bool gzipCompress = false, string contentType = "application/octet-stream",
            int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                if (gzipCompress) UnderlyingResponse.AddHeader("Content-Encoding", "gzip");
                UnderlyingResponse.ContentType = contentType;
                UnderlyingResponse.ContentLength64 = length;
                if (!gzipCompress)
                    await stream.CopyToAsync(UnderlyingResponse.OutputStream);
                else
                    using (var zip = new GZipStream(stream, CompressionMode.Compress, true))
                        await zip.CopyToAsync(UnderlyingResponse.OutputStream);
                await UnderlyingResponse.OutputStream.FlushAsync();
                UnderlyingResponse.OutputStream.Close();
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends object serialized to text using the current IJsonConverter plugin
        /// </summary>
        /// <param name="data">The object to be serialized and send</param>
        /// <param name="status">The status code for the response</param>
        public async void SendJson(object data, int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                var bytes = Encoding.UTF8.GetBytes(Plugins.Use<IJsonConverter>().Serialize(data));
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = "application/json";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                await UnderlyingResponse.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                await UnderlyingResponse.OutputStream.FlushAsync();
                UnderlyingResponse.OutputStream.Close();
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends object serialized to text using the current IXmlConverter plugin
        /// </summary>
        /// <param name="data">The object to be serialized and send</param>
        /// <param name="status">The status code for the response</param>
        public async void SendXml(object data, int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                var bytes = Encoding.UTF8.GetBytes(Plugins.Use<IXmlConverter>().Serialize(data));
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentType = "application/xml";
                UnderlyingResponse.ContentLength64 = bytes.Length;
                await UnderlyingResponse.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                await UnderlyingResponse.OutputStream.FlushAsync();
                UnderlyingResponse.OutputStream.Close();
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
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
        public async void SendFile(string filepath, string mime = null, int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (mime == null)
                    UnderlyingResponse.ContentType = MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime)
                        ? mime
                        : "application/octet-stream";
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.AddHeader("Content-disposition", "inline; filename=" + Path.GetFileName(filepath));
                using (Stream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await input.CopyToAsync(UnderlyingResponse.OutputStream);
                    await UnderlyingResponse.OutputStream.FlushAsync();
                    UnderlyingResponse.OutputStream.Close();
                }
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Sends file as response and requests the data to be downloaded as an attachment
        /// </summary>
        /// <param name="filepath">The local path of the file to send</param>
        /// <param name="filename">The name filename the client receives the file with, defaults to using the actual filename</param>
        /// <param name="mime">The mime type for the file, when set to null, the system will try to detect based on file extension</param>
        /// <param name="status">The status code for the response</param>
        public async void Download(string filepath, string filename = "", string mime = null,
            int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                if (mime == null)
                    UnderlyingResponse.ContentType = MimeTypes.TryGetValue(Path.GetExtension(filepath), out mime)
                        ? mime
                        : "application/octet-stream";
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.AddHeader("Content-disposition", "attachment; filename=" +
                    (string.IsNullOrWhiteSpace(filename) ? Path.GetFileName(filepath) : filename));
                using (var input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await input.CopyToAsync(UnderlyingResponse.OutputStream);
                    await UnderlyingResponse.OutputStream.FlushAsync();
                    UnderlyingResponse.OutputStream.Close();
                }
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }

        /// <summary>
        ///     Renders a page file using the current IPageRenderer plugin
        /// </summary>
        /// <param name="pagefilepath">The path of the file to be rendered</param>
        /// <param name="parameters">The parameter collection used when replacing data</param>
        /// <param name="status">The status code for the response</param>
        public async void RenderPage(string pagefilepath, RenderParams parameters, int status = (int) HttpStatusCode.OK)
        {
            if (Closed) throw new RHttpServerException("You can only send the response once");
            try
            {
                UnderlyingResponse.StatusCode = status;
                var data = Encoding.UTF8.GetBytes(Plugins.Use<IPageRenderer>().Render(pagefilepath, parameters));
                UnderlyingResponse.ContentType = "text/html";
                if (HttpServer.IncludeServerHeader) UnderlyingResponse.AddHeader("Server", $"RHttpServer.CSharp/{HttpServer.Version}");
                UnderlyingResponse.ContentLength64 = data.Length;
                await UnderlyingResponse.OutputStream.WriteAsync(data, 0, data.Length);
                await UnderlyingResponse.OutputStream.FlushAsync();
            }
            catch (Exception ex)
            {
                if (HttpServer.ThrowExceptions) throw;
                Logger.Log(ex);
            }
            finally
            {
                UnderlyingResponse.Close();
                Closed = true;
            }
        }
    }
}