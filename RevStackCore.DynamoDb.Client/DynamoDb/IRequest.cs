using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RevStackCore.DynamoDb.Client
{
    public interface IResolver
    {
        /// <summary>
        /// Resolve a dependency from the AppHost's IOC
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T TryResolve<T>();
    }

    public interface IHttpFile
    {
        string Name { get; }
        string FileName { get; }
        long ContentLength { get; }
        string ContentType { get; }
        Stream InputStream { get; }
    }

    public interface IRequestPreferences
    {
        bool AcceptsGzip { get; }

        bool AcceptsDeflate { get; }
    }

    public interface IResponse
    {
        /// <summary>
        /// The underlying ASP.NET, .NET Core or HttpListener HttpResponse
        /// </summary>
        object OriginalResponse { get; }

        /// <summary>
        /// The corresponding IRequest API for this Response
        /// </summary>
        IRequest Request { get; }

        /// <summary>
        /// The Response Status Code
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// The Response Status Description
        /// </summary>
        string StatusDescription { get; set; }

        /// <summary>
        /// The Content-Type for this Response
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Add a Header to this Response
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void AddHeader(string name, string value);

        /// <summary>
        /// Remove an existing Header added on this Response
        /// </summary>
        /// <param name="name"></param>
        void RemoveHeader(string name);

        /// <summary>
        /// Get an existing Header added to this Response
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string GetHeader(string name);

        /// <summary>
        /// Return a Redirect Response to the URL specified
        /// </summary>
        /// <param name="url"></param>
        void Redirect(string url);

        /// <summary>
        /// The Response Body Output Stream
        /// </summary>
        Stream OutputStream { get; }

        /// <summary>
        /// The Response DTO
        /// </summary>
        object Dto { get; set; }

        /// <summary>
        /// Buffer the Response OutputStream so it can be written in 1 batch
        /// </summary>
        bool UseBufferedStream { get; set; }

        /// <summary>
        /// Signal that this response has been handled and no more processing should be done.
        /// When used in a request or response filter, no more filters or processing is done on this request.
        /// </summary>
        void Close();

        /// <summary>
        /// Close this Response Output Stream Async
        /// </summary>
        Task CloseAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Calls Response.End() on ASP.NET HttpResponse otherwise is an alias for Close().
        /// Useful when you want to prevent ASP.NET to provide it's own custom error page.
        /// </summary>
        void End();

        /// <summary>
        /// Response.Flush() and OutputStream.Flush() seem to have different behaviour in ASP.NET
        /// </summary>
        void Flush();

        /// <summary>
        /// Flush this Response Output Stream Async
        /// </summary>
        Task FlushAsync(CancellationToken token = default(CancellationToken));

        /// <summary>
        /// Gets a value indicating whether this instance is closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Set the Content Length in Bytes for this Response
        /// </summary>
        /// <param name="contentLength"></param>
        void SetContentLength(long contentLength);

        /// <summary>
        /// Whether the underlying TCP Connection for this Response should remain open
        /// </summary>
        bool KeepAlive { get; set; }

        /// <summary>
        /// Whether the HTTP Response Headers have already been written.
        /// </summary>
        bool HasStarted { get; }

        //Add Metadata to Response
        Dictionary<string, object> Items { get; }
    }

    public interface IRequest : IResolver
    {
        /// <summary>
        /// The underlying ASP.NET or HttpListener HttpRequest
        /// </summary>
        object OriginalRequest { get; }

        /// <summary>
        /// The Response API for this Request
        /// </summary>
        IResponse Response { get; }

        /// <summary>
        /// The name of the service being called (e.g. Request DTO Name)
        /// </summary>
        string OperationName { get; set; }

        /// <summary>
        /// The Verb / HttpMethod or Action for this request
        /// </summary>
        string Verb { get; }

        /// <summary>
        /// Different Attribute Enum flags classifying this Request
        /// </summary>
        RequestAttributes RequestAttributes { get; set; }

        /// <summary>
        /// Optional preferences for the processing of this Request
        /// </summary>
        IRequestPreferences RequestPreferences { get; }

        /// <summary>
        /// The Request DTO, after it has been deserialized.
        /// </summary>
        object Dto { get; set; }

        /// <summary>
        /// The request ContentType
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Whether this was an Internal Request
        /// </summary>
        bool IsLocal { get; }

        /// <summary>
        /// The UserAgent for the request
        /// </summary>
        string UserAgent { get; }

        /// <summary>
        /// A Dictionary of HTTP Cookies sent with this Request
        /// </summary>
        IDictionary<string, System.Net.Cookie> Cookies { get; }

        /// <summary>
        /// The expected Response ContentType for this request
        /// </summary>
        string ResponseContentType { get; set; }

        /// <summary>
        /// Whether the ResponseContentType has been explicitly overriden or whether it was just the default
        /// </summary>
        bool HasExplicitResponseContentType { get; }

        /// <summary>
        /// Attach any data to this request that all filters and services can access.
        /// </summary>
        Dictionary<string, object> Items { get; }

        /// <summary>
        /// The HTTP Headers in a NameValueCollection
        /// </summary>
        NameValueCollection Headers { get; }

        /// <summary>
        /// The ?query=string in a NameValueCollection
        /// </summary>
        NameValueCollection QueryString { get; }

        /// <summary>
        /// The HTTP POST'ed Form Data in a NameValueCollection
        /// </summary>
        NameValueCollection FormData { get; }
        /// <summary>
        /// Buffer the Request InputStream so it can be re-read
        /// </summary>
        bool UseBufferedStream { get; set; }

        /// <summary>
        /// The entire string contents of Request.InputStream
        /// </summary>
        /// <returns></returns>
        string GetRawBody();

        /// <summary>
        /// Relative URL containing /path/info?query=string
        /// </summary>
        string RawUrl { get; }

        /// <summary>
        /// The Absolute URL for the request
        /// </summary>
        string AbsoluteUri { get; }

        /// <summary>
        /// The Remote IP as reported by Request.UserHostAddress
        /// </summary>
        string UserHostAddress { get; }

        /// <summary>
        /// The Remote Ip as reported by X-Forwarded-For, X-Real-IP or Request.UserHostAddress
        /// </summary>
        string RemoteIp { get; }

        /// <summary>
        /// The value of the Authorization Header used to send the Api Key, null if not available
        /// </summary>
        string Authorization { get; }

        /// <summary>
        /// e.g. is https or not
        /// </summary>
        bool IsSecureConnection { get; }

        /// <summary>
        /// Array of different Content-Types accepted by the client
        /// </summary>
        string[] AcceptTypes { get; }

        /// <summary>
        /// The normalized /path/info for the request
        /// </summary>
        string PathInfo { get; }

        /// <summary>
        /// The original /path/info as sent
        /// </summary>
        string OriginalPathInfo { get; }

        /// <summary>
        /// The Request Body Input Stream
        /// </summary>
        Stream InputStream { get; }

        /// <summary>
        /// The size of the Request Body if provided
        /// </summary>
        long ContentLength { get; }

        /// <summary>
        /// Access to the multi-part/formdata files posted on this request
        /// </summary>
        IHttpFile[] Files { get; }

        /// <summary>
        /// The value of the Referrer, null if not available
        /// </summary>
        Uri UrlReferrer { get; }
    }

    [Flags]
    public enum RequestAttributes : long
    {
        None = 0,

        Any = AnyNetworkAccessType | AnySecurityMode | AnyHttpMethod | AnyCallStyle | AnyFormat | AnyEndpoint,
        AnyNetworkAccessType = External | LocalSubnet | Localhost | InProcess,
        AnySecurityMode = Secure | InSecure,
        AnyHttpMethod = HttpHead | HttpGet | HttpPost | HttpPut | HttpDelete | HttpPatch | HttpOptions | HttpOther,
        AnyCallStyle = OneWay | Reply,
        AnyFormat = Soap11 | Soap12 | Xml | Json | Jsv | Html | ProtoBuf | Csv | MsgPack | Wire | FormatOther,
        AnyEndpoint = Http | MessageQueue | Tcp | EndpointOther,
        InternalNetworkAccess = InProcess | Localhost | LocalSubnet,

        //Whether it came from an Internal or External address
        Localhost = 1 << 0,
        LocalSubnet = 1 << 1,
        External = 1 << 2,

        //Called over a secure or insecure channel
        Secure = 1 << 3,
        InSecure = 1 << 4,

        //HTTP request type
        HttpHead = 1 << 5,
        HttpGet = 1 << 6,
        HttpPost = 1 << 7,
        HttpPut = 1 << 8,
        HttpDelete = 1 << 9,
        HttpPatch = 1 << 10,
        HttpOptions = 1 << 11,
        HttpOther = 1 << 12,

        //Call Styles
        OneWay = 1 << 13,
        Reply = 1 << 14,

        //Different formats
        Soap11 = 1 << 15,
        Soap12 = 1 << 16,
        //POX
        Xml = 1 << 17,
        //Javascript
        Json = 1 << 18,
        //Jsv i.e. TypeSerializer
        Jsv = 1 << 19,
        //e.g. protobuf-net
        ProtoBuf = 1 << 20,
        //e.g. text/csv
        Csv = 1 << 21,
        Html = 1 << 22,
        Wire = 1 << 23,
        MsgPack = 1 << 24,
        FormatOther = 1 << 25,

        //Different endpoints
        Http = 1 << 26,
        MessageQueue = 1 << 27,
        Tcp = 1 << 28,
        EndpointOther = 1 << 29,

        InProcess = 1 << 30, //Service was executed within code (e.g. ResolveService<T>)
    }


}
