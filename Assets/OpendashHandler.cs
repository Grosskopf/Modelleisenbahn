using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using UnityEngine.UI;

public class OpendashHandler : MonoBehaviour
{
    public Text textout;
    private string websocket_url="ws://opendash.kompetenzzentrum-siegen.digital:4567/subscription";
    private string parselogin_url="https://users.kompetenzzentrum-siegen.digital/parse/login";
    private string email = "test@zug40.de";
    private string password = "demo";
    private string sessionid = "";
    private ClientWebSocket socket;
    private byte[] result = new byte[1024];
    private Task<WebSocketReceiveResult> receiveResult;

    //private WebSocket socket;
    // Start is called before the first frame update
    void Start()
    {
        readconfig();
        writeconfig();
        getSessionId();
        connectSocketAsync();
    }

    private async Task handleinput()
    {

        Console.WriteLine("got " + Encoding.UTF8.GetString(
            result, 0, result.Length));
        textout.text = "got " + Encoding.UTF8.GetString(
            result, 0, result.Length);
        ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
        receiveResult= socket.ReceiveAsync(bytesReceived, CancellationToken.None);
        //WebSocketReceiveResult result = await socket.ReceiveAsync(bytesReceived, CancellationToken.None);
    }
    private async System.Threading.Tasks.Task connectSocketAsync()
    {
        socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(websocket_url),System.Threading.CancellationToken.None);
        JObject loginjson = new JObject();
        loginjson.Add("user", email);
        loginjson.Add("session", sessionid);
        System.Text.UTF8Encoding uTF8Encoding = new System.Text.UTF8Encoding();
        if (socket.State == WebSocketState.Open)
        {
            print("is connected");
        }
        else
        {
            print("is_not_connected");
        }
        Task sendingtask=socket.SendAsync( new ArraySegment<byte>(uTF8Encoding.GetBytes( loginjson.ToString())),WebSocketMessageType.Text,true,System.Threading.CancellationToken.None);
        await sendingtask;
        if (sendingtask.Status == TaskStatus.RanToCompletion)
        {
            print("logged_in");
        }
        else
        {
            print("didn't log in");
        }
        result = new byte[1024];
        receiveResult = socket.ReceiveAsync(new ArraySegment<byte>(result), System.Threading.CancellationToken.None);
    }

    void getSessionId()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(parselogin_url + "?username=" + email + "&password=" + password);
        request.Headers.Add("X-Parse-Application-Id", "1234567890");
        request.Headers.Add("X-Parse-Revocable-Session", "1");
        WebResponse response_Opendash = request.GetResponse();
        string response_text = new StreamReader(response_Opendash.GetResponseStream()).ReadToEnd();
        print("got " + response_text);
        JObject response_json = JObject.Parse(response_text);
        sessionid=response_json.GetValue("sessionToken").ToString();
        print("sessiontoken: " + sessionid);
        response_Opendash.Close();
    }

    void readconfig()
    {

    }
    void writeconfig()
    {

    }

    // Update is called once per frame
    void Update()
    {

        if (receiveResult != null && receiveResult.GetAwaiter().IsCompleted)
        {
            handleinput();
        }
        /*
        if (receiveResult!=null && receiveResult.IsCompleted)
        {
            print("gotresult: ");
            string resultgot=Encoding.UTF8.GetString(result, 0, result.Length);
            print(resultgot);
            result = new byte[2048];
            receiveResult = socket.ReceiveAsync(new ArraySegment<byte>(result), System.Threading.CancellationToken.None);
        }*/
    }
}
