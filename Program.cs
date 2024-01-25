using System;
using System.Data;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


class Program
{
    private static int h = 0;
    static async Task Main(string[] args){
        string brokerAddress = "127.0.0.1";
        int brokerPort = 1883;
        MqttClient client = new MqttClient(brokerAddress,brokerPort,false,null,null,MqttSslProtocols.None);
        client.Connect(Guid.NewGuid().ToString());
        while (true)
        {
            await Data(client);
            await Task.Delay(10000);
        }
        
//      string msg = "Hi2";
//      client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

    }
    static async Task Data(MqttClient mqttclient)
    {   
        using (HttpClient client = new HttpClient())
        {
            try{
                
                string topic = "test_Topic";
                string tm = DateTime.Now.AddDays(-1).AddHours(h).ToString("yyyyMMddHHmm");
                h++;
                string url = $"http://apis.data.go.kr/1360000/GtsInfoService/getSynop?serviceKey=P5M6l%2BAiLQ3PknQ%2FC4KyDqPZx22%2FyLVYcX%2Feq%2FkuSWlrTbz2okiCsU3ih2pSsydn%2ForpSlFMP2XwsgTOmp3cYA%3D%3D&pageNo=1&numOfRows=1&tm={tm}&stnId=1366&dataType=JSON";
                HttpResponseMessage res = await client.GetAsync(url);
                if(res.IsSuccessStatusCode)
                {
                    //Console.WriteLine(tm);
                    string data = await res.Content.ReadAsStringAsync();                    
                    mqttclient.Publish(topic, System.Text.Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    Console.WriteLine("Complete");
                }
                else{
                    Console.WriteLine("error: " + res.StatusCode);
                }
                
            } catch (Exception e)
            {
                Console.WriteLine("error" + e.Message);
            }
            
        }
    }
}