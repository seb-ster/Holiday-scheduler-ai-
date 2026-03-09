#nowarn "3261"
module HolidayScheduler.Logger

open System
open System.IO
open System.Net.Http
open System.Text

// ── Log levels ───────────────────────────────────────────────────────────────

type LogLevel =
    | Debug
    | Info
    | Warning
    | Error
    | Critical

let private levelLabel = function
    | Debug    -> "DEBUG"
    | Info     -> "INFO "
    | Warning  -> "WARN "
    | Error    -> "ERROR"
    | Critical -> "CRIT "

// ── Constants ────────────────────────────────────────────────────────────────

[<Literal>]
let private maxGitHubIssueTitleLength = 80

// ── Configuration ────────────────────────────────────────────────────────────

type LogConfig =
    { MaxFileSizeBytes  : int64
      MaxRotatedFiles   : int
      GitHubToken       : string option
      GitHubRepo        : string option   // "owner/repo"
      MinLevel          : LogLevel }

let defaultConfig =
    { MaxFileSizeBytes  = 10L * 1024L * 1024L  // 10 MB
      MaxRotatedFiles   = 5
      GitHubToken       = None
      GitHubRepo        = None
      MinLevel          = Info }

// ── Log directory selection ──────────────────────────────────────────────────

/// Try candidate directories in order; return first writable one.
let private pickLogDir () : string option =
    let candidates =
        [ // 1. Alongside the executable
          try
              let loc = System.Reflection.Assembly.GetExecutingAssembly().Location
              if System.String.IsNullOrEmpty(loc) then None
              else
                  match Path.GetDirectoryName(loc) with
                  | null -> None
                  | "" -> None
                  | dir -> Some dir
          with _ -> None
          // 2. User home
          try Some (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "logs"))
          with _ -> None
          // 3. Temp
          try Some (Path.Combine(Path.GetTempPath(), "HolidayScheduler", "logs"))
          with _ -> None
          // 4. LocalAppData
          try Some (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                 "HolidayScheduler", "logs"))
          with _ -> None ]
        |> List.choose id

    candidates
    |> List.tryFind (fun (dir : string) ->
        try
            Directory.CreateDirectory(dir) |> ignore
            let probe = Path.Combine(dir, ".probe")
            File.WriteAllText(probe, "")
            File.Delete(probe)
            true
        with _ -> false)

// ── Log rotation ─────────────────────────────────────────────────────────────

let private rotateIfNeeded (cfg : LogConfig) (logPath : string) =
    try
        if File.Exists(logPath) then
            let info = FileInfo(logPath)
            if info.Length >= cfg.MaxFileSizeBytes then
                // Shift existing rotated files
                for i = cfg.MaxRotatedFiles - 1 downto 1 do
                    let src  = sprintf "%s.%d" logPath i
                    let dest = sprintf "%s.%d" logPath (i + 1)
                    if File.Exists(src) then
                        if File.Exists(dest) then File.Delete(dest)
                        File.Move(src, dest)
                // Rotate current → .1
                let rotated = sprintf "%s.1" logPath
                if File.Exists(rotated) then File.Delete(rotated)
                File.Move(logPath, rotated)
    with ex ->
        eprintfn "[Logger] Rotation error: %s" ex.Message

// ── GitHub issue creation ─────────────────────────────────────────────────────

let private createGitHubIssue (cfg : LogConfig) (title : string) (body : string) =
    async {
        match cfg.GitHubToken, cfg.GitHubRepo with
        | Some token, Some repo ->
            try
                use client = new HttpClient()
                client.DefaultRequestHeaders.Add("User-Agent", "HolidayScheduler")
                client.DefaultRequestHeaders.Add("Authorization", sprintf "token %s" token)
                let payload =
                    sprintf """{"title":"%s","body":"%s","labels":["crash"]}"""
                        (title.Replace("\"", "\\\""))
                        (body.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", ""))
                let content = new StringContent(payload, Encoding.UTF8, "application/json")
                let url = sprintf "https://api.github.com/repos/%s/issues" repo
                let! response = client.PostAsync(url, content) |> Async.AwaitTask
                if not response.IsSuccessStatusCode then
                    eprintfn "[Logger] GitHub issue creation failed: %d" (int response.StatusCode)
            with ex ->
                eprintfn "[Logger] GitHub issue creation error: %s" ex.Message
        | _ -> ()
    }

// ── Logger ────────────────────────────────────────────────────────────────────

type Logger(config : LogConfig) =
    let logDir  = pickLogDir ()
    let logPath =
        logDir
        |> Option.map (fun d -> Path.Combine(d, "holiday-scheduler.log"))

    let lockObj = obj()

    let writeToFile (line : string) =
        match logPath with
        | None -> ()
        | Some path ->
            try
                rotateIfNeeded config path
                File.AppendAllText(path, line + Environment.NewLine)
            with ex ->
                eprintfn "[Logger] Write error: %s" ex.Message

    let format (level : LogLevel) (message : string) =
        sprintf "[%s] [%s] %s"
            (DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            (levelLabel level)
            message

    member _.LogPath = logPath

    member _.Log (level : LogLevel) (message : string) =
        if level >= config.MinLevel then
            lock lockObj (fun () ->
                let line = format level message
                printfn "%s" line
                writeToFile line)

    member this.Debug   msg = this.Log Debug   msg
    member this.Info    msg = this.Log Info    msg
    member this.Warning msg = this.Log Warning msg
    member this.Error   msg = this.Log Error   msg

    member this.Critical (message : string) (ex : exn option) =
        let exStr =
            ex
            |> Option.map (fun e -> sprintf "\nException: %s\nStack Trace:\n%s" e.Message e.StackTrace)
            |> Option.defaultValue ""
        let fullMsg = message + exStr
        this.Log Critical fullMsg

        // Fire-and-forget GitHub issue creation
        async {
            let title = sprintf "[CRASH] %s" (message.Split('\n').[0] |> fun s -> if s.Length > maxGitHubIssueTitleLength then s.[..maxGitHubIssueTitleLength - 1] else s)
            let body  =
                sprintf "**Automatic crash report**\n\nTime: %s\n\n```\n%s\n```"
                    (DateTime.UtcNow.ToString("O"))
                    fullMsg
            do! createGitHubIssue config title body
        }
        |> Async.Start

    member _.Statistics () =
        match logPath with
        | None ->
            printfn "[Logger] Writing to console only (no writable log directory)"
        | Some path ->
            let size =
                if File.Exists(path) then FileInfo(path).Length else 0L
            let rotCount =
                [ 1 .. config.MaxRotatedFiles ]
                |> List.filter (fun i -> File.Exists(sprintf "%s.%d" path i))
                |> List.length
            printfn "[Logger] Log path   : %s" path
            printfn "[Logger] Current log: %d bytes" size
            printfn "[Logger] Rotated    : %d file(s)" rotCount

// ── Singleton convenience ─────────────────────────────────────────────────────

let mutable private _instance : Logger option = None

let initialize (cfg : LogConfig) =
    _instance <- Some (Logger(cfg))

let instance () =
    match _instance with
    | Some l -> l
    | None   ->
        let l = Logger(defaultConfig)
        _instance <- Some l
        l
