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
    string filename = "config/everything.conf";
    public string topic_markers;
    public string topic_Temperature;
    public string topic_updatespeed;
    public string topic_stop;
    public string websocket_url="";
    public int websocket_port=8080;
    public double currentspeed = 0;
    public List<String> markers;
    public List<double> distances;
    public string firstmarker;
    public int marker_to_edit=0;
    public double max_accelleration=1.0;
    public TextMesh out_text;
    public InputField textfield_markerpos;
    public InputField textfield_speedTopic;
    public InputField textfield_stopTopic;
    public InputField textfield_tagTopic;
    public InputField textfield_tempTopic;
    public InputField textfield_webSocket;
    public InputField textfield_accelleration;


    public Dropdown tagconfig;
    public Dropdown markerfirst;
    public Transform settings;
    private bool settings_open = false;
    private Move_along_path movement;
    private float markertime;
    private double maxspeed;
    private double accelleration=0.0f;
    //Parse.
    
    //ClientWebSocket WebSocket;
    // Start is called before the first frame update
    void Start()
    {
        if (System.IO.File.Exists(filename))
        {
            readConfig();
        }
        else
        {
            writeOutConfig();
        }
        //connectWSAsync();
    }
    void ReConnectWS()
    {
        client = new MqttClient(IPAddress.Parse(websocket_url), websocket_port, false, null);

        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

        string clientId = Guid.NewGuid().ToString();
        client.Connect(clientId);

        client.Subscribe(new string[] { topic_markers }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        client.Subscribe(new string[] { topic_Temperature }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        client.Subscribe(new string[] { topic_stop }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });

    }
    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {

        Debug.Log("Received: " + System.Text.Encoding.UTF8.GetString(e.Message));
        if (e.Topic.Contains(topic_markers))
        {
            if (!markers.Contains(e.Message.ToString()))//got a new Marker, add it to the list
            {
                markers.Add(e.Message.ToString());
                tagconfig.options[markers.Count - 1].text=markers[markers.Count - 1];
            }else if (distances.Count == 0)//Got all Markers, update my gui, sort my markers
            {
                if(markerfirst.options[0].text!="Option A")
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
                            markerfirst.AddOptions(option_new);
                            List<Dropdown.OptionData> tag_new = new List<Dropdown.OptionData>();
                            tag_new.Add(new Dropdown.OptionData("Tag"+i.ToString()));
                            markerfirst.AddOptions(tag_new);
                        }
                    }
                }
                for(int i=0;i<markers.Count;i++){
                    distances.Add(movement.maxdistance / markers.Count);
                }
            }
            if(markers.Contains(e.Message.ToString()))//update speeds and position of train
            {
                int markernum = markers.FindIndex(x => x.StartsWith(e.Message.ToString())); ;
                double markerpos = distances[markernum];
                double last_markerpos = distances[distances.Count - 1];
                if (markernum != 0)
                {
                    last_markerpos = distances[markernum - 1];
                }
                double distance = markerpos - last_markerpos;
                if (markerpos < last_markerpos)
                {
                    distance = movement.maxdistance - last_markerpos + markerpos;
                }
                
                if (distance/(Time.realtimeSinceStartup - markertime)>maxspeed)
                {
                    maxspeed = distance / (Time.realtimeSinceStartup - markertime);
                }
                markertime = Time.realtimeSinceStartup;
                movement.DistanceTraveled = markerpos;
            }
        }
        if (e.Topic.Contains(topic_stop))
        {
            if ((e.Message.ToString().Contains("steht"))){
                movement.MoveSpeed = 0.0f;
                accelleration = 0.0f;
            }
            else
            {
                accelleration = max_accelleration;
            }

        }
        if (e.Topic.Contains(topic_Temperature))
        {
            out_text.text = e.Message.ToString();
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
                websocket_url = line.Substring(8).Split(new char[] { ':' })[0];
                if (line.Substring(8).Split(new char[] { ':' }).Length > 1)
                {
                    websocket_port = Int32.Parse(line.Substring(7).Split(new char[] { ':' })[1]);
                }


            }
            else if (line.StartsWith("markertopic:"))
            {
                topic_markers = line.Substring(13);
            }
            else if (line.StartsWith("temptopic:"))
            {
                topic_Temperature = line.Substring(12);
            }
            else if (line.StartsWith("speedtopic:"))
            {
                topic_updatespeed = line.Substring(13);
            }
            else if (line.StartsWith("stoptopic:"))
            {
                topic_stop = line.Substring(12);
            }
            else if (line.StartsWith("firstmarker:"))
            {
                firstmarker = line.Substring(13);
            }
            else if (line.StartsWith("accelleration:"))
            {
                accelleration = double.Parse(line.Substring(15));
            }
            /*else if (line.StartsWith("user:"))
            {
                username = line.Substring(5);
            }
            else if (line.StartsWith("passwd:"))
            {
                password = line.Substring(7);
            }
            else if (line.StartsWith("email:"))
            {
                email = line.Substring(6);
            }*/
        }
        textfield_webSocket.text = websocket_url + ":" + websocket_port.ToString();
        textfield_tempTopic.text = topic_Temperature;
        textfield_tagTopic.text = topic_markers;
        textfield_stopTopic.text = topic_stop;
        textfield_speedTopic.text = topic_updatespeed;
        textfield_accelleration.text = accelleration.ToString();
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
        /*writer.WriteLine("user:" + username);
        writer.WriteLine("passwd:" + password);
        writer.WriteLine("email:" + email);*/
        writer.Close();
    }

    //string temporarystorage = "";

    // Update is called once per frame
    void Update()
    {
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
    public void OnSpeedChanged(Single toset)
    {
        currentspeed = toset;
        client.Publish(topic_updatespeed, System.Text.Encoding.UTF8.GetBytes("speed to: " + toset), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
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
