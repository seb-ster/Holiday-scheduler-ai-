namespace HolidayScheduler.Gui

module Support =
    open System
    open System.IO
    open System.Net.Http
    open System.Reflection
    open System.Runtime.InteropServices
    open System.Text
    open System.Text.Json
    open System.Threading.Tasks

    type FeedbackPayload =
        { Category: string
          Message: string
          Contact: string
          CurrentVersion: string }

    let private supportEndpoint = Environment.GetEnvironmentVariable("HOLIDAY_SUPPORT_ENDPOINT")
    let private appSupportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HolidayScheduler", "support")
    let private crashDir = Path.Combine(appSupportDir, "crashes")
    let private feedbackDir = Path.Combine(appSupportDir, "feedback")

    let private ensureSupportDirs () =
        Directory.CreateDirectory(crashDir) |> ignore
        Directory.CreateDirectory(feedbackDir) |> ignore

    let currentVersionText () =
        let version =
            match Assembly.GetEntryAssembly() with
            | null -> Version(1, 0, 0)
            | asm ->
                match asm.GetName().Version with
                | null -> Version(1, 0, 0)
                | value -> value

        $"{version.Major}.{version.Minor}.{version.Build}"

    let supportStoragePath () =
        ensureSupportDirs ()
        appSupportDir

    let revealPath (path: string) =
        try
            if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
                let psi = Diagnostics.ProcessStartInfo("open")
                psi.ArgumentList.Add("-R")
                psi.ArgumentList.Add(path)
                psi.UseShellExecute <- false
                Diagnostics.Process.Start(psi) |> ignore
            elif RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                let psi = Diagnostics.ProcessStartInfo("explorer.exe")
                psi.ArgumentList.Add("/select,")
                psi.ArgumentList.Add(path)
                psi.UseShellExecute <- false
                Diagnostics.Process.Start(psi) |> ignore
            else
                let folder = Path.GetDirectoryName(path)
                if not (String.IsNullOrWhiteSpace(folder)) then
                    let psi = Diagnostics.ProcessStartInfo("xdg-open")
                    psi.ArgumentList.Add(folder)
                    psi.UseShellExecute <- false
                    Diagnostics.Process.Start(psi) |> ignore
        with _ ->
            ()

    let private writeJson path data =
        let options = JsonSerializerOptions(WriteIndented = true)
        File.WriteAllText(path, JsonSerializer.Serialize(data, options))

    let private tryPost kind data =
        task {
            if String.IsNullOrWhiteSpace(supportEndpoint) then
                return false
            else
                try
                    use client = new HttpClient()
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("HolidaySchedulerDemonstrator/1.0")
                    use content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json")
                    content.Headers.Add("X-Holiday-Report-Kind", kind)
                    let! response = client.PostAsync(supportEndpoint, content)
                    return response.IsSuccessStatusCode
                with _ ->
                    return false
        }

    let saveCrashReport (exception: exn) =
        try
            ensureSupportDirs ()

            let stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")
            let path = Path.Combine(crashDir, $"crash-{stamp}.json")
            let report =
                {| kind = "crash"
                   createdUtc = DateTimeOffset.UtcNow
                   version = currentVersionText ()
                   os = RuntimeInformation.OSDescription
                   processArchitecture = RuntimeInformation.ProcessArchitecture.ToString()
                   machineName = Environment.MachineName
                   exceptionType = exception.GetType().FullName
                   message = exception.Message
                   stackTrace = exception.StackTrace
                   innerException = if isNull exception.InnerException then null else exception.InnerException.ToString() |}

            writeJson path report
            Task.Run(fun () -> tryPost "crash" report :> Task) |> ignore
            path
        with _ ->
            String.Empty

    let saveFeedback (payload: FeedbackPayload) =
        task {
            ensureSupportDirs ()

            let stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")
            let path = Path.Combine(feedbackDir, $"feedback-{stamp}.json")
            let report =
                {| kind = "feedback"
                   createdUtc = DateTimeOffset.UtcNow
                   version = currentVersionText ()
                   os = RuntimeInformation.OSDescription
                   payload = payload |}

            writeJson path report
            let! sent = tryPost "feedback" report
            return path, sent
        }