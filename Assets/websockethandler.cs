using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;
using System.Net;
using System.IO;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using UnityEngine.UI;
//using Parse;

public class websockethandler : MonoBehaviour
{

    private MqttClient client;
    string filename = "/everything.conf";
    public string topic_markers;
    public string topic_Temperature;
    public string topic_updatespeed;
    public string topic_stop;
    public string websocket_url="192.168.1.199";
    public int websocket_port=1883;
    public double currentspeed = 0;
    public List<String> markers;
    public List<double> distances;
    public string firstmarker;
    private string lastmarker="";
    public int marker_to_edit=0;
    public double max_accelleration=1.0;
    public TextMesh out_text;
    private string _out_string;
    public InputField textfield_markerpos;
    public InputField textfield_speedTopic;
    public InputField textfield_stopTopic;
    public InputField textfield_tagTopic;
    public InputField textfield_tempTopic;
    public InputField textfield_webSocket;
    public InputField textfield_accelleration;
    public InputField textfield_username;
    public InputField textfield_password;
    public InputField textfield_email;

    public Dropdown tagconfig;
    public Dropdown markerfirst;
    public Transform settings;
    private bool settings_open = false;
    private Move_along_path movement;
    private float markertime;
    private double maxspeed;
    private double accelleration=0.0f;
    public string username;
    public string password;
    public string email;

    //Parse.
    
    //ClientWebSocket WebSocket;
    // Start is called before the first frame update
    void Start()
    {
        markertime = DateTime.Now.Second;
        filename = Application.persistentDataPath + filename;
        movement = GetComponent<Move_along_path>();
        Debug.Log("starting...");
        if (System.IO.File.Exists(filename))
        {
            print("reader config");
            readConfig();
        }
        else
        {
            print("writer config");
            writeOutConfig();
        }
        //ReConnectWS();
        //connectWSAsync();
    }
    void ReConnectWS()
    {
        print("Connecting to "+websocket_url);
        client = new MqttClient(websocket_url);// + ":" + websocket_port.ToString());//IPAddress.Parse(websocket_url), websocket_port, false,null);

        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

        string clientId = Guid.NewGuid().ToString();
        print("using username: " + username);
        print("using password: " + password);
        byte code=client.Connect(clientId,username.Trim(),password.Trim());
        print("got code from login: "+code.ToString());
        client.Subscribe(new string[] {"#"}, new byte[] {MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
        //client.Subscribe(new string[] { topic_markers }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        //client.Subscribe(new string[] { topic_Temperature }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        //client.Subscribe(new string[] { topic_stop }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

    }
    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {

        print("At Topic: " + e.Topic);
        string messagetext= System.Text.Encoding.UTF8.GetString(e.Message);
        if (e.Topic.Trim()==topic_markers.Trim())
        {
            //print("Received: (" + messagetext+")");
            //print("got a marker");
            int markernum = markers.FindIndex(x => x.StartsWith(messagetext));
            //print("marker is number "+markernum.ToString());
            if (markernum<0)//got a new Marker, add it to the list
            {
                //print("adding this");
                markers.Add(messagetext);
                tagconfig.options[markers.Count - 1].text=markers[markers.Count - 1];
                
            }else if (markernum == 0 && distances.Count==0)//Got all Markers, update my gui, sort my markers
            {

                //print("got all markers, first is: "+markerfirst.options[0].text);
                if (markers.IndexOf(markerfirst.options[0].text)!=-1)
                {
                    OnFirstTagChanged(markers.IndexOf(markerfirst.options[0].text));
                    for(int i = 1; i < markers.Count; i++)
                    {
                        if (markerfirst.options.Count > i)
                        {
                            markerfirst.options[i].text = markers[i];
                        }
                        else
                        {
                            List<Dropdown.OptionData> option_new = new List<Dropdown.OptionData>();
                            option_new.Add(new Dropdown.OptionData(markers[i]));
                            markerfirst.options.Clear();
                            markerfirst.AddOptions(option_new);
                            markerfirst.RefreshShownValue();
                            List<Dropdown.OptionData> tag_new = new List<Dropdown.OptionData>();
                            tag_new.Add(new Dropdown.OptionData("Tag"+i.ToString()));
                            tagconfig.options.Clear();
                            tagconfig.AddOptions(tag_new);
                            markerfirst.RefreshShownValue();
                        }
                    }
                }
                else
                {

                    OnFirstTagChanged(0);
                    for (int i = 1; i < markers.Count; i++)
                    {
                        if (markerfirst.options.Count > i)
                        {
                            markerfirst.options[i].text = markers[i];
                        }
                        else
                        {
                            List<Dropdown.OptionData> option_new = new List<Dropdown.OptionData>();
                            option_new.Add(new Dropdown.OptionData(markers[i]));
                            markerfirst.options.Clear();
                            markerfirst.AddOptions(option_new);
                            markerfirst.RefreshShownValue();
                            List<Dropdown.OptionData> tag_new = new List<Dropdown.OptionData>();
                            tag_new.Add(new Dropdown.OptionData("Tag" + i.ToString()));
                            tagconfig.options.Clear();
                            tagconfig.AddOptions(tag_new);
                            tagconfig.RefreshShownValue();
                        }
                    }
                }
                for(int i=0;i<markers.Count;i++){
                    distances.Add(i* movement.maxdistance / markers.Count);
                }
            }
            if(markernum>=0)//>update speeds and position of train
            {

                //print("updating speeds and feeds");
                markernum = markers.FindIndex(x => x.StartsWith(messagetext));
                int oldmarkernum = markers.FindIndex(x => x.StartsWith(lastmarker));
                //print("got markernum " + markernum);

                if (distances.Count <= markernum)
                {
                    markers = new List<string>();
                    distances = new List<double>();
                }
                else
                {

                    double markerpos = distances[markernum];
                    double last_markerpos = 0;
                    if (oldmarkernum!=-1)
                        last_markerpos = distances[oldmarkernum];
                    //if (markernum != 0)
                    //{
                    //    last_markerpos = distances[markernum - 1];
                    //}
                    double distance = markerpos - last_markerpos;
                    if (markerpos < last_markerpos)
                    {
                        distance = movement.maxdistance - last_markerpos + markerpos;
                    }

                    if (distance / ((float)DateTime.Now.Millisecond/1000 - markertime) > maxspeed)
                    {
                        maxspeed = distance / ((float)DateTime.Now.Millisecond / 1000 - markertime);
                    }
                    markertime = (float)DateTime.Now.Millisecond / 1000;
                    movement.DistanceTraveled = markerpos;
                    accelleration = max_accelleration;
                }
                lastmarker = messagetext;
            }
        }
        if (e.Topic.Trim() == topic_stop.Trim())
        {
            print("stopping/starting");
            if ((messagetext.Contains("steht"))){
                movement.MoveSpeed = 0.0f;
                accelleration = 0.0f;
            }
            else
            {
                accelleration = max_accelleration;
            }

        }
        if (e.Topic.Trim() == topic_Temperature.Trim())
        {
            print("temperature set"+messagetext);
            _out_string = messagetext + "°C";
            //out_text.text = messagetext + "°C";
            
        }
    }

    /*    async Task connectWSAsync()
        {
            if (sessiontoken != null && sessiontoken != "")
                await ParseUser.BecomeAsync(sessiontoken);
            else
                await ParseUser.LogInAsync(username, password);
            //websocket_task = ConnectAsync(websocket_url, new CancellationTokenSource().Token);
        }*/
    void readConfig()
    {
        StreamReader reader = new StreamReader(filename);
        String text = reader.ReadToEnd();
        foreach (string line in text.Split('\n'))
        {
            if (line.StartsWith("WS_URL:"))
            {
                websocket_url = line.Substring(8).Split(new char[] { ':' })[0];//+":"+line.Substring(8).Split(new char[] { ':' })[1];
                if (line.Substring(8).Split(new char[] { ':' }).Length > 1)
                {
                    websocket_port = Int32.Parse(line.Substring(7).Split(new char[] { ':' })[1]);
                }


            }
            else if (line.StartsWith("markertopic:"))
            {
                topic_markers = line.Substring("markertopic: ".Length);
            }
            else if (line.StartsWith("temptopic:"))
            {
                topic_Temperature = line.Substring("temptopic: ".Length);
            }
            else if (line.StartsWith("speedtopic:"))
            {
                topic_updatespeed = line.Substring("speedtopic: ".Length);
            }
            else if (line.StartsWith("stoptopic:"))
            {
                topic_stop = line.Substring("stoptopic: ".Length);
            }
            else if (line.StartsWith("firstmarker:"))
            {
                firstmarker = line.Substring("firstmarker: ".Length);
            }
            else if (line.StartsWith("accelleration:"))
            {
                accelleration = double.Parse(line.Substring("accelleration: ".Length));
            }
            else if (line.StartsWith("user:"))
            {
                username = line.Substring("user: ".Length);
            }
            else if (line.StartsWith("passwd:"))
            {
                password = line.Substring("passwd: ".Length);
            }
            else if (line.StartsWith("email:"))
            {
                email = line.Substring("email: ".Length);
            }
        }
        textfield_webSocket.text = websocket_url + ":" + websocket_port.ToString();
        textfield_tempTopic.text = topic_Temperature;
        textfield_tagTopic.text = topic_markers;
        textfield_stopTopic.text = topic_stop;
        textfield_speedTopic.text = topic_updatespeed;
        textfield_accelleration.text = accelleration.ToString();
        textfield_username.text = username;
        textfield_password.text = password;
        textfield_email.text = email;
        markerfirst.options[0].text = firstmarker;
        if (!String.IsNullOrEmpty(websocket_url))
        {
            ReConnectWS();
        }
    }
    void writeOutConfig()
    {
        StreamWriter writer = new StreamWriter(filename, true);
        writer.WriteLine("WS_URL: " + websocket_url + ":" + websocket_port.ToString());
        writer.WriteLine("markertopic: " + topic_markers);
        writer.WriteLine("temptopic: " + topic_Temperature);
        writer.WriteLine("speedtopic: " + topic_updatespeed);
        writer.WriteLine("stoptopic: " + topic_stop);
        writer.WriteLine("firstmarker: " + firstmarker);
        writer.WriteLine("accelleration: " + accelleration.ToString());
        writer.WriteLine("user: " + username);
        writer.WriteLine("passwd: " + password);
        writer.WriteLine("email: " + email);
        writer.Close();

        textfield_webSocket.text = websocket_url + ":" + websocket_port.ToString();
        textfield_tempTopic.text = topic_Temperature;
        textfield_tagTopic.text = topic_markers;
        textfield_stopTopic.text = topic_stop;
        textfield_speedTopic.text = topic_updatespeed;
        textfield_accelleration.text = accelleration.ToString();
        textfield_username.text = username;
        textfield_password.text = password;
        textfield_email.text = email;
        markerfirst.options[0].text = firstmarker;
    }

    //string temporarystorage = "";

    // Update is called once per frame
    void Update()
    {
        out_text.text = _out_string;
        if (movement.MoveSpeed < maxspeed)
        {
            movement.MoveSpeed += accelleration * Time.deltaTime;
        }
        else
        {
            movement.MoveSpeed = maxspeed;
        }
        //getSpeedAsync();

    }

    public void OnAccelerationUpdated(string toset)
    {
        try
        {
            max_accelleration = double.Parse(toset);
        }
        catch
        {

        }
    }
    public void OnEditTagChanged(int toset)
    {
        marker_to_edit = toset;
        textfield_markerpos.text=distances[marker_to_edit].ToString();
    }
    public void OnFirstTagChanged(int toset)
    {
        print("SetfirstTag: " + toset.ToString());
        if (toset != 0)
        {
            for (int i = 0; i < toset; i++)
            {
                markers.Add(markers[i]);
            }
            markers.RemoveRange(0, toset);

        }
        writeOutConfig();
    }
    public void OnWebSocketChanged(string toset)
    {
        websocket_url = toset.Split(':')[0];
        if (toset.Contains(":"))
        {
            websocket_url = toset.Split(':')[1];
        }
        if (!String.IsNullOrEmpty(websocket_url))
        {
            ReConnectWS();
        }
        writeOutConfig();
    }
    public void OnTempTopicChanged(string toset)
    {
        topic_Temperature = toset;
        if (!String.IsNullOrEmpty(websocket_url))
        {
            ReConnectWS();
        }
        writeOutConfig();
    }
    public void OnTagTopicChanged(string toset)
    {
        topic_markers = toset;
        if (!String.IsNullOrEmpty(websocket_url))
        {
            ReConnectWS();
        }
        writeOutConfig();
    }
    public void OnStopTopicChanged(string toset)
    {
        topic_stop = toset;
        if (!String.IsNullOrEmpty(websocket_url))
        {
            ReConnectWS();
        }
        writeOutConfig();
    }
    public void OnSpeedTopicChanged(string toset)
    {
        topic_updatespeed = toset;
        writeOutConfig();
    }
    public void OnUsernameChanged(string toset)
    {
        username = toset;
        writeOutConfig();
    }
    public void OnEmailChanged(string toset)
    {
        email = toset;
        writeOutConfig();
    }
    public void OnPasswordChanged(string toset)
    {
        password = toset;
        writeOutConfig();
    }
    public void OnSpeedChanged(Single toset)
    {
        currentspeed = toset;
        // HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://192.168.1.102/motor?direction=1&speed=" + (70 + toset).ToString() + "&mode=0");
        //request.GetResponse();
        client.Publish(topic_updatespeed, System.Text.Encoding.UTF8.GetBytes("speed to: " + 70+toset), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
    }

    public void ToggleConfig()
    {
        settings_open = !settings_open;
        settings.gameObject.SetActive(settings_open);
    }
/*    async Task getSpeedAsync()
    {

        ParseObject speed = new ParseObject("Geschwindigkeit");
        speed.FetchAsync();
        //ParseQuery<ParseObject> speedQuery = ParseObject.GetQuery("Geschwindigkeit");
        //speed = await speedQuery.FirstAsync();
        float actspeed = speed.Get<float>("value");
    }*/

}
