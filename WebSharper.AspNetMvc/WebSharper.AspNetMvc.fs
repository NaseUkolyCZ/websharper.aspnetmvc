namespace WebSharper.AspNetMvc

open System.IO
open System.Runtime.CompilerServices
open System.Web
open System.Web.Mvc
open WebSharper
open log4net

module ScriptManager =

    [<Literal>]
    let private smKey = "WebSharper.AspNetMvc.ScriptManager"

    /// <summary>
    /// Register a control so that its dependencies are rendered by the script manager.
    /// </summary>
    /// <param name="control">The control to render.</param>
    /// <returns>The code to insert the control in the page.</returns>
    let Register (control: IControl) =
        let items = HttpContext.Current.Items
        let sm =
            match items.[smKey] with
            | :? Web.ScriptManager as sm -> sm
            | _ ->
                let sm = new Web.ScriptManager()
                items.[smKey] <- sm
                sm
        let id = sm.Register (Some control.Id) control
        MvcHtmlString("<div id=\"" + id + "\"></div>")

    /// <summary>
    /// Render the head tags (style and script) required by the controls in the current page.
    /// </summary>
    /// <returns>The code to insert inside the page's <head> tag.</returns>
    let Head() =
        match HttpContext.Current.Items.[smKey] with
        | :? Web.ScriptManager as sm ->
            use sw = new StringWriter()
            use tw = new UI.HtmlTextWriter(sw)
            sm.RenderControl(tw)
            MvcHtmlString(sw.ToString())
        | _ -> MvcHtmlString("")

/// <summary>
/// Create a filter that enables WebSharper sitelets and remoting.
/// </summary>
type Filter() =

    /// When both the sitelet and ASP.NET MVC accept a URL, the sitelet is served.
    /// Default: true
    member val SiteletsOverrideMvc = true with get, set

    /// The name of the WebSharper sitelets module declared in Web.config.
    /// Default: "WebSharper.Sitelets"
    member val SiteletsModuleName = "WebSharper.Sitelets" with get, set

    /// The name of the WebSharper remoting module declared in Web.config.
    /// Default: "WebSharper.RemotingModule"
    member val RemotingModuleName = "WebSharper.RemotingModule" with get, set

    interface IActionFilter with

        member this.OnActionExecuted(filterCtx) =
            0 |> ignore

        member this.OnActionExecuting(filterCtx) =
            let logger = LogManager.GetLogger("WebSharper.AspNetMvc")            
            logger.Debug( "OnActionExecuting entered" )

            try             
                let httpCtx = filterCtx.HttpContext
                let tryRun (action: option<Async<unit>>) =
                    action |> Option.map (fun run ->
                        filterCtx.Result <-
                            { new ActionResult() with
                                member this.ExecuteResult(_) =
                                    Async.RunSynchronously run })
                let isRemoting =
                    match httpCtx.ApplicationInstance.Modules.[this.RemotingModuleName] with
                    | :? Web.RpcModule as m ->
                        tryRun (m.TryProcessRequest httpCtx) |> Option.isSome
                    | _ -> false
                logger.DebugFormat( "SiteletsOverrideMvc {0}, isRemoting {1}", this.SiteletsOverrideMvc, isRemoting )
                if this.SiteletsOverrideMvc && not isRemoting then
                    logger.DebugFormat( "SiteletsModuleName {0}, httpCtx.ApplicationInstance.Modules.[this.SiteletsModuleName] {1}", this.SiteletsModuleName, httpCtx.ApplicationInstance.Modules.[this.SiteletsModuleName] )
                    match httpCtx.ApplicationInstance.Modules.[this.SiteletsModuleName] with
                    | :? Sitelets.HttpModule as m ->
                        logger.DebugFormat( "m {0}", m )
                        tryRun (m.TryProcessRequest httpCtx) |> ignore
                    | _ -> ()
                logger.Debug( "OnActionExecuting done" )
            with
                | ex -> logger.Fatal( (sprintf "OnActionExecuting failed" ), ex )
