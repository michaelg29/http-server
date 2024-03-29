# http-server
 Collection of servers that can be run to host content over the web

# Usage
All of the files needed to run the server are in the directory *bin*. You should add this directory to your PATH environment variable so it can be located from any terminal window, regardles of the current directory.
## Static File Server: HttpServer.StaticFileServer
This executable serves the purpose of allowing users to browse through their local file explorer on a browser page and view various files.

To run the static file server, execute the following command:
```HttpServer.StaticFileServer [-d <directory>] [-u <url>] [-p]```
The parameters are optional, but here are their uses:
* `<directory>`: The directory that will be used to fetch contents. It can be absolute or relative to the current working directory in the terminal window. If left unset, then the server will fetch contents from the current working directory.
* `<url>`: The url the server will listen on. It must end with a slash (`/`). If left unset, it will default to `http://+:8080/`, which will tell the server to listen on the following URLs:
    * `http://localhost:8080`
    * `http://127.0.0.1:8080`
    * Your LAN IP over port 8080 (e.g. `http://192.168.0.1:8080`)
* `-p`: allow for uploading

## Dynamic Web Server: HttpServer.WebServer
This library allows users to create custom web apps or APIs. See the [README.md file](./WebServerREADME.md) for more detailed information.

# Troubleshooting
* If you get the error: `Access is denied` when trying to start, you must allow the program through the firewall along with the specific port.
    * Also run the following command in a powershell session with administrator permissions: `netsh http add urlacl url=http://+:8080/ user=Everyone listen=yes`
    * You can also try running either Visual Studio as an administrator or the executable as an adminstrator, though you cannot debug it in the latter way.