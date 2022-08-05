# Setup server to be hosted on HTTPS
Expanded upon from this [GitHub answer](https://stackoverflow.com/a/33905011)

### 1. Install OpenSSL
1. Go to [Google Code](https://code.google.com/archive/p/openssl-for-windows/downloads)
2. Select your version and download the archive
3. Extract the archive to `C:\openssl` and rename the unzipped folder to `ssl`
    * In the end, your directory should look like:
```
C:\openssl\ssl:
    - \bin
    - \include
    - \lib
    - openssl.cnf
```
4. Unzip the archive
5. Add the path `C:\openssl\ssl\bin` to your environment variable, PATH

### 2. Generate the certificates
1. Open up powershell in administrator mode
2. Find a folder on your computer where you can put certificates (like `C:\Users\USERNAME\cert`)
3. Execute the command:
```
    openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes
```
* It does not matter what you enter for most of the fields, but you must enter `localhost` or your target IP address for the `Common Name (eg, YOUR name) []`
4. Execute the command to export the certificates so it can be used elsewhere
```
    openssl pkcs12 -inkey key.pem -in cert.pem -export -out bob_pfx.pfx
```
* This will ask you for a password, enter it and remember it

### 3. Open Certificate Manager
1. In the same admin powershell session, enter `mmc`
2. Go to `File` > `Add/Remove Snap-in` (CTRL+M)
3. Highlight `Certificates`, click `Add`
4. Select `Computer Account`, click `Next`
5. Select `Local Computer`, click `Finish`
6. Click `Ok`

### 4. Import the certificates
1. Expand `Certificates (Local Computer)`
2. Perform the same steps for the subfolders, `Personal`, and `Trusted Root Certification Authority`
    1. Right click the folder, select `All Tasks` > `Import`
    2. Click `Next`
    3. Click `Browse`, and then instruct the file explorer to look for `Personal Information Exchange .pfx` files
    4. Locate the directory you exported the `bob_pfx.pfx` file to and select it
    5. Click `Open` and then `Next`
    6. Enter the password created in step 2.4 then click `Next`
    8. Select `Place all certificates in the following store` and select the current folder (either `Personal` or `Trusted Root Certification Authority`)
    9. Click `Next` then `Finish`

### 5. Attach the certificates to your port for the application
1. You will need two values for this part
    1. The certificate thumbprint, `<Thumbprint>`
        1. Open up either the `Personal` or `Trusted Root Certification Authority` subfolder and select the `Certificates` to list all the certificates
        2. Find the certificate created for `localhost` (or whatever you entered as the common name in step 2.3), double click it
        3. Select the `Details` tab and scroll to find `Thumbprint`
        4. The value in the window will be your `<Thumbprint>`
    2. The project Guid value, `<ProjectGuid>`
        1. Open up your `.csproj` file for the project you are hosting the server from
        2. Search for the propert `ProjectGuid`, the value (including the brackets) will be your `<ProjectGuid>`
2. Open up an administrator command prompt (not powershell) and run the following command
```
    netsh http add sslcert ipport=0.0.0.0:8443 certhash=<Thumbprint> appid=<ProjectGuid>
```
* Again, `<ProjectGuid>` must be the guid value enclosed by curly braces: i.e. `{B47DC35C-86D6-440F-8547-6858C119C026}`
* The port value, `8443` will be whatever you set in step 6 with your project
3. If you need to redo it, run the following command first:
```
    netsh http delete sslcert ipport=0.0.0.0:8443
```

### 6. Run your project
1. If using a standard `HttpListener` without the `HttpServer` projects in this repo, use the following code:
```
    var prefixes = { "http://localhost:8080/", "https://localhost:8443/" };
    HttpListener listener = new HttpListener();
    foreach (string prefix in prefixes)
    {
        listener.Prefixes.Add(prefix);
    }
    listener.Start();
```
2. If using the `HttpServer.WebServer` library included in the repository
```
    var ws = new WebServer(logger: Logger.ConsoleLogger, config: new WebServerConfig
    {
        HostUrl = "http://localhost:8080;https://localhost:8443",
    });
    await ws.RunAsync();
```