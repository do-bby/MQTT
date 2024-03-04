using Microsoft.VisualBasic;
using mqttclient.models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


class Program
{
    static string uID;
    static async Task InituID(){
        uID = await getUID();
    }
    
    static async Task Main(string[] args){
        string brokerAddress = "139.150.83.249";
        int brokerPort = 1883;
        await InituID();
        string username = "root";
        string password = "public";
        MqttClient client = new MqttClient(brokerAddress,brokerPort,false,null,null,MqttSslProtocols.None);
        client.Connect(Guid.NewGuid().ToString(),username,password);
        Task heartbeatTask = Task.Run(() => PublishHeartbeat(client));
        while (true)
        {
            //await Publishing(client);
            //await Publishing2(client);
            await Publishing_ocean(client);
            //await Publishing_UV(client);
            //await Publishing_ATMO(client);
            //await Publishing_WAVE(client);
            await Task.Delay(600000); // 10 minute
        }
        
//      string msg = "Hi2";
//      client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(msg), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

    }
    static async Task<string> getUID()
    {
        using(HttpClient client = new HttpClient())
        {
            string url = "https://pohang.ictpeople.co.kr/api/Equipment/GetEquipment?SerialNo=DX20240220-0001";
            HttpResponseMessage res = await client.GetAsync(url);
            try{
                HttpResponseMessage response = await client.GetAsync(url);
                if(response.IsSuccessStatusCode){
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseBody);
                    JArray arr = (JArray)json["data"];
                    JObject obj = (JObject) arr[0];
                    string data = (string)obj["serialNo"];
                    return data;
                }
                else{
                    return null;
                }
            } catch(HttpRequestException e){
                Console.WriteLine("fail" + e.Message);
                return null;
            }
        }
    }
    static async Task PublishHeartbeat(MqttClient mqttclient)
    {
        while (true)
        {
            try
            {
                string obsrValue = "HeartBeat Data";
                string topic = "PohangPG/"+uID+"/heartbeat";
                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                
                Console.WriteLine("Heartbeat published");
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Error publishing heartbeat: " + e.Message);
            }
            
            await Task.Delay(60000); // 1 minute delay
        }
    }
    //초단기실황 API
    static async Task Publishing(MqttClient mqttclient)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                //string topic = "PohangPG/uID/rain";
                string currentDay = DateTime.Now.ToString("yyyyMMdd");
                string currentTime = DateTime.Now.ToString("HHmm");
                //포항 송도 nx, ny = 54, 123
                string url = "https://apis.data.go.kr/1360000/VilageFcstInfoService_2.0/getUltraSrtNcst?serviceKey=P5M6l%2BAiLQ3PknQ%2FC4KyDqPZx22%2FyLVYcX%2Feq%2FkuSWlrTbz2okiCsU3ih2pSsydn%2ForpSlFMP2XwsgTOmp3cYA%3D%3D&numOfRows=10&pageNo=1&base_date="+currentDay+"&base_time="+currentTime+"&nx=102&ny=94&&dataType=JSON";
                HttpResponseMessage res = await client.GetAsync(url);                
                if (res.IsSuccessStatusCode)
                {
                    //Console.WriteLine(tm);
                    string data = await res.Content.ReadAsStringAsync();                  
                    if (data != null)
                    {                
                        //강수량, 온도 등 모든 기상 데이터 PUBLISH.
                        // mqttclient.Publish(topic, Encoding.UTF8.GetBytes(data), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                        // Console.WriteLine("data: " + data);

                        //Ex) PTY(강수상태)인 데이터만 PUBLISH 
                        var parsedData = JsonConvert.DeserializeObject<dynamic>(data);
                        var items = parsedData.response.body.items.item;
                        foreach(var item in items){
                            if (item.category == "PTY"){
                                string obsrValue = "rain: " + item.obsrValue;
                                string topic = "PohangPG/"+uID+"/rain";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                //heartbeat data publish
                                Console.WriteLine("강수형태: " + obsrValue);
                            }
                            else if (item.category == "REH"){
                                string obsrValue = "hum: " + item.obsrValue;                       
                                string topic = "PohangPG/"+uID+"/hum";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("습도: " + obsrValue + "%");
                            }
                            else if (item.category == "RN1"){
                                string obsrValue = "hourRain: " + item.obsrValue + "mm";         
                                string topic = "PohangPG/"+uID+"/hourRain";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("1시간 강수량: " + obsrValue);
                            }
                            else if (item.category == "T1H"){
                                string obsrValue = "temp: " + item.obsrValue + "C";       
                                string topic = "PohangPG/"+uID+"/temp";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("기온: " + obsrValue);
                            }                                                        
                            else if (item.category == "UUU"){
                                string obsrValue = "wind: " + item.obsrValue + "m/s";    
                                string topic = "PohangPG/"+uID+"/weWind";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("동서바람성분 " + obsrValue);
                            }
                            else if (item.category == "VEC"){
                                string obsrValue = "windDir: " + item.obsrValue + "deg";
                                string topic = "PohangPG/"+uID+"/windDir";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("풍향: " + obsrValue);
                            }
                            else if (item.category == "VVV"){
                                string obsrValue = "wind: " + item.obsrValue + "m/s";
                                string topic = "PohangPG/"+uID+"/snWind";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("남북바람성분: " + obsrValue);
                            }                                                                                  
                            else if (item.category == "WSD"){
                                string obsrValue = "wind: " + item.obsrValue + "m/s";
                                string topic = "PohangPG/"+uID+"/windSpeed";
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                Console.WriteLine("풍속: " + obsrValue);
                            }
                            // else{
                            //     string obsrValue = "HeartBeat Data";
                            //     string topic = "PohangPG/aef6d12a-6355-4e42-a28c-0773fb7f32fa/heartbeat";
                            //     mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                            // }                            
                        }                        
                    }
                    else{
                        Console.WriteLine("api data error");    
                    }
                }
                else
                {                    
                    Console.WriteLine("error: " + res.StatusCode);
                    //mqttclient.Publish(topic, "fail", MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                }
                //heartbeat Data publish
                //mqttclient.Publish(topic, Encoding.UTF8.GetBytes("heartbeat"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

            }
            catch (Exception e)
            {
                //throw;
                string obsrValue = "HeartBeat Data";                
                string topic = "PohangPG/"+uID+"/heartbeat";
                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                //Console.WriteLine("error" + e.Message);
            }

        }
    }
    //초단기예보 API
    static async Task Publishing2(MqttClient mqttclient)
{
    using (HttpClient client = new HttpClient())
    {
        try
        {
            //string topic = "PohangPG/uID/rain";
            string currentDay = DateTime.Now.ToString("yyyyMMdd");
            string currentTime = DateTime.Now.ToString("HHmm");
            string url = "https://apis.data.go.kr/1360000/VilageFcstInfoService_2.0/getUltraSrtFcst?serviceKey=P5M6l%2BAiLQ3PknQ%2FC4KyDqPZx22%2FyLVYcX%2Feq%2FkuSWlrTbz2okiCsU3ih2pSsydn%2ForpSlFMP2XwsgTOmp3cYA%3D%3D&numOfRows=60&pageNo=1&base_date="+currentDay+"&base_time="+currentTime+"&nx=102&ny=94&&dataType=JSON";
            HttpResponseMessage res = await client.GetAsync(url);                
            if (res.IsSuccessStatusCode)
            {
                //Console.WriteLine(tm);
                string data = await res.Content.ReadAsStringAsync();                  
                if (data != null)
                {                
                    var parsedData = JsonConvert.DeserializeObject<dynamic>(data);                    
                    var items = parsedData.response.body.items.item;
                    for (int i = 5; i<items.Count; i+=6){                        
                        JObject item = items[i];
                        string category = (string)item["category"];                        
                        string fcstValue = (string)item["fcstValue"];
                        string fcstTime = (string)item["fcstTime"];
                        int nx = (int)item["nx"];
                        int ny = (int)item["ny"];
                        switch (category) {
                            case "SKY":
                                string obsrValue = "sky: " + (string) item["fcstValue"];
                                string topic = "PohangPG/" + uID + "/sky";
                                //Console.WriteLine(obsrValue);
                                mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "LGT":
                                string obsrValue2 = "lgt: " + (string) item["fcstValue"];
                                string topic2 = "PohangPG/" + uID + "/lgt";
                                //Console.WriteLine(obsrValue2);
                                mqttclient.Publish(topic2, Encoding.UTF8.GetBytes(obsrValue2), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "PTY":
                                string obsrValue3 = "rain: " + (string) item["fcstValue"];
                                string topic3 = "PohangPG/" + uID + "/rain";
                                //onsole.WriteLine(obsrValue3);
                                mqttclient.Publish(topic3, Encoding.UTF8.GetBytes(obsrValue3), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "REH":
                                string obsrValue4 = "hum: " + (string) item["fcstValue"];
                                string topic4 = "PohangPG/" + uID + "/hum";
                                mqttclient.Publish(topic4, Encoding.UTF8.GetBytes(obsrValue4), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "RN1":
                                string obsrValue5 = "hourRain: " + (string) item["fcstValue"];
                                string topic5 = "PohangPG/" + uID + "/hourRain";
                                mqttclient.Publish(topic5, Encoding.UTF8.GetBytes(obsrValue5), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "T1H":
                                string obsrValue6 = "temp: " + (string) item["fcstValue"];
                                string topic6 = "PohangPG/" + uID + "/temp";
                                mqttclient.Publish(topic6, Encoding.UTF8.GetBytes(obsrValue6), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "UUU":
                                string obsrValue7 = "Wind: " + (string) item["fcstValue"];
                                string topic7 = "PohangPG/" + uID + "/weWind";
                                mqttclient.Publish(topic7, Encoding.UTF8.GetBytes(obsrValue7), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "VEC":
                                string obsrValue8 = "windDir: " + (string) item["fcstValue"];
                                string topic8 = "PohangPG/" + uID + "/windDir";
                                mqttclient.Publish(topic8, Encoding.UTF8.GetBytes(obsrValue8), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "VVV":
                                string obsrValue9 = "Wind: " + (string) item["fcstValue"];
                                string topic9 = "PohangPG/" + uID + "/snWind";
                                mqttclient.Publish(topic9, Encoding.UTF8.GetBytes(obsrValue9), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);                                
                                break;
                            case "WSD":
                                string obsrValue10 = "windSpeed: " + (string) item["fcstValue"];
                                string topic10 = "PohangPG/" + uID + "/windSpeed";
                                mqttclient.Publish(topic10, Encoding.UTF8.GetBytes(obsrValue10), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                                break;    
                            }
                    }
                }
            }
            else
            {                    
                Console.WriteLine("error: " + res.StatusCode);
                //mqttclient.Publish(topic, "fail", MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            }
            //heartbeat Data publish
            //mqttclient.Publish(topic, Encoding.UTF8.GetBytes("heartbeat"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }
        catch (Exception e)
        {
            //throw;
            string obsrValue = "HeartBeat Data";                
            string topic = "PohangPG/"+uID+"/heartbeat";
            mqttclient.Publish(topic, Encoding.UTF8.GetBytes(obsrValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
            //Console.WriteLine("error" + e.Message);
        }
    }
}
    //해양데이터 API    
static async Task Publishing_ocean(MqttClient mqttclient)
{
    using (HttpClient client = new HttpClient())
    {
        try{
            Console.WriteLine("suc");
            string url = "https://www.khoa.go.kr/api/oceangrid/tideObsRecent/search.do?ServiceKey=EDrcCIFdi7qEsdqj0mgrjQ==&ObsCode=DT_0091&ResultType=json";
            HttpResponseMessage res = await client.GetAsync(url);
            Console.WriteLine("suc2");
            if(res.IsSuccessStatusCode){
                string data = await res.Content.ReadAsStringAsync();
                    if (data != null)
                    {                
                        var parsedData = JsonConvert.DeserializeObject<dynamic>(data);                    
                        var items = parsedData.result.data;
                        Console.WriteLine("items");
                        Console.WriteLine(items);
                        string record = (string)items["record_time"];
                        string tide_level = (string)items["tide_level"];
                        string water_temp = (string)items["water_temp"];
                        string Salinity = (string)items["Salinity"];
                        string air_temp = (string)items["air_temp"];
                        string air_press = (string)items["air_press"];
                        string wind_dir = (string)items["wind_dir"];
                        string wind_speed = (string)items["wind_speed"];
                        string wind_gust = (string)items["wind_gust"];
                        Console.WriteLine(record);
                        Console.WriteLine(tide_level);
                        Console.WriteLine(water_temp);
                        Console.WriteLine(Salinity);
                        Console.WriteLine(air_temp);
                        Console.WriteLine(air_press);
                        Console.WriteLine(wind_dir);
                        Console.WriteLine(wind_speed);
                        Console.WriteLine(wind_gust);
                    }
            }
            else{
                Console.WriteLine("fail");
            }
            //heartbeat Data publish
            //mqttclient.Publish(topic, Encoding.UTF8.GetBytes("heartbeat"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        } catch(Exception e){
            Console.WriteLine(e.Message);
        }
    }
}
//자외선 API
static async Task Publishing_UV(MqttClient mqttclient)
{
    using(HttpClient client = new HttpClient())
    {
        try{
            string currentDay = DateTime.Now.ToString("yyyyMMdd");
            string currentTime = DateTime.Now.ToString("HHmm");
            string dayTime = currentDay + currentTime.Substring(0,2);            
            string url = "https://apis.data.go.kr/1360000/LivingWthrIdxServiceV4/getUVIdxV4?serviceKey=ClSg8YETAqDKd35FklryZ%2BYPkYWY6Q24PTpGdagqszHjRSWzowl08EhLqpdZKJvBkIUv%2FmrEcPwIK3iV8I3QVQ%3D%3D&pageNo=1&numOfRows=10&dataType=JSON&areaNo=4711155000&time=" + dayTime;
            HttpResponseMessage res = await client.GetAsync(url);
            if(res.IsSuccessStatusCode){
                string data = await res.Content.ReadAsStringAsync();
                if(data != null){
                    var parsedData = JsonConvert.DeserializeObject<dynamic>(data);
                    string item = parsedData.response.body.items.item[0].ToString();
                    string topic = "PohangPG/" + uID + "/uv";                    
                    Console.WriteLine("uv"+item);
                    mqttclient.Publish(topic, Encoding.UTF8.GetBytes(item), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                }
                else{
                    Console.WriteLine("Null");
                }
            }
        } catch(Exception e){
            Console.WriteLine(e.Message);
        }
    }
}
//대기 API
static async Task Publishing_ATMO(MqttClient mqttclient)
{
    using(HttpClient client = new HttpClient())
    {
        try{
            string currentDay = DateTime.Now.ToString("yyyyMMdd");
            string currentTime = DateTime.Now.ToString("HHmm");
            string dayTime = currentDay + currentTime.Substring(0,2);            
            String url = "https://apis.data.go.kr/1360000/LivingWthrIdxServiceV4/getAirDiffusionIdxV4?serviceKey=ClSg8YETAqDKd35FklryZ%2BYPkYWY6Q24PTpGdagqszHjRSWzowl08EhLqpdZKJvBkIUv%2FmrEcPwIK3iV8I3QVQ%3D%3D&pageNo=10&numOfRows=10&dataType=JSON&areaNo=4711155000&time="+dayTime;
            HttpResponseMessage res = await client.GetAsync(url);
            if(res.IsSuccessStatusCode){
                string data = await res.Content.ReadAsStringAsync();
                if(data != null){
                    var parsedData = JsonConvert.DeserializeObject<dynamic>(data);
                    string item = parsedData.response.body.items.item[0].ToString();
                    string topic = "PohangPG/" + uID + "/atmo";                    
                    Console.WriteLine("atmo"+item);
                    mqttclient.Publish(topic, Encoding.UTF8.GetBytes(item), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                }
                else{
                    Console.WriteLine("Null");
                }
            }
        } catch(Exception e){
            Console.WriteLine(e.Message);
        }
    }
}

//파고 API
static async Task Publishing_WAVE(MqttClient mqttclient)
{
    using(HttpClient client = new HttpClient())
    {
        try{
            string currentDay = DateTime.Now.ToString("yyyyMMdd");
            string currentTime = DateTime.Now.ToString("HHmm");
            string dayTime = currentDay + currentTime.Substring(0,2);            
            String url = "https://apis.data.go.kr/1360000/BeachInfoservice/getWhBuoyBeach?serviceKey=ClSg8YETAqDKd35FklryZ%2BYPkYWY6Q24PTpGdagqszHjRSWzowl08EhLqpdZKJvBkIUv%2FmrEcPwIK3iV8I3QVQ%3D%3D&numOfRows=1&pageNo=10&dataType=JSON&beach_num=211&searchTime=" + dayTime;
            HttpResponseMessage res = await client.GetAsync(url);
            if(res.IsSuccessStatusCode){
                string data = await res.Content.ReadAsStringAsync();
                if(data != null){
                    var parsedData = JsonConvert.DeserializeObject<dynamic>(data);
                    string item = parsedData.response.body.items.item[0].ToString();
                    string topic = "PohangPG/" + uID + "/wave";
                    Console.WriteLine("wave"+item);
                    mqttclient.Publish(topic, Encoding.UTF8.GetBytes(item), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                }
                else{
                    Console.WriteLine("Null");
                }
            }
        } catch(Exception e){
            Console.WriteLine(e.Message);
        }
    }
}



}
