using System.Runtime.InteropServices;
using MongooseSharp.Interop;

// ReSharper disable FunctionNeverReturns

Run();
return;

static unsafe void Handler(mg_connection* c, int ev, void* evData)
{
    if (ev == Mongoose.MG_EV_HTTP_MSG)
    {
        var hm = (mg_http_message*)evData;

        var glob = "/api/hi"u8;

        bool matched;
        fixed (byte* rawGlob = glob)
        {
            matched = Mongoose.mg_http_match_uri(hm, (sbyte*)rawGlob) != 0;
        }

        if (matched)
        {
            var empty = ""u8;
            var uri = "uri"u8;
            var body = "body"u8;
            var fmt = "{%m:%m,%m:%m}\\n"u8;

            var escPtr = Marshal.GetFunctionPointerForDelegate(Something.Esc);
            fixed (byte* rawEmpty = empty)
            {
                fixed (byte* rawFmt = fmt)
                {
                    fixed (byte* rawUri = uri)
                    {
                        fixed (byte* rawBody = body)
                        {
                            // TODO: Unhandled exception. System.BadImageFormatException: Index not found. (0x80131124)
                            Mongoose.mg_http_reply(c, 200, (sbyte*)rawEmpty, (sbyte*)rawFmt,
                                __arglist(escPtr, 0, (sbyte*)rawUri, escPtr, hm->uri.len, hm->uri.ptr,
                                    escPtr, 0, (sbyte*)rawBody, escPtr, hm->body.len, hm->body.ptr));
                        }
                    }
                }
            }
        }
        else
        {
            var rootDir = "."u8;
            fixed (byte* rawRootDir = rootDir)
            {
                var opts = new mg_http_serve_opts { root_dir = (sbyte*)rawRootDir };
                Mongoose.mg_http_serve_dir(c, hm, &opts);
            }
        }
    }
}

static unsafe void Run()
{
    mg_mgr mgr;
    Mongoose.mg_mgr_init(&mgr);

    var handler = new mg_event_handler_t(Handler);
    var handlerPtr = Marshal.GetFunctionPointerForDelegate(handler);
    GC.KeepAlive(Something.Esc);

    var url = "http://0.0.0.0:8000"u8;
    fixed (byte* rawUrl = url)
    {
        Mongoose.mg_http_listen(&mgr, (sbyte*)rawUrl,
            (delegate*unmanaged[Cdecl]<mg_connection*, int, void*, void>)handlerPtr, null);
    }

    while (true)
    {
        Mongoose.mg_mgr_poll(&mgr, 1000);
    }
}

internal static unsafe class Something
{
    public static readonly mg_print_esc_t Esc = Mongoose.mg_print_esc;
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate void mg_event_handler_t(mg_connection* c, int ev, void* evData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate nuint mg_print_esc_t(delegate* unmanaged[Cdecl]<sbyte, void*, void> @out, void* arg,
    sbyte** ap);