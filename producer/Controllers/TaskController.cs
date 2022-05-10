using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using RabbitMQ.Client;
using RabbitMQProducer.Models;
using System.Net.Http;
using Newtonsoft.Json;

namespace producer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        [HttpPost]
        public ActionResult<Token> Post( producer.Models.Task task)
        {
            Token content = null;

            try
            {
                content = GetToken(task);

                if (content == null)
                {
                    return StatusCode(401, "Authentication Failed");
                }

                var factory = new ConnectionFactory()
                {   //HostName = "localhost" , 
                    //Port = 30724
                    HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                    Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
                };

                Console.WriteLine(factory.HostName + ":" + factory.Port);
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "TaskQueue",
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    string message = content.token;
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "TaskQueue",
                                         basicProperties: null,
                                         body: body);
                }
            }
            catch
            {
                throw;
            }

            return content;
           
        }

        
        private Token GetToken(producer.Models.Task task)
        {
            Token t = null;

            string url = "https://reqres.in/api/login";

            using (var client = new System.Net.Http.HttpClient())
            {
                client.BaseAddress = new Uri(url);

                //HTTP GET
                try
                {
                    var content = new StringContent(JsonConvert.SerializeObject(task), System.Text.Encoding.UTF8, "application/json");

                    var responseTask = client.PostAsync(url, content);
                    responseTask.Wait();

                    var result = responseTask.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var readTask = result.Content.ReadAsStringAsync();
                        readTask.Wait();

                        var alldata = readTask.Result;
                        t = JsonConvert.DeserializeObject<Token>(alldata);
                    }
                }
                catch (Exception e)
                {
                    throw;
                }

            }

            return t;
        }

    }
}
